using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Auto-creates the PDA menu UI hierarchy if scene references are missing.
/// This is a fallback for when the editor tool hasn't been used.
/// Prefer using Tools > GreenEnergy > Create PDA Menu UI to set up the hierarchy in the editor.
/// </summary>
public class PDAUIAutoSetup : MonoBehaviour
{
    /// <summary>
    /// Entry point called from UIManager.Start(). If all PDA references are already wired via the Inspector,
    /// this just initializes the controller. If any references are missing it builds the full UI hierarchy at runtime.
    /// </summary>
    public static void SetupPDAMenuInUIManager(UIManager uiManager)
    {
        // Only create at runtime if references are missing (fallback only if editor tool wasn't used)
        bool hasAllReferences = uiManager.pdaMenuButton != null && 
                               uiManager.pdaMenuPanel != null && 
                               uiManager.pdaMenuController != null;
        
        if (hasAllReferences)
        {
            // References are already set via editor, just ensure controller is initialized
            if (uiManager.pdaMenuController != null)
            {
                uiManager.pdaMenuController.Initialize();
            }
            return;
        }

        Debug.LogWarning("PDA Menu UI references not set in UIManager. Creating at runtime as fallback. " +
                         "Prefer using Tools > GreenEnergy > Create PDA Menu UI in the editor. " +
                         "You can then edit everything directly in the scene hierarchy and Inspector.");

        // Find or create PDA menu button in the TopMenuBar
        if (uiManager.pdaMenuButton == null)
        {
            Transform topMenuBar = uiManager.menuBarPanel.transform;
            GameObject pdaButtonGO = CreateMenuButton(topMenuBar, "PDAMenuButton", "PDA");
            uiManager.pdaMenuButton = pdaButtonGO.GetComponent<Button>();
        }

        // Find or create PDA menu panel
        if (uiManager.pdaMenuPanel == null)
        {
            Transform canvasTransform = uiManager.menuBarPanel.transform.parent;
            uiManager.pdaMenuPanel = CreatePDAPanel(canvasTransform);
        }

        // Find or create PDA menu controller
        if (uiManager.pdaMenuController == null)
        {
            uiManager.pdaMenuController = uiManager.pdaMenuPanel.AddComponent<PDAMenuController>();
            SetupPDAController(uiManager.pdaMenuController, uiManager.pdaMenuPanel);
        }
    }

    /// <summary>
    /// Creates a minimal UI Button GameObject with a TextMeshPro label, parents it to <paramref name="parent"/>,
    /// and returns the root button GameObject.
    /// </summary>
    private static GameObject CreateMenuButton(Transform parent, string name, string label)
    {
        GameObject buttonGO = new GameObject(name);
        RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
        Image image = buttonGO.AddComponent<Image>();
        Button button = buttonGO.AddComponent<Button>();

        // Set up RectTransform
        rectTransform.SetParent(parent);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(100, 0);
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        // Set up Image
        image.color = new Color(0.14f, 0.32f, 0.5f, 1f);

        // Set up Button
        button.navigation = Navigation.defaultNavigation;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock()
        {
            normalColor = new Color(0.14f, 0.32f, 0.5f, 1f),
            highlightedColor = new Color(0.2f, 0.44f, 0.66f, 1f),
            pressedColor = new Color(0.09f, 0.24f, 0.38f, 1f),
            selectedColor = new Color(0.2f, 0.44f, 0.66f, 1f),
            disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        // Add text label
        GameObject textGO = new GameObject("Label");
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();

        textRT.SetParent(rectTransform);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = Vector2.zero;

        textComponent.text = label;
        textComponent.fontSize = 36;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;

        return buttonGO;
    }

    /// <summary>
    /// Builds the complete PDA panel hierarchy at runtime:
    /// panel → header (title + close button) + content area (sidebar scrollview + details panel).
    /// Returns the root panel GameObject. Call <see cref="SetupPDAController"/> afterward to wire up references.
    /// </summary>
    private static GameObject CreatePDAPanel(Transform canvasParent)
    {
        // Main panel
        GameObject panelGO = new GameObject("PDAMenu_Panel");
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        Image panelImage = panelGO.AddComponent<Image>();
        VerticalLayoutGroup layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        CanvasGroup canvasGroup = panelGO.AddComponent<CanvasGroup>();

        // Set up panel RectTransform
        panelRT.SetParent(canvasParent);
        panelRT.anchorMin = new Vector2(0, 1);
        panelRT.anchorMax = new Vector2(0, 1);
        panelRT.anchoredPosition = new Vector2(16, -80);
        panelRT.sizeDelta = new Vector2(600, 500);
        panelRT.localRotation = Quaternion.identity;
        panelRT.localScale = Vector3.one;

        // Set up Image (dark background)
        panelImage.color = new Color(0.06f, 0.12f, 0.2f, 0.97f);

        // Set up VerticalLayoutGroup
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 8;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;

        // Set up CanvasGroup for animations
        canvasGroup.alpha = 0;

        // Create header with title and close button
        GameObject headerGO = new GameObject("Header");
        RectTransform headerRT = headerGO.AddComponent<RectTransform>();
        LayoutElement headerLayout = headerGO.AddComponent<LayoutElement>();

        headerRT.SetParent(panelRT);
        headerRT.anchorMin = Vector2.zero;
        headerRT.anchorMax = new Vector2(1, 0);
        headerRT.sizeDelta = new Vector2(0, 40);
        headerLayout.preferredHeight = 40;

        // Title
        GameObject titleGO = new GameObject("Title");
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();

        titleRT.SetParent(headerRT);
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = Vector2.zero;

        titleText.text = "PDA";
        titleText.fontSize = 40;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.2f, 0.8f, 1f, 1f); // Cyan

        // Close button
        GameObject closeButtonGO = new GameObject("CloseButton");
        RectTransform closeButtonRT = closeButtonGO.AddComponent<RectTransform>();
        Image closeButtonImage = closeButtonGO.AddComponent<Image>();
        Button closeButton = closeButtonGO.AddComponent<Button>();

        closeButtonRT.SetParent(headerRT);
        closeButtonRT.anchorMin = new Vector2(1, 0.5f);
        closeButtonRT.anchorMax = new Vector2(1, 0.5f);
        closeButtonRT.anchoredPosition = new Vector2(-20, 0);
        closeButtonRT.sizeDelta = new Vector2(30, 30);

        closeButtonImage.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        closeButton.targetGraphic = closeButtonImage;
        closeButtonGO.SetActive(false);

        // X symbol for close button
        GameObject closeXGO = new GameObject("X");
        RectTransform closeXRT = closeXGO.AddComponent<RectTransform>();
        TextMeshProUGUI closeXText = closeXGO.AddComponent<TextMeshProUGUI>();

        closeXRT.SetParent(closeButtonRT);
        closeXRT.anchorMin = Vector2.zero;
        closeXRT.anchorMax = Vector2.one;
        closeXRT.sizeDelta = Vector2.zero;

        closeXText.text = "✕";
        closeXText.fontSize = 32;
        closeXText.alignment = TextAlignmentOptions.Center;
        closeXText.color = Color.white;

        // Create main content area with sidebar + details
        GameObject contentGO = new GameObject("Content");
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        HorizontalLayoutGroup contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
        LayoutElement contentLayoutElement = contentGO.AddComponent<LayoutElement>();

        contentRT.SetParent(panelRT);
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.sizeDelta = Vector2.zero;

        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 10;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = true;
        contentLayoutElement.flexibleHeight = 1f;

        // Sidebar
        GameObject sidebarGO = new GameObject("Sidebar");
        RectTransform sidebarRT = sidebarGO.AddComponent<RectTransform>();
        Image sidebarImage = sidebarGO.AddComponent<Image>();
        ScrollRect scrollRect = sidebarGO.AddComponent<ScrollRect>();
        VerticalLayoutGroup sidebarLayout = sidebarGO.AddComponent<VerticalLayoutGroup>();
        LayoutElement sidebarElement = sidebarGO.AddComponent<LayoutElement>();

        sidebarRT.SetParent(contentRT);
        sidebarRT.anchorMin = Vector2.zero;
        sidebarRT.anchorMax = Vector2.one;
        sidebarRT.sizeDelta = Vector2.zero;

        sidebarImage.color = new Color(0.05f, 0.1f, 0.15f, 0.8f);
        sidebarElement.preferredWidth = 150;

        sidebarLayout.padding = new RectOffset(5, 5, 5, 5);
        sidebarLayout.spacing = 4;
        sidebarLayout.childForceExpandWidth = true;
        sidebarLayout.childForceExpandHeight = false;

        // Sidebar viewport and content
        GameObject sidebarViewport = new GameObject("Viewport");
        RectTransform sidebarViewportRT = sidebarViewport.AddComponent<RectTransform>();
        Image sidebarViewportImage = sidebarViewport.AddComponent<Image>();
        RectMask2D sidebarMask = sidebarViewport.AddComponent<RectMask2D>();

        sidebarViewportRT.SetParent(sidebarRT);
        sidebarViewportRT.anchorMin = Vector2.zero;
        sidebarViewportRT.anchorMax = Vector2.one;
        sidebarViewportRT.sizeDelta = Vector2.zero;
        sidebarViewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        GameObject sidebarContent = new GameObject("SidebarContent");
        RectTransform sidebarContentRT = sidebarContent.AddComponent<RectTransform>();
        VerticalLayoutGroup sidebarContentLayout = sidebarContent.AddComponent<VerticalLayoutGroup>();

        sidebarContentRT.SetParent(sidebarViewportRT);
        sidebarContentRT.anchorMin = new Vector2(0, 1);
        sidebarContentRT.anchorMax = new Vector2(1, 1);
        sidebarContentRT.pivot = new Vector2(0.5f, 1);
        sidebarContentRT.sizeDelta = new Vector2(0, 0);

        sidebarContentLayout.padding = new RectOffset(0, 0, 0, 0);
        sidebarContentLayout.spacing = 2;
        sidebarContentLayout.childForceExpandWidth = true;
        sidebarContentLayout.childForceExpandHeight = false;

        scrollRect.content = sidebarContentRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        // Details panel
        GameObject detailsGO = new GameObject("Details");
        RectTransform detailsRT = detailsGO.AddComponent<RectTransform>();
        Image detailsImage = detailsGO.AddComponent<Image>();
        VerticalLayoutGroup detailsLayout = detailsGO.AddComponent<VerticalLayoutGroup>();

        detailsRT.SetParent(contentRT);
        detailsRT.anchorMin = Vector2.zero;
        detailsRT.anchorMax = Vector2.one;
        detailsRT.sizeDelta = Vector2.zero;

        detailsImage.color = new Color(0.05f, 0.1f, 0.15f, 0.6f);

        detailsLayout.padding = new RectOffset(10, 10, 10, 10);
        detailsLayout.spacing = 10;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = false;

        // Detail title
        GameObject detailTitleGO = new GameObject("DetailTitle");
        RectTransform detailTitleRT = detailTitleGO.AddComponent<RectTransform>();
        TextMeshProUGUI detailTitleText = detailTitleGO.AddComponent<TextMeshProUGUI>();
        LayoutElement detailTitleLayout = detailTitleGO.AddComponent<LayoutElement>();

        detailTitleRT.SetParent(detailsRT);
        detailTitleRT.anchorMin = Vector2.zero;
        detailTitleRT.anchorMax = new Vector2(1, 0);
        detailTitleRT.sizeDelta = new Vector2(0, 40);
        detailTitleLayout.preferredHeight = 40;

        detailTitleText.text = "Select an entry";
        detailTitleText.fontSize = 32;
        detailTitleText.alignment = TextAlignmentOptions.TopLeft;
        detailTitleText.color = new Color(0.2f, 0.8f, 1f, 1f); // Cyan

        // Detail status
        GameObject detailStatusGO = new GameObject("DetailStatus");
        RectTransform detailStatusRT = detailStatusGO.AddComponent<RectTransform>();
        TextMeshProUGUI detailStatusText = detailStatusGO.AddComponent<TextMeshProUGUI>();
        LayoutElement detailStatusLayout = detailStatusGO.AddComponent<LayoutElement>();

        detailStatusRT.SetParent(detailsRT);
        detailStatusRT.anchorMin = Vector2.zero;
        detailStatusRT.anchorMax = new Vector2(1, 0);
        detailStatusRT.sizeDelta = new Vector2(0, 30);
        detailStatusLayout.preferredHeight = 30;

        detailStatusText.text = "Status: Locked";
        detailStatusText.fontSize = 20;
        detailStatusText.alignment = TextAlignmentOptions.TopLeft;
        detailStatusText.color = new Color(1f, 0.5f, 0.2f, 1f); // Orange for locked

        // Detail body
        GameObject detailBodyGO = new GameObject("DetailBody");
        RectTransform detailBodyRT = detailBodyGO.AddComponent<RectTransform>();
        TextMeshProUGUI detailBodyText = detailBodyGO.AddComponent<TextMeshProUGUI>();
        LayoutElement detailBodyLayout = detailBodyGO.AddComponent<LayoutElement>();

        detailBodyRT.SetParent(detailsRT);
        detailBodyRT.anchorMin = Vector2.zero;
        detailBodyRT.anchorMax = Vector2.one;
        detailBodyRT.sizeDelta = Vector2.zero;

        detailBodyText.text = "No entry selected. Browse the sidebar to view PDA entries.";
        detailBodyText.fontSize = 18;
        detailBodyText.alignment = TextAlignmentOptions.TopLeft;
        detailBodyText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        detailBodyText.wordWrappingRatios = 0.4f;
        detailBodyLayout.preferredHeight = 200;

        // Store references for PDAMenuController
        // Store the close button in the panel's children for later reference
        closeButton.onClick.AddListener(() =>
        {
            UIManager uiManagerInstance = Object.FindAnyObjectByType<UIManager>();
            if (uiManagerInstance != null)
            {
                uiManagerInstance.TogglePDAMenu();
            }
        });

        return panelGO;
    }

    /// <summary>
    /// Wires the <see cref="PDAMenuController"/>'s serialized references by finding named child
    /// transforms in the panel hierarchy, then calls <c>controller.Initialize()</c>.
    /// </summary>
    private static void SetupPDAController(PDAMenuController controller, GameObject panel)
    {
        // Auto-discover the required child UI elements by name
        controller.SetSidebarContent(panel.transform.Find("Content/Sidebar/Viewport/SidebarContent") as RectTransform);
        controller.SetEntryTitleText(panel.transform.Find("Content/Details/DetailTitle").GetComponent<TextMeshProUGUI>());
        controller.SetEntryStatusText(panel.transform.Find("Content/Details/DetailStatus").GetComponent<TextMeshProUGUI>());
        controller.SetEntryBodyText(panel.transform.Find("Content/Details/DetailBody").GetComponent<TextMeshProUGUI>());
        controller.SetCloseButton(panel.transform.Find("Header/CloseButton").GetComponent<Button>());

        // Initialize the controller
        controller.Initialize();
    }
}
