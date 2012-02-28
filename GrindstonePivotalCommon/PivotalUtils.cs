using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrindstonePivotalCommon
{
    public class PivotalUtils
    {
        public static Config GetConfig(string path)
        {
            // Attempt to load the config file
            try
            {
                var config = new Config();

                // Check that the file exists
                if (!File.Exists(path))
                {
                    throw new Exception(String.Format("Unable to find the config file {0}.", path));
                }

                // Load the config file into the string builder
                var sbConfig = new StringBuilder();
                var srConfig = new StreamReader(path);
                while (!srConfig.EndOfStream)
                {
                    sbConfig.Append((srConfig.ReadLine() ?? string.Empty).Trim(' '));
                }
                srConfig.Close();

                // Deserialize the json string
                var jsonObject = (JObject)JsonConvert.DeserializeObject(sbConfig.ToString().Replace("\t", string.Empty));

                // Load the grindstone path
                if (jsonObject["GrindstonePath"] == null)
                {
                    throw new Exception("The 'GrindstonePath' in the config file appears to be missing.");
                }
                config.GrindstonePath = jsonObject["GrindstonePath"].ToString().Trim('"').Replace("\\\\", "\\");
                if (config.GrindstonePath == string.Empty)
                {
                    throw new Exception("The 'GrindstonePath' in the config file does not appear to be set.");
                }

                // Locate grindstone executable
                if (!File.Exists(config.GrindstonePath))
                {
                    throw new Exception(String.Concat("Unable to find grindstone (", config.GrindstonePath, ")."));
                }

                // Load the pivotal email
                if (jsonObject["PivotalEmail"] == null)
                {
                    throw new Exception("The 'PivotalEmail' in the config file appears to be missing.");
                }
                config.Email = jsonObject["PivotalEmail"].ToString().Trim('"');
                if (config.Email == string.Empty)
                {
                    throw new Exception("The 'PivotalEmail' in the config file does not appear to be set.");
                }

                // Load the pivotal password
                if (jsonObject["PivotalPassword"] == null)
                {
                    throw new Exception("The 'PivotalPassword' in the config file appears to be missing.");
                }
                config.Password = jsonObject["PivotalPassword"].ToString().Trim('"');

                // Load the AutoSubmit bool
                if (jsonObject["AutoSubmit"] == null)
                {
                    throw new Exception("The 'AutoSubmit' in the config file appears to be missing.");
                }
                if (!bool.TryParse(jsonObject["AutoSubmit"].ToString(), out config.AutoSubmit))
                {
                    throw new Exception("Unable to parse the 'AutoSubmit' field (boolean) in the config file.");
                }

                // Load the AutoSubmit bool
                if (jsonObject["AutoClose"] == null)
                {
                    throw new Exception("The 'AutoClose' in the config file appears to be missing.");
                }
                if (!bool.TryParse(jsonObject["AutoClose"].ToString(), out config.AutoClose))
                {
                    throw new Exception("Unable to parse the 'AutoClose' field (boolean) in the config file.");
                }

                // Load the ProfileName string
                if (jsonObject["ProfileName"] == null)
                {
                    throw new Exception("The 'ProfileName' in the config file appears to be missing.");
                }
                config.ProfileName = jsonObject["ProfileName"].ToString().Trim('"');

                // Load the ShowTasksFor string
                if (jsonObject["ShowTasksFor"] == null)
                {
                    throw new Exception("The 'ShowTasksFor' in the config file appears to be missing.");
                }
                config.ShowTasksFor = jsonObject["ShowTasksFor"].ToString().Trim('"');

                return config;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetPivotalTrackerUserToken(string username, string password)
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
            catch (Exception e)
            {
                throw e;
            }

            // Get the token from the xml
            var tokenNode = xmlDoc.SelectNodes("/token/guid");
            if (tokenNode != null && tokenNode.Count > 0)
            {
                token = tokenNode[0].InnerText;
            }
            if (token == string.Empty)
            {
                throw new Exception("Empty user token retrieved.");
            }

            // Return the token
            return token;
        }

        public static CookieCollection GetPivotalTrackerSessionCookie(string username, string password)
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

            if (cookies == null || cookies.Count == 0)
            {
                throw new Exception("Unable to retrieve Pivotal Tracker session cookies.");
            }

            // Return the cookies
            return cookies;
        }

        // Screen scraping to find user id. User id not provided in api
        public static Dictionary<string, string> GetUserIds(ref CookieCollection cookies)
        {
            // Setup the return string
            var userIds = new Dictionary<string, string>();

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
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(String.Format("Web requested returned with a status of {0}.", response.StatusCode.ToString()));
                }

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
            var options = html.Split(Environment.NewLine.ToArray());
            foreach (var option in options)
            {
                startTrim = option.IndexOf('"') + 1;
                endTrim = option.IndexOf('"', startTrim);
                var userId = option.Substring(startTrim, endTrim - startTrim);
                startTrim = option.IndexOf('>', endTrim) + 1;
                endTrim = option.IndexOf('&', startTrim) - 1;
                var user = option.Substring(startTrim, endTrim - startTrim);
                //user = user.Replace("&lt;", "<").Replace("&gt;", ">");
                userIds.Add(user, userId);
            }

            // Return the user id
            return userIds;
        }

        // Screen scraping to find user id. User id not provided in api
        public static string GetUserIdByEmail(ref CookieCollection cookies, string email)
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

            // Check to make sure the user id is not empty
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("Unable to retrieve Pivotal Tracker user id.");
            }

            // Return the user id
            return userId;
        }

        public static List<Project> GetAllProjects(string token)
        {
            // Setup the return list
            var projects = new List<Project>();

            // Get the xml doc containing the token using the pivotal tracker api
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create("https://www.pivotaltracker.com/services/v3/projects");
            request.Headers.Add("X-TrackerToken", token);
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

            // Get the project data from the xml
            var projectNodes = xmlDoc.SelectNodes("//project");
            if (projectNodes == null)
            {
                throw new Exception("Unable to retrieve project nodes from xml");
            }
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
            var storyCount = 0;
            var storyNodes = xmlDoc.SelectNodes("//story");
            if (storyNodes == null) return stories;
            foreach (XmlNode storyNode in storyNodes)
            {
                var story = BuildStoryFromStoryNode(storyNode, timeZoneModifier);
                story.order = storyCount++;
                stories.Add(story);
            }

            // Return the stories
            return stories;
        }

        public static XmlNode GetStoryNode(string token, int projectId, int storyId)
        {
            // Get the xml doc containing the token using the pivotal tracker api
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories/{1}", projectId, storyId));
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
                throw ex;
            }

            return xmlDoc.SelectSingleNode("/story");
        }

        public static Story GetStoryFromStoryNode(XmlNode storyNode)
        {
            return storyNode == null ? new Story() : BuildStoryFromStoryNode(storyNode, GetTimeZones());
        }

        public static List<Story> GetStoriesFromStoryNodes(XmlNodeList storyNodes)
        {
            var stories = new List<Story>();
            if (storyNodes == null) return stories;

            // Get Time Zones
            var timeZoneModifier = GetTimeZones();

            // Get the story data from the xml
            stories.AddRange(from XmlNode storyNode in storyNodes select BuildStoryFromStoryNode(storyNode, timeZoneModifier));

            return stories;
        }

        protected static Story BuildStoryFromStoryNode(XmlNode storyNode, Dictionary<string, string> timeZoneModifier)
        {
            var dtCompleted = DateTime.MinValue;
            if (storyNode["accepted_at"] != null && storyNode["accepted_at"].InnerText.Contains(" "))
            {
                var acceptedAt = storyNode["accepted_at"].InnerText;
                var timeZone = acceptedAt.Substring(acceptedAt.LastIndexOf(' ') + 1);
                if (timeZoneModifier.ContainsKey(timeZone)) acceptedAt = acceptedAt.Replace(timeZone, timeZoneModifier[timeZone]);
                DateTime.TryParse(acceptedAt, out dtCompleted);
            }
            var sbFilter = new StringBuilder();
            sbFilter.Append(storyNode["owned_by"] != null ? storyNode["owned_by"].InnerText.Split(new char[] { ' ' })[0] : string.Empty);
            sbFilter.Append("-");
            sbFilter.Append(storyNode["current_state"] != null ? GetStatus(storyNode["current_state"].InnerText) : string.Empty);
            return new Story
            {
                id = Convert.ToInt32(storyNode["id"].InnerText),
                name = (storyNode["name"] != null ? storyNode["name"].InnerText : string.Empty),
                type = (storyNode["story_type"] != null ? storyNode["story_type"].InnerText : string.Empty),
                url = (storyNode["url"] != null ? storyNode["url"].InnerText : string.Empty),
                state = (storyNode["current_state"] != null ? storyNode["current_state"].InnerText : string.Empty),
                status = (storyNode["current_state"] != null ? GetStatus(storyNode["current_state"].InnerText) : string.Empty),
                dateTimeCompleted = dtCompleted,
                description = (storyNode["description"] != null ? storyNode["description"].InnerText : string.Empty),
                owner = (storyNode["owned_by"] != null ? storyNode["owned_by"].InnerText : string.Empty),
                labels = (storyNode["labels"] != null ? storyNode["labels"].InnerText : string.Empty),
                filter = sbFilter.ToString()
            };
        }

        public static List<Comment> GetCommentsFromStoryNode(XmlNode storyNode)
        {
            var comments = new List<Comment>();
            if (storyNode == null) return comments;

            // Get Time Zones
            var timeZoneModifier = GetTimeZones();

            // Get comment nodes
            var commentNodes = storyNode.SelectNodes("/story/notes/note");
            if (commentNodes == null) return comments;

            // Get the story data from the xml
            foreach (XmlNode node in commentNodes)
            {
                var dtNotedAt = DateTime.MinValue;
                if (node["noted_at"] != null && node["noted_at"].InnerText.Contains(" "))
                {
                    var notedAt = node["noted_at"].InnerText;
                    var timeZone = notedAt.Substring(notedAt.LastIndexOf(' ') + 1);
                    if (timeZoneModifier.ContainsKey(timeZone)) notedAt = notedAt.Replace(timeZone, timeZoneModifier[timeZone]);
                    DateTime.TryParse(notedAt, out dtNotedAt);
                }
                comments.Add(new Comment
                {
                    id = Convert.ToInt32(node["id"].InnerText),
                    text = (node["text"] != null ? node["text"].InnerText : string.Empty),
                    author = (node["author"] != null ? node["author"].InnerText : string.Empty),
                    timestamp = dtNotedAt
                });
            }

            return comments.OrderBy(c => c.timestamp).ToList();
        }

        public static List<Task> GetStoryTasks(string token, int projectId, int storyId)
        {
            // Setup the return list
            var tasks = new List<Task>();

            // Get the xml doc containing the story tasks
            var xmlDoc = new XmlDocument();
            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories/{1}/tasks", projectId, storyId));
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
                throw new Exception("Unable to retieve story tasks.");
            }

            // Get the task data from the xml
            var taskNodes = xmlDoc.SelectNodes("//task");
            if (taskNodes == null) return tasks;
            foreach (XmlNode node in taskNodes)
            {
                tasks.Add(new Task
                {
                    id = (node["id"] != null ? Convert.ToInt32(node["id"].InnerText) : -1),
                    description = (node["description"] != null ? node["description"].InnerText : string.Empty),
                    position = (node["position"] != null ? Convert.ToInt32(node["position"].InnerText) : -1),
                    completed = (node["complete"].InnerText == "true")
                });
            }

            return tasks.OrderBy(t => t.position).ToList();
        }

        public static void UpdateStory(string token, int projectId, int storyId, XmlNode storyNode)
        {
            byte[] dataBuffer = Encoding.ASCII.GetBytes(storyNode.OuterXml);

            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories/{1}", projectId, storyId));
            request.Headers.Add("X-TrackerToken", token);
            request.Method = "PUT";
            request.ContentType = "application/xml";
            request.ContentLength = dataBuffer.Length;

            Stream postData = request.GetRequestStream();
            postData.Write(dataBuffer, 0, dataBuffer.Length);
            postData.Close();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Retrieved unexpected response from the server: {0}.", response.StatusCode.ToString()));
                }
            }
        }

        public static void AddCommentToStory(string token, int projectId, int storyId, string commentText)
        {
            var comment = String.Format("<note><text>{0}</text></note>", commentText);
            byte[] dataBuffer = Encoding.ASCII.GetBytes(comment);

            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories/{1}/notes", projectId, storyId));
            request.Headers.Add("X-TrackerToken", token);
            request.Method = "POST";
            request.ContentType = "application/xml";
            request.ContentLength = dataBuffer.Length;

            Stream postData = request.GetRequestStream();
            postData.Write(dataBuffer, 0, dataBuffer.Length);
            postData.Close();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Retrieved unexpected response from the server: {0}.", response.StatusCode.ToString()));
                }
            }
        }

        public static void UpdateTask(string token, int projectId, int storyId, Task task)
        {
            var comment = String.Format("<task><description>{0}</description><complete>{1}</complete></task>", task.description, (task.completed ? "true" : "false"));
            byte[] dataBuffer = Encoding.ASCII.GetBytes(comment);

            var request = WebRequest.Create(String.Format("https://www.pivotaltracker.com/services/v3/projects/{0}/stories/{1}/tasks/{2}", projectId, storyId, task.id));
            request.Headers.Add("X-TrackerToken", token);
            request.Method = "PUT";
            request.ContentType = "application/xml";
            request.ContentLength = dataBuffer.Length;

            Stream postData = request.GetRequestStream();
            postData.Write(dataBuffer, 0, dataBuffer.Length);
            postData.Close();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Retrieved unexpected response from the server: {0}.", response.StatusCode.ToString()));
                }
            }
        }

        public static bool SubmitTime(ref CookieCollection cookies, string userId, string projectId, DateTime startTime, TimeSpan timespan, string projectName, string storyName, string taskNote)
        {
            // Determine the task time and round up to the nearest 15 minute interval
            var totalTime = timespan.Hours + Math.Ceiling((timespan.Minutes % 60) / 15.0) / 4.00;
            if (totalTime < 0.25) return true;

            // Determine the task description to be submitted to Pivotal Tracker
            var description =
                String.Concat((!String.IsNullOrEmpty(projectName) ? String.Concat(projectName, ": ") : string.Empty),
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

        protected static string GetStatus(string state)
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
                case "finished":
                    status = "Delivered";
                    break;
                case "accepted":
                    status = "Done";
                    break;
            }
            return status;
        }

        protected static Dictionary<string, string> GetTimeZones()
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

    public struct Config
    {
        public string GrindstonePath;
        public string Email;
        public string Password;
        public bool AutoSubmit;
        public bool AutoClose;
        public string ProfileName;
        public string ShowTasksFor;
    }

    public struct Project
    {
        public int id;
        public string name;
        public List<Story> stories;
    }

    public struct Story
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
        public int order;
    }

    public struct Task
    {
        public int id;
        public string description;
        public int position;
        public bool completed;
    }

    public struct Comment
    {
        public int id;
        public string text;
        public string author;
        public DateTime timestamp;
    }
}