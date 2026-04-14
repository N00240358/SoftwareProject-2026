using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the PDA (Personal Data Assistant) encyclopedia menu.
/// Renders a scrollable sidebar of category headers and entry buttons, and a detail
/// panel showing the selected entry's title, unlock status, and content.
/// Entries are driven by <see cref="PDAProgressionSystem"/>; locked entries are visible
/// but greyed-out until the player meets their unlock conditions.
/// </summary>
public class PDAMenuController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] [Tooltip("The PDA menu panel GameObject")] public GameObject pdaMenuPanel;

    [Header("Runtime Layout")]
    [SerializeField] [Tooltip("Content container for dynamically generated entry buttons")] public RectTransform sidebarContent;
    [SerializeField] [Tooltip("TextMeshPro field for entry title display")] public TextMeshProUGUI entryTitleText;
    [SerializeField] [Tooltip("TextMeshPro field for lock status and unlock hints")] public TextMeshProUGUI entryStatusText;
    [SerializeField] [Tooltip("TextMeshPro field for entry content/description")] public TextMeshProUGUI entryBodyText;
    [SerializeField] [Tooltip("Close button to hide the PDA menu")] public Button closeButton;

    // Setters for auto-setup — used by PDAUIAutoSetup to inject references when the hierarchy is built at runtime.
    public void SetSidebarContent(RectTransform content) => sidebarContent = content;
    public void SetEntryTitleText(TextMeshProUGUI text) => entryTitleText = text;
    public void SetEntryStatusText(TextMeshProUGUI text) => entryStatusText = text;
    public void SetEntryBodyText(TextMeshProUGUI text) => entryBodyText = text;
    public void SetCloseButton(Button button) => closeButton = button;

    private readonly List<Button> generatedButtons = new List<Button>();
    private TMP_FontAsset sidebarFont;
    private string selectedCategory;
    private string selectedEntryId;

    private void Awake()
    {
        if (pdaMenuPanel == null)
        {
            pdaMenuPanel = gameObject;
        }

        EnsureRuntimeLayoutComponents();
        EnsureLayoutReferences();
    }

    private void OnEnable()
    {
        BindProgressionEvents();
        RefreshPanel();
    }

    private void OnDisable()
    {
        UnbindProgressionEvents();
    }

    /// <summary>
    /// Sets up layout components, resolves UI references, subscribes to progression events,
    /// and performs the first full panel refresh. Called by UIManager on startup.
    /// </summary>
    public void Initialize()
    {
        EnsureRuntimeLayoutComponents();
        EnsureLayoutReferences();
        BindProgressionEvents();
        RefreshPanel();
    }

    /// <summary>
    /// Rebuilds the sidebar and refreshes the detail panel to match current progression state.
    /// Automatically selects the first unlocked entry if the current selection is no longer valid.
    /// Called on enable and whenever <see cref="PDAProgressionSystem.OnProgressionChanged"/> fires.
    /// </summary>
    public void RefreshPanel()
    {
        if (PDAProgressionSystem.Instance == null)
        {
            SetEmptyState("PDA unavailable", "PDA data could not be loaded.");
            return;
        }

        // Pull latest unlock state (research and milestones) before rendering.
        PDAProgressionSystem.Instance.SyncWithCurrentGameState(false);

        if (PDAProgressionSystem.Instance.GetCategories().Count == 0)
        {
            SetEmptyState("No entries", "No PDA entries are available yet.");
            return;
        }

        BuildSidebar();

        if (string.IsNullOrWhiteSpace(selectedCategory) || !CategoryExists(selectedCategory))
        {
            selectedCategory = PDAProgressionSystem.Instance.GetCategories()[0].categoryName;
        }

        if (string.IsNullOrWhiteSpace(selectedEntryId) || !EntryExists(selectedEntryId) || !PDAProgressionSystem.Instance.IsUnlocked(selectedEntryId))
        {
            selectedEntryId = GetFirstUnlockedEntryIdForCategory(selectedCategory);

            if (string.IsNullOrWhiteSpace(selectedEntryId))
            {
                selectedEntryId = GetFirstUnlockedEntryId();
            }
        }

        if (string.IsNullOrWhiteSpace(selectedEntryId))
        {
            SetEmptyState("No unlocked entries", "All PDA entries are currently locked. Unlock research tiers or milestones to view details.");
        }
        else
        {
            RefreshEntryDetails(selectedEntryId);
        }

        UpdateSidebarSelectionState();
    }

    /// <summary>
    /// Switches the active category and auto-selects its first unlocked entry.
    /// If the category has no unlocked entries, clears the detail panel.
    /// </summary>
    public void SelectCategory(string categoryName)
    {
        selectedCategory = categoryName;
        selectedEntryId = GetFirstUnlockedEntryIdForCategory(categoryName);

        if (string.IsNullOrWhiteSpace(selectedEntryId))
        {
            SetEmptyState("No unlocked entries", "This category has no unlocked entries yet.");
            UpdateSidebarSelectionState();
            return;
        }

        RefreshPanel();
    }

    /// <summary>
    /// Selects a specific entry by ID and updates the detail panel.
    /// Does nothing if the entry is locked or does not exist.
    /// </summary>
    public void SelectEntry(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return;
        }

        if (PDAProgressionSystem.Instance == null || !PDAProgressionSystem.Instance.IsUnlocked(entryId))
        {
            return;
        }

        selectedEntryId = entryId;
        PDAEntryDefinition entry = PDAProgressionSystem.Instance != null ? PDAProgressionSystem.Instance.GetEntry(entryId) : null;
        if (entry != null)
        {
            selectedCategory = entry.category;
        }

        RefreshEntryDetails(entryId);
        UpdateSidebarSelectionState();
    }

    /// <summary>
    /// Applies theme colours and font sizes to the panel background, close button, and text fields.
    /// Should be called after the hierarchy is fully built (e.g. from PDAUIAutoSetup).
    /// </summary>
    public void ApplyStyling()
    {
        if (pdaMenuPanel == null)
        {
            return;
        }

        Image panelImage = pdaMenuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            PanelStyle.ApplyDarkPanelStyle(panelImage);
            PanelStyle.ApplyShadowEffect(panelImage);
        }

        if (closeButton != null)
        {
            ButtonStyle.ApplyMinimalStyle(closeButton, "X");
        }

        if (entryTitleText != null)
        {
            entryTitleText.color = UITheme.ColorTextPrimary;
            entryTitleText.fontSize = 34;
            entryTitleText.fontStyle = FontStyles.Bold;
        }

        if (entryStatusText != null)
        {
            entryStatusText.color = UITheme.ColorTextSecondary;
            entryStatusText.fontSize = 20;
        }

        if (entryBodyText != null)
        {
            entryBodyText.color = UITheme.ColorTextPrimary;
            entryBodyText.fontSize = 22;
        }
    }

    /// <summary>
    /// Subscribes <see cref="RefreshPanel"/> to <see cref="PDAProgressionSystem.OnProgressionChanged"/>
    /// so the panel auto-updates whenever a research tier or milestone is unlocked.
    /// Remove-before-add prevents double-subscription if called more than once.
    /// </summary>
    private void BindProgressionEvents()
    {
        PDAProgressionSystem progressionSystem = PDAProgressionSystem.Instance;
        if (progressionSystem != null)
        {
            progressionSystem.OnProgressionChanged -= RefreshPanel;
            progressionSystem.OnProgressionChanged += RefreshPanel;
        }
    }

    /// <summary>
    /// Unsubscribes from <see cref="PDAProgressionSystem.OnProgressionChanged"/>.
    /// Uses <see cref="PDAProgressionSystem.ExistingInstance"/> (non-creating accessor) to
    /// avoid spawning the system during scene teardown.
    /// </summary>
    private void UnbindProgressionEvents()
    {
        PDAProgressionSystem progressionSystem = PDAProgressionSystem.ExistingInstance;
        if (progressionSystem != null)
        {
            progressionSystem.OnProgressionChanged -= RefreshPanel;
        }
    }

    /// <summary>
    /// Attempts to resolve all required UI references (sidebar, title, status, body, close button)
    /// by walking the panel hierarchy using known path strings and a fallback depth-first search.
    /// Safe to call multiple times — skips any reference that is already assigned.
    /// </summary>
    private void EnsureLayoutReferences()
    {
        if (pdaMenuPanel == null)
        {
            pdaMenuPanel = gameObject;
        }

        if (sidebarContent == null)
        {
            Transform sidebarTransform = pdaMenuPanel.transform.Find("Content/Sidebar/Viewport/SidebarContent");
            if (sidebarTransform == null)
            {
                sidebarTransform = FindDescendantByName(pdaMenuPanel.transform, "SidebarContent");
            }
            if (sidebarTransform != null)
            {
                sidebarContent = sidebarTransform as RectTransform;
            }
        }

        if (entryTitleText == null)
        {
            Transform titleTransform = pdaMenuPanel.transform.Find("Content/Details/DetailTitle");
            if (titleTransform == null)
            {
                titleTransform = FindDescendantByName(pdaMenuPanel.transform, "DetailTitle");
            }
            if (titleTransform != null)
            {
                entryTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (entryStatusText == null)
        {
            Transform statusTransform = pdaMenuPanel.transform.Find("Content/Details/DetailStatus");
            if (statusTransform == null)
            {
                statusTransform = FindDescendantByName(pdaMenuPanel.transform, "DetailStatus");
            }
            if (statusTransform != null)
            {
                entryStatusText = statusTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (entryBodyText == null)
        {
            Transform bodyTransform = pdaMenuPanel.transform.Find("Content/Details/DetailBody");
            if (bodyTransform == null)
            {
                bodyTransform = FindDescendantByName(pdaMenuPanel.transform, "DetailBody");
            }
            if (bodyTransform != null)
            {
                entryBodyText = bodyTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (closeButton == null)
        {
            Transform closeTransform = pdaMenuPanel.transform.Find("Header/CloseButton");
            if (closeTransform == null)
            {
                closeTransform = FindDescendantByName(pdaMenuPanel.transform, "CloseButton");
            }
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (sidebarFont == null)
        {
            sidebarFont = ResolveSidebarFont();
        }
    }

    private TMP_FontAsset ResolveSidebarFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        if (entryTitleText != null && entryTitleText.font != null)
        {
            return entryTitleText.font;
        }

        if (entryBodyText != null && entryBodyText.font != null)
        {
            return entryBodyText.font;
        }

        return null;
    }

    /// <summary>
    /// Adds or configures layout components (LayoutGroups, ScrollRect, ContentSizeFitter, RectMask2D)
    /// on the existing panel hierarchy so it lays out correctly at runtime.
    /// Called from <see cref="Awake"/> and <see cref="Initialize"/> to handle both editor-built
    /// and auto-generated hierarchies.
    /// </summary>
    private void EnsureRuntimeLayoutComponents()
    {
        if (pdaMenuPanel == null)
        {
            pdaMenuPanel = gameObject;
        }

        Transform contentTransform = pdaMenuPanel.transform.Find("Content");
        if (contentTransform != null)
        {
            LayoutElement contentLayoutElement = contentTransform.GetComponent<LayoutElement>();
            if (contentLayoutElement == null)
            {
                contentLayoutElement = contentTransform.gameObject.AddComponent<LayoutElement>();
            }
            contentLayoutElement.flexibleHeight = 1f;

            HorizontalLayoutGroup contentLayout = contentTransform.GetComponent<HorizontalLayoutGroup>();
            if (contentLayout == null)
            {
                contentLayout = contentTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;

            Transform detailsTransform = contentTransform.Find("Details");
            if (detailsTransform != null)
            {
                LayoutElement detailsLayoutElement = detailsTransform.GetComponent<LayoutElement>();
                if (detailsLayoutElement == null)
                {
                    detailsLayoutElement = detailsTransform.gameObject.AddComponent<LayoutElement>();
                }
                detailsLayoutElement.flexibleWidth = 1f;
            }
        }

        Transform sidebarTransform = pdaMenuPanel.transform.Find("Content/Sidebar");
        if (sidebarTransform != null)
        {
            RectTransform sidebarRect = sidebarTransform as RectTransform;
            if (sidebarRect != null)
            {
                sidebarRect.anchorMin = Vector2.zero;
                sidebarRect.anchorMax = Vector2.one;
                sidebarRect.pivot = new Vector2(0.5f, 0.5f);
                sidebarRect.anchoredPosition = Vector2.zero;
                sidebarRect.sizeDelta = Vector2.zero;
            }

            LayoutElement sidebarLayoutElement = sidebarTransform.GetComponent<LayoutElement>();
            if (sidebarLayoutElement == null)
            {
                sidebarLayoutElement = sidebarTransform.gameObject.AddComponent<LayoutElement>();
            }
            sidebarLayoutElement.minWidth = 180f;
            sidebarLayoutElement.preferredWidth = 220f;
            sidebarLayoutElement.flexibleWidth = 0f;

            VerticalLayoutGroup sidebarLayoutGroup = sidebarTransform.GetComponent<VerticalLayoutGroup>();
            if (sidebarLayoutGroup == null)
            {
                sidebarLayoutGroup = sidebarTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            sidebarLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            sidebarLayoutGroup.spacing = 4f;
            sidebarLayoutGroup.childControlWidth = true;
            sidebarLayoutGroup.childControlHeight = true;
            sidebarLayoutGroup.childForceExpandWidth = true;
            sidebarLayoutGroup.childForceExpandHeight = true;

            ScrollRect scrollRect = sidebarTransform.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = sidebarTransform.gameObject.AddComponent<ScrollRect>();
            }

            Transform viewportTransform = sidebarTransform.Find("Viewport");
            if (viewportTransform != null)
            {
                RectTransform viewportRect = viewportTransform as RectTransform;
                if (viewportRect != null)
                {
                    viewportRect.anchorMin = Vector2.zero;
                    viewportRect.anchorMax = Vector2.one;
                    viewportRect.pivot = new Vector2(0.5f, 0.5f);
                    viewportRect.anchoredPosition = Vector2.zero;
                    viewportRect.sizeDelta = Vector2.zero;
                }

                LayoutElement viewportLayoutElement = viewportTransform.GetComponent<LayoutElement>();
                if (viewportLayoutElement == null)
                {
                    viewportLayoutElement = viewportTransform.gameObject.AddComponent<LayoutElement>();
                }
                viewportLayoutElement.flexibleWidth = 1f;
                viewportLayoutElement.flexibleHeight = 1f;

                Image viewportImage = viewportTransform.GetComponent<Image>();
                if (viewportImage == null)
                {
                    viewportImage = viewportTransform.gameObject.AddComponent<Image>();
                }
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

                Mask viewportMask = viewportTransform.GetComponent<Mask>();
                if (viewportMask != null)
                {
                    viewportMask.enabled = false;
                }

                RectMask2D rectMask = viewportTransform.GetComponent<RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = viewportTransform.gameObject.AddComponent<RectMask2D>();
                }

                Transform sidebarContentTransform = viewportTransform.Find("SidebarContent");
                if (sidebarContentTransform != null)
                {
                    RectTransform sidebarContentRect = sidebarContentTransform as RectTransform;
                    VerticalLayoutGroup sidebarContentLayout = sidebarContentTransform.GetComponent<VerticalLayoutGroup>();
                    if (sidebarContentLayout == null)
                    {
                        sidebarContentLayout = sidebarContentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    sidebarContentLayout.childControlWidth = true;
                    sidebarContentLayout.childControlHeight = true;
                    sidebarContentLayout.childForceExpandWidth = true;
                    sidebarContentLayout.childForceExpandHeight = false;
                    sidebarContentLayout.spacing = 2f;

                    ContentSizeFitter contentSizeFitter = sidebarContentTransform.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter == null)
                    {
                        contentSizeFitter = sidebarContentTransform.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    if (sidebarContentRect != null)
                    {
                        sidebarContentRect.anchorMin = new Vector2(0f, 1f);
                        sidebarContentRect.anchorMax = new Vector2(1f, 1f);
                        sidebarContentRect.pivot = new Vector2(0.5f, 1f);
                        sidebarContentRect.anchoredPosition = Vector2.zero;
                        sidebarContentRect.sizeDelta = Vector2.zero;
                    }

                    scrollRect.viewport = viewportRect;
                    scrollRect.content = sidebarContentRect;
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                }
            }
        }

        Transform headerTitleTransform = pdaMenuPanel.transform.Find("Header/Title");
        if (headerTitleTransform != null)
        {
            TextMeshProUGUI headerTitle = headerTitleTransform.GetComponent<TextMeshProUGUI>();
            if (headerTitle != null)
            {
                headerTitle.alignment = TextAlignmentOptions.Center;
            }
        }

        Transform detailTitleTransform = pdaMenuPanel.transform.Find("Content/Details/DetailTitle");
        if (detailTitleTransform != null)
        {
            TextMeshProUGUI detailTitleText = detailTitleTransform.GetComponent<TextMeshProUGUI>();
            if (detailTitleText != null)
            {
                detailTitleText.alignment = TextAlignmentOptions.Left;
            }
        }

        Transform closeButtonTransform = pdaMenuPanel.transform.Find("Header/CloseButton");
        if (closeButtonTransform != null)
        {
            closeButtonTransform.gameObject.SetActive(false);
        }
    }

    private Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == targetName)
            {
                return child;
            }

            Transform nested = FindDescendantByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>
    /// Destroys all previously generated sidebar children and rebuilds the list from scratch.
    /// Iterates categories in display order, emitting a bold category header followed by one
    /// button per entry. Locked entries are created but made non-interactable and greyed out.
    /// </summary>
    private void BuildSidebar()
    {
        if (sidebarContent == null)
        {
            EnsureLayoutReferences();
        }

        if (sidebarContent == null)
        {
            return;
        }

        sidebarContent.gameObject.SetActive(true);

        RectTransform sidebarRect = sidebarContent;
        if (sidebarRect != null && sidebarRect.rect.width <= 1f)
        {
            RectTransform parentRect = sidebarRect.parent as RectTransform;
            if (parentRect != null)
            {
                sidebarRect.anchorMin = new Vector2(0f, 1f);
                sidebarRect.anchorMax = new Vector2(1f, 1f);
                sidebarRect.pivot = new Vector2(0.5f, 1f);
                sidebarRect.anchoredPosition = Vector2.zero;
                sidebarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentRect.rect.width);
            }
        }

        VerticalLayoutGroup sidebarLayout = sidebarContent.GetComponent<VerticalLayoutGroup>();
        if (sidebarLayout == null)
        {
            sidebarLayout = sidebarContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        sidebarLayout.childControlWidth = true;
        sidebarLayout.childControlHeight = true;
        sidebarLayout.childForceExpandWidth = true;
        sidebarLayout.childForceExpandHeight = false;

        ContentSizeFitter sidebarFitter = sidebarContent.GetComponent<ContentSizeFitter>();
        if (sidebarFitter == null)
        {
            sidebarFitter = sidebarContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        sidebarFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sidebarFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = sidebarContent.childCount - 1; i >= 0; i--)
        {
            Transform child = sidebarContent.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        generatedButtons.Clear();

        IReadOnlyList<PDAEntryCategory> categories = PDAProgressionSystem.Instance.GetCategories();
        foreach (PDAEntryCategory category in categories)
        {
            CreateCategoryHeader(category.categoryName);

            foreach (PDAEntryDefinition entry in category.entries)
            {
                CreateEntryButton(entry);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(sidebarContent);
        Canvas.ForceUpdateCanvases();
    }

    private void CreateCategoryHeader(string categoryName)
    {
        GameObject headerGO = new GameObject($"Category_{categoryName}");
        headerGO.transform.SetParent(sidebarContent, false);

        RectTransform headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.5f);
        headerRect.anchorMax = new Vector2(1f, 0.5f);
        headerRect.sizeDelta = new Vector2(0f, 28f);

        TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
        headerText.text = categoryName.ToUpperInvariant();
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = UITheme.ColorTextAccent;
        if (sidebarFont == null)
        {
            sidebarFont = ResolveSidebarFont();
        }
        if (sidebarFont != null)
        {
            headerText.font = sidebarFont;
        }
        headerText.textWrappingMode = TextWrappingModes.NoWrap;
        headerText.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layoutElement = headerGO.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 28f;
    }

    private void CreateEntryButton(PDAEntryDefinition entry)
    {
        GameObject buttonGO = new GameObject(entry.entryId);
        buttonGO.transform.SetParent(sidebarContent, false);

        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(0f, 42f);

        Image image = buttonGO.AddComponent<Image>();
        image.color = UITheme.ColorButtonNormal;

        Button button = buttonGO.AddComponent<Button>();
        ButtonStyle.ApplySecondaryStyle(button);
        generatedButtons.Add(button);

        TextMeshProUGUI label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(buttonGO.transform, false);
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Left;
        label.color = UITheme.ColorTextPrimary;
        label.text = GetEntryLabel(entry);
        if (sidebarFont == null)
        {
            sidebarFont = ResolveSidebarFont();
        }
        if (sidebarFont != null)
        {
            label.font = sidebarFont;
        }
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);

        bool isUnlocked = PDAProgressionSystem.Instance.IsUnlocked(entry.entryId);
        button.interactable = isUnlocked;

        if (!isUnlocked)
        {
            image.color = UITheme.WithAlpha(UITheme.ColorButtonNormal, 0.55f);
            label.color = UITheme.ColorTextPrimary;
        }
        else if (selectedEntryId == entry.entryId)
        {
            PanelStyle.ApplyAccentBorderPanelStyle(image, UITheme.ColorAccentCyan);
        }

        button.onClick.RemoveAllListeners();
        if (isUnlocked)
        {
            button.onClick.AddListener(() => SelectEntry(entry.entryId));
        }

        LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 42f;
    }

    /// <summary>
    /// Populates the detail panel (title, status, body) for the given entry ID.
    /// Shows unlock hint and greyed body text for locked entries so the player
    /// knows how to unlock them without revealing the content.
    /// </summary>
    private void RefreshEntryDetails(string entryId)
    {
        if (entryTitleText == null || entryStatusText == null || entryBodyText == null)
        {
            return;
        }

        PDAEntryDefinition entry = PDAProgressionSystem.Instance != null ? PDAProgressionSystem.Instance.GetEntry(entryId) : null;
        if (entry == null)
        {
            SetEmptyState("Select an entry", "Choose a PDA entry from the sidebar.");
            return;
        }

        bool unlocked = PDAProgressionSystem.Instance.IsUnlocked(entry.entryId);
        entryTitleText.text = entry.title;
        entryStatusText.text = unlocked ? "Unlocked" : $"Locked: {entry.unlockHint}";
        entryStatusText.color = unlocked ? UITheme.ColorAccentGreen : UITheme.ColorAccentOrange;
        entryBodyText.text = unlocked ? entry.content : "This entry is still locked.";
        entryBodyText.color = unlocked ? UITheme.ColorTextPrimary : UITheme.ColorTextSecondary;
    }

    /// <summary>
    /// Updates the visual state (colour, interactable) of every generated sidebar button
    /// to reflect the current selection and unlock state without rebuilding the whole list.
    /// </summary>
    private void UpdateSidebarSelectionState()
    {
        foreach (Button button in generatedButtons)
        {
            if (button == null)
            {
                continue;
            }

            PDAEntryDefinition entry = PDAProgressionSystem.Instance.GetEntry(button.gameObject.name);
            if (entry == null)
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            bool isUnlocked = PDAProgressionSystem.Instance.IsUnlocked(entry.entryId);
            button.interactable = isUnlocked;

            if (entry.entryId == selectedEntryId && isUnlocked)
            {
                PanelStyle.ApplyAccentBorderPanelStyle(image, UITheme.ColorAccentCyan);
                if (label != null)
                {
                    label.color = UITheme.ColorTextPrimary;
                }
            }
            else if (!isUnlocked)
            {
                image.color = UITheme.WithAlpha(UITheme.ColorButtonNormal, 0.55f);
                if (label != null)
                {
                    label.color = UITheme.ColorTextSecondary;
                }
            }
            else if (image != null)
            {
                image.color = UITheme.ColorButtonNormal;
                if (label != null)
                {
                    label.color = UITheme.ColorTextPrimary;
                }
            }
        }
    }

    private void SetEmptyState(string title, string body)
    {
        if (entryTitleText != null)
        {
            entryTitleText.text = title;
        }

        if (entryStatusText != null)
        {
            entryStatusText.text = string.Empty;
        }

        if (entryBodyText != null)
        {
            entryBodyText.text = body;
        }
    }

    private bool CategoryExists(string categoryName)
    {
        IReadOnlyList<PDAEntryCategory> categories = PDAProgressionSystem.Instance.GetCategories();
        foreach (PDAEntryCategory category in categories)
        {
            if (category.categoryName == categoryName)
            {
                return true;
            }
        }

        return false;
    }

    private bool EntryExists(string entryId)
    {
        return PDAProgressionSystem.Instance != null && PDAProgressionSystem.Instance.GetEntry(entryId) != null;
    }

    private string GetFirstUnlockedEntryIdForCategory(string categoryName)
    {
        if (PDAProgressionSystem.Instance == null)
        {
            return null;
        }

        foreach (PDAEntryCategory category in PDAProgressionSystem.Instance.GetCategories())
        {
            if (category.categoryName != categoryName)
            {
                continue;
            }

            foreach (PDAEntryDefinition entry in category.entries)
            {
                if (PDAProgressionSystem.Instance.IsUnlocked(entry.entryId))
                {
                    return entry.entryId;
                }
            }
        }

        return null;
    }

    private string GetFirstUnlockedEntryId()
    {
        if (PDAProgressionSystem.Instance == null)
        {
            return null;
        }

        foreach (PDAEntryCategory category in PDAProgressionSystem.Instance.GetCategories())
        {
            foreach (PDAEntryDefinition entry in category.entries)
            {
                if (PDAProgressionSystem.Instance.IsUnlocked(entry.entryId))
                {
                    return entry.entryId;
                }
            }
        }

        return null;
    }

    private string GetEntryLabel(PDAEntryDefinition entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (PDAProgressionSystem.Instance.IsUnlocked(entry.entryId))
        {
            return entry.title;
        }

        return $"{entry.title}  [Locked]";
    }
}
