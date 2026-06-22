using Il2CppServiceStack;
using MelonLoader;
using System.Xml;

namespace FlexiPanelMod;

public class ConfigParser()
{
    // Parses the Panel configuration from the XML file and uses it to setup the panels
    public void ParseConfig(ref Dictionary<string, PanelConfig> panelConfigDictionary, ref List<string> includeAllBuffsBlacklist)
    {
        // Ensure the panel config store and the blacklist is clear 
        panelConfigDictionary.Clear();
        includeAllBuffsBlacklist.Clear();

        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(".\\UserData\\FlexiPanelConfig.xml"); // Load the XML document from the specified file

        // Get elements
        XmlNodeList panelsList = xmlDoc.GetElementsByTagName("Panels");
        XmlNodeList panelList = xmlDoc.GetElementsByTagName("Panel");
        XmlNodeList blacklist = xmlDoc.GetElementsByTagName("IncludeAllBuffsBlackList");

        // Process all Panels
        for (int panelIndex = 0; panelIndex < panelList.Count; panelIndex++)
        {
            PanelConfig panelConfig = new PanelConfig();

            // Current Panel
            XmlNode panel = panelList[panelIndex];
            XmlAttributeCollection panelAttributes = panel.Attributes;
            panelConfig.panelID = panelAttributes["ID"].Value; // This is mandatory if it is not provided exception and force everything to stop
            panelConfig.panelTitle = panelAttributes["Title"].Value; // This is mandatory if it is not provided exception and force everything to stop
            panelConfig.targetOrTitle = panelAttributes["TargetOrTitle"].Value; // This is mandatory if it is not provided exception and force everything to stop
            panelConfig.excludeAllBuffs = (panelAttributes["ExcludeAllBuffs"] != null) ? bool.Parse(panelAttributes["ExcludeAllBuffs"].Value) : false;
            panelConfig.excludeAllDebuffs = (panelAttributes["ExcludeAllDebuffs"] != null) ? bool.Parse(panelAttributes["ExcludeAllDebuffs"].Value) : false;
            panelConfig.includeAllBuffs = (panelAttributes["IncludeAllBuffs"] != null) ? bool.Parse(panelAttributes["IncludeAllBuffs"].Value) : false;
            panelConfig.includeAllDebuffs = (panelAttributes["IncludeAllDebuffs"] != null) ? bool.Parse(panelAttributes["IncludeAllDebuffs"].Value) : false;
            panelConfig.rowsToDisplay = (panelAttributes["RowsToDisplay"] != null) ? FlexiPanelUtils.SanitiseNumRows(Int32.Parse(panelAttributes["RowsToDisplay"].Value)) : 10;
            // Configure globals used to draw the panels
            panelConfig.panelWidth = (panelAttributes["PanelWidthPx"] != null) ? (Int32.Parse(panelAttributes["PanelWidthPx"].Value)) : Globals.DefaultPanelWidth;
            panelConfig.rowNameWidth = panelConfig.panelWidth - Globals.PixelsNeededForUptime;
            panelConfig.panelOpacity = (panelAttributes["PanelOpacity"] != null) ? (float.Parse(panelAttributes["PanelOpacity"].Value) / 100) : 1.0f;


            // VERY basic XML validation to prevent obviously contradictory configurations

            // Exclude takes priority over include
            if (panelConfig.excludeAllBuffs == true)
            {
                panelConfig.includeAllBuffs = false;
            }

            // Exclude takes priority over include
            if (panelConfig.excludeAllDebuffs == true)
            {
                panelConfig.includeAllDebuffs = false;
            }

            // Get the Row data for this panel
            XmlNodeList rowsList = panel.ChildNodes;
            XmlNode currentRows = rowsList[0];
            XmlNodeList allRows = currentRows.ChildNodes;

            // Process the rows
            panelConfig.rowConfig = new List<RowConfig>();
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                // Current Row
                XmlNode row = allRows[rowIndex];
                XmlAttributeCollection rowAttributes = row.Attributes;

                // Store in PanelConfig
                RowConfig rowConfig = new RowConfig();
                rowConfig.displayText = (rowAttributes["Name"] != null) ? rowAttributes["Name"].Value.ToUpperSafe() : string.Empty;
                rowConfig.color = (rowAttributes["Color"] != null) ? rowAttributes["Color"].Value : "orange";
                rowConfig.include = (rowAttributes["Include"] != null) ? rowAttributes["Include"].Value : string.Empty;
                panelConfig.rowConfig.Add(rowConfig);
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

        // Process blacklist information if available
        if (blacklist.Count == 1)
        {
            XmlNodeList entryList = blacklist[0].ChildNodes;

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
    }
}