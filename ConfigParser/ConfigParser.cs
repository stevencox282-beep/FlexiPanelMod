using Il2CppServiceStack;
using Il2CppSystem.Data;
using MelonLoader;
using System.Drawing;
using System.Xml;

namespace FlexiPanelMod;

public class ConfigParser()
{
    // Parses the Panel configuration from the XML file and uses it to setup the panels
    public void ParseConfig(ref Dictionary<string, PanelConfig> panelConfigDictionary)
    {
        // Ensure the panel config store is clear
        panelConfigDictionary.Clear();

        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(".\\UserData\\FlexiPanelConfig.xml"); // Load the XML document from the specified file

        // Get elements
        XmlNodeList panelsList = xmlDoc.GetElementsByTagName("Panels");
        XmlNodeList panelList = xmlDoc.GetElementsByTagName("Panel");

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

            MelonLogger.Warning($"ParseConfig() panelConfig.excludeAllBuffs = {panelConfig.excludeAllBuffs}, panelConfig.excludeAllDebuffs = {panelConfig.excludeAllDebuffs}, panelConfig.includeAllBuffs = {panelConfig.includeAllBuffs}, panelConfig.includeAllDebuffs = {panelConfig.includeAllDebuffs}");
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
                rowConfig.displayText = (rowAttributes["Name"] != null)  ? rowAttributes["Name"].Value.ToUpperSafe() : string.Empty;
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
    }
}