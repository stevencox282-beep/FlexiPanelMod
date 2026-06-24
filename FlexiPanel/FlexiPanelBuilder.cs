using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlexiPanelMod;

// Class that holds builder functions used by the panel
public static class FlexiPanelBuilder
{
    // Holds a template we will need later
    private static UITutorialPopup tutorialPopup = new UITutorialPopup();

    // Base names of the transforms we are going to create
    private static string baseTargetName = "FBDP_TargetName_FBDP_";
    private static string baseTextName = "FBDP_TextName_FBDP_";
    private static string baseTimeTextName = "FBDP_TimeTextName_FBDP_";
    private static string baseImageName = "FBDP_ImageName_FBDP_";

    // Constructs the close button and set the background
    public static void BuildCloseButtonAndBackground(Transform parentPanel, GameObject gameObject, UIWindowPanel uiWindowPanel, PanelConfig panelConfig)
    {
        // Source for copying button and backgrounds            
        Transform tutorialButton = tutorialPopup.transform.GetChild(0);

        // Initialise the background for the new panel (MANDATORY)
        Image imageToCopy = tutorialPopup.GetComponent<Image>();
        var image = gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        image.sprite = imageToCopy.sprite;
        Color newColor = image.color;
        newColor.a = panelConfig.panelOpacity;
        image.color = newColor;

        // Initialise the close button of the panel (MANDATORY)
        var closeButton = GameObject.Instantiate(tutorialButton, tutorialButton.transform.position, tutorialButton.transform.rotation, uiWindowPanel.transform);
        var closeButtonRect = closeButton.GetComponent<RectTransform>();
        closeButtonRect.sizeDelta = new Vector2(30, 30);
        closeButtonRect.anchoredPosition = new Vector2(-13.5f, -12); // Tiny size, top right corner, this ruins the box detection though
        closeButtonRect.pivot = new Vector2(0f, 0f);

        // Initialise on click behaviour of the close button
        var buttonComponent = closeButton.GetComponent<Button>();
        buttonComponent.onClick = new Button.ButtonClickedEvent();
        buttonComponent.onClick.RemoveAllListeners();
        buttonComponent.onClick.AddCall(new InvokableCall(new Action(() =>
        {
            // Actually unloads the panel, not hide
            uiWindowPanel.Hide();
        })));
        // Make clicking sound
        buttonComponent.onClick.AddCall(new InvokableCall(new Action(() =>
        {
            closeButton.GetComponent<UI_Audio_Function>().Play_UI_Generic_Click();
        })));
    }

    // Builder function to create a TextMesh component
    private static TextMeshProUGUI BuildTextMeshComponent(GameObject gameObject)
    {
        // Add and configure the TextMeshPros for rendering the time data
        TextMeshProUGUI textMesh = gameObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Left;
        textMesh.fontSize = Globals.FontSize;
        textMesh.color = Color.white;
        textMesh.text = "";
        textMesh.autoSizeTextContainer = false;
        textMesh.enableAutoSizing = false;

        return textMesh;
    }

    // Builder function to create an TextMesh
    private static void BuildTextMesh(RectTransform rectTransform, string name, int height, int width, float heightOffset, float widthOffset)
    {
        GameObject gameObject = new GameObject(name);
        // Set its parent to the new window panel (which is parented to Mid)
        gameObject.transform.SetParent(rectTransform, false);
        ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        TextMeshProUGUI textMesh = BuildTextMeshComponent(gameObject);
        var rectTransformOne = textMesh.rectTransform;
        rectTransformOne.sizeDelta = new Vector2(width, height);
        rectTransformOne.anchorMin = new Vector2(widthOffset, heightOffset);
        rectTransformOne.anchorMax = new Vector2(widthOffset, heightOffset);
        rectTransformOne.anchoredPosition = new Vector2(0f, 0f);
        rectTransformOne.pivot = new Vector2(0f, 0f);
    }

    // Builder function to create an Image
    private static void BuildImage(RectTransform rectTransform, string name, int height, int width, float heightOffset, float widthOffset)
    {
        GameObject gameObject = new GameObject(name);
        // Set its parent to the new window panel (which is parented to Mid)
        gameObject.transform.SetParent(rectTransform, false);
        ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Image image = BuildImageComponent(gameObject);
        var transform = image.rectTransform;
        transform.sizeDelta = new Vector2(width, height);
        transform.anchorMin = new Vector2(widthOffset, heightOffset);
        transform.anchorMax = new Vector2(widthOffset, heightOffset);
        transform.anchoredPosition = new Vector2(0f, 0f);
        transform.pivot = new Vector2(0f, 0f);
    }

    // Builder function to create an image component
    private static Image BuildImageComponent(GameObject gameObject)
    {
        // Make a solid colour sprite for use in the bar
        Texture2D tex = new Texture2D(1, 1);
        // NEVER set this to black or clear, it stops the progress bar from changing color for reasoons that are not obvious
        tex.SetPixel(0, 0, Color.pink);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        gameObject.layer = Layers.UI;
        var image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.color = Color.black;
        image.fillAmount = 0.0f; // 1.0f is full 0.0f is empty
        return image;
    }

    // Builds all images (progress bars) to be display in the panel 
    public static List<Transform> BuildImages(RectTransform rectTransform, PanelConfig panelConfig)
    {
        float heightOffset = 0.0f;
        float interBarOffset = 0.0f;
        FlexiPanelUtils.GetOffsetsForPanel(ref heightOffset, ref interBarOffset, panelConfig.rowsToDisplay);

        // Make all the progress bars
        List<Transform> transformList = new List<Transform>();
        for (int i = 0; i < panelConfig.rowsToDisplay; i++)
        {
            string imageName = $"{baseImageName}{i}_{panelConfig.panelID}";
            BuildImage(rectTransform, imageName, Globals.NameMeshHeight, panelConfig.rowNameWidth, heightOffset, Globals.RowLeftMargin);
            transformList.Add(rectTransform.transform.Find(imageName));
            heightOffset = heightOffset - interBarOffset;
        }
        return transformList;
    }

    // Builds all Target/Panel Name TextMeshes to be display in the panel
    public static List<Transform> BuildTargetTextMeshs(RectTransform rectTransform, PanelConfig panelConfig)
    {
        // Text Mesh for Target Name
        BuildTextMesh(rectTransform, baseTargetName, Globals.NameMeshHeight, panelConfig.rowNameWidth, 1.0f, 0.0f);
        List<Transform> transformList = new List<Transform>();
        transformList.Add(rectTransform.Find(baseTargetName));
        return transformList;
    }

    // Builds all Name TextMeshes to be display in the panel
    public static List<Transform> BuildNameTextMeshs(RectTransform rectTransform, PanelConfig panelConfig)
    {
        // Build the name meshes
        float heightOffset = 0.0f;
        float interBarOffset = 0.0f;
        FlexiPanelUtils.GetOffsetsForPanel(ref heightOffset, ref interBarOffset, panelConfig.rowsToDisplay);

        List<Transform> textMeshTransformList = new List<Transform>();
        for (int i = 0; i < panelConfig.rowsToDisplay; i++)
        {
            string textName = $"{baseTextName}{i}_{panelConfig.panelID}";
            BuildTextMesh(rectTransform, textName, Globals.NameMeshHeight, panelConfig.rowNameWidth, heightOffset, Globals.RowLeftMargin);
            textMeshTransformList.Add(rectTransform.Find(textName));
            heightOffset = heightOffset - interBarOffset;
        }
        return textMeshTransformList;
    }

    // Builds all Time TextMeshes to be display in the panel
    public static List<Transform> BuildTimeTextMeshs(RectTransform rectTransform, PanelConfig panelConfig)
    {
        // Build the meshes
        float heightOffset = 0.0f;
        float interBarOffset = 0.0f;
        FlexiPanelUtils.GetOffsetsForPanel(ref heightOffset, ref interBarOffset, panelConfig.rowsToDisplay);

        List<Transform> timeTextMeshTransformList = new List<Transform>();
        for (int i = 0; i < panelConfig.rowsToDisplay; i++)
        {
            string timeTextName = $"{baseTimeTextName}{i}_{panelConfig.panelID}";
            BuildTextMesh(rectTransform, timeTextName, Globals.TimeMeshHeight, Globals.TimeMeshWidth, heightOffset, Globals.TimeLeftMargin);
            timeTextMeshTransformList.Add(rectTransform.Find(timeTextName));
            heightOffset = heightOffset - interBarOffset;
        }
        return timeTextMeshTransformList;
    }

    // Preserves the UIToutorial transform which is needed to re-make the close button on the window
    public static void PreserveRequiredTransforms()
    {
        tutorialPopup = UIPanelRoots.Instance.Mid.transform.GetComponentInChildren<UITutorialPopup>();
    }
}