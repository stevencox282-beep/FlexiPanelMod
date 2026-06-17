using FlexiBuffDisplayPannel;
using FlexiBuffDisplayPannel.FlexiPanel;
using Il2CppServiceStack;
using System.Xml;

namespace FlexiBuffDisplayPanel.ConfigParser
{
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
                panelConfig.panelID = panelAttributes["ID"].Value;
                panelConfig.panelTitle = panelAttributes["Title"].Value;
                panelConfig.targetOrTitle = panelAttributes["TargetOrTitle"].Value;
                panelConfig.excludeBuffs = bool.Parse(panelAttributes["ExcludeBuffs"].Value);
                panelConfig.excludeDebuffs = bool.Parse(panelAttributes["ExcludeDebuffs"].Value);
                panelConfig.rowsToDisplay = FlexiPanelUtils.SanitiseNumRows(Int32.Parse(panelAttributes["RowsToDisplay"].Value));

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
                    rowConfig.displayText = rowAttributes["DisplayText"].Value;
                    rowConfig.color = rowAttributes["Color"].Value;
                    rowConfig.persistant = rowAttributes["Persistant"].Value;
                    rowConfig.showUpTime = rowAttributes["ShowUpTime"].Value;
                    panelConfig.rowConfig.Add(rowConfig);
                }
                // If we have a new panel, add it
                if (!panelConfig.panelID.IsEmpty())
                {
                    panelConfigDictionary.Add(panelConfig.panelID, panelConfig);
                }
            }
        }
    }
}
