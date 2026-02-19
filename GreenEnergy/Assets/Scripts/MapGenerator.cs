using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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

    private void GenerateBiomesWithPerlin()
    {
        float scale = 0.05f; // Adjust for biome size
        float offsetX = random.Next(0, 10000);
        float offsetY = random.Next(0, 10000);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Sample multiple octaves of Perlin noise
                float noiseValue = GetPerlinValue(x, y, offsetX, offsetY, scale);
                float moistureValue = GetPerlinValue(x, y, offsetX + 1000, offsetY + 1000, scale * 1.5f);
                
                // Determine biome based on noise values
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

    private BiomeType DetermineBiome(float elevation, float moisture, int y)
    {
        // Water at low elevations (bottom 15% of map)
        if (y < mapHeight * 0.15f && elevation < 0.3f)
        {
            return BiomeType.Water;
        }
        
        // Coastal near water
        if (y < mapHeight * 0.25f && elevation < 0.4f)
        {
            return BiomeType.Coastal;
        }
        
        // Mountains at high elevations
        if (elevation > 0.7f)
        {
            return BiomeType.Mountain;
        }
        
        // Desert in dry, mid-elevation areas
        if (moisture < 0.3f && elevation > 0.4f && elevation < 0.6f)
        {
            return BiomeType.Desert;
        }
        
        // Forest in wet, mid-elevation areas
        if (moisture > 0.6f && elevation > 0.3f && elevation < 0.7f)
        {
            return BiomeType.Forest;
        }
        
        // Default to plains
        return BiomeType.Plains;
    }

    private void EnsureMinimumBiomes()
    {
        Dictionary<BiomeType, int> biomeCounts = CountBiomes();
        int totalTiles = mapWidth * mapHeight;
        int minTiles = Mathf.CeilToInt(totalTiles * minBiomePercentage);

        foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
        {
            if (biomeCounts.ContainsKey(biome) && biomeCounts[biome] < minTiles)
            {
                int tilesToAdd = minTiles - biomeCounts[biome];
                AddRandomBiomeTiles(biome, tilesToAdd);
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

    public BiomeType GetBiomeAt(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return BiomeType.Plains; // Default
        }
        return biomeMap[x, y];
    }

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

    public enum GeneratorType
    {
        Solar,
        Wind,
        Hydroelectric,
        Tidal,
        Nuclear,
        Battery
    }
}
