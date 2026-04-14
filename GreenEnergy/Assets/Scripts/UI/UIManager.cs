using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages all UI interactions, displays, and menus
/// Handles stats display and menu coordination, delegating build and research to specialized controllers
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
    public Button settingsButton;
    [Tooltip("Button in top menu bar to open/close PDA menu")] public Button pdaMenuButton;
    public Button timeControlButton;

    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button newGameButton;
    public Button loadGameButton;
    public TMP_Text loadGameButtonLabel;
    
    [Header("Menu Panels")]
    public GameObject buildMenuPanel;
    public GameObject researchMenuPanel;
    public GameObject settingsPanel;
    [Tooltip("Main PDA menu panel GameObject")] public GameObject pdaMenuPanel;

    [Header("Game Over Screens")]
    public GameObject winScreen;
    public GameObject loseScreen;
    
    [Header("Time Control")]
    public Sprite pauseSprite;
    public Sprite normalSprite;
    public Sprite speed2xSprite;
    public Sprite speed5xSprite;
    public Sprite speed10xSprite;
    
    [Header("Menu Controllers")]
    public BuildMenuController buildMenuController;
    public ResearchMenuController researchMenuController;
    public SettingsManager settingsManager;
    [Tooltip("PDA menu controller component")] public PDAMenuController pdaMenuController;

    private Button mainMenuDifficultyButton;
    private TMP_Text mainMenuDifficultyLabel;
    private Coroutine _returnToMenuCoroutine;

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

    private void OnDestroy()
    {
        if (buildMenuButton != null) buildMenuButton.onClick.RemoveAllListeners();
        if (researchMenuButton != null) researchMenuButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
        if (pdaMenuButton != null) pdaMenuButton.onClick.RemoveAllListeners();
        if (timeControlButton != null) timeControlButton.onClick.RemoveAllListeners();

        if (newGameButton != null) newGameButton.onClick.RemoveAllListeners();
        if (loadGameButton != null) loadGameButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        // Auto-create PDA menu UI if scene references are missing
        PDAUIAutoSetup.SetupPDAMenuInUIManager(this);
        
        SetupButtons();
        EnsureMainMenuDifficultyControl();
        
        // Apply modern UI styling
        UIStyleBootstrapper styleBootstrapper = GetComponent<UIStyleBootstrapper>();
        if (styleBootstrapper != null)
        {
            styleBootstrapper.ApplyTheme();
        }
        
        // Apply specialized menu styling
        ApplyMenuStyling();
        RefreshMainMenuDifficultyDisplay();
        
        CloseAllMenus();
    }
    
    /// <summary>
    /// Adds specialized styler components for each menu panel and triggers their
    /// <c>Apply*Styling()</c> methods. Also adds <see cref="MenuAnimationSetup"/> to every
    /// panel so fade animations are available. Called once from <see cref="Start"/>.
    /// </summary>
    private void ApplyMenuStyling()
    {
        // Style stats bar and menu bar
        StatsBarStyler statsStyler = gameObject.AddComponent<StatsBarStyler>();
        statsStyler.ApplyStatsBarStyling(this);
        statsStyler.ApplyMenuBarButtonStyling(this);
        
        // Setup animations for all menus
        SetupMenuAnimations();
        
        // Style build menu
        if (buildMenuController != null)
        {
            BuildMenuStyler buildStyler = gameObject.AddComponent<BuildMenuStyler>();
            buildStyler.ApplyBuildMenuStyling(buildMenuController);
        }
        
        // Style research menu
        if (researchMenuController != null)
        {
            ResearchMenuStyler researchStyler = gameObject.AddComponent<ResearchMenuStyler>();
            researchStyler.ApplyResearchMenuStyling(researchMenuController);
        }
        
        // Style settings menu
        if (settingsManager != null)
        {
            SettingsMenuStyler settingsStyler = gameObject.AddComponent<SettingsMenuStyler>();
            settingsStyler.ApplySettingsMenuStyling(settingsManager);
        }

        if (pdaMenuController != null)
        {
            pdaMenuController.ApplyStyling();
        }

        RefreshMainMenuDifficultyDisplay();
    }

    /// <summary>
    /// Guarantees a difficulty-cycle button exists inside <see cref="mainMenuPanel"/>.
    /// First tries to find an existing "DifficultyCycleButton" child; if none is found,
    /// builds one at runtime with the correct layout and colour block.
    /// </summary>
    private void EnsureMainMenuDifficultyControl()
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        if (mainMenuDifficultyButton == null)
        {
            Transform existingButton = mainMenuPanel.transform.Find("DifficultyCycleButton");
            if (existingButton != null)
            {
                mainMenuDifficultyButton = existingButton.GetComponent<Button>();
                mainMenuDifficultyLabel = existingButton.GetComponentInChildren<TMP_Text>();
            }

            if (mainMenuDifficultyButton == null)
            {
                GameObject buttonGO = new GameObject("DifficultyCycleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonGO.transform.SetParent(mainMenuPanel.transform, false);

                RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.localScale = Vector3.one;
                buttonRect.localRotation = Quaternion.identity;
                buttonRect.sizeDelta = new Vector2(0f, 46f);

                LayoutElement buttonLayout = buttonGO.GetComponent<LayoutElement>();
                buttonLayout.preferredHeight = 46f;
                buttonLayout.minHeight = 46f;
                buttonLayout.flexibleWidth = 1f;

                Image buttonImage = buttonGO.GetComponent<Image>();
                buttonImage.color = new Color(0.14f, 0.32f, 0.5f, 1f);

                Button button = buttonGO.GetComponent<Button>();
                button.targetGraphic = buttonImage;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = new ColorBlock
                {
                    normalColor = new Color(0.14f, 0.32f, 0.5f, 1f),
                    highlightedColor = new Color(0.2f, 0.44f, 0.66f, 1f),
                    pressedColor = new Color(0.09f, 0.24f, 0.38f, 1f),
                    selectedColor = new Color(0.2f, 0.44f, 0.66f, 1f),
                    disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f),
                    colorMultiplier = 1f,
                    fadeDuration = 0.1f
                };

                GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(buttonGO.transform, false);

                RectTransform labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                TMP_Text label = labelGO.GetComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 24f;
                label.color = Color.white;

                mainMenuDifficultyButton = button;
                mainMenuDifficultyLabel = label;
            }
        }

        if (mainMenuDifficultyButton != null)
        {
            mainMenuDifficultyButton.onClick.RemoveAllListeners();
            mainMenuDifficultyButton.onClick.AddListener(CycleDifficultyFromMainMenu);
        }

        RefreshMainMenuDifficultyDisplay();
    }

    private void RefreshMainMenuDifficultyDisplay()
    {
        if (mainMenuDifficultyLabel == null)
        {
            return;
        }

        GameDifficulty difficulty = GameManager.Instance != null
            ? GameManager.Instance.currentDifficulty
            : GameDifficulty.Normal;

        mainMenuDifficultyLabel.text = $"Difficulty: {difficulty}";
    }

    /// <summary>
    /// Cycles difficulty Easy → Normal → Hard → Easy when the difficulty button is clicked
    /// on the main menu. Also refreshes the settings panel's load-button availability,
    /// since difficulty affects whether a compatible save exists.
    /// </summary>
    private void CycleDifficultyFromMainMenu()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameDifficulty next = GameManager.Instance.currentDifficulty switch
        {
            GameDifficulty.Easy => GameDifficulty.Normal,
            GameDifficulty.Normal => GameDifficulty.Hard,
            GameDifficulty.Hard => GameDifficulty.Easy,
            _ => GameDifficulty.Normal
        };

        GameManager.Instance.SetDifficulty(next);
        RefreshMainMenuDifficultyDisplay();

        if (settingsManager != null)
        {
            settingsManager.UpdateLoadAvailability();
        }
    }
    
    /// <summary>
    /// Adds a <see cref="MenuAnimationSetup"/> component to every menu panel that is missing one.
    /// <see cref="MenuAnimationSetup"/> guarantees a <c>CanvasGroup</c> exists, which
    /// <see cref="UIAnimationHelper"/> requires for fade-in/out coroutines.
    /// </summary>
    private void SetupMenuAnimations()
    {
        // Ensure all menus have CanvasGroup for fade animations
        if (buildMenuPanel != null)
        {
            MenuAnimationSetup buildAnimSetup = buildMenuPanel.GetComponent<MenuAnimationSetup>();
            if (buildAnimSetup == null)
                buildMenuPanel.AddComponent<MenuAnimationSetup>();
        }
        
        if (researchMenuPanel != null)
        {
            MenuAnimationSetup researchAnimSetup = researchMenuPanel.GetComponent<MenuAnimationSetup>();
            if (researchAnimSetup == null)
                researchMenuPanel.AddComponent<MenuAnimationSetup>();
        }
        
        if (settingsPanel != null)
        {
            MenuAnimationSetup settingsAnimSetup = settingsPanel.GetComponent<MenuAnimationSetup>();
            if (settingsAnimSetup == null)
                settingsPanel.AddComponent<MenuAnimationSetup>();
        }

        if (pdaMenuPanel != null)
        {
            MenuAnimationSetup pdaAnimSetup = pdaMenuPanel.GetComponent<MenuAnimationSetup>();
            if (pdaAnimSetup == null)
                pdaMenuPanel.AddComponent<MenuAnimationSetup>();
        }
        
    }

    /// <summary>
    /// Initializes the UI at game start or after a new game/load.
    /// Resets win/lose screens and refreshes all stat displays.
    /// </summary>
    public void Initialize()
    {
        if (_returnToMenuCoroutine != null)
        {
            StopCoroutine(_returnToMenuCoroutine);
            _returnToMenuCoroutine = null;
        }

        if (GameManager.Instance != null)
        {
            RefreshAllDisplays();
        }
        else
        {
            UpdateEnergyDisplay(0, 1000);
            UpdateCarbonDisplay(GameManager.STARTING_CARBON, GameManager.TARGET_CARBON);
            UpdateTimeDisplay(0, 0.25f);
            UpdateTimeSpeedDisplay(GameManager.TimeSpeed.Normal);
        }
        
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        if (settingsManager != null) settingsManager.UpdateLoadAvailability();
    }

    private void SetupButtons()
    {
        // Menu bar buttons
        if (buildMenuButton != null) { buildMenuButton.onClick.RemoveAllListeners(); buildMenuButton.onClick.AddListener(ToggleBuildMenu); }
        if (researchMenuButton != null) { researchMenuButton.onClick.RemoveAllListeners(); researchMenuButton.onClick.AddListener(ToggleResearchMenu); }
        if (settingsButton != null) { settingsButton.onClick.RemoveAllListeners(); settingsButton.onClick.AddListener(ToggleSettings); }
        if (pdaMenuButton != null) { pdaMenuButton.onClick.RemoveAllListeners(); pdaMenuButton.onClick.AddListener(TogglePDAMenu); }
        if (timeControlButton != null) { timeControlButton.onClick.RemoveAllListeners(); timeControlButton.onClick.AddListener(CycleTimeSpeed); }

        // Main menu buttons
        if (newGameButton != null) { newGameButton.onClick.RemoveAllListeners(); newGameButton.onClick.AddListener(OnNewGameClicked); }
        if (loadGameButton != null) { loadGameButton.onClick.RemoveAllListeners(); loadGameButton.onClick.AddListener(OnLoadGameClicked); }

        // Initialize menu controllers
        if (buildMenuController != null) buildMenuController.Initialize();
        if (researchMenuController != null) researchMenuController.Initialize();
        if (settingsManager != null) settingsManager.Initialize();
        if (pdaMenuController != null) pdaMenuController.Initialize();
    }

    private void Update()
    {
        // Update build menu display and input handling
        if (buildMenuPanel != null && buildMenuPanel.activeSelf && buildMenuController != null)
        {
            buildMenuController.UpdateDisplay();
        }

        // Update research menu display
        if (researchMenuPanel != null && researchMenuPanel.activeSelf && researchMenuController != null)
        {
            researchMenuController.RefreshResearchMenuButtons();
        }

    }

    #region Stats Display
    /// <summary>
    /// Updates the energy text and fill bar in the stats bar.
    /// The fill bar uses a red→yellow→green gradient based on storage percentage.
    /// </summary>
    public void UpdateEnergyDisplay(float current, float max)
    {
        if (energyText != null)
        {
            energyText.text = $"Energy: {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }
        
        if (energyFillBar != null)
        {
            float fillAmount = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            energyFillBar.fillAmount = fillAmount;
            
            // Three-point gradient so the middle reads as yellow instead of a muddy blend.
            if (fillAmount <= 0.5f)
            {
                energyFillBar.color = Color.Lerp(Color.red, Color.yellow, fillAmount * 2f);
            }
            else
            {
                energyFillBar.color = Color.Lerp(Color.yellow, Color.green, (fillAmount - 0.5f) * 2f);
            }
        }
    }

    /// <summary>
    /// Updates the carbon text and fill bar. Text is color-coded green/yellow/red by proximity to target.
    /// Fill bar is inverse: more carbon = more fill (represents danger level).
    /// </summary>
    public void UpdateCarbonDisplay(float current, float target)
    {
        if (carbonText != null)
        {
            float diff = current - target;
            carbonText.text = $"Carbon: {Mathf.FloorToInt(current)} ppm";
            
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
            float maxCarbon = GameManager.Instance != null
                ? GameManager.Instance.GetLoseCarbonThreshold()
                : GameManager.STARTING_CARBON + 180f;
            carbonFillBar.fillAmount = Mathf.Clamp01((current - target) / (maxCarbon - target));
            carbonFillBar.color = Color.Lerp(Color.green, Color.red, carbonFillBar.fillAmount);
        }
    }

    /// <summary>
    /// Updates the day/time label to show the current in-game day and time in HH:MM format.
    /// </summary>
    public void UpdateTimeDisplay(int day, float timeOfDay)
    {
        if (dayText != null)
        {
            int hour = Mathf.FloorToInt(timeOfDay * 24);
            int minute = Mathf.FloorToInt((timeOfDay * 24 - hour) * 60);
            dayText.text = $"Day {day} - {hour:00}:{minute:00}";
        }
    }

    /// <summary>
    /// Updates the time speed indicator text with the appropriate pause/play/fast-forward symbol.
    /// </summary>
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
    /// <summary>
    /// Animates all currently open menus closed and cancels any active build mode.
    /// Called before opening a new menu so only one panel is visible at a time.
    /// </summary>
    private void CloseAllMenus()
    {
        if (buildMenuPanel != null && buildMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(buildMenuPanel));
        }
        if (buildMenuController != null) buildMenuController.CancelBuildMode();
        
        if (researchMenuPanel != null && researchMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(researchMenuPanel));
        }
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(settingsPanel));
        }
        if (pdaMenuPanel != null && pdaMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(pdaMenuPanel));
        }
    }
    
    /// <summary>
    /// Fades out <paramref name="menuPanel"/> using <see cref="UIAnimationHelper"/> then deactivates it.
    /// Always animate through this coroutine rather than calling SetActive(false) directly
    /// so the close transition is never skipped.
    /// </summary>
    private System.Collections.IEnumerator AnimateMenuClose(GameObject menuPanel)
    {
        CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = menuPanel.AddComponent<CanvasGroup>();
        
        yield return StartCoroutine(UIAnimationHelper.AnimateMenuDisappear(canvasGroup, UITheme.AnimationFastDuration));
        menuPanel.SetActive(false);
    }
    
    /// <summary>
    /// Activates <paramref name="menuPanel"/> then fades it in using <see cref="UIAnimationHelper"/>.
    /// Always animate through this coroutine rather than calling SetActive(true) directly
    /// so the open transition is never skipped.
    /// </summary>
    private System.Collections.IEnumerator AnimateMenuOpen(GameObject menuPanel)
    {
        menuPanel.SetActive(true);
        
        CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = menuPanel.AddComponent<CanvasGroup>();
        
        yield return StartCoroutine(UIAnimationHelper.AnimateMenuAppear(canvasGroup, UITheme.AnimationNormalDuration));
    }

    /// <summary>Shows the main menu panel and hides the gameplay menu bar.</summary>
    public void ShowMainMenu()
    {
        CloseAllMenus();
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (menuBarPanel != null) menuBarPanel.SetActive(false);
        if (settingsManager != null) settingsManager.UpdateLoadAvailability();
    }

    /// <summary>Hides the main menu and shows the gameplay menu bar.</summary>
    public void HideMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (menuBarPanel != null) menuBarPanel.SetActive(true);
    }

    /// <summary>
    /// Re-reads all values from GameManager and updates every stats bar display.
    /// Called after loading a save or changing difficulty.
    /// </summary>
    public void RefreshAllDisplays()
    {
        if (GameManager.Instance == null) return;

        UpdateEnergyDisplay(GameManager.Instance.currentEnergy, GameManager.Instance.maxEnergyStorage);
        UpdateCarbonDisplay(GameManager.Instance.carbonLevel, GameManager.Instance.CurrentTargetCarbon);
        UpdateTimeDisplay(GameManager.Instance.currentDay, GameManager.Instance.timeOfDay);
        UpdateTimeSpeedDisplay(GameManager.Instance.currentTimeSpeed);
        RefreshMainMenuDifficultyDisplay();
    }

    private void ToggleBuildMenu()
    {
        if (buildMenuPanel == null)
            return;
        
        if (buildMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(buildMenuPanel));
            if (buildMenuController != null) buildMenuController.CancelBuildMode();
        }
        else
        {
            CloseAllMenus();
            StartCoroutine(AnimateMenuOpen(buildMenuPanel));
        }
    }

    private void ToggleResearchMenu()
    {
        if (researchMenuPanel == null)
            return;
        
        if (researchMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(researchMenuPanel));
        }
        else
        {
            CloseAllMenus();
            StartCoroutine(AnimateMenuOpen(researchMenuPanel));
            if (researchMenuController != null)
            {
                researchMenuController.RefreshResearchMenuButtons();
            }
        }
    }

    private void ToggleSettings()
    {
        if (settingsPanel == null)
            return;
        
        if (settingsPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(settingsPanel));
        }
        else
        {
            CloseAllMenus();
            StartCoroutine(AnimateMenuOpen(settingsPanel));
            if (settingsManager != null) settingsManager.UpdateLoadAvailability();
        }
    }

    /// <summary>Opens the PDA menu if closed, or closes it if open. Also callable from the PDA close button.</summary>
    public void TogglePDAMenu()
    {
        if (pdaMenuPanel == null)
            return;

        if (pdaMenuPanel.activeSelf)
        {
            StartCoroutine(AnimateMenuClose(pdaMenuPanel));
        }
        else
        {
            CloseAllMenus();
            StartCoroutine(AnimateMenuOpen(pdaMenuPanel));

            if (pdaMenuController != null)
            {
                pdaMenuController.RefreshPanel();
            }
        }
    }

    private void OnNewGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
            RefreshAllDisplays();
        }
    }

    private void OnLoadGameClicked()
    {
        if (GameManager.Instance == null) return;
        bool loaded = GameManager.Instance.LoadSavedGameFromMenu();
        if (!loaded)
        {
            Debug.Log("No valid save found to load.");
            if (settingsManager != null) settingsManager.UpdateLoadAvailability();
            return;
        }
        RefreshAllDisplays();
    }

    /// <summary>
    /// Advances <see cref="GameManager.TimeSpeed"/> through the cycle
    /// Paused → Normal → 2× → 5× → 10× → Paused and updates the speed indicator.
    /// </summary>
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
    // Build mode is now handled by BuildMenuController
    #endregion

    #region Research Menu
    // Research menu is now handled by ResearchMenuController
    #endregion

    #region Game Over
    /// <summary>Shows the win screen and starts a 10-second timer before returning to the main menu.</summary>
    public void ShowWinScreen()
    {
        if (winScreen != null)
            winScreen.SetActive(true);
        if (_returnToMenuCoroutine != null)
            StopCoroutine(_returnToMenuCoroutine);
        _returnToMenuCoroutine = StartCoroutine(ReturnToMenuAfterDelay());
    }

    /// <summary>Shows the lose screen and starts a 10-second timer before returning to the main menu.</summary>
    public void ShowLoseScreen()
    {
        if (loseScreen != null)
            loseScreen.SetActive(true);
        if (_returnToMenuCoroutine != null)
            StopCoroutine(_returnToMenuCoroutine);
        _returnToMenuCoroutine = StartCoroutine(ReturnToMenuAfterDelay());
    }

    private System.Collections.IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(10f);
        if (GameManager.Instance != null)
            GameManager.Instance.EnterMainMenu();
    }
    #endregion
}
