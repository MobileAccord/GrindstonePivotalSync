using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using GrindstonePivotalCommon;

namespace GrindstonePivotalSync
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup the base directory location
            const string configFileName = "config.json";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", string.Empty);

            // Attempt to load the config file
            Config config = new Config();
            try
            {
                // Load the config
                config = PivotalUtils.GetConfig(baseDir + configFileName);

                // Get the password if it is not set
                if (string.IsNullOrEmpty(config.Password))
                {
                    string pass = "";
                    Console.Write("Enter your PivotalTracker password: ");
                    ConsoleKeyInfo key;

                    do
                    {
                        key = Console.ReadKey(true);

                        // Backspace Should Not Work
                        if (key.Key != ConsoleKey.Backspace)
                        {
                            if (key.Key != ConsoleKey.Enter)
                            {
                                pass += key.KeyChar;
                                Console.Write("*");
                            }
                        }
                        else
                        {
                            if (pass.Length > 0)
                            {
                                pass = pass.Substring(0, (pass.Length - 1));
                                Console.Write("\b \b");
                            }
                        }
                    }
                    // Stops Receving Keys Once Enter is Pressed
                    while (key.Key != ConsoleKey.Enter);

                    Console.WriteLine();
                    config.Password = pass;
                }
                if (string.IsNullOrEmpty(config.Password))
                {
                    ShowErrorAndWait("The PivotalTracker password cannot be empty.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }
            Console.WriteLine("Configs loaded.");

            // Parse input parameters
            var submitTime = true;
            foreach (var arg in args.Select(a => a.ToLower()))
            {
                if (arg == "submittime=false") submitTime = false;
                if (arg == "autoclose=true") config.AutoClose = true;
            }

            // Close Grindstone
            try
            {
                var processes = Process.GetProcessesByName("Grindstone 2");
                if (processes.Length > 0)
                {
                    if (!config.AutoClose)
                    {
                        Console.WriteLine("Press any key to close grindstone and continue the sync...");
                        Console.CursorVisible = false;
                        Console.ReadKey(true);
                        Console.CursorVisible = true;
                    }
                    GrindstoneUtils.CloseGrindstone(config.GrindstonePath);
                }
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }

            // This is needed to update the Grindstone xml file
            try
            {
                GrindstoneUtils.RemoveGrindstoneBin();
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }

            // Get the Grindstone xml file
            XmlDocument xmlDoc;
            try
            {
                xmlDoc = GrindstoneUtils.GetGrindstoneXml();
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }
            Console.WriteLine("Grindstone xml file loaded.");

            var totalTime = new TimeSpan();
            var timeNodes = GrindstoneUtils.GetUnsubmittedTimeNodes(xmlDoc, config.ProfileName);
            if (timeNodes == null)
            {
                ShowErrorAndWait("Unable to get time nodes from grindstone xml.");
                return;
            }
            if (timeNodes.Count > 0)
            {
                foreach (XmlElement timeNode in timeNodes)
                {
                    var startTime = Convert.ToDateTime(timeNode.Attributes["start"].InnerText);
                    var endTime = Convert.ToDateTime(timeNode.Attributes["end"].InnerText);
                    totalTime = totalTime.Add(endTime - startTime);
                }
            }
            if (submitTime)
            {
                Console.WriteLine(String.Concat(totalTime.Hours, " hours and ", totalTime.Minutes, " minutes of unsubmitted time found (unrounded)."));
                if (!config.AutoSubmit)
                {
                    Console.WriteLine("Hit enter to continue, esc to exit, or spacebar to open grindstone.");
                    var foregroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = Console.BackgroundColor;
                    Console.CursorVisible = false;
                    while (true)
                    {
                        ConsoleKeyInfo keyPressed = Console.ReadKey();
                        if (keyPressed.Key == ConsoleKey.Escape)
                        {
                            Console.ForegroundColor = foregroundColor;
                            Console.CursorVisible = true;
                            return;
                        }
                        if (keyPressed.Key == ConsoleKey.Spacebar)
                        {
                            Process.Start(config.GrindstonePath);
                            Console.ForegroundColor = foregroundColor;
                            Console.CursorVisible = true;
                            return;
                        }
                        if (keyPressed.Key == ConsoleKey.Enter)
                        {
                            Console.ForegroundColor = foregroundColor;
                            Console.CursorVisible = true;
                            break;
                        }
                    }
                }
            }

            // Get Pivotal Tracker token for user
            Console.WriteLine("Getting Pivotal Tracker user token.");
            string token;
            try
            {
                token = PivotalUtils.GetPivotalTrackerUserToken(config.Email, config.Password);
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }

            // Get all projects and stories
            Console.WriteLine("Getting Pivotal Tracker projects and stories.");
            List<Project> projects;
            try
            {
                projects = PivotalUtils.GetAllProjects(token);
            }
            catch (Exception ex)
            {
                ShowErrorAndWait(ex.Message);
                return;
            }
            if (projects.Count == 0)
            {
                ShowErrorAndWait("Unable to retrieve projects from Pivotal Tracker.");
                return;
            }
            var storyCount = projects.Sum(project => project.stories.Count);
            Console.WriteLine(String.Format("{0} projects and {1} stories found in Pivotal Tracker.", projects.Count, storyCount));

            // Get the GrindstonePivotalLink batch files
            var gplBatchFolder = string.Concat(Application.UserAppDataPath, "\\batch_files");
            if (!Directory.Exists(gplBatchFolder))
            {
                Directory.CreateDirectory(gplBatchFolder);
            }
            var gplBatchFiles = Directory.GetFiles(gplBatchFolder);
            
            // Iterate through projects
            Console.WriteLine("Processing stories...");
            var storiesProcessed = 0;
            var projectIds = new Dictionary<string, string>();
            foreach (var project in projects)
            {
                // Add project id to dictionary
                projectIds.Add(project.name, project.id.ToString());

                // Find the profile node
                var profileNode = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", config.ProfileName));

                // Add the profile node if it does not exist
                if (profileNode == null)
                {
                    var element = GrindstoneUtils.CreateProfileElement(xmlDoc, config.ProfileName);
                    var configNode = xmlDoc.SelectSingleNode("/config");
                    if (configNode != null) configNode.AppendChild(element);
                    profileNode = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", config.ProfileName));
                }

                // Make sure the profile node is not null, this should never happen
                if (profileNode == null) continue;

                // Iterate through the stories of the project
                foreach (var story in project.stories)
                {
                    storiesProcessed++;

                    // Skip any stories that are still in the icebox
                    if (story.state == "unscheduled")
                    {
                        Console.Write(String.Format("\r{0} / {1} stories processed.", storiesProcessed, storyCount));
                        continue;
                    }

                    // Build the GrindstonPivotalLinkBatch file if it does not exist. Get file path.
                    var gplBatchFile = string.Concat(gplBatchFolder, string.Format("\\gpl_{0}_{1}.bat", project.id, story.id));
                    if (!gplBatchFiles.Contains(string.Format("gpl_{0}_{1}.bat", project.id, story.id)))
                    {
                        TextWriter tw = new StreamWriter(gplBatchFile);
                        tw.WriteLine(string.Format("start \"\" \"{0}GrindstonePivotalLink.exe\" ProjectId={1} StoryId={2}", baseDir, project.id, story.id));
                        tw.WriteLine("exit");
                        tw.Close();
                    }

                    // Find the story id node
                    var storyIdNode = xmlDoc.SelectSingleNode(String.Format("/config/profile/task/customValue[@name = \"id\"][@value = \"{0}\"]", story.id));

                    // Add the story node if it does not exist
                    if (storyIdNode == null)
                    {
                        var element = GrindstoneUtils.CreateTaskElement(xmlDoc, project.name, story, gplBatchFile);
                        var textNode = xmlDoc.CreateTextNode((!string.IsNullOrEmpty(config.ShowTasksFor) && config.ShowTasksFor == story.filter) ?
                            String.Concat(element.InnerXml, BuildStoryDescription(token, project.id, story.id, story.description)) :
                            story.description);
                        element.AppendChild(textNode);
                        var profileNodeByName = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", config.ProfileName));
                        if (profileNodeByName != null) profileNodeByName.AppendChild(element);
                        Console.Write(String.Format("\r{0} / {1} stories processed.", storiesProcessed, storyCount));
                        continue;
                    }

                    // Select the story node based on the story id node
                    var storyNode = ((XmlElement)storyIdNode.ParentNode);

                    // Update the name of the story node if it has changed
                    if (storyNode.Attributes["name"] != null && storyNode.Attributes["name"].InnerText != story.name)
                    {
                        storyNode.SetAttribute("name", story.name);
                    }

                    // Update story custom values if they have changed
                    var properties = GetStoryProperties(story);
                    foreach (var property in properties)
                    {
                        var propertyNode = (XmlElement)storyNode.SelectSingleNode(String.Format("customValue[@name = \"{0}\"]", property.Key));
                        if (propertyNode == null) continue; // This should never happen
                        if (propertyNode.Attributes["value"] != null && propertyNode.Attributes["value"].InnerText != property.Value)
                        {
                            propertyNode.SetAttribute("value", property.Value);
                        }
                    }

                    // Update the update node if it does not exist or has changed
                    var updateNode = (XmlElement)storyNode.SelectSingleNode("customValue[@name = \"Update\"]");
                    if (updateNode == null)
                    {
                        var updateElement = xmlDoc.CreateElement("customValue");
                        updateElement.SetAttribute("name", "Update");
                        updateElement.SetAttribute("value", gplBatchFile);
                        storyNode.AppendChild(updateElement);
                    }
                    else if (updateNode.Attributes["value"] != null && updateNode.Attributes["value"].InnerText != gplBatchFile)
                    {
                        updateNode.SetAttribute("value", gplBatchFile);
                    }

                    // Update description and story tasks for current stories
                    if (!string.IsNullOrEmpty(config.ShowTasksFor) && config.ShowTasksFor == story.filter)
                    {
                        var innerText = new StringBuilder();
                        foreach (XmlNode innerNode in storyNode.ChildNodes)
                        {
                            if (innerNode.NodeType != XmlNodeType.Text) innerText.Append(innerNode.OuterXml);
                        }

                        storyNode.InnerXml = String.Concat(innerText, BuildStoryDescription(token, project.id, story.id, story.description));
                    }

                    // Add completed attribute to story if it has been completed
                    if (story.dateTimeCompleted != DateTime.MinValue && storyNode.Attributes["complete"] == null)
                    {
                        storyNode.SetAttribute("complete", story.dateTimeCompleted.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffK"));
                    }

                    Console.Write(String.Format("\r{0} / {1} stories processed.", storiesProcessed, storyCount));
                }
            }

            Console.WriteLine();

            // Save the xml document
            GrindstoneUtils.SaveGrindstonXml(xmlDoc);

            if (submitTime)
            {
                // Get pivotal tracker session cookies
                CookieCollection cookies;
                try
                {
                    cookies = PivotalUtils.GetPivotalTrackerSessionCookie(config.Email, config.Password);
                }
                catch (Exception ex)
                {
                    ShowErrorAndWait(ex.Message);
                    return;
                }

                // Get user id for the pivotal tracker email address
                string userId;
                try
                {
                    userId = PivotalUtils.GetUserIdByEmail(ref cookies, config.Email);
                }
                catch (Exception ex)
                {
                    ShowErrorAndWait(ex.Message);
                    return;
                }

                // Submit all unsubmitted time to pivotal tracker
                timeNodes = GrindstoneUtils.GetUnsubmittedTimeNodes(xmlDoc, config.ProfileName);
                if (timeNodes.Count > 0)
                {
                    Console.WriteLine("Submitting time to Pivotal Tracker...");
                    var taskTimes = new List<TaskTime>();
                    foreach (XmlElement timeNode in timeNodes)
                    {
                        var startTime = Convert.ToDateTime(timeNode.Attributes["start"].InnerText);
                        var endTime = Convert.ToDateTime(timeNode.Attributes["end"].InnerText);
                        var taskTimeSpan = endTime - startTime;
                        var taskNode = (XmlElement)(timeNode.ParentNode);

                        var taskTime = taskTimes.FirstOrDefault(tt => tt.storyName == taskNode.Attributes["name"].InnerText
                            && tt.taskNote == timeNode.InnerText
                            && tt.startTime.ToShortDateString() == startTime.ToShortDateString());
                        if (taskTime.storyName == null)
                        {
                            var profileNode = (XmlElement)(taskNode.ParentNode);
                            if (profileNode.Attributes["name"].InnerText != config.ProfileName) continue;
                            var projectNode = taskNode.SelectSingleNode("customValue[@name = \"Project\"]");
                            var labelsNode = taskNode.SelectSingleNode("customValue[@name = \"Labels\"]");
                            taskTime = new TaskTime
                            {
                                projectName = (projectNode != null ? projectNode.Attributes["value"].InnerText : string.Empty),
                                startTime = startTime,
                                startTimes = new List<string>(),
                                timeSpans = new List<TimeSpan>(),
                                storyLabels = (labelsNode != null ? labelsNode.Attributes["value"].InnerText : string.Empty),
                                storyName = taskNode.Attributes["name"].InnerText,
                                taskNote = timeNode.InnerText
                            };
                            taskTimes.Add(taskTime);
                        }
                        else
                        {
                            if (startTime < taskTime.startTime) taskTime.startTime = startTime;
                        }
                        taskTime.startTimes.Add(timeNode.Attributes["start"].InnerText);
                        taskTime.timeSpans.Add(taskTimeSpan);
                    }

                    var tasksProcessed = 0;
                    foreach (var taskTime in taskTimes)
                    {
                        var projectId = projectIds[taskTime.projectName];

                        var totalTaskTime = new TimeSpan();
                        totalTaskTime = taskTime.timeSpans.Aggregate(totalTaskTime, (current, time) => current.Add(time));

                        if (PivotalUtils.SubmitTime(ref cookies, userId, projectId, taskTime.startTime, totalTaskTime, taskTime.projectName, taskTime.storyName, taskTime.taskNote))
                        {
                            var timeNodesByStoryName = xmlDoc.SelectNodes(String.Format("/config/profile[@name = {0}]/task[@name = {1}]/time[not(text()[contains(.,'[submitted]')])]", GetXpathStringForAttributeValue(taskTime.projectName), GetXpathStringForAttributeValue(taskTime.storyName)));
                            if (timeNodesByStoryName == null) continue;
                            foreach (XmlElement timeNode in timeNodesByStoryName)
                            {
                                if (timeNode != null && taskTime.startTimes.Contains(timeNode.Attributes["start"].InnerText) && timeNode.InnerText.IndexOf("[submitted]") < 0)
                                {
                                    timeNode.InnerText += " [submitted]";
                                }
                            }
                        }

                        tasksProcessed++;
                        Console.Write(String.Format("\r{0} / {1} tasks processed.", tasksProcessed, taskTimes.Count));
                    }

                    // Re-save the xml document
                    GrindstoneUtils.SaveGrindstonXml(xmlDoc);
                }
            }

            Console.WriteLine();
            Console.WriteLine("GrindstonePivotalSync complete.");

            // Start Grindstone
            Process.Start(config.GrindstonePath);
        }

        static string BuildStoryDescription(string token, int projectId, int storyId, string storyDescription)
        {
            var tasks = PivotalUtils.GetStoryTasks(token, projectId, storyId);
            var storyTasks = new StringBuilder();
            if (!string.IsNullOrEmpty(storyDescription))
            {
                storyTasks.AppendLine(storyDescription);
                if (tasks.Count > 0) storyTasks.AppendLine(Environment.NewLine);
            }
            if (tasks.Count > 0)
            {
                storyTasks.AppendLine("---------------------------");
                storyTasks.AppendLine("Tasks");
                storyTasks.AppendLine("---------------------------");
                var taskDescriptions = tasks.Select(t => string.Concat((t.completed ? "[Done] " : string.Empty), t.description)).ToArray();
                storyTasks.AppendLine(String.Join(Environment.NewLine, taskDescriptions));
            }

            // Return the projects
            return storyTasks.ToString();
        }

        public struct TaskTime
        {
            public string projectName;
            public DateTime startTime;
            public List<string> startTimes;
            public List<TimeSpan> timeSpans;
            public string storyLabels;
            public string storyName;
            public string taskNote;
        }

        static Dictionary<string, string> GetStoryProperties(Story story)
        {
            var properties = new Dictionary<string, string>
                                 {
                                     {"Type", story.type},
                                     {"Url", story.url},
                                     {"State", story.state},
                                     {"Status", story.status},
                                     {"Owner", story.owner},
                                     {"Labels", story.labels},
                                     {"Filter", story.filter},
                                     {"Order", story.order.ToString()}
                                 };
            return properties;
        }

        static void ShowErrorAndWait(string errorMessage)
        {
            Console.WriteLine(String.Concat("Error: ", errorMessage));
            System.Threading.Thread.Sleep(10000);
        }

        /// <summary>
        /// Returns a valid XPath statement to use for searching attribute values regardless of 's or "s
        /// </summary>
        /// <param name="attributeValue">Attribute value to parse</param>
        /// <returns>Parsed attribute value in concat() if needed</returns>
        static string GetXpathStringForAttributeValue(string attributeValue)
        {
            var hasApos = attributeValue.Contains("'");
            var hasQuote = attributeValue.Contains("\"");

            if (!hasApos)
            {
                return "'" + attributeValue + "'";
            }
            if (!hasQuote)
            {
                return "\"" + attributeValue + "\"";
            }

            var result = new StringBuilder("concat(");
            var currentArgument = new StringBuilder();
            for (var pos = 0; pos < attributeValue.Length; pos++)
            {
                switch (attributeValue[pos])
                {
                    case '\'':
                        result.Append('\"');
                        result.Append(currentArgument.ToString());
                        result.Append("'\",");
                        currentArgument.Length = 0;
                        break;
                    case '\"':
                        result.Append('\'');
                        result.Append(currentArgument.ToString());
                        result.Append("\"\',");
                        currentArgument.Length = 0;
                        break;
                    default:
                        currentArgument.Append(attributeValue[pos]);
                        break;
                }
            }
            if (currentArgument.Length == 0)
            {
                result[result.Length - 1] = ')';
            }
            else
            {
                result.Append("'");
                result.Append(currentArgument.ToString());
                result.Append("')");
            }
            return result.ToString();
        }
    }
}
