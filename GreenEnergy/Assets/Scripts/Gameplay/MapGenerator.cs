using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Generates the procedural tile-based world map using seeded Perlin noise.
/// Defines the six biome types and which generator types can be placed in each biome.
/// Also provides per-biome efficiency multipliers used by GeneratorManager.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 200;
    public int mapHeight = 100;
    public int seed = 0; // 0 = random seed
    
    [Header("Tilemap References")]
    public Tilemap terrainTilemap;
    public Tilemap generatorTilemap;
    
    [Header("Biome Tiles")]
    public TileBase desertTile;
    public TileBase plainsTile;
    public TileBase mountainTile;
    public TileBase waterTile;
    public TileBase coastalTile;
    public TileBase forestTile;
    
    [Header("Biome Settings")]
    [Range(0.1f, 0.5f)]
    public float minBiomePercentage = 0.1f;
    
    private BiomeType[,] biomeMap;
    private System.Random random;

    public enum BiomeType
    {
        Desert,      // Solar works best, nuclear possible
        Plains,      // Wind works best, solar good, nuclear possible
        Mountain,    // Hydro only, wind possible
        Water,       // Tidal only (ocean)
        Coastal,     // Tidal works best, wind possible, solar possible
        Forest       // Hydro possible, solar limited, nuclear possible
    }

    private void Start()
    {
        if (terrainTilemap == null)
        {
            Debug.LogError("Terrain Tilemap not assigned!");
        }
    }

    /// <summary>
    /// Generates the full tile map. If <see cref="seed"/> is 0 a random seed is chosen and stored
    /// so the same map can be regenerated on load. Runs Perlin noise biome assignment,
    /// enforces minimum biome coverage, then renders to the tilemap.
    /// </summary>
    public void GenerateMap()
    {
        // Use seed for reproducibility in testing, random otherwise
        if (seed == 0)
        {
            seed = UnityEngine.Random.Range(1, 999999);
        }
        random = new System.Random(seed);

        biomeMap = new BiomeType[mapWidth, mapHeight];

        // Generate using Perlin noise for natural-looking biomes
        GenerateBiomesWithPerlin();

        // Ensure minimum biome percentages
        EnsureMinimumBiomes();

        // Render the tilemap
        RenderTilemap();

        Debug.Log($"Map generated with seed: {seed}");
    }

    /// <summary>
    /// Fills <see cref="biomeMap"/> using two independent Perlin noise passes:
    /// one for elevation and one for moisture. Sampling at different offsets and scales
    /// keeps them visually uncorrelated so biome borders feel organic rather than banded.
    /// </summary>
    private void GenerateBiomesWithPerlin()
    {
        float scale = 0.05f; // Controls biome patch size; smaller = larger biomes

        // Large random offsets push each noise pass into a unique region of the Perlin field,
        // ensuring the elevation and moisture maps don't accidentally mirror each other.
        float offsetX = random.Next(0, 10000);
        float offsetY = random.Next(0, 10000);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Elevation pass — base scale, drives mountain/water/flat terrain
                float noiseValue = GetPerlinValue(x, y, offsetX, offsetY, scale);
                // Moisture pass — 1.5× scale gives finer variation; offset by 1000 to decorrelate
                float moistureValue = GetPerlinValue(x, y, offsetX + 1000, offsetY + 1000, scale * 1.5f);

                biomeMap[x, y] = DetermineBiome(noiseValue, moistureValue, y);
            }
        }
    }

    private float GetPerlinValue(int x, int y, float offsetX, float offsetY, float scale)
    {
        float sampleX = (x + offsetX) * scale;
        float sampleY = (y + offsetY) * scale;
        return Mathf.PerlinNoise(sampleX, sampleY);
    }

    /// <summary>
    /// Maps a (elevation, moisture, y) sample to a <see cref="BiomeType"/>.
    /// Rules are checked top-to-bottom; the first match wins.
    /// </summary>
    /// <param name="elevation">Perlin elevation value (0–1). Higher = more mountainous.</param>
    /// <param name="moisture">Perlin moisture value (0–1). Higher = wetter.</param>
    /// <param name="y">Tile row, used to place ocean and coast at the bottom of the map.</param>
    private BiomeType DetermineBiome(float elevation, float moisture, int y)
    {
        // Bottom 15% of the map + low elevation → open ocean (tidal generators only)
        if (y < mapHeight * 0.15f && elevation < 0.3f)
        {
            return BiomeType.Water;
        }

        // Bottom 25% + moderate elevation → coastal strip between ocean and land
        if (y < mapHeight * 0.25f && elevation < 0.4f)
        {
            return BiomeType.Coastal;
        }

        // Elevation above 0.7 → mountain peaks (hydro and wind only)
        if (elevation > 0.7f)
        {
            return BiomeType.Mountain;
        }

        // Dry mid-elevation band → desert (best solar output)
        if (moisture < 0.3f && elevation > 0.4f && elevation < 0.6f)
        {
            return BiomeType.Desert;
        }

        // Wet mid-elevation band → forest (hydro viable, solar reduced)
        if (moisture > 0.6f && elevation > 0.3f && elevation < 0.7f)
        {
            return BiomeType.Forest;
        }

        // Everything else → flat plains (best all-rounder: wind, solar, nuclear)
        return BiomeType.Plains;
    }

    /// <summary>
    /// Guarantees every biome type has at least <see cref="minBiomePercentage"/> coverage.
    /// Under-represented biomes are padded by randomly overwriting non-water, non-coastal tiles.
    /// This ensures every generator type has viable placement locations on every map.
    /// </summary>
    private void EnsureMinimumBiomes()
    {
        Dictionary<BiomeType, int> biomeCounts = CountBiomes();
        int totalTiles = mapWidth * mapHeight;
        int minTiles = Mathf.CeilToInt(totalTiles * minBiomePercentage);

        foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
        {
            // Use 0 if the biome has no tiles at all (not in dictionary)
            int count = biomeCounts.ContainsKey(biome) ? biomeCounts[biome] : 0;
            if (count < minTiles)
            {
                AddRandomBiomeTiles(biome, minTiles - count);
            }
        }
    }

    private Dictionary<BiomeType, int> CountBiomes()
    {
        Dictionary<BiomeType, int> counts = new Dictionary<BiomeType, int>();
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                BiomeType biome = biomeMap[x, y];
                if (!counts.ContainsKey(biome))
                {
                    counts[biome] = 0;
                }
                counts[biome]++;
            }
        }
        
        return counts;
    }

    private void AddRandomBiomeTiles(BiomeType targetBiome, int count)
    {
        int added = 0;
        int maxAttempts = count * 10; // Prevent infinite loop
        int attempts = 0;

        while (added < count && attempts < maxAttempts)
        {
            int x = random.Next(0, mapWidth);
            int y = random.Next(0, mapHeight);
            
            // Don't replace water or coastal tiles
            if (biomeMap[x, y] != BiomeType.Water && biomeMap[x, y] != BiomeType.Coastal)
            {
                biomeMap[x, y] = targetBiome;
                added++;
            }
            
            attempts++;
        }
    }

    /// <summary>
    /// Writes every tile from <see cref="biomeMap"/> to the Unity <see cref="Tilemap"/>.
    /// Clears all existing tiles first to handle map regeneration cleanly.
    /// </summary>
    private void RenderTilemap()
    {
        terrainTilemap.ClearAllTiles();
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                TileBase tile = GetTileForBiome(biomeMap[x, y]);
                terrainTilemap.SetTile(tilePosition, tile);
            }
        }
    }

    /// <summary>Returns the <see cref="TileBase"/> asset assigned to the given biome type.</summary>
    private TileBase GetTileForBiome(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert: return desertTile;
            case BiomeType.Plains: return plainsTile;
            case BiomeType.Mountain: return mountainTile;
            case BiomeType.Water: return waterTile;
            case BiomeType.Coastal: return coastalTile;
            case BiomeType.Forest: return forestTile;
            default: return plainsTile;
        }
    }

    /// <summary>
    /// Returns the biome type at the given tile coordinates.
    /// Returns Plains as a safe default if coordinates are out of bounds.
    /// </summary>
    public BiomeType GetBiomeAt(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return BiomeType.Plains; // Default
        }
        return biomeMap[x, y];
    }

    /// <summary>
    /// Returns true if the given generator type is allowed on the biome at (x, y).
    /// Used by GeneratorManager and BuildMenuController to validate placement.
    /// </summary>
    public bool CanPlaceGeneratorType(GeneratorType genType, int x, int y)
    {
        BiomeType biome = GetBiomeAt(x, y);
        
        switch (genType)
        {
            case GeneratorType.Solar:
                // Solar works in desert (best), plains (good), coastal (possible)
                return biome == BiomeType.Desert || biome == BiomeType.Plains || 
                       biome == BiomeType.Coastal || biome == BiomeType.Forest;
            
            case GeneratorType.Wind:
                // Wind works in plains (best), coastal (good), mountain (possible)
                return biome == BiomeType.Plains || biome == BiomeType.Coastal || 
                       biome == BiomeType.Mountain;
            
            case GeneratorType.Hydroelectric:
                // Hydro only in mountains and forests near water
                return biome == BiomeType.Mountain || biome == BiomeType.Forest;
            
            case GeneratorType.Tidal:
                // Tidal only in coastal and water
                return biome == BiomeType.Coastal || biome == BiomeType.Water;
            
            case GeneratorType.Nuclear:
                // Nuclear only on stable land (not water, not mountain)
                return biome == BiomeType.Desert || biome == BiomeType.Plains || 
                       biome == BiomeType.Forest;
            
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns the efficiency multiplier for a generator type in a given biome.
    /// 1.0 = 100% efficiency; values above 1.0 are bonus biomes for that generator type.
    /// Returns 0.0 for invalid combinations (generator cannot operate in that biome).
    /// </summary>
    public float GetBiomeEfficiencyMultiplier(GeneratorType genType, BiomeType biome)
    {
        // Returns efficiency multiplier (1.0 = 100% efficiency)
        switch (genType)
        {
            case GeneratorType.Solar:
                if (biome == BiomeType.Desert) return 1.2f;
                if (biome == BiomeType.Plains) return 1.0f;
                if (biome == BiomeType.Coastal) return 0.9f;
                if (biome == BiomeType.Forest) return 0.7f;
                return 0f;
            
            case GeneratorType.Wind:
                if (biome == BiomeType.Plains) return 1.2f;
                if (biome == BiomeType.Coastal) return 1.1f;
                if (biome == BiomeType.Mountain) return 1.0f;
                return 0f;
            
            case GeneratorType.Hydroelectric:
                if (biome == BiomeType.Mountain) return 1.3f;
                if (biome == BiomeType.Forest) return 1.0f;
                return 0f;
            
            case GeneratorType.Tidal:
                if (biome == BiomeType.Coastal) return 1.2f;
                if (biome == BiomeType.Water) return 1.0f;
                return 0f;
            
            case GeneratorType.Nuclear:
                // Nuclear has consistent output regardless of location
                return 1.0f;
            
            default:
                return 0f;
        }
    }

    /// <summary>
    /// All energy generator types in the game, including Battery (used for research, not placed on map).
    /// </summary>
    public enum GeneratorType
    {
        Solar,
        Wind,
        Hydroelectric,
        Tidal,
        Nuclear,
        Battery // Not placed on map — used only as a research category
    }
}
