using Il2Cpp;
using Il2CppPantheonPersist;
using Il2CppServiceStack;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlexiBuffDisplayPannel.FlexiPanel
{
    // Debuff Panel 
    public class FlexiPanel : MonoBehaviour
    {
        // basis for the nNames of the transforms we are going to create
        private static string baseTargetName = "FBDP_TargetName_FBDP_";
        private static string baseTextName = "FBDP_TextName_FBDP_";
        private static string baseTimeTextName = "FBDP_TimeTextName_FBDP_";
        private static string baseImageName = "FBDP_ImageName_FBDP_";
        // Returns a Color for the progress bar based on the debuff spellType for that row
        private static string basePanelName = "FBDP_DebuffPanel_FBDP_";

        // Setup lists that will hold all our transforms
        Dictionary<string, List<Transform>> targetNameTextMeshDictionary = new Dictionary<string, List<Transform>>();
        Dictionary<string, List<Transform>> textMeshDictionary = new Dictionary<string, List<Transform>>();
        Dictionary<string, List<Transform>> timeTextMeshDictionary = new Dictionary<string, List<Transform>>();
        Dictionary<string, List<Transform>> imageDictionary = new Dictionary<string, List<Transform>>();
        UITutorialPopup gTutorialPopup = new UITutorialPopup();
        
        // Holds the debuff window
        private static List<UIWindowPanel> uiWindowPanelList  = new List<UIWindowPanel>();
        private static Dictionary<string, PanelConfig> gPanelConfigDictionary = new Dictionary<string, PanelConfig>();
        private static string pullMessage = "";
        private static string popMessage = "";

        // Tidy up the alloated resources when we logout / rechange the number of rows in the panel
        public void ClearPanelLists()
        {
            // Static variables can persist and not be garbage collected on zoning, logout or panel reloading so explicitly clear them out, we will rebuild them on loading into a zone
            targetNameTextMeshDictionary.Clear();
            textMeshDictionary.Clear();
            timeTextMeshDictionary.Clear();
            imageDictionary.Clear();
        }

        // Preserves the UIToutorial transform which is needed to re-make the close button on the window
        public void PreserveRequiredTransforms()
        {
            gTutorialPopup = UIPanelRoots.Instance.Mid.transform.GetComponentInChildren<UITutorialPopup>();
        }

        // Called by the closing of the offensive target window
        public void HideDebuffPanel()
        {
            foreach (var uiWindowPanel in uiWindowPanelList)
            {
                uiWindowPanel.Hide();
            }
        }

        // Called by the /debuff command and on offensive target select
        public void ShowDebuffPanels()
        {
            // Display the panel if the gloabl is set to allow it
            if (Globals.ShowDebuffPanel == true)
            {
                foreach (var uiWindowPanel in uiWindowPanelList)
                {
                    uiWindowPanel.Show();
                }
            }
        }

        public void SetPanelConfig(ref Dictionary<string, PanelConfig> panelConfigDictionary)
        {
            gPanelConfigDictionary = panelConfigDictionary;
        }

        // Displays panels
        public void DisplayPanels()
        {
            // This allows us to remake the panel with any number of rows we want without having left over transforms corrupting the display
            for (int i = uiWindowPanelList.Count - 1; i >= 0; i--)
            {
                if (uiWindowPanelList[i].gameObject != null && uiWindowPanelList[i] != null)
                {
                    // PROBLEM?  This removes UITotorialPopup from Mid?  If any other mod is using Mid and UITotorialPopup will this ruin their party?
                    Destroy(uiWindowPanelList[i].gameObject);
                    Destroy(uiWindowPanelList[i]);
                }
            }
            // Destroy the panels
            uiWindowPanelList.Clear();

            // Create the panels
            CreatePanels();
        }

        // Creates the panels to display
        private void CreatePanels()
        {
            foreach (KeyValuePair<string, PanelConfig> item in gPanelConfigDictionary)
            {
                PanelConfig panelConfig = item.Value;

                // Setup the general panel parameters
                GameObject gameObject = new GameObject($"{basePanelName}{panelConfig.panelID}");
                // Add the panel to the Mid, this ensures we get rendered
                gameObject.transform.SetParent(UIPanelRoots.Instance.Mid.transform);
                gameObject.layer = Layers.UI;

                // Add the necessary component for a panel
                CanvasRenderer canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
                CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
                UIDraggable uiDraggable = gameObject.AddComponent<UIDraggable>();
                RectTransform rectTransform = gameObject.AddComponent<RectTransform>();

                UIWindowPanel uiWindowPanel = new UIWindowPanel();
                uiWindowPanel = gameObject.AddComponent<UIWindowPanel>();

                // Block Raycasts to work around wonky click detection on the close button due other UI elements overlapping the close button image
                // I am not going to spend time making all my TextMesh's layout perfectly for this mod so block raycasts instead
                canvasGroup.blocksRaycasts = true;

                // Setup the Window Panel
                uiDraggable._windowPanel = uiWindowPanel;
                uiWindowPanel.CanvasGroup = canvasGroup;
                uiWindowPanel._displayName = item.Key;

                // Add the MANDATORY elements to a panel, the compilor will not error if you don't do this but nothing will work
                BuildCloseButtonAndBackground(rectTransform, gameObject, uiWindowPanel);

                // Set the panel size based on the number of rows we have to draw
                SetPanelSize(ref uiWindowPanel);

                // Add in the row data
                AddPanelRows(ref uiWindowPanel, panelConfig.panelID);

                // Add the new panel to the list of all panels
                uiWindowPanelList.Add(uiWindowPanel);
            }
            ShowDebuffPanels();
        }

        // Sets the size of the panel based on the number of rows to add
        public void SetPanelSize(ref UIWindowPanel uiWindowPanel)
        {
            // Get the RectTransform to add the rows too
            GameObject gameObject = uiWindowPanel.gameObject;
            RectTransform rectTransform = gameObject.transform.GetComponent<RectTransform>();

            // The space we need per row 
            int heightPerRow = Globals.NameMeshHeight;
            int totalHeightNeeded = (heightPerRow + Globals.PixelsToAdd) * Globals.NumDisplayableDebuffs;
            // We can not change the width, just the height
            Vector2 panelSize = new Vector2(Globals.DefaultPanelWidth, totalHeightNeeded);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(-(panelSize.x / 2), panelSize.y / 2);
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = panelSize;
        }

        // Adds all the images and text meshes to the panel
        public void AddPanelRows(ref UIWindowPanel uiWindowPanel, string panelID)
        {
            // Get the RectTransform to add the rows too
            GameObject gameObject = uiWindowPanel.gameObject;
            RectTransform rectTransform = gameObject.transform.GetComponent<RectTransform>();

            // Add in the images that will be the progress bars
            BuildImages(rectTransform, panelID);

            // Add in Text Meshs that display the data
            BuildTextMeshs(rectTransform, panelID);
        }

        // Constructs the close button and set the background
        private void BuildCloseButtonAndBackground(Transform parentPanel, GameObject gameObject, UIWindowPanel uiWindowPanel)
        {
            // Source for copying button and backgrounds            
            Transform tutorialButton = gTutorialPopup.transform.GetChild(0);

            // Initialise the background for the new panel (MANDATORY)
            Image imageToCopy = gTutorialPopup.GetComponent<Image>();
            var image = gameObject.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.sprite = imageToCopy.sprite;

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
        private TextMeshProUGUI BuildTextMeshComponent(GameObject gameObject)
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
        private void BuildTextMesh(RectTransform rectTransform, string name, int height, int width, float heightOffset, float widthOffset)
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
        private void BuildImage(RectTransform rectTransform, string name, int height, int width, float heightOffset, float widthOffset)
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
        private Image BuildImageComponent(GameObject gameObject)
        {
            // Make a solid colour sprite for use in the bar
            Texture2D tex = new Texture2D(1, 1);
            // NEVER set this to black, it stops the progress bar from displaying prperly for reasoons that are not obvious to me.
            tex.SetPixel(0, 0, Color.pink);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

            gameObject.layer = Layers.UI;
            var image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.color = Color.black;
            image.fillAmount = 0.5f; // 1.0f is full 0.0f is empty
            return image;
        }

        // Builds all images (progress bars) to be display in the panel 
        private void BuildImages(RectTransform rectTransform, string panelID) 
        {
            float heightOffset = 0.0f;
            float interBarOffset = 0.0f;
            FlexiPanelUtils.GetOffsetsForPanel(ref heightOffset, ref interBarOffset);

            // Make all the progress bars
            List<Transform> transformList = new List<Transform>();
            for (int i = 0 ; i < Globals.NumDisplayableDebuffs; i++)
            {
                string imageName = $"{baseImageName}{i}_{panelID}";
                BuildImage(rectTransform, imageName, Globals.NameMeshHeight, Globals.NameMeshWidth, heightOffset, Globals.RowLeftMargin);
                transformList.Add(rectTransform.transform.Find(imageName));
                heightOffset = heightOffset - interBarOffset;
            }
            imageDictionary.Add(panelID, transformList);
        }

        // Builds all TextMeshes (debuff/time) to be display in the panel
        private void BuildTextMeshs(RectTransform rectTransform, string panelID)
        {
            // Text Mesh for Target Name
            BuildTextMesh(rectTransform, baseTargetName, Globals.NameMeshHeight, Globals.NameMeshWidth, 1.0f, 0.0f);
            List<Transform> transformList = new List<Transform>();
            transformList.Add(rectTransform.Find(baseTargetName));
            targetNameTextMeshDictionary.Add(panelID, transformList);

            // Build the meshes
            float heightOffset = 0.0f;
            float interBarOffset = 0.0f;
            FlexiPanelUtils.GetOffsetsForPanel(ref heightOffset, ref interBarOffset);

            List<Transform> textMeshTransformList = new List<Transform>();
            List<Transform> timeTextMeshTransformList = new List<Transform>();
            for (int i = 0; i < Globals.NumDisplayableDebuffs; i++)
            {
                string textName = $"{baseTextName}{i}_{panelID}";
                string timeTextName = $"{baseTimeTextName}{i}_{panelID}";
                BuildTextMesh(rectTransform, textName, Globals.NameMeshHeight, Globals.NameMeshWidth, heightOffset, Globals.RowLeftMargin);
                BuildTextMesh(rectTransform, timeTextName, Globals.TimeMeshHeight, Globals.TimeMeshWidth, heightOffset, Globals.TimeLeftMargin);

                textMeshTransformList.Add(rectTransform.Find(textName));
                timeTextMeshTransformList.Add(rectTransform.Find(timeTextName));
                heightOffset = heightOffset - interBarOffset;
            }
            textMeshDictionary.Add(panelID, textMeshTransformList);
            timeTextMeshDictionary.Add(panelID, timeTextMeshTransformList);
        }

        // Clears the text displayed in the Panel
        public void ClearPanels()
        {
            // This is a terrible way to handle change of character but I can't find a better way, there might be a Hook to use but I can't find one
            // Using Player Network Start causes crashes as it fires before the UI is ready to render the panel, UICompass or similar Hooks dont fire on change of character
            if (targetNameTextMeshDictionary.Count > 1)
            {
                // Quick and dirty re-initialisation on a change of zone
                ClearPanelLists();
                DisplayPanels();
            }

            // Try and stop unwanted access to the panel to prevent exceptions
            if (uiWindowPanelList.Count > 0)
            {
                // Clear out the data
                foreach (List<Transform> targetTransformList in targetNameTextMeshDictionary.Values)
                {
                    foreach (Transform targetTransform in targetTransformList)
                    {
                        targetTransform.GetComponent<TextMeshProUGUI>().text = "";
                    }
                }

                foreach (List<Transform> textMeshTransformList in textMeshDictionary.Values)
                {
                    foreach (Transform textMeshTransform in textMeshTransformList)
                    {
                        textMeshTransform.GetComponent<TextMeshProUGUI>().text = "";
                    }
                }

                foreach (List<Transform> timeTextMeshTransformList in timeTextMeshDictionary.Values)
                {
                    foreach (Transform timeTextMeshTransform in timeTextMeshTransformList)
                    {
                        timeTextMeshTransform.GetComponent<TextMeshProUGUI>().text = "";
                    }
                }

                foreach (List<Transform> imageTransformList in imageDictionary.Values)
                {
                    foreach (Transform imageTransform in imageTransformList)
                    {
                        // Now update the progress bar colour and time
                        Image image = imageTransform.transform.GetComponent<Image>();
                        // Set colour to black on reset
                        image.color = Color.black;
                        image.fillAmount = 0.5f;
                    }
                }
            }
        }

        // Update the text displayed in the Debuff Box
        public void UpdatePanels(EntityData enemyEntityData, EntityData partyEntityData)
        {
            // If we have no buffs or debuffs, exit
            if (enemyEntityData.debuffData.Count == 0 && partyEntityData.debuffData.Count == 0)
            {
                return;
            }

            if (uiWindowPanelList.Count > 0)
            {
                // Try and stop unwanted access to the panel to prevent exceptions
                EntityData entityData = MergeEntityData(enemyEntityData, partyEntityData);
                // Get the difference in levels between player and entity
                int levelDelta = entityData.entityLevel - Globals.PlayerLevel;
                string levelDeltaString = (levelDelta < 0) ? $"{levelDelta}" : $"+{levelDelta}";

                // We must now search every panel and find if that panel is tracking this buff/debuff and if it is follow its row rules
                foreach (UIWindowPanel uiWindowPanel in uiWindowPanelList)
                {
                    // Get the panel details for this window
                    string panelID = uiWindowPanel._displayName;
                    PanelConfig panelConfig = gPanelConfigDictionary[panelID];
                    List<Transform> targetTransformList = targetNameTextMeshDictionary[panelID];
                    List<Transform> timeTextMeshTransformList = timeTextMeshDictionary[panelID];
                    List<Transform> textMeshTransformList = textMeshDictionary[panelID];
                    List<Transform> imageTransformList = imageDictionary[panelID];

                    // Update the panel title
                    foreach (Transform targetTransform in targetTransformList)
                    {
                        if (panelConfig.displayTargetInfo.Equals("title"))
                        {
                            targetTransform.GetComponent<TextMeshProUGUI>().text = panelConfig.panelTitle;
                        }
                        else
                        {
                            targetTransform.GetComponent<TextMeshProUGUI>().text = $" <b>Target:</b> {entityData.targetName.ToUpperSafe()}({levelDeltaString}), {entityData.targetClass}, {entityData.targetKind}, {entityData.traits}";
                        }
                    }

                    // Tracks the row in the panel that is the next to use
                    int panelDisplayIndex = 0;
                    // Parse the list of all viable rows then find a mtach in the buffs list on the current target and update the rows for the panel
                    foreach (RowConfig rowConfig in panelConfig.rowConfig)
                    {
                        // Search every buff for the row that contains the buff in the RowConfig
                        for (int i = 0; (i < entityData.debuffData.Count && i < Globals.NumDisplayableDebuffs) ; i++)
                        {
                            DebuffData debuff = entityData.debuffData[i];
                            if (debuff.debuffName.Contains(rowConfig.displayText))
                            {
                                // Found a required buff/debuff, update the panel with this data
                                textMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $" {debuff.debuffName} ({debuff.numStacks}/{debuff.maxStacks}), ({debuff.casterName})";

                                // Format the time remianing to be human redable
                                if (debuff.debuffDurationRemaining < 60)
                                {
                                    if (debuff.categoryType == BuffCategoryType.Beneficial.ToString())
                                    {
                                        timeTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $"{debuff.debuffDurationRemaining}s (Buff)";
                                    }
                                    else
                                    {
                                        // Display the remaining time in seconds
                                        timeTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $"{debuff.debuffDurationRemaining}s ({debuff.consolidatedEncounterUptimePercent.ToString("0")}%)";
                                    }
                                }
                                else
                                {

                                    if (debuff.categoryType == BuffCategoryType.Beneficial.ToString())
                                    {
                                        timeTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $"{Math.Floor((decimal)debuff.debuffDurationRemaining / 60)}m{Math.Floor((decimal)debuff.debuffDurationRemaining) % 60}s (Buff)";
                                    }
                                    else
                                    {
                                        // Display the remaining time in minutes and seconds
                                        timeTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $"{Math.Floor((decimal)debuff.debuffDurationRemaining / 60)}m{Math.Floor((decimal)debuff.debuffDurationRemaining) % 60}s, ({debuff.consolidatedEncounterUptimePercent.ToString("0")}%)";
                                    }
                                }

                                // Now update the progress bar colour and time
                                Image image = imageTransformList[panelDisplayIndex].transform.GetComponent<Image>();
                                // Set colour based on the user defined color or spell type, if the user has given us an invalid colour, default to orange
                                try
                                {
                                    image.color = (Color)typeof(Color).GetProperty(rowConfig.color.ToLowerInvariant()).GetValue(null, null);
                                }
                                catch
                                {
                                    image.color = Color.orange;
                                }
                                
                                // Set the fill amount 1.0f is full, 0.0f is empty
                                image.fillAmount = ((1 / debuff.debuffDuration) * debuff.debuffDurationRemaining);
                                // Move to the next row in the panel
                                panelDisplayIndex++;
                            }
                        }
                    }
                }
            }
        }

        // This function takes the current enemies debuffs anad the party buffs and merges them into a single EntityData to make the update of the display panels simpler
        public EntityData MergeEntityData(EntityData enemyEntityData, EntityData partyEntityData)
        {
            EntityData finalEntityData = new EntityData();

            // This MUST be a copy by value, otherwise a reference to enemyEntityData.dbeuffData is created and enemyEntityData.debuffData constaly doubles in size each call to OnUpdate()
            CopyByValue(partyEntityData, ref finalEntityData);
            CopyByValue(enemyEntityData, ref finalEntityData);

            return finalEntityData;
        }

        private void CopyByValue(EntityData source, ref EntityData destination)
        {
            destination.traits = source.traits;
            destination.isDead = source.isDead;
            destination.encounterStartTime = source.encounterStartTime;
            destination.entityLevel = source.entityLevel;
            destination.entityNetworkId = source.entityNetworkId;
            destination.totalEncounterTime = source.totalEncounterTime;
            destination.targetClass = source.targetClass;
            destination.targetKind = source.targetKind;
            destination.targetName = (source.targetName.IsEmpty()) ? Globals.Party : source.targetName;

            // Now copy by value to avoid the nasty duplication caused if you copy using a reference
            foreach (var buff in source.debuffData)
            {
                DebuffData newDebuffdata = new DebuffData();
                newDebuffdata.debuffDuration = buff.debuffDuration;
                newDebuffdata.debuffName = buff.debuffName;
                newDebuffdata.debuffDurationRemaining = buff.debuffDurationRemaining;
                newDebuffdata.targetName = buff.targetName;
                newDebuffdata.casterName = buff.casterName;
                newDebuffdata.casterNetworkId = buff.casterNetworkId;
                newDebuffdata.targetNetworkId = buff.targetNetworkId;
                newDebuffdata.consolidatedEncounterUptime = buff.consolidatedEncounterUptime;
                newDebuffdata.consolidatedEncounterUptimePercent = buff.consolidatedEncounterUptimePercent;
                newDebuffdata.maxStacks = buff.maxStacks;
                newDebuffdata.numStacks = buff.numStacks;
                newDebuffdata.spellType = buff.spellType;
                newDebuffdata.targetClass = buff.targetClass;
                newDebuffdata.targetKind = buff.targetKind;
                newDebuffdata.categoryType = buff.categoryType;
                destination.debuffData.Add(newDebuffdata);
            }
        }
        public void ShowPullMessage(EntityClientMessaging.Logic __instance)
        {
            __instance.SendChatMessage(pullMessage, ChatChannelType.Group);
        }

        public void ShowPopMessage(EntityClientMessaging.Logic __instance)
        {
            __instance.SendChatMessage(popMessage, ChatChannelType.Group);
        }

    }
}