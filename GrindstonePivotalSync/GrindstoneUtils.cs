using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using GrindstonePivotalCommon;

namespace GrindstonePivotalSync
{
    public class GrindstoneUtils
    {
        public static void CloseGrindstone(string grindstonePath)
        {
            // Locate Grindstone path
            if (!File.Exists(grindstonePath))
            {
                throw new Exception(String.Concat("Unable to find grindstone (", grindstonePath, ")."));
            }

            // Close Grindstone and wait for exit
            var processes = Process.GetProcessesByName("Grindstone 2");
            if (processes.Length > 1)
            {
                throw new Exception("Multiple instances of Grindstone running. Close each instance.");
            }
            if (processes.Length == 1)
            {
                Process.Start(grindstonePath, "-exit");
                processes[0].WaitForExit();
            }
        }

        public static void RemoveGrindstoneBin()
        {
            // Find gsc2bin file and delete it
            // This is needed to update the config.gsc2 xml file
            var gscPath = String.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Grindstone 2");
            var files = Directory.GetFiles(gscPath, "*.gsc2bin");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public static string GetGrindstonXmlPath()
        {
            return String.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Grindstone 2\\config.gsc2");
        }

        public static XmlDocument GetGrindstoneXml()
        {
            // Find the Grindstone xml document
            var filePath = GetGrindstonXmlPath();
            if (!File.Exists(filePath))
            {
                throw new Exception(String.Concat("Unable to find grindstone xml (", filePath, ")"));
            }

            // Load the Grindstone xml document
            var xmlDoc = new XmlDocument();
            using (var reader = new StreamReader(filePath))
            {
                try
                {
                    xmlDoc.LoadXml(reader.ReadToEnd());
                }
                catch (Exception)
                {
                    throw new Exception("Unable to load the Grindstone xml file.");
                }
            }

            return xmlDoc;
        }

        public static void SaveGrindstonXml(XmlDocument xmlDoc)
        {
            xmlDoc.Save(GetGrindstonXmlPath());
        }

        public static XmlElement CreateProfileElement(XmlDocument xmlDoc, string profileName)
        {
            var element = xmlDoc.CreateElement("profile");
            element.SetAttribute("name", profileName);
            element.SetAttribute("searchTerm", "");
            element.SetAttribute("sortColumn", "Name");
            element.SetAttribute("sortOrder", "Ascending");
            element.SetAttribute("timeSortColumn", "Start");
            element.SetAttribute("timeSortOrder", "Ascending");
            element.SetAttribute("twitterFormat", "Started timing {task} on {date} at {time}");
            var column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Name");
            column.SetAttribute("width", "300");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Time");
            column.SetAttribute("width", "50");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Project");
            column.SetAttribute("width", "100");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Type");
            column.SetAttribute("width", "50");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Owner");
            column.SetAttribute("width", "100");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Url");
            column.SetAttribute("width", "50");
            element.AppendChild(column);
            column = xmlDoc.CreateElement("column");
            column.SetAttribute("name", "Update");
            column.SetAttribute("width", "50");
            element.AppendChild(column);
            var customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "id");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Type");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Url");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Update");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "State");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Status");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Project");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Labels");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Owner");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Filter");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            customField = xmlDoc.CreateElement("customField");
            customField.SetAttribute("formatPattern", "");
            customField.SetAttribute("matchPattern", "");
            customField.SetAttribute("name", "Order");
            customField.SetAttribute("removeMatch", "true");
            customField.SetAttribute("taskForceRemoveMatch", "true");
            element.AppendChild(customField);
            return element;
        }

        public static XmlElement CreateTaskElement(XmlDocument xmlDoc, string projectName, Story story, string batchFilePath)
        {
            var element = xmlDoc.CreateElement("task");
            element.SetAttribute("name", story.name);
            if (story.dateTimeCompleted != DateTime.MinValue)
                element.SetAttribute("complete", story.dateTimeCompleted.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffK"));
            element.SetAttribute("dueAlerted", "False");
            element.SetAttribute("estimateAlerted", "False");
            var customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "id");
            customValue.SetAttribute("value", story.id.ToString());
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Type");
            customValue.SetAttribute("value", story.type);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Url");
            customValue.SetAttribute("value", story.url);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Update");
            customValue.SetAttribute("value", batchFilePath);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "State");
            customValue.SetAttribute("value", story.state);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Status");
            customValue.SetAttribute("value", story.status);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Project");
            customValue.SetAttribute("value", projectName);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Labels");
            customValue.SetAttribute("value", story.labels);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Owner");
            customValue.SetAttribute("value", story.owner);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Filter");
            customValue.SetAttribute("value", story.filter);
            element.AppendChild(customValue);
            customValue = xmlDoc.CreateElement("customValue");
            customValue.SetAttribute("name", "Order");
            customValue.SetAttribute("value", story.order.ToString());
            element.AppendChild(customValue);
            return element;
        }

        public static XmlNodeList GetUnsubmittedTimeNodes(XmlDocument xmlDoc, string profileName)
        {
            return xmlDoc.SelectNodes(String.Format("/config/profile[@name = \"{0}\"]/task/time[not(text()[contains(.,'[submitted]')])]", profileName));
        }
    }
}
