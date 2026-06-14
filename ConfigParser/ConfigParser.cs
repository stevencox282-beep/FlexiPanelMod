using FlexiBuffDisplayPannel;
using Il2CppServiceStack;
using MelonLoader;
using System.Xml;

namespace FlexiBuffDisplayPanel.ConfigParser
{
    public class ConfigParser()
    {

        // Parses the Panel configuration from the XML file and uses it to setup the panels
        public void ParseConfig(ref Dictionary<string, PanelConfig> panelConfigDictionary)
        {
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
                panelConfig.displayTargetInfo = panelAttributes["TargetOrTitle"].Value;
//                MelonLogger.Warning($"ParseConfig() panelID = {panelConfig.panelID}, panelTitle = {panelConfig.panelTitle}, displayTargetInfo = {panelConfig.displayTargetInfo}");

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

//                    MelonLogger.Warning($"RowConfig() displayText = {rowConfig.displayText}, color = {rowConfig.color}, persistant = {rowConfig.persistant}, showUpTime = {rowConfig.showUpTime}");

                }
                // If we have a new panel, add it
                if (!panelConfig.panelID.IsEmpty())
                {
//                    MelonLogger.Warning($"ParseConfig() Adding new Panel to Dictionary with panelConfig.panelID = {panelConfig.panelID}");
                    panelConfigDictionary.Add(panelConfig.panelID, panelConfig);
                }
            }


        }
    }
}
