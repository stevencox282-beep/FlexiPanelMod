using Il2Cpp;
using Il2CppPantheonPersist;
using Il2CppServiceStack;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppServiceStack.NetStandardPclExport;

namespace FlexiPanelMod;

// Debuff Panel 
public class FlexiPanel : MonoBehaviour
{
    // Base names of the transforms we are going to create for panels
    private static string basePanelName = "FBDP_DebuffPanel_FBDP_";

    // Setup lists that will hold all our transforms
    Dictionary<string, List<Transform>> targetNameTextMeshDictionary = new Dictionary<string, List<Transform>>();
    Dictionary<string, List<Transform>> nameTextMeshDictionary = new Dictionary<string, List<Transform>>();
    Dictionary<string, List<Transform>> timeTextMeshDictionary = new Dictionary<string, List<Transform>>();
    Dictionary<string, List<Transform>> imageDictionary = new Dictionary<string, List<Transform>>();

    // Holds the windows
    private static List<UIWindowPanel> uiWindowPanelList = new List<UIWindowPanel>();
    // Holds the XML panel configuration
    private static Dictionary<string, PanelConfig> panelConfigDictionary = new Dictionary<string, PanelConfig>();
    // Used to hold the current target information
    private static string targetMessage = "";

    // Tidy up the alloated resources when we logout / change the panel configuration
    public void ClearTransformDictionaries()
    {
        // Static variables persist and are not garbage collected on zoning, logout or panel reloading so explicitly clear them out, we will rebuild them on loading into a zone
        targetNameTextMeshDictionary.Clear();
        nameTextMeshDictionary.Clear();
        timeTextMeshDictionary.Clear();
        imageDictionary.Clear();
    }

    // Hides all configured panels
    public void HideFlexiPanels()
    {
        foreach (var uiWindowPanel in uiWindowPanelList)
        {
            uiWindowPanel.Hide();
        }
    }

    // Shows all configured panels
    public void ShowFlexiPanels()
    {
        // Display the panel if the gloabl is set to allow it
        if (Globals.ShowPanels.Equals(true))
        {
            foreach (var uiWindowPanel in uiWindowPanelList)
            {
                uiWindowPanel.Show();
            }
        }
    }

    // Stores the current panel configuration
    public void SetPanelConfig(Dictionary<string, PanelConfig> panelConfigs)
    {
        panelConfigDictionary = panelConfigs;
    }

    // Tears down the resources allocated for the panels
    private void DestroyWindowPanelList()
    {
        ClearTransformDictionaries();
        // Free the resources for every panel we have already created, then empty the list
        for (int i = 0; i < uiWindowPanelList.Count; i++)
        {
            if (uiWindowPanelList[i])
            {
                if (uiWindowPanelList[i].gameObject != null)
                {
                    // WARNING.  This removes UITotorialPopup from Mid.  Make sure you have a copy somewhere or stuff breaks
                    Destroy(uiWindowPanelList[i].gameObject);
                }
            }
        }

        // Destroy the window list
        uiWindowPanelList.Clear();
    }

    // Init panels
    public void InitialiseFlexiPanels()
    {
        // Destroy the windows and its list
        DestroyWindowPanelList();

        // Create the panels
        CreateFlexiPanels();
    }

    // Creates panels
    private void CreateFlexiPanels()
    {
        foreach (KeyValuePair<string, PanelConfig> panelKVP in panelConfigDictionary)
        {
            PanelConfig panelConfig = panelKVP.Value;

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
            uiWindowPanel._displayName = panelKVP.Key;

            // Add the MANDATORY elements to a panel, the compilor will not error if you don't do this but nothing will work
            FlexiPanelBuilder.BuildCloseButtonAndBackground(rectTransform, gameObject, uiWindowPanel, panelConfig);

            // Set the panel size based on the number of rows we have to draw
            SetPanelSize(ref uiWindowPanel, panelConfig);

            // Add in the row data
            AddRowsToPanel(ref uiWindowPanel, panelConfig);

            // Add the new panel to the list of all panels
            uiWindowPanelList.Add(uiWindowPanel);
        }
        ShowFlexiPanels();
    }

    // Sets the size of the panel based on the panel configuration
    public void SetPanelSize(ref UIWindowPanel uiWindowPanel, PanelConfig panelConfig)
    {
        // Get the RectTransform to add the rows too
        GameObject gameObject = uiWindowPanel.gameObject;
        RectTransform rectTransform = gameObject.transform.GetComponent<RectTransform>();

        // The space we need per row 
        int heightPerRow = Globals.NameMeshHeight;
        int totalHeightNeeded = (heightPerRow + Globals.PixelsToAdd) * panelConfig.rowsToDisplay;
        // We can not change the width, just the height
        Vector2 panelSize = new Vector2(panelConfig.panelWidth, totalHeightNeeded);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(-(panelSize.x / 2), panelSize.y / 2);
        rectTransform.localScale = new Vector3(1, 1, 1);
        rectTransform.sizeDelta = panelSize;
    }

    // Adds all the images and text meshes to the panel
    public void AddRowsToPanel(ref UIWindowPanel uiWindowPanel, PanelConfig panelConfig)
    {
        // Get the RectTransform to add the rows too
        GameObject gameObject = uiWindowPanel.gameObject;
        RectTransform rectTransform = gameObject.transform.GetComponent<RectTransform>();

        // Add in the images that will be the progress bars
        imageDictionary.Add(panelConfig.panelID, FlexiPanelBuilder.BuildImages(rectTransform, panelConfig));

        // Add in Text Meshs that display the data
        targetNameTextMeshDictionary.Add(panelConfig.panelID, FlexiPanelBuilder.BuildTargetTextMeshs(rectTransform, panelConfig));
        nameTextMeshDictionary.Add(panelConfig.panelID, FlexiPanelBuilder.BuildNameTextMeshs(rectTransform, panelConfig));
        timeTextMeshDictionary.Add(panelConfig.panelID, FlexiPanelBuilder.BuildTimeTextMeshs(rectTransform, panelConfig));
    }

    // Clears the text displayed in the Panel
    public void ClearPanelsDisplay()
    {
        // This is a terrible way to handle change of character but I can't find a better way, there might be a Hook to use but I can't find one
        // Using Player Network Start causes crashes as it fires before the UI is ready to render the panel, UICompass or similar Hooks dont fire on change of character

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

            foreach (List<Transform> textMeshTransformList in nameTextMeshDictionary.Values)
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
                    // Set fill amount to zero
                    image.fillAmount = 0.0f;
                }
            }
        }
    }

    // Update the data displayed in the Panels
    public void UpdatePanelsDisplay(EntityData enemyEntityData, EntityData partyEntityData, List<string> includeAllBuffsBlacklist, List<string> includeAllDebuffsBlacklist)
    {
        // Merge the data into a single object to ease its parsing
        EntityData entityData = MergeEntityData(enemyEntityData, partyEntityData);

        // If we have any panels
        if (uiWindowPanelList.Count > 0)
        {
            // Get the difference in levels between player and entity
            int levelDelta = entityData.entityLevel - Globals.PlayerLevel;
            string levelDeltaString = (levelDelta < 0) ? $"{levelDelta}" : $"+{levelDelta}";

            // We must now search every panel and find if that panel is tracking this buff and if it is follow its row rules
            foreach (UIWindowPanel uiWindowPanel in uiWindowPanelList)
            {
                // Get the panel details for this window
                string panelID = uiWindowPanel._displayName;
                PanelConfig panelConfig = panelConfigDictionary[panelID];
                List<Transform> targetTransformList = targetNameTextMeshDictionary[panelID];
                List<Transform> nameTextMeshTransformList = nameTextMeshDictionary[panelID];
                List<Transform> timeTextMeshTransformList = timeTextMeshDictionary[panelID];
                List<Transform> imageTransformList = imageDictionary[panelID];

                // Update the target / panel title
                foreach (Transform targetTransform in targetTransformList)
                {
                    targetTransform.GetComponent<TextMeshProUGUI>().text = GetTargetTransformText(panelConfig, entityData, levelDeltaString);
                }

                // Tracks the row in the panel that is the next to use
                int panelDisplayIndex = 0;
                // Parse the list of all viable rows then find a mtach in the buffs list on the current target and update the rows for the panel
                foreach (RowConfig rowConfig in panelConfig.rowConfig)
                {
                    // Search every buff for RowConfig.displayText, but dont display more rows than we allocated in the panel
                    for (int i = 0; (i < entityData.buffData.Count && panelDisplayIndex < panelConfig.rowsToDisplay); i++)
                    {
                        // Get the next buff in the list of all buff
                        BuffData buff = entityData.buffData[i];
                        string buffNameUpperCase = buff.buffName.ToUpperSafe();

                        // Check the include criteria
                        if (HandleIncludeCriteria(panelConfig, rowConfig, buff))
                        {
                            // If the buff is valid or we have a valid override
                            if (true == IsValidBuff(panelConfig, rowConfig, buff, buffNameUpperCase) || true == HasValidOverride(panelConfig, buff, buffNameUpperCase, includeAllBuffsBlacklist, includeAllDebuffsBlacklist))
                            {
                                bool includeThisBuff = (buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()) && panelConfig.includeAllBuffs.Equals(true)) ? true : false;
                                bool includThisDebuff = (buff.categoryType.Equals(BuffCategoryType.Harmful.ToString()) && panelConfig.includeAllDebuffs.Equals(true)) ? true : false;

                                // Found a required buff/debuff, update the panel with this data
                                nameTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = $" {buff.buffName} ({buff.numStacks}/{buff.maxStacks}), ({buff.targetName})";
                                // Set the time value for the row
                                timeTextMeshTransformList[panelDisplayIndex].GetComponent<TextMeshProUGUI>().text = GetTimeTextMeshsText(buff);

                                // Now update the progress bar color and time
                                Image image = imageTransformList[panelDisplayIndex].GetComponent<Image>();
                                UpdateImageDisplay(rowConfig, buff, image, includeThisBuff, includThisDebuff);
                                // Move to the next row in the panel
                                panelDisplayIndex++;
                            }
                        }
                    }
                }
            }
        }
    }


    // Determines if this buff is valid for further processing
    private static bool IsValidBuff(PanelConfig panelConfig, RowConfig rowConfig, BuffData buff, string buffNameUpperCase)
    {
        // Check the name and the buff type are valid
        if (buffNameUpperCase.Contains(rowConfig.displayText))
        {
            if (panelConfig.excludeAllBuffs.Equals(true) && buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()))
            {
                return false;
            }
            else if (panelConfig.excludeAllDebuffs.Equals(true) && buff.categoryType.Equals(BuffCategoryType.Harmful.ToString()))
            {
                return false;
            }
            return true;
        }

        return false;
    }

    // Returns true if we have a valid overide
    private static bool HasValidOverride(PanelConfig panelConfig, BuffData buff, string buffNameUpperCase, List<string> includeAllBuffsBlacklist, List<string> includeAllDebuffsBlacklist)
    {
        bool excludeThisBuff = (buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()) && panelConfig.excludeAllBuffs.Equals(true)) ? true : false;
        bool excludeThisDebuff = (buff.categoryType.Equals(BuffCategoryType.Harmful.ToString()) && panelConfig.excludeAllDebuffs.Equals(true)) ? true : false;
        bool includeThisBuff = (buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()) && panelConfig.includeAllBuffs.Equals(true)) ? true : false;
        bool includThisDebuff = (buff.categoryType.Equals(BuffCategoryType.Harmful.ToString() ) && panelConfig.includeAllDebuffs.Equals(true)) ? true : false;

        // Process the excludes first, we want to keep the panel as clear as possible
        if (excludeThisBuff.Equals(true))
        {
            return false;
        }
        else if (excludeThisDebuff.Equals(true))
        {
            return false;
        }
        else if (includeThisBuff.Equals(true) && false == BuffInBlacklist(includeAllBuffsBlacklist, buffNameUpperCase))
        {
            return true;
        }
        else if (includThisDebuff.Equals(true) && false == BuffInBlacklist(includeAllDebuffsBlacklist, buffNameUpperCase))
        {
            return true;
        }

        return false;
    }
    // Determines is the provided buff exists in the provided blacklist
    private static bool BuffInBlacklist(List<string> blacklist, string buffNameUpperCase)
    {
        // buff contains the full buff name, blacklist may contain the full buiff name or partial buff name
        foreach (string blacklistName in blacklist)
        {
            if (buffNameUpperCase.Contains(blacklistName))
            {
                return true;
            }
        }
        return false;
    }

    // Returns a boolean indicating if we should process the current buff further based on who the buff/debuff is applied to and any over-rides
    private static bool HandleIncludeCriteria(PanelConfig panelConfig, RowConfig rowConfig, BuffData buff)
    {
        string includeCriteria = rowConfig.include;
        string targetNetworkId = buff.targetNetworkId;
        string casterNetworkId = buff.casterNetworkId;
        string targetName = buff.targetName;
        string casterName = buff.casterName;
        string localPlayerName = Globals.LocalPlayer.Nameplate.nameText.text;

        // If no include is provided deny by default
        if (includeCriteria.IsEmpty())
        {
            return false;
        }
        else if (includeCriteria.ToUpperSafe().Equals("[ME]"))
        {
            // If this is for the local player
            if ((targetNetworkId.Equals(Globals.PlayerNetworkId) || casterNetworkId.Equals(Globals.PlayerNetworkId)))
            {
                return true;
            }
        }
        else if (includeCriteria.ToUpperSafe().Contains(","))  // If this is not a player specifically added to track, we filter it out
        {
            bool inList = false;
            // Split the comma separated list into names
            string[] playerNameArray = includeCriteria.Split(',');
            // for every player in the list of player names provided in the config file
            for (int fi = 0; fi < playerNameArray.Length; fi++)
            {
                // Only display this row if the caster or the target for this buff is in the list, or you are the person who cast this buff (so healers can track their own buffs on specific players)
                if (targetName.Equals(playerNameArray[fi].Trim()))
                {
                    inList = true;
                    break;
                }
            }
            return inList;
        }
        else if (includeCriteria.ToUpperSafe().Contains("[PARTY]"))
        {
            if (targetNetworkId.Equals(Globals.PlayerNetworkId) || casterNetworkId.Equals(Globals.PlayerNetworkId) || Globals.GroupMemberNetworkIds.Contains(targetNetworkId) || Globals.GroupMemberNetworkIds.Contains(casterNetworkId))
            {
                return true;
            }
        }

        // Default to not display
        return false;
    }

    // Updates the display of a row
    private void UpdateImageDisplay(RowConfig rowConfig, BuffData buff, Image image, bool includeThisBuff, bool includThisDebuff)
    {
        if (includeThisBuff.Equals(true) || includThisDebuff.Equals(true))
        {
            image.color = FlexiPanelUtils.getBarColours(buff.spellType);
        }
        else
        {
            // Set color based on the user defined color, if the user has given us an invalid colour, default to orange
            try
            {
                image.color = (Color)typeof(Color).GetProperty(rowConfig.color.ToLowerInvariant()).GetValue(null, null);
            }
            catch
            {
                image.color = Color.orange;
            }
        }

        // Set the fill amount 1.0f is full, 0.0f is empty
        image.fillAmount = ((1 / buff.buffDuration) * buff.buffDurationRemaining);
    }

    // Sets the target information based on the currently selected target
    public void SetTargetInformation(string baseMessage)
    {
        targetMessage = baseMessage;
    }

    // Get the string that will be in the panel title / target text
    private string GetTargetTransformText(PanelConfig panelConfig, EntityData entityData, string levelDeltaString)
    {
        // Display the panel title if the user has selected that, otherwise display a suitable target name
        if (panelConfig.targetOrTitle.Equals("title"))
        {
            return $" {panelConfig.panelTitle}";
        }
        else if (entityData.targetName.Equals(Globals.Party))
        {
            return $"  <b>Target:</b> None";
        }
        else if (entityData.traits.IsEmpty())
        {
            return $"  <b>Target:</b> {entityData.targetName.ToUpperSafe()}({levelDeltaString}), {entityData.targetClass}, {entityData.targetKind}";
        }
        else
        {
            return $"  <b>Target:</b> {entityData.targetName.ToUpperSafe()}({levelDeltaString}), {entityData.targetClass}, {entityData.targetKind}, {entityData.traits}";
        }
    }

    // Gets the string that will be in the time part of the row
    private string GetTimeTextMeshsText(BuffData buff)
    {
        // Format the time remianing to be human redable
        if (buff.buffDurationRemaining < 60)
        {
            // Display the remaining time in seconds
            if (buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()))
            {
                return $"{buff.buffDurationRemaining}s (Buff)";
            }
            else
            {
                return $"{buff.buffDurationRemaining}s ({buff.consolidatedEncounterUptimePercent.ToString("0")}%)";
            }
        }
        else
        {
            // Display the remaining time in minutes and seconds
            if (buff.categoryType.Equals(BuffCategoryType.Beneficial.ToString()))
            {
                return $"{Math.Floor((decimal)buff.buffDurationRemaining / 60)}m{Math.Floor((decimal)buff.buffDurationRemaining) % 60}s (Buff)";
            }
            else
            {
                return $"{Math.Floor((decimal)buff.buffDurationRemaining / 60)}m{Math.Floor((decimal)buff.buffDurationRemaining) % 60}s, ({buff.consolidatedEncounterUptimePercent.ToString("0")}%)";
            }
        }
    }

    // Ensure we dont loose the template we need to remake our panels
    public void PreserveRequiredTransforms()
    {
        FlexiPanelBuilder.PreserveRequiredTransforms();
    }

    // This function takes the current enemies and party buffs and merges them into a single EntityData to make the update of the display panels simpler
    public EntityData MergeEntityData(EntityData enemyEntityData, EntityData partyEntityData)
    {
        EntityData finalEntityData = new EntityData();

        // This MUST be a copy by value, otherwise a reference to enemyEntityData.dbeuffData is created and enemyEntityData.buffData constaly doubles in size each call to OnUpdate()
        CopyByValue(partyEntityData, ref finalEntityData);
        CopyByValue(enemyEntityData, ref finalEntityData);
        return finalEntityData;
    }

    // A basic copy by value function
    private void CopyByValue(EntityData source, ref EntityData destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

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
        foreach (var buff in source.buffData)
        {
            BuffData newDebuffdata = new BuffData();
            newDebuffdata.buffDuration = buff.buffDuration;
            newDebuffdata.buffName = buff.buffName;
            newDebuffdata.buffDurationRemaining = buff.buffDurationRemaining;
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
            destination.buffData.Add(newDebuffdata);
        }
    }

    // Add the target information to the Group chat
    public void ShowTargetMessage(EntityClientMessaging.Logic __instance, string message)
    {
        string[] split = message.Split();
        // If we have an argument
        if (split.Length > 1)
        {
            // Preprend the text to the target information
            targetMessage = $"{split[1]} {targetMessage}";
        }
        
        __instance.SendChatMessage(targetMessage, ChatChannelType.Group);
    }
}