using Il2CppServiceStack;
using MelonLoader;
using System.Xml;

namespace FlexiPanelMod;

public class ConfigParser()
{
    // Parses the Panel configuration from the XML file and uses it to setup the panels
    public void ParseConfig(Dictionary<string, PanelConfig> panelConfigDictionary, List<string> includeAllBuffsBlacklist, List<string> includeAllDebuffsBlacklist)
    {
        // Ensure the panel config store and the blacklist is clear 
        panelConfigDictionary.Clear();
        includeAllBuffsBlacklist.Clear();
        includeAllDebuffsBlacklist.Clear();

        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(".\\UserData\\FlexiPanelConfig.xml"); // Load the XML document from the specified file

        // Get elements
        XmlNodeList panelsList = xmlDoc.GetElementsByTagName("Panels");
        XmlNodeList panelList = xmlDoc.GetElementsByTagName("Panel");
        XmlNodeList allBuffBlacklist = xmlDoc.GetElementsByTagName("IncludeAllBuffsBlackList");
        XmlNodeList allDebuffBlacklist = xmlDoc.GetElementsByTagName("IncludeAllDebuffsBlackList");
        XmlNodeList targetInfoConfigList = xmlDoc.GetElementsByTagName("TargetInfoConfig");

        // Process all Panels
        for (int panelIndex = 0; panelIndex < panelList.Count; panelIndex++)
        {
            PanelConfig panelConfig = new PanelConfig();

            // Current Panel
            XmlNode panel = panelList[panelIndex];
            XmlAttributeCollection panelAttributes = panel.Attributes;
            panelConfig.panelID = $"{panelIndex}";
            panelConfig.panelTitle = (panelAttributes["Title"] != null) ? panelAttributes["Title"].Value : Globals.DefaultPanelTitle;
            panelConfig.targetOrTitle = (panelAttributes["TargetOrTitle"] != null) ? panelAttributes["TargetOrTitle"].Value.ToLowerSafe() : Globals.PanelDisplaysTitle;
            panelConfig.excludeAllBuffs = (panelAttributes["ExcludeAllBuffs"] != null) ? bool.Parse(panelAttributes["ExcludeAllBuffs"].Value) : false;
            panelConfig.excludeAllDebuffs = (panelAttributes["ExcludeAllDebuffs"] != null) ? bool.Parse(panelAttributes["ExcludeAllDebuffs"].Value) : false;
            panelConfig.includeAllBuffs = (panelAttributes["IncludeAllBuffs"] != null) ? bool.Parse(panelAttributes["IncludeAllBuffs"].Value) : false;
            panelConfig.includeAllDebuffs = (panelAttributes["IncludeAllDebuffs"] != null) ? bool.Parse(panelAttributes["IncludeAllDebuffs"].Value) : false;
            panelConfig.rowsToDisplay = (panelAttributes["RowsToDisplay"] != null) ? FlexiPanelUtils.SanitiseNumRows(Int32.Parse(panelAttributes["RowsToDisplay"].Value)) : Globals.DefaultNumRows;
            panelConfig.panelOpacity = (panelAttributes["PanelOpacity"] != null) ? (float.Parse(panelAttributes["PanelOpacity"].Value) / 100) : Globals.DefaultPanelOpacity;
            panelConfig.panelWidth = (panelAttributes["PanelWidthPx"] != null) ? (Int32.Parse(panelAttributes["PanelWidthPx"].Value)) : Globals.DefaultPanelWidthPx;
            panelConfig.rowNameWidth = panelConfig.panelWidth - Globals.UptimeMinimumWidthPx;

            // VERY basic XML validation to prevent obviously contradictory configurations
            RangeCheckXMLParams(panelConfig);

            // Get the Row data for this panel
            XmlNodeList rowsList = panel.ChildNodes;
            XmlNode currentRows = rowsList[0];
            XmlNodeList allRows = currentRows.ChildNodes;

            // Process the rows
            panelConfig.rowConfig = new List<RowConfig>();
            if (allRows.Count.Equals(0))
            {
                RowConfig defaultRow = new RowConfig();
                defaultRow.displayText = Globals.DefaultRowDisplayText;
                defaultRow.color = Globals.DefaultRowColor.ToString();
                defaultRow.include = Globals.IncludeParty;
                panelConfig.rowConfig.Add(defaultRow);
            }
            else
            {
                // IF we have rows
                for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
                {
                    // Current Row
                    XmlNode row = allRows[rowIndex];
                    XmlAttributeCollection rowAttributes = row.Attributes;

                    // Store in PanelConfig
                    RowConfig rowConfig = new RowConfig();
                    rowConfig.displayText = (rowAttributes["Name"] != null) ? rowAttributes["Name"].Value.ToUpperSafe() : string.Empty;
                    rowConfig.color = (rowAttributes["Color"] != null) ? rowAttributes["Color"].Value : Globals.DefaultRowColor.ToString();
                    rowConfig.include = (rowAttributes["Include"] != null) ? rowAttributes["Include"].Value : string.Empty;
                    panelConfig.rowConfig.Add(rowConfig);
                }
            }

            // If we have a new panel, add it
            if (!panelConfig.panelID.IsEmpty())
            {
                // The user may have accidentally added panel with a duplicate id (copy paste)
                try
                {
                    panelConfigDictionary.Add(panelConfig.panelID, panelConfig);
                }
                catch
                {
                    MelonLogger.Error($"Duplicate Panel ID {panelConfig.panelID} Found.  MUST be unique");
                    throw;
                }
            }
        }

        // Process IncludeAllBuff blacklist information if available
        if (allBuffBlacklist.Count.Equals(1))
        {
            XmlNodeList entryList = allBuffBlacklist[0].ChildNodes;

            // Process the blacklist for IncludeAllBuffsBlackList
            for (int entryIndex = 0; entryIndex < entryList.Count; entryIndex++)
            {
                XmlNode entryNode = entryList[entryIndex];
                XmlAttributeCollection entryAttributess = entryNode.Attributes;
                string name = (entryAttributess["Name"] != null) ? entryAttributess["Name"].Value.ToUpperSafe() : string.Empty;
                if (!name.IsEmpty())
                {
                    includeAllBuffsBlacklist.Add(name);
                }
            }
        }

        // Process IncludeAllDebuffs blacklist information if available
        if (allDebuffBlacklist.Count.Equals(1))
        {
            XmlNodeList entryList = allDebuffBlacklist[0].ChildNodes;

            // Process the blacklist for IncludeAllBuffsBlackList
            for (int entryIndex = 0; entryIndex < entryList.Count; entryIndex++)
            {
                XmlNode entryNode = entryList[entryIndex];
                XmlAttributeCollection entryAttributess = entryNode.Attributes;
                string name = (entryAttributess["Name"] != null) ? entryAttributess["Name"].Value.ToUpperSafe() : string.Empty;
                if (!name.IsEmpty())
                {
                    includeAllDebuffsBlacklist.Add(name);
                }
            }
        }
    }

    // Performs basic range checking on the input parameters
    private void RangeCheckXMLParams(PanelConfig panelConfig)
    {
        // The panel width must be at least the with of the time text mesh, in reality is pro
        if (panelConfig.panelWidth < Globals.MinimumRowWidthPx)
        {
            panelConfig.panelWidth = Globals.DefaultPanelWidthPx;
            panelConfig.rowNameWidth = panelConfig.panelWidth - Globals.UptimeMinimumWidthPx;
        }

        // Opacity cant be more than 1.0f or less than 0.0f
        if (panelConfig.panelOpacity > 1.0f)
        {
            panelConfig.panelOpacity = 1.0f;
        }
        else if (panelConfig.panelOpacity < 0.0f)
        {
            panelConfig.panelOpacity = 0.0f;
        }

        // Exclude takes priority over include
        if (panelConfig.excludeAllBuffs.Equals(true))
        {
            panelConfig.includeAllBuffs = false;
        }

        // Exclude takes priority over include
        if (panelConfig.excludeAllDebuffs.Equals(true))
        {
            panelConfig.includeAllDebuffs = false;
        }
    }
}