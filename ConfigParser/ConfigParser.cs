using FlexiBuffDisplayPannel;
using MelonLoader;
using System.Xml;

namespace FlexiBuffDisplayPanel.ConfigParser
{
    public class ConfigParser()
    {

        // Parses the Panel configuration from the XML file and uses it to setup the panels
        public void ParseConfig()
        {
            PanelConfig panelConfig = new PanelConfig();

            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.Load("ExampleConfig.xml"); // Load the XML document from the specified file

            // Get elements
            XmlNodeList panelsList = xmlDoc.GetElementsByTagName("Panels");
            XmlNodeList panelList = xmlDoc.GetElementsByTagName("Panel");

            // Process all Panels
            for (int panelIndex = 0; panelIndex < panelList.Count; panelIndex++)
            {
                // Current Panel
                XmlNode panel = panelList[panelIndex];
                XmlAttributeCollection panelAttributes = panel.Attributes;
                panelConfig.panelID = panelAttributes["ID"].Value;
                panelConfig.panelTitle = panelAttributes["Title"].Value;
                panelConfig.displayTargetInfo = panelAttributes["DisplayTargetInfo"].Value;
                MelonLogger.Warning($"ParseConfig() panelID = {panelConfig.panelID}, panelTitle = {panelConfig.panelTitle}, displayTargetInfo = {panelConfig.displayTargetInfo}");

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
                    rowConfig.position = rowAttributes["Position"].Value;
                    panelConfig.rowConfig.Add(rowConfig);

                    MelonLogger.Warning($"RowConfig() displayText = {rowConfig.displayText}, color = {rowConfig.color}, persistant = {rowConfig.persistant}, showUpTime = {rowConfig.showUpTime}, position = {rowConfig.position}");

                }
            }
        }
    }
}
