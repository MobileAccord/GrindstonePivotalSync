using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using GrindstonePivotalCommon;

namespace GrindstonePivotalLink
{
    public partial class Form1 : Form
    {
        private const string configFileName = "config.json";
        private string baseDir = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", string.Empty);

        private int ProjectId = 0;
        private int StoryId = 0;
        private string token;
        private Story story;
        private List<Task> tasks;
        private string comments;
        private Dictionary<string, string> owners;

        private string[] UpdateOptions = new[]
                                             {
                                                 "Update and sync (no time submission)",
                                                 "Update and sync (with time submission)",
                                                 "Update and do not sync"
                                             };  // First 2 options will require grindstone to restart

        public bool LoadPrivateData()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (var arg in args.Select(a => a.ToLower()))
            {
                if (arg.StartsWith("projectid=")) ProjectId = Convert.ToInt32(arg.Replace("projectid=", ""));
                if (arg.StartsWith("storyid=")) StoryId = Convert.ToInt32(arg.Replace("storyid=", ""));
            }
            if (ProjectId == 0 || StoryId == 0)
            {
                MessageBox.Show("ProjectId and StoryId are both required.");
                return false;
            }

            // Load the config
            Config config = new Config();
            try
            {
                config = PivotalUtils.GetConfig(baseDir + configFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            // Get the password if it is not set
            if (string.IsNullOrEmpty(config.Password))
            {
                if (PasswordBox("Password Required", "PivotalTracker Password:", ref config.Password) == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(config.Password))
                    {
                        MessageBox.Show("GrindstonPivotalLink unable to run without password.");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("GrindstonPivotalLink unable to run without password.");
                    return false;
                }
            }

            try
            {
                token = PivotalUtils.GetPivotalTrackerUserToken(config.Email, config.Password);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retrieve user token.");
                return false;
            }

            // Get story node and populate private story object
            XmlNode storyNode = new XmlDocument();
            try
            {
                storyNode = PivotalUtils.GetStoryNode(token, ProjectId, StoryId);
                story = PivotalUtils.GetStoryFromStoryNode(storyNode);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retireve story data from PivotalTracker.");
                return false;
            }

            // Get comments from story node and build private comments string
            try
            {
                var listComments = PivotalUtils.GetCommentsFromStoryNode(storyNode);
                var sbComments = new StringBuilder();
                if (listComments.Count > 0)
                {
                    foreach (var comment in listComments)
                    {
                        sbComments.AppendLine(string.Concat(comment.timestamp.ToShortTimeString(), " - ", comment.author));
                        sbComments.AppendLine(comment.text);
                        sbComments.AppendLine();
                    }
                }
                else
                {
                    sbComments.AppendLine("No comments found.");
                }
                comments = sbComments.ToString();
            }
            catch (Exception)
            {
                comments = "Unable to retrieve story comments from PivotalTracker.";
            }

            // Set the form tasks
            try
            {
                tasks = PivotalUtils.GetStoryTasks(token, ProjectId, StoryId);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retrieve story tasks from PivotalTracker.");
                return false;
            }

            // Set up the story owner
            try
            {
                var authenticityToken = string.Empty;
                var sessionCookie = PivotalUtils.GetPivotalTrackerSessionCookie(config.Email, config.Password, out authenticityToken);
                owners = PivotalUtils.GetUserIds(ref sessionCookie);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to retrieve potential story owners from PivotalTracker.");
                return false;
            }

            return true;
        }

        public void LoadForm()
        {
            InitializeComponent();

            // Set the form title
            lblTitle.Text = story.name;

            // Set the form tasks
            foreach (var task in tasks)
            {
                clbTasks.Items.Add(task.description, task.completed);
            }

            // Set up the story state
            var states = new[] {"unscheduled", "unstarted", "started", "finished", "rejected", "accepted"};
            foreach (var state in states) ddlState.Items.Add(state);
            ddlState.SelectedIndex = ddlState.FindStringExact(story.state);

            // Set up the story owner
            foreach (var owner in owners) ddlOwner.Items.Add(owner.Key);
            ddlOwner.SelectedIndex = ddlOwner.FindStringExact(story.owner);

            // Set up update types
            foreach (var updateOption in UpdateOptions)
            {
                ddlUpdateType.Items.Add(updateOption);
            }
            ddlUpdateType.SelectedIndex = 0;
        }

        private void linkStory_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(string.Concat("https://www.pivotaltracker.com/story/show/", StoryId));
        }

        private void linkDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(story.description);
        }

        private void linkComments_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(comments);
        }

        private void ddlUpdateType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            btnUpdate.Text = ddlUpdateType.SelectedIndex == ddlUpdateType.Items.Count - 1 ? "Update" : "Update/Restart";
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            // Check to see if story state/owner has changed and update
            try
            {
                if (ddlState.Text != story.state || ddlOwner.Text != story.owner)
                {
                    var xmlDoc = new XmlDocument();
                    var storyElement = xmlDoc.CreateElement("story");
                    if (ddlState.Text != story.state)
                    {
                        var stateElement = xmlDoc.CreateElement("current_state");
                        stateElement.InnerText = ddlState.Text;
                        storyElement.AppendChild(stateElement);
                    }
                    if (ddlOwner.Text != story.owner)
                    {
                        var ownerElement = xmlDoc.CreateElement("owned_by");
                        ownerElement.InnerText = ddlOwner.Text;
                        storyElement.AppendChild(ownerElement);
                    }
                    PivotalUtils.UpdateStory(token, ProjectId, StoryId, storyElement);
                    story.state = ddlState.Text;
                    story.owner = ddlOwner.Text;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to update story state/owner.");
                return;
            }

            // Check to see if a comment has been added
            try
            {
                if (!string.IsNullOrEmpty(tbNewComment.Text))
                {
                    PivotalUtils.AddCommentToStory(token, ProjectId, StoryId, tbNewComment.Text);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to add a new comment to the story.");
                return;
            }

            // Check to see if the tasks have changed
            try
            {
                var updatedTasks = new List<Task>();
                for (var i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    var taskIndex = clbTasks.FindStringExact(task.description);
                    if (clbTasks.GetItemChecked(taskIndex) != task.completed)
                    {
                        task.completed = clbTasks.GetItemChecked(taskIndex);
                        PivotalUtils.UpdateTask(token, ProjectId, StoryId, task);
                    }
                    updatedTasks.Add(task);
                }
                tasks = updatedTasks;
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to update story tasks.");
                return;
            }

            if (ddlUpdateType.Text == UpdateOptions[0])
            {
                Process.Start(string.Concat(baseDir, "GrindstonePivotalSync.exe"), "SubmitTime=false AutoClose=true");
                Application.Exit();
            }

            if (ddlUpdateType.Text == UpdateOptions[1])
            {
                Process.Start(string.Concat(baseDir, "GrindstonePivotalSync.exe"));
                Application.Exit();
            }
        }

        public static DialogResult PasswordBox(string title, string promptText, ref string value)
        {
            var form = new Form();
            var label = new Label();
            var textBox = new TextBox();
            var buttonOk = new Button();
            var buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.PasswordChar = '*';
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }
}
