using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the settings panel. Exposes Save, Load, Delete Save, Return to Menu, and Difficulty
/// controls. The Load and Delete buttons are toggled by <see cref="UpdateLoadAvailability"/>
/// whenever the save-file state changes. Difficulty can be set directly (Easy/Normal/Hard buttons)
/// or cycled with a single button. All changes are forwarded to <see cref="GameManager"/>.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const string DifficultyButtonName = "DifficultyCycleButton";
    private const string DifficultyLabelName = "DifficultyLabel";

    [Header("Settings Controls")]
    public Button saveGameButton;
    public Button loadFromSettingsButton;
    public Button returnToMenuButton;
    public Button deleteSaveButton;
    public TMP_Text loadGameButtonLabel;

    [Header("Difficulty Controls")]
    public Button difficultyEasyButton;
    public Button difficultyNormalButton;
    public Button difficultyHardButton;
    public Button difficultyCycleButton;
    public TMP_Text difficultyLabel;

    private void OnDestroy()
    {
        RemoveAllListeners();
    }

    /// <summary>
    /// Wires up all button listeners and ensures difficulty controls exist in the hierarchy.
    /// Called by UIManager on startup.
    /// </summary>
    public void Initialize()
    {
        EnsureDifficultyControls();
        SetupButtons();
        UpdateDifficultyDisplay();
    }

    /// <summary>
    /// Guarantees the cycle button and difficulty label exist in this panel's hierarchy.
    /// Tries to find existing children by name first; creates them at runtime if absent.
    /// This mirrors the pattern used by <see cref="UIManager.EnsureMainMenuDifficultyControl"/>.
    /// </summary>
    private void EnsureDifficultyControls()
    {
        if (difficultyCycleButton == null)
        {
            Transform existingButton = transform.Find(DifficultyButtonName);
            if (existingButton != null)
            {
                difficultyCycleButton = existingButton.GetComponent<Button>();
            }

            if (difficultyCycleButton == null)
            {
                GameObject buttonGO = new GameObject(DifficultyButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonGO.transform.SetParent(transform, false);

                RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(260f, 48f);

                Image buttonImage = buttonGO.GetComponent<Image>();
                buttonImage.color = new Color(0.14f, 0.32f, 0.5f, 1f);

                TMP_Text buttonText = CreateButtonLabel(buttonGO.transform, "Cycle Difficulty");
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.fontSize = 24f;

                difficultyCycleButton = buttonGO.GetComponent<Button>();
            }
        }

        if (difficultyLabel == null)
        {
            Transform existingLabel = transform.Find(DifficultyLabelName);
            if (existingLabel != null)
            {
                difficultyLabel = existingLabel.GetComponent<TMP_Text>();
            }

            if (difficultyLabel == null)
            {
                GameObject labelGO = new GameObject(DifficultyLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(transform, false);

                RectTransform labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(320f, 32f);

                difficultyLabel = labelGO.GetComponent<TextMeshProUGUI>();
                difficultyLabel.fontSize = 22f;
                difficultyLabel.color = Color.white;
                difficultyLabel.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private TMP_Text CreateButtonLabel(Transform parent, string text)
    {
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(parent, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = Color.white;

        return label;
    }

    /// <summary>Registers click listeners on all settings and difficulty buttons.</summary>
    private void SetupButtons()
    {
        if (saveGameButton != null)
        {
            saveGameButton.onClick.RemoveAllListeners();
            saveGameButton.onClick.AddListener(OnSaveGameClicked);
        }
        if (loadFromSettingsButton != null)
        {
            loadFromSettingsButton.onClick.RemoveAllListeners();
            loadFromSettingsButton.onClick.AddListener(OnLoadGameClicked);
        }
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.RemoveAllListeners();
            deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        }

        if (difficultyEasyButton != null)
        {
            difficultyEasyButton.onClick.RemoveAllListeners();
            difficultyEasyButton.onClick.AddListener(SetDifficultyEasy);
        }
        if (difficultyNormalButton != null)
        {
            difficultyNormalButton.onClick.RemoveAllListeners();
            difficultyNormalButton.onClick.AddListener(SetDifficultyNormal);
        }
        if (difficultyHardButton != null)
        {
            difficultyHardButton.onClick.RemoveAllListeners();
            difficultyHardButton.onClick.AddListener(SetDifficultyHard);
        }
        if (difficultyCycleButton != null)
        {
            difficultyCycleButton.onClick.RemoveAllListeners();
            difficultyCycleButton.onClick.AddListener(CycleDifficulty);
        }
    }

    private void RemoveAllListeners()
    {
        if (saveGameButton != null) saveGameButton.onClick.RemoveAllListeners();
        if (loadFromSettingsButton != null) loadFromSettingsButton.onClick.RemoveAllListeners();
        if (returnToMenuButton != null) returnToMenuButton.onClick.RemoveAllListeners();
        if (deleteSaveButton != null) deleteSaveButton.onClick.RemoveAllListeners();
        if (difficultyEasyButton != null) difficultyEasyButton.onClick.RemoveAllListeners();
        if (difficultyNormalButton != null) difficultyNormalButton.onClick.RemoveAllListeners();
        if (difficultyHardButton != null) difficultyHardButton.onClick.RemoveAllListeners();
        if (difficultyCycleButton != null) difficultyCycleButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Enables or disables the Load and Delete buttons based on whether a save file exists.
    /// Also updates the load button label text.
    /// </summary>
    public void UpdateLoadAvailability()
    {
        bool hasSave = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.SaveFileExists();
        if (loadFromSettingsButton != null) loadFromSettingsButton.interactable = hasSave;
        if (deleteSaveButton != null) deleteSaveButton.interactable = hasSave;
        if (loadGameButtonLabel != null)
        {
            loadGameButtonLabel.text = hasSave ? "Load Game" : "Load Game (No Save)";
        }
    }

    // ===== BUTTON HANDLERS =====

    /// <summary>Saves the current game and refreshes load button availability.</summary>
    private void OnSaveGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            UpdateLoadAvailability();
        }
    }

    /// <summary>
    /// Loads the saved game. If loading fails (no save file or parse error) the load
    /// button availability is refreshed so it correctly shows "No Save".
    /// </summary>
    private void OnLoadGameClicked()
    {
        if (GameManager.Instance == null) return;
        bool loaded = GameManager.Instance.LoadSavedGameFromMenu();
        if (!loaded)
        {
            Debug.Log("No valid save found to load.");
            UpdateLoadAvailability();
            return;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshAllDisplays();
        }
    }

    /// <summary>Returns to the main menu, pausing time and showing the menu panel.</summary>
    private void OnReturnToMenuClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterMainMenu();
        }
    }

    /// <summary>Deletes the save file from disk and refreshes button availability.</summary>
    private void OnDeleteSaveClicked()
    {
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSave();
            UpdateLoadAvailability();
        }
    }

    /// <summary>Sets difficulty to Easy. Bindable to a UI button.</summary>
    public void SetDifficultyEasy()
    {
        SetDifficulty(GameDifficulty.Easy);
    }

    /// <summary>Sets difficulty to Normal. Bindable to a UI button.</summary>
    public void SetDifficultyNormal()
    {
        SetDifficulty(GameDifficulty.Normal);
    }

    /// <summary>Sets difficulty to Hard. Bindable to a UI button.</summary>
    public void SetDifficultyHard()
    {
        SetDifficulty(GameDifficulty.Hard);
    }

    /// <summary>
    /// Sets the active difficulty and refreshes the difficulty label.
    /// Propagates to GameManager which distributes it to all game systems.
    /// </summary>
    public void SetDifficulty(GameDifficulty difficulty)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDifficulty(difficulty);
        }

        UpdateDifficultyDisplay();
    }

    /// <summary>
    /// Advances difficulty one step in the cycle: Easy → Normal → Hard → Easy.
    /// Applies the new setting and refreshes the label.
    /// </summary>
    public void CycleDifficulty()
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
        UpdateDifficultyDisplay();
    }

    /// <summary>Updates the difficulty label text to reflect the current <see cref="GameDifficulty"/>.</summary>
    private void UpdateDifficultyDisplay()
    {
        if (difficultyLabel == null)
        {
            return;
        }

        GameDifficulty difficulty = GameManager.Instance != null
            ? GameManager.Instance.currentDifficulty
            : GameDifficulty.Normal;

        difficultyLabel.text = $"Difficulty: {difficulty}";
    }
}
