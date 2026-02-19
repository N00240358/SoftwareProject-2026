using UnityEngine;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro support
using System.Collections.Generic;

/// <summary>
/// Manages all UI interactions, displays, and menus
/// Handles stats display, build mode, research menu, and all user interactions
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    // ===== BOTTOM STATS BAR =====
    [Header("Bottom Stats Bar")]
    public TMP_Text energyText; // Changed from Text to TMP_Text
    public TMP_Text carbonText; // Changed from Text to TMP_Text
    public TMP_Text dayText; // Changed from Text to TMP_Text
    public TMP_Text timeSpeedText; // Changed from Text to TMP_Text
    public Image energyFillBar;
    public Image carbonFillBar;
    
    [Header("Top Menu Bar")]
    public GameObject menuBarPanel;
    public Button buildMenuButton;
    public Button researchMenuButton;
    public Button pdaMenuButton;
    public Button settingsButton;
    public Button timeControlButton;
    
    [Header("Menu Panels")]
    public GameObject buildMenuPanel;
    public GameObject researchMenuPanel;
    public GameObject pdaListPanel;
    public GameObject settingsPanel;
    
    // ===== BUILD MENU =====
    [Header("Build Menu")]
    public Button solarBuildButton;
    public Button windBuildButton;
    public Button hydroBuildButton;
    public Button tidalBuildButton;
    public Button nuclearBuildButton;
    public TMP_Text solarCostText; // Changed to TMP_Text
    public TMP_Text windCostText; // Changed to TMP_Text
    public TMP_Text hydroCostText; // Changed to TMP_Text
    public TMP_Text tidalCostText; // Changed to TMP_Text
    public TMP_Text nuclearCostText; // Changed to TMP_Text
    
    [Header("Research Menu")]
    public GameObject researchTreePanel;
    public GameObject researchNodePrefab;
    public Transform solarTreeParent;
    public Transform windTreeParent;
    public Transform hydroTreeParent;
    public Transform tidalTreeParent;
    public Transform nuclearTreeParent;
    public Transform batteryTreeParent;
    
    [Header("Game Over Screens")]
    public GameObject winScreen;
    public GameObject loseScreen;
    
    [Header("Time Control")]
    public Sprite pauseSprite;
    public Sprite normalSprite;
    public Sprite speed2xSprite;
    public Sprite speed5xSprite;
    public Sprite speed10xSprite;
    
    private MapGenerator.GeneratorType selectedGeneratorType;
    private int selectedTier = 1;
    private bool isBuildModeActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupButtons();
        CloseAllMenus();
    }

    public void Initialize()
    {
        UpdateEnergyDisplay(0, 1000);
        UpdateCarbonDisplay(GameManager.STARTING_CARBON, GameManager.TARGET_CARBON);
        UpdateTimeDisplay(0, 0.25f);
        UpdateTimeSpeedDisplay(GameManager.TimeSpeed.Normal);
        
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
    }

    private void SetupButtons()
    {
        // Menu bar buttons
        if (buildMenuButton != null) buildMenuButton.onClick.AddListener(ToggleBuildMenu);
        if (researchMenuButton != null) researchMenuButton.onClick.AddListener(ToggleResearchMenu);
        if (pdaMenuButton != null) pdaMenuButton.onClick.AddListener(OpenPDAList);
        if (settingsButton != null) settingsButton.onClick.AddListener(ToggleSettings);
        if (timeControlButton != null) timeControlButton.onClick.AddListener(CycleTimeSpeed);
        
        // Build menu buttons
        if (solarBuildButton != null) solarBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Solar));
        if (windBuildButton != null) windBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Wind));
        if (hydroBuildButton != null) hydroBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Hydroelectric));
        if (tidalBuildButton != null) tidalBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Tidal));
        if (nuclearBuildButton != null) nuclearBuildButton.onClick.AddListener(() => SelectGenerator(MapGenerator.GeneratorType.Nuclear));
    }

    private void Update()
    {
        UpdateBuildMenuCosts();
        
        if (isBuildModeActive)
        {
            HandleBuildMode();
        }
    }

    #region Stats Display
    public void UpdateEnergyDisplay(float current, float max)
    {
        if (energyText != null)
        {
            energyText.text = $"Energy: {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }
        
        if (energyFillBar != null)
        {
            energyFillBar.fillAmount = max > 0 ? current / max : 0;
        }
    }

    public void UpdateCarbonDisplay(float current, float target)
    {
        if (carbonText != null)
        {
            float diff = current - target;
            carbonText.text = $"Carbon: {Mathf.FloorToInt(current)} ppm (Target: {target} ppm)";
            
            // Color code: green if close to target, yellow if moderate, red if high
            if (diff < 20)
            {
                carbonText.color = Color.green;
            }
            else if (diff < 50)
            {
                carbonText.color = Color.yellow;
            }
            else
            {
                carbonText.color = Color.red;
            }
        }
        
        if (carbonFillBar != null)
        {
            // Inverse fill - more carbon = more fill (bad)
            float maxCarbon = GameManager.STARTING_CARBON + 100f;
            carbonFillBar.fillAmount = Mathf.Clamp01((current - target) / (maxCarbon - target));
            carbonFillBar.color = Color.Lerp(Color.green, Color.red, carbonFillBar.fillAmount);
        }
    }

    public void UpdateTimeDisplay(int day, float timeOfDay)
    {
        if (dayText != null)
        {
            int hour = Mathf.FloorToInt(timeOfDay * 24);
            int minute = Mathf.FloorToInt((timeOfDay * 24 - hour) * 60);
            dayText.text = $"Day {day} - {hour:00}:{minute:00}";
        }
    }

    public void UpdateTimeSpeedDisplay(GameManager.TimeSpeed speed)
    {
        if (timeSpeedText != null)
        {
            switch (speed)
            {
                case GameManager.TimeSpeed.Paused:
                    timeSpeedText.text = "||";
                    break;
                case GameManager.TimeSpeed.Normal:
                    timeSpeedText.text = "►";
                    break;
                case GameManager.TimeSpeed.Speed2x:
                    timeSpeedText.text = "►►";
                    break;
                case GameManager.TimeSpeed.Speed5x:
                    timeSpeedText.text = "►►►";
                    break;
                case GameManager.TimeSpeed.Speed10x:
                    timeSpeedText.text = "►►►►";
                    break;
            }
        }
    }
    #endregion

    #region Menu Management
    private void CloseAllMenus()
    {
        if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
        if (researchMenuPanel != null) researchMenuPanel.SetActive(false);
        if (pdaListPanel != null) pdaListPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        isBuildModeActive = false;
    }

    private void ToggleBuildMenu()
    {
        bool newState = buildMenuPanel != null && !buildMenuPanel.activeSelf;
        CloseAllMenus();
        if (buildMenuPanel != null) buildMenuPanel.SetActive(newState);
    }

    private void ToggleResearchMenu()
    {
        bool newState = researchMenuPanel != null && !researchMenuPanel.activeSelf;
        CloseAllMenus();
        if (researchMenuPanel != null)
        {
            researchMenuPanel.SetActive(newState);
            if (newState) PopulateResearchTrees();
        }
    }

    private void OpenPDAList()
    {
        CloseAllMenus();
        // Open the PDA panel directly - sidebar is always visible when PDA is open
        if (PDASystem.Instance != null && PDASystem.Instance.pdaPanel != null)
        {
            PDASystem.Instance.pdaPanel.SetActive(true);
        }
    }

    private void ToggleSettings()
    {
        bool newState = settingsPanel != null && !settingsPanel.activeSelf;
        CloseAllMenus();
        if (settingsPanel != null) settingsPanel.SetActive(newState);
    }

    private void CycleTimeSpeed()
    {
        if (GameManager.Instance == null) return;
        
        GameManager.TimeSpeed current = GameManager.Instance.currentTimeSpeed;
        GameManager.TimeSpeed next;
        
        switch (current)
        {
            case GameManager.TimeSpeed.Paused:
                next = GameManager.TimeSpeed.Normal;
                break;
            case GameManager.TimeSpeed.Normal:
                next = GameManager.TimeSpeed.Speed2x;
                break;
            case GameManager.TimeSpeed.Speed2x:
                next = GameManager.TimeSpeed.Speed5x;
                break;
            case GameManager.TimeSpeed.Speed5x:
                next = GameManager.TimeSpeed.Speed10x;
                break;
            case GameManager.TimeSpeed.Speed10x:
                next = GameManager.TimeSpeed.Paused;
                break;
            default:
                next = GameManager.TimeSpeed.Normal;
                break;
        }
        
        GameManager.Instance.SetTimeSpeed(next);
        UpdateTimeSpeedDisplay(next);
    }
    #endregion

    #region Build Mode
    private void SelectGenerator(MapGenerator.GeneratorType type)
    {
        // Check if this type is unlocked
        if (ResearchManager.Instance != null && !ResearchManager.Instance.IsUnlocked(type, 1))
        {
            Debug.Log($"{type} not yet unlocked!");
            return;
        }
        
        selectedGeneratorType = type;
        selectedTier = 1; // Default to tier 1
        isBuildModeActive = true;
        
        Debug.Log($"Build mode: {type}");
    }

    private void HandleBuildMode()
    {
        // Get mouse position in world
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(mousePos.x), Mathf.FloorToInt(mousePos.y));
        
        // Show preview (you could add a sprite preview here)
        
        // Place on click
        if (Input.GetMouseButtonDown(0))
        {
            if (GeneratorManager.Instance != null)
            {
                bool success = GeneratorManager.Instance.PlaceGenerator(selectedGeneratorType, gridPos, selectedTier);
                if (success)
                {
                    // Optionally exit build mode or keep placing
                }
            }
        }
        
        // Cancel build mode on right click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            isBuildModeActive = false;
        }
    }

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
    #endregion

    #region Research Menu
    private void PopulateResearchTrees()
    {
        if (ResearchManager.Instance == null) return;
        
        // Clear existing nodes
        ClearResearchTree(solarTreeParent);
        ClearResearchTree(windTreeParent);
        ClearResearchTree(hydroTreeParent);
        ClearResearchTree(tidalTreeParent);
        ClearResearchTree(nuclearTreeParent);
        ClearResearchTree(batteryTreeParent);
        
        // Create nodes for each tree
        CreateResearchTreeNodes(MapGenerator.GeneratorType.Solar, solarTreeParent);
        CreateResearchTreeNodes(MapGenerator.GeneratorType.Wind, windTreeParent);
        CreateResearchTreeNodes(MapGenerator.GeneratorType.Hydroelectric, hydroTreeParent);
        CreateResearchTreeNodes(MapGenerator.GeneratorType.Tidal, tidalTreeParent);
        CreateResearchTreeNodes(MapGenerator.GeneratorType.Nuclear, nuclearTreeParent);
        CreateBatteryTreeNodes(batteryTreeParent);
    }

    private void ClearResearchTree(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateResearchTreeNodes(MapGenerator.GeneratorType type, Transform parent)
    {
        if (parent == null || researchNodePrefab == null) return;
        
        for (int tier = 1; tier <= 10; tier++)
        {
            string nodeId = $"{type}_Tier{tier}";
            ResearchNode node = ResearchManager.Instance.GetNode(nodeId);
            if (node == null) continue;
            
            GameObject nodeObj = Instantiate(researchNodePrefab, parent);
            ResearchNodeUI nodeUI = nodeObj.GetComponent<ResearchNodeUI>();
            if (nodeUI != null)
            {
                nodeUI.Setup(node);
            }
        }
    }

    private void CreateBatteryTreeNodes(Transform parent)
    {
        if (parent == null || researchNodePrefab == null) return;
        
        for (int tier = 1; tier <= 10; tier++)
        {
            BatteryNode node = ResearchManager.Instance.GetBatteryNode(tier);
            if (node == null) continue;
            
            GameObject nodeObj = Instantiate(researchNodePrefab, parent);
            BatteryNodeUI nodeUI = nodeObj.GetComponent<BatteryNodeUI>();
            if (nodeUI != null)
            {
                nodeUI.Setup(node);
            }
        }
    }
    #endregion

    #region Game Over
    public void ShowWinScreen()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(true);
        }
    }

    public void ShowLoseScreen()
    {
        if (loseScreen != null)
        {
            loseScreen.SetActive(true);
        }
    }
    #endregion
}

// ===== HELPER COMPONENT: RESEARCH NODE UI =====
/// <summary>
/// UI component for individual research nodes in the tech tree
/// Displays tier, cost, progress, and research button
/// </summary>
public class ResearchNodeUI : MonoBehaviour
{
    public TMP_Text titleText; // Changed to TMP_Text
    public TMP_Text costText; // Changed to TMP_Text
    public TMP_Text progressText; // Changed to TMP_Text
    public Button researchButton;
    public Image lockedOverlay;
    
    private ResearchNode node;
    
    public void Setup(ResearchNode researchNode)
    {
        node = researchNode;
        
        if (titleText != null)
            titleText.text = $"Tier {node.tier}";
        
        if (costText != null)
            costText.text = $"Cost: {node.energyCost}";
        
        if (researchButton != null)
        {
            researchButton.onClick.AddListener(OnResearchClicked);
            researchButton.interactable = !node.isUnlocked && !node.isResearching;
        }
        
        UpdateUI();
    }
    
    private void Update()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (node == null) return;
        
        if (progressText != null)
        {
            if (node.isUnlocked)
            {
                progressText.text = "Unlocked";
            }
            else if (node.isResearching)
            {
                progressText.text = $"{Mathf.FloorToInt(node.researchProgress * 100)}%";
            }
            else
            {
                progressText.text = "Locked";
            }
        }
        
        if (lockedOverlay != null)
        {
            lockedOverlay.gameObject.SetActive(!node.isUnlocked);
        }
    }
    
    private void OnResearchClicked()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.StartResearch(node.nodeId);
        }
    }
}

// ===== HELPER COMPONENT: BATTERY NODE UI =====
/// <summary>
/// UI component for battery research nodes
/// Similar to ResearchNodeUI but for battery storage upgrades
/// </summary>
public class BatteryNodeUI : MonoBehaviour
{
    public TMP_Text titleText; // Changed to TMP_Text
    public TMP_Text costText; // Changed to TMP_Text
    public TMP_Text progressText; // Changed to TMP_Text
    public Button researchButton;
    public Image lockedOverlay;
    
    private BatteryNode node;
    
    public void Setup(BatteryNode batteryNode)
    {
        node = batteryNode;
        
        if (titleText != null)
            titleText.text = $"Battery T{node.tier}";
        
        if (costText != null)
            costText.text = $"Cost: {node.energyCost}";
        
        if (researchButton != null)
        {
            researchButton.onClick.AddListener(OnResearchClicked);
            researchButton.interactable = !node.isUnlocked && !node.isResearching;
        }
        
        UpdateUI();
    }
    
    private void Update()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (node == null) return;
        
        if (progressText != null)
        {
            if (node.isUnlocked)
            {
                progressText.text = "Unlocked";
            }
            else if (node.isResearching)
            {
                progressText.text = $"{Mathf.FloorToInt(node.researchProgress * 100)}%";
            }
            else
            {
                progressText.text = "Locked";
            }
        }
        
        if (lockedOverlay != null)
        {
            lockedOverlay.gameObject.SetActive(!node.isUnlocked);
        }
    }
    
    private void OnResearchClicked()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.StartBatteryResearch(node.tier);
        }
    }
}
