using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manages the build menu panel and the build mode state machine.
/// When the player clicks a generator button, build mode becomes active and the next
/// left-click on a map tile calls <see cref="GeneratorManager.PlaceGenerator"/>.
/// Right-click or Escape cancels without placing. Cost labels are refreshed every frame
/// while the menu is open via <see cref="UpdateDisplay"/>, which is polled by UIManager.
/// </summary>
public class BuildMenuController : MonoBehaviour
{
    // ===== BUILD MENU UI REFERENCES =====
    [Header("Build Menu Buttons")]
    public Button solarBuildButton;
    public Button windBuildButton;
    public Button hydroBuildButton;
    public Button tidalBuildButton;
    public Button nuclearBuildButton;
    
    [Header("Build Menu Cost Display")]
    public TMP_Text solarCostText;
    public TMP_Text windCostText;
    public TMP_Text hydroCostText;
    public TMP_Text tidalCostText;
    public TMP_Text nuclearCostText;
    
    [Header("Menu Panel")]
    public GameObject buildMenuPanel;
    
    // ===== BUILD MODE STATE =====
    private MapGenerator.GeneratorType selectedGeneratorType;
    private int selectedTier = 1;
    private bool isBuildModeActive = false;

    private void OnDestroy()
    {
        RemoveAllListeners();
    }

    /// <summary>Wires up all build button listeners. Called by UIManager on startup.</summary>
    public void Initialize()
    {
        SetupButtons();
    }

    /// <summary>Registers click listeners on all five generator type buttons.</summary>
    private void SetupButtons()
    {
        if (solarBuildButton != null)
        {
            solarBuildButton.onClick.RemoveAllListeners();
            solarBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Solar));
        }
        if (windBuildButton != null)
        {
            windBuildButton.onClick.RemoveAllListeners();
            windBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Wind));
        }
        if (hydroBuildButton != null)
        {
            hydroBuildButton.onClick.RemoveAllListeners();
            hydroBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Hydroelectric));
        }
        if (tidalBuildButton != null)
        {
            tidalBuildButton.onClick.RemoveAllListeners();
            tidalBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Tidal));
        }
        if (nuclearBuildButton != null)
        {
            nuclearBuildButton.onClick.RemoveAllListeners();
            nuclearBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Nuclear));
        }
    }

    private void RemoveAllListeners()
    {
        if (solarBuildButton != null) solarBuildButton.onClick.RemoveAllListeners();
        if (windBuildButton != null) windBuildButton.onClick.RemoveAllListeners();
        if (hydroBuildButton != null) hydroBuildButton.onClick.RemoveAllListeners();
        if (tidalBuildButton != null) tidalBuildButton.onClick.RemoveAllListeners();
        if (nuclearBuildButton != null) nuclearBuildButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Refreshes cost labels and processes any pending build-mode click or cancel input.
    /// Called every frame by UIManager while the build menu is open.
    /// </summary>
    public void UpdateDisplay()
    {
        UpdateBuildMenuCosts();
        HandleBuildMode();
    }

    /// <summary>
    /// Activates build mode for the given generator type at tier 1.
    /// Does nothing if the type has not yet been researched (tier 1 not unlocked).
    /// </summary>
    private void SelectGenerator(MapGenerator.GeneratorType type)
    {
        // Require tier 1 to be researched before the player can place this generator type
        if (ResearchManager.Instance != null && !ResearchManager.Instance.IsUnlocked(type, 1))
        {
            Debug.Log($"{type} not yet unlocked!");
            return;
        }

        selectedGeneratorType = type;
        selectedTier = 1; // Always place at the lowest unlocked tier; upgrade via the generator menu
        isBuildModeActive = true;

        Debug.Log($"Build mode: {type}");
    }

    /// <summary>
    /// Processes build-mode input each frame while a generator type is selected.
    /// Right-click or Escape cancels; left-click over the map converts the screen position
    /// to a grid tile and attempts placement via <see cref="GeneratorManager.PlaceGenerator"/>.
    /// UI clicks are ignored so the player can close the menu without accidentally placing.
    /// </summary>
    private void HandleBuildMode()
    {
        if (!isBuildModeActive) return;

        // Cancel takes priority — check before any placement logic
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            isBuildModeActive = false;
            return;
        }

        // Ignore clicks that land on a UI element (e.g. the build menu itself)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null) return;

        // Convert screen pixel → world space → integer grid tile (floor so tile (2,3) covers x∈[2,3))
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(mousePos.x), Mathf.FloorToInt(mousePos.y));

        if (Input.GetMouseButtonDown(0))
        {
            if (GeneratorManager.Instance != null)
            {
                GeneratorManager.Instance.PlaceGenerator(selectedGeneratorType, gridPos, selectedTier);
            }
        }
    }

    /// <summary>
    /// Refreshes the cost label for each generator type using tier-1 placement cost.
    /// Called every frame so costs update immediately if difficulty changes mid-game.
    /// </summary>
    private void UpdateBuildMenuCosts()
    {
        if (GeneratorManager.Instance == null) return;
        
        if (solarCostText != null)
            solarCostText.text = $"Cost: {GeneratorManager.Instance.GetPlacementCost(MapGenerator.GeneratorType.Solar, 1)}";
        
        if (windCostText != null)
            windCostText.text = $"Cost: {GeneratorManager.Instance.GetPlacementCost(MapGenerator.GeneratorType.Wind, 1)}";
        
        if (hydroCostText != null)
            hydroCostText.text = $"Cost: {GeneratorManager.Instance.GetPlacementCost(MapGenerator.GeneratorType.Hydroelectric, 1)}";
        
        if (tidalCostText != null)
            tidalCostText.text = $"Cost: {GeneratorManager.Instance.GetPlacementCost(MapGenerator.GeneratorType.Tidal, 1)}";
        
        if (nuclearCostText != null)
            nuclearCostText.text = $"Cost: {GeneratorManager.Instance.GetPlacementCost(MapGenerator.GeneratorType.Nuclear, 1)}";
    }

    /// <summary>Returns true if the player has selected a generator type and is waiting to place it on the map.</summary>
    public bool IsBuildModeActive()
    {
        return isBuildModeActive;
    }

    /// <summary>Exits build mode without placing a generator. Called externally when the menu is closed.</summary>
    public void CancelBuildMode()
    {
        isBuildModeActive = false;
    }
}
