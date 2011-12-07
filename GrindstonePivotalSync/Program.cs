using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrindstonePivotalSync
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup the constants
            const string configFileName = "config.json";
            const string grindstoneExe = "Grindstone 2.exe";

            // Setup the base directory location
            var baseDir = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", string.Empty);

            // Setup the config objects
            var grindstonePath = string.Empty;
            var pivotalEmail = string.Empty;
            var pivotalPassword = string.Empty;

            // Attempt to load the config file
            try
            {
                // Check that the file exists
                if (!File.Exists(baseDir + configFileName))
                {
                    Console.WriteLine(String.Format("Error: Unable to find the config file {0}.", configFileName));
                    return;
                }

                // Load the config file into the string builder
                var sbConfig = new StringBuilder();
                var srConfig = new StreamReader(baseDir + configFileName);
                while (!srConfig.EndOfStream)
                {
                    sbConfig.Append((srConfig.ReadLine() ?? string.Empty).Trim(' '));
                }
                srConfig.Close();

                // Deserialize the json string
                var jsonObject = (JObject)JsonConvert.DeserializeObject(sbConfig.ToString().Replace("\t", string.Empty));

                // Load the grindstone path
                grindstonePath = jsonObject["GrindstonePath"].ToString().Trim('"').Replace("\\\\", "\\");
                if (grindstonePath == string.Empty)
                {
                    Console.WriteLine("Error: The 'GrindstonePath' in the config file does not appear to be set.");
                    return;
                }

                // Load the pivotal email
                pivotalEmail = jsonObject["PivotalEmail"].ToString().Trim('"');
                if (grindstonePath == string.Empty)
                {
                    Console.WriteLine("Error: The 'PivotalEmail' in the config file does not appear to be set.");
                    return;
                }

                // Load the pivotal password
                pivotalPassword = jsonObject["PivotalPassword"].ToString().Trim('"');
                if (grindstonePath == string.Empty)
                {
                    Console.WriteLine("Error: The 'PivotalPassword' in the config file does not appear to be set.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to parse the config.json file.");
                return;
            }

            // Locate Grindstone path
            grindstonePath = String.Concat(grindstonePath, "\\", grindstoneExe);
            if (!File.Exists(grindstonePath))
            {
                Console.WriteLine(String.Concat("Unable to find grindstone (", grindstonePath, ")."));
                return;
            }

            // Close Grindstone and wait for exit
            var processes = Process.GetProcessesByName("Grindstone 2");
            if (processes.Length > 0)
            {
                Process.Start(grindstonePath, "-exit");
                processes[0].WaitForExit();
            }

            // Find gsc2bin file and delete it
            // This is needed to update the config.gsc2 xml file
            var gscPath = String.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Grindstone 2");
            var files = Directory.GetFiles(gscPath, "*.gsc2bin");
            foreach (var file in files)
            {
                File.Delete(file);
            }

            // Find the Grindstone xml document
            var filePath = String.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Grindstone 2\\config.gsc2");
            if (!File.Exists(grindstonePath))
            {
                Console.WriteLine(String.Concat("Unable to find grindstone xml (", filePath, ")"));
                return;
            }

            // Read the Grindstone xml document
            var xmlDoc = new XmlDocument();
            using (var reader = new StreamReader(filePath))
            {
                try
                {
                    xmlDoc.LoadXml(reader.ReadToEnd());
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to load the Grindstone xml file.");
                    return;
                }
            }

            // Get Pivotal Tracker token for user
            var token = GetPivotalTrackerUserToken(pivotalEmail, pivotalPassword);
            if (token == string.Empty)
            {
                Console.WriteLine("Unable to retrieve Pivotal Tracker token.");
                Console.WriteLine("Please check your username, password and internet connection.");
                return;
            }

            // Get all projects and stories
            var projects = GetAllProjects(token);
            if (projects.Count == 0)
            {
                Console.WriteLine("Unable to retrieve projects from Pivotal Tracker.");
                return;
            }

            // Iterate through projects
            var projectIds = new Dictionary<string, string>();
            foreach (var project in projects)
            {
                // Add project id to dictionary
                projectIds.Add(project.name, project.id.ToString());

                // Find the profile node
                var profileNode = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", project.name));

                // Add the profile node if it does not exist
                if (profileNode == null)
                {
                    var element = xmlDoc.CreateElement("profile");
                    element.SetAttribute("name", project.name);
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
                    column.SetAttribute("name", "Labels");
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
                    customField.SetAttribute("name", "Owner");
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
                    customField.SetAttribute("name", "Filter");
                    customField.SetAttribute("removeMatch", "true");
                    customField.SetAttribute("taskForceRemoveMatch", "true");
                    element.AppendChild(customField);
                    var configNode = xmlDoc.SelectSingleNode("/config");
                    if (configNode != null) configNode.AppendChild(element);
                    profileNode = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", project.name));
                }

                // Make sure the profile node is not null, this should never happen
                if (profileNode == null) continue;

                // Update the name of the profile node if it has changed
                if (profileNode.Attributes["name"] != null && profileNode.Attributes["name"].InnerText != project.name)
                {
                    ((XmlElement)profileNode).SetAttribute("name", project.name);
                }

                // Iterate through the stories of the project
                foreach (var story in project.stories)
                {
                    // Skip any stories that are still in the icebox
                    if (story.state == "unscheduled") continue;

                    // Find the story id node
                    var storyIdNode = xmlDoc.SelectSingleNode(String.Format("/config/profile/task/customValue[@name = \"id\"][@value = \"{0}\"]", story.id));

                    // Add the story node if it does not exist
                    if (storyIdNode == null)
                    {
                        var element = xmlDoc.CreateElement("task");
                        element.SetAttribute("name", story.name);
                        if (story.dateTimeCompleted != DateTime.MinValue)
                            element.SetAttribute("complete", story.dateTimeCompleted.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffK"));
                        element.SetAttribute("dueAlerted", "False");
                        element.SetAttribute("estimateAlerted", "False");
                        element.InnerText = story.description;
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
                        customValue.SetAttribute("name", "State");
                        customValue.SetAttribute("value", story.state);
                        element.AppendChild(customValue);
                        customValue = xmlDoc.CreateElement("customValue");
                        customValue.SetAttribute("name", "Status");
                        customValue.SetAttribute("value", story.status);
                        element.AppendChild(customValue);
                        customValue = xmlDoc.CreateElement("customValue");
                        customValue.SetAttribute("name", "Owner");
                        customValue.SetAttribute("value", story.owner);
                        element.AppendChild(customValue);
                        customValue = xmlDoc.CreateElement("customValue");
                        customValue.SetAttribute("name", "Labels");
                        customValue.SetAttribute("value", story.labels);
                        element.AppendChild(customValue);
                        customValue = xmlDoc.CreateElement("customValue");
                        customValue.SetAttribute("name", "Filter");
                        customValue.SetAttribute("value", story.filter);
                        element.AppendChild(customValue);
                        var configNode = xmlDoc.SelectSingleNode(String.Format("/config/profile[@name = \"{0}\"]", project.name));
                        if (configNode != null) configNode.AppendChild(element);
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

                    // Add completed attribute to story if it has been completed
                    if (story.dateTimeCompleted != DateTime.MinValue && storyNode.Attributes["complete"] == null)
                    {
                        storyNode.SetAttribute("complete", story.dateTimeCompleted.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffK"));
                    }
                }
            }

            // Save the xml document
            xmlDoc.Save(filePath);

            // Get pivotal tracker session cookies
            var cookies = GetPivotalTrackerSessionCookie(pivotalEmail, pivotalPassword);
            if (cookies == null || cookies.Count == 0)
            {
                Console.WriteLine("Unable to retrieve Pivotal Tracker session cookies.");
                return;
            }

            // Get user id for the pivotal tracker email address
            var userId = GetUserId(ref cookies, pivotalEmail);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Unable to retrieve Pivotal Tracker user id.");
                return;
            }

            // Get all unsubmitted time
            var timeNodes = xmlDoc.SelectNodes("/config/profile/task/time[not(text()[contains(.,'[submitted]')])]");
            if (timeNodes == null)
            {
                Console.WriteLine("Unable to get time nodes from grindstone xml.");
                return;
            }

            // Submit all unsubmitted time to pivotal tracker
            if (timeNodes.Count > 0)
            {
                foreach (XmlElement timeNode in timeNodes)
                {
                    var startTime = Convert.ToDateTime(timeNode.Attributes["start"].InnerText);
                    var endTime = Convert.ToDateTime(timeNode.Attributes["end"].InnerText);
                    var taskNode = (XmlElement)(timeNode.ParentNode);
                    var profileNode = (XmlElement)(taskNode.ParentNode);
                    if (!projectIds.ContainsKey(profileNode.Attributes["name"].InnerText)) continue;
                    var labelsNode = taskNode.SelectSingleNode("customValue[@name = \"Labels\"]");
                    var labels = (labelsNode != null ? labelsNode.Attributes["value"].InnerText : string.Empty);
                    if (SubmitTime(ref cookies, userId, projectIds[profileNode.Attributes["name"].InnerText], startTime, endTime, labels, taskNode.Attributes["name"].InnerText, timeNode.InnerText))
                    {
                        timeNode.InnerText += " [submitted]";
                    }
                }

                // Re-save the xml document
                xmlDoc.Save(filePath);
            }

            // Start Grindstone
            Process.Start(grindstonePath);
        }

        static string GetPivotalTrackerUserToken(string username, string password)
        {
            // Setup the return string
            var token = string.Empty;

            // Get the xml doc containing the token using the pivotal tracker api
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create("https://www.pivotaltracker.com/services/v3/tokens/active");
            request.Credentials = new NetworkCredential(username, password);
            try
            {
                using (var response = request.GetResponse())
                {
                    var dataStream = response.GetResponseStream();
                    if (dataStream != null)
                    {
                        using (var reader = new StreamReader(dataStream))
                        {
                            xmlDoc.LoadXml(reader.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception)
            {
                return token;
            }

            // Get the token from the xml
            var tokenNode = xmlDoc.SelectNodes("/token/guid");
            if (tokenNode != null && tokenNode.Count > 0)
            {
                token = tokenNode[0].InnerText;
            }

            // Return the token
            return token;
        }

        static List<Project> GetAllProjects(string token)
        {
            // Setup the return list
            var projects = new List<Project>();

            // Get the xml doc containing the token using the pivotal tracker api
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create("https://www.pivotaltracker.com/services/v3/projects");
            request.Headers.Add("X-TrackerToken", token);
            try
            {
                using (var response = request.GetResponse())
                {
                    var dataStream = response.GetResponseStream();
                    if (dataStream != null)
                    {
                        using (var reader = new StreamReader(dataStream))
                        {
                            xmlDoc.LoadXml(reader.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception)
            {
                return projects;
            }

            // Get the project data from the xml
            var projectNodes = xmlDoc.SelectNodes("//project");
            if (projectNodes == null) return projects;
            foreach (XmlNode node in projectNodes)
            {
                var projectId = Convert.ToInt32(node["id"].InnerText);
                projects.Add(new Project
                {
                    id = projectId,
                    name = node["name"].InnerText,
                    stories = GetProjectStories(token, projectId)
                });
            }

            // Return the projects
            return projects;
        }

        static List<Story> GetProjectStories(string token, int projectId)
        {
            // Setup the return list
            var stories = new List<Story>();

            // Get the xml doc containing the token using the pivotal tracker api
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories", projectId));
            request.Headers.Add("X-TrackerToken", token);
            try
            {
                using (var response = request.GetResponse())
                {
                    var dataStream = response.GetResponseStream();
                    if (dataStream != null)
                    {
                        using (var reader = new StreamReader(dataStream))
                        {
                            xmlDoc.LoadXml(reader.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return stories;
            }

            // Get Time Zones
            var timeZoneModifier = GetTimeZones();

            // Get the story data from the xml
            var storyNodes = xmlDoc.SelectNodes("//story");
            if (storyNodes == null) return stories;
            foreach (XmlNode node in storyNodes)
            {
                var dtCompleted = DateTime.MinValue;
                if (node["accepted_at"] != null && node["accepted_at"].InnerText.Contains(" "))
                {
                    var acceptedAt = node["accepted_at"].InnerText;
                    var timeZone = acceptedAt.Substring(acceptedAt.LastIndexOf(' ')+1);
                    if (timeZoneModifier.ContainsKey(timeZone)) acceptedAt = acceptedAt.Replace(timeZone, timeZoneModifier[timeZone]);
                    DateTime.TryParse(acceptedAt, out dtCompleted);
                }
                var sbFilter = new StringBuilder();
                sbFilter.Append(node["owned_by"] != null ? node["owned_by"].InnerText.Split(new char[] { ' ' })[0] : string.Empty);
                sbFilter.Append("-");
                sbFilter.Append(node["current_state"] != null ? GetStatus(node["current_state"].InnerText) : string.Empty);
                stories.Add(new Story
                {
                    id = Convert.ToInt32(node["id"].InnerText),
                    name = (node["name"] != null ? node["name"].InnerText : string.Empty),
                    type = (node["story_type"] != null ? node["story_type"].InnerText : string.Empty),
                    url = (node["url"] != null ? node["url"].InnerText : string.Empty),
                    state = (node["current_state"] != null ? node["current_state"].InnerText : string.Empty),
                    status = (node["current_state"] != null ? GetStatus(node["current_state"].InnerText) : string.Empty),
                    dateTimeCompleted = dtCompleted,
                    description = (node["description"] != null ? node["description"].InnerText : string.Empty),
                    owner = (node["owned_by"] != null ? node["owned_by"].InnerText : string.Empty),
                    labels = (node["labels"] != null ? node["labels"].InnerText : string.Empty),
                    filter = sbFilter.ToString()
                });
            }

            // Return the stories
            return stories;
        }

        static CookieCollection GetPivotalTrackerSessionCookie(string username, string password)
        {
            // Setup the return cookie collection
            CookieCollection cookies = null;

            // Set up the web request
            var request = (HttpWebRequest)WebRequest.Create("https://www.pivotaltracker.com/signin");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();

            // Build the post data string
            var postData = String.Format("credentials%5Busername%5D={0}&credentials%5Bpassword%5D={1}&time_zone_offset={2}",
                                         Uri.EscapeDataString(username), Uri.EscapeDataString(password), TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours);

            // Encode the post data and write the data to the request stream
            var encoding = new ASCIIEncoding();
            var data = encoding.GetBytes(postData);
            request.ContentLength = data.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            // Get the cookies from the response
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK && response.Cookies != null)
                {
                    cookies = response.Cookies;
                }
            }

            // Return the cookies
            return cookies;
        }

        // Screen scraping to find user id. User id not provided in api
        static string GetUserId(ref CookieCollection cookies, string email)
        {
            // Setup the return string
            var userId = string.Empty;

            // Set up the web request
            var request = (HttpWebRequest)WebRequest.Create("https://www.pivotaltracker.com/time_shifts/new");
            request.Method = "GET";
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies);

            // Get the html from the request response
            var html = string.Empty;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                // Return an empty string for a bad web request
                if (response.StatusCode != HttpStatusCode.OK) return string.Empty;

                // Save new cookie information
                cookies = response.Cookies;

                // Parse the html to get the form secret value
                var responseStream = response.GetResponseStream();
                if (responseStream != null) html = (new StreamReader(responseStream)).ReadToEnd();
            }

            // Isolate the options that contain the user ids
            var startTrim = html.IndexOf("shift_person_id");
            startTrim = html.IndexOf("<option", startTrim);
            var endTrim = html.IndexOf("</select>", startTrim);
            html = html.Substring(startTrim, endTrim - startTrim);

            // Find the option for the provided email address and get the user id
            var options = html.ToLower().Split(Environment.NewLine.ToArray());
            foreach (var option in options)
            {
                if (option.Contains(email))
                {
                    startTrim = option.IndexOf('"') + 1;
                    endTrim = option.IndexOf('"', startTrim);
                    userId = option.Substring(startTrim, endTrim - startTrim);
                    break;
                }
            }

            // Return the user id
            return userId;
        }

        static bool SubmitTime(ref CookieCollection cookies, string userId, string projectId, DateTime startTime, DateTime endTime, string storyLabels, string storyName, string taskNote)
        {
            // Determine the task time and round up to the nearest 15 minute interval
            var timespan = endTime - startTime;
            var totalTime = timespan.Hours + Math.Ceiling((timespan.Minutes % 60) / 15.0) / 4.00;

            // Determine the task description to be submitted to Pivotal Tracker
            var description =
                String.Concat((!String.IsNullOrEmpty(storyLabels) ? String.Concat(storyLabels, ": ") : string.Empty),
                              storyName,
                              (!String.IsNullOrEmpty(taskNote) ? String.Concat(": ", taskNote) : string.Empty));

            // Set up the web request
            var request = (HttpWebRequest)WebRequest.Create("https://www.pivotaltracker.com/time_shifts");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies);

            // Build the post data string
            var postData = String.Format("shift%5Bperson_id%5D={0}&shift%5Bproject_id%5D={1}&shift%5Bdate%5D={2}&shift%5Bhours%5D={3}&shift%5Bdescription%5D={4}&commit=Save",
                                         userId, projectId, Uri.EscapeDataString(startTime.ToShortDateString()), totalTime, Uri.EscapeDataString(description));

            // Encode the post data and write the data to the request stream
            var encoding = new ASCIIEncoding();
            var data = encoding.GetBytes(postData);
            request.ContentLength = data.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            // Check the response to see if the shift was successfully created
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var html = string.Empty;
                    var responseStream = response.GetResponseStream();
                    if (responseStream != null) html = (new StreamReader(responseStream)).ReadToEnd();
                    if (html.Contains("Shift was successfully created")) return true;
                }
            }

            // If the code gets to this point the time was not submitted correctly
            return false;
        }

        protected struct Project
        {
            public int id;
            public string name;
            public List<Story> stories;
        }

        protected struct Story
        {
            public int id;
            public string name;
            public string type;
            public string url;
            public string state;
            public string status;
            public DateTime dateTimeCompleted;
            public string description;
            public string owner;
            public string labels;
            public string filter;
        }

        protected struct TaskTime
        {
            public DateTime startTime;
            public DateTime endTime;
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
                                     {"Filter", story.filter}
                                 };
            return properties;
        }

        static string GetStatus(string state)
        {
            var status = "Current";
            switch (state)
            {
                case "unscheduled":
                    status = "Icebox";
                    break;
                case "unstarted":
                    status = "Backlog";
                    break;
                case "accepted":
                    status = "Done";
                    break;
            }
            return status;
        }

        static Dictionary<string,string> GetTimeZones()
        {
            var timeZones = new Dictionary<string, string>()
                                {
                                    {"ACDT", "+1030"},
                                    {"ACST", "+0930"},
                                    {"ADT", "-0300"},
                                    {"AEDT", "+1100"},
                                    {"AEST", "+1000"},
                                    {"AHDT", "-0900"},
                                    {"AHST", "-1000"},
                                    {"AST", "-0400"},
                                    {"AT", "-0200"},
                                    {"AWDT", "+0900"},
                                    {"AWST", "+0800"},
                                    {"BAT", "+0300"},
                                    {"BDST", "+0200"},
                                    {"BET", "-1100"},
                                    {"BST", "-0300"},
                                    {"BT", "+0300"},
                                    {"BZT2", "-0300"},
                                    {"CADT", "+1030"},
                                    {"CAST", "+0930"},
                                    {"CAT", "-1000"},
                                    {"CCT", "+0800"},
                                    {"CDT", "-0500"},
                                    {"CED", "+0200"},
                                    {"CET", "+0100"},
                                    {"CST", "-0600"},
                                    {"CENTRAL", "-0600"},
                                    {"EAST", "+1000"},
                                    {"EDT", "-0400"},
                                    {"EED", "+0300"},
                                    {"EET", "+0200"},
                                    {"EEST", "+0300"},
                                    {"EST", "-0500"},
                                    {"EASTERN", "-0500"},
                                    {"FST", "+0200"},
                                    {"FWT", "+0100"},
                                    {"GMT", "-0000"},
                                    {"GST", "+1000"},
                                    {"HDT", "-0900"},
                                    {"HST", "-1000"},
                                    {"IDLE", "+1200"},
                                    {"IDLW", "-1200"},
                                    {"IST", "+0530"},
                                    {"IT", "+0330"},
                                    {"JST", "+0900"},
                                    {"JT", "+0700"},
                                    {"MDT", "-0600"},
                                    {"MED", "+0200"},
                                    {"MET", "+0100"},
                                    {"MEST", "+0200"},
                                    {"MEWT", "+0100"},
                                    {"MST", "-0700"},
                                    {"MOUNTAIN", "-0700"},
                                    {"MT", "+0800"},
                                    {"NDT", "-0230"},
                                    {"NFT", "-0330"},
                                    {"NT", "-1100"},
                                    {"NST", "+0630"},
                                    {"NZ", "+1100"},
                                    {"NZST", "+1200"},
                                    {"NZDT", "+1300"},
                                    {"NZT", "+1200"},
                                    {"PDT", "-0700"},
                                    {"PST", "-0800"},
                                    {"PACIFIC", "-0800"},
                                    {"ROK", "+0900"},
                                    {"SAD", "+1000"},
                                    {"SAST", "+0900"},
                                    {"SAT", "+0900"},
                                    {"SDT", "+1000"},
                                    {"SST", "+0200"},
                                    {"SWT", "+0100"},
                                    {"USZ3", "+0400"},
                                    {"USZ4", "+0500"},
                                    {"USZ5", "+0600"},
                                    {"USZ6", "+0700"},
                                    {"UT", "-0000"},
                                    {"UTC", "-0000"},
                                    {"UZ10", "+1100"},
                                    {"WAT", "-0100"},
                                    {"WET", "-0000"},
                                    {"WST", "+0800"},
                                    {"YDT", "-0800"},
                                    {"YST", "-0900"},
                                    {"ZP4", "+0400"},
                                    {"ZP5", "+0500"},
                                    {"ZP6", "+0600"}
                                };

            return timeZones;
        }
    }
}
