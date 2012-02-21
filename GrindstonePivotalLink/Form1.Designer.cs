namespace GrindstonePivotalLink
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.clbTasks = new System.Windows.Forms.CheckedListBox();
            this.linkStory = new System.Windows.Forms.LinkLabel();
            this.linkDescription = new System.Windows.Forms.LinkLabel();
            this.linkComments = new System.Windows.Forms.LinkLabel();
            this.tbNewComment = new System.Windows.Forms.TextBox();
            this.groupTasks = new System.Windows.Forms.GroupBox();
            this.groupAddComment = new System.Windows.Forms.GroupBox();
            this.labelState = new System.Windows.Forms.Label();
            this.ddlState = new System.Windows.Forms.ComboBox();
            this.labelOwner = new System.Windows.Forms.Label();
            this.ddlOwner = new System.Windows.Forms.ComboBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.labelUpdateType = new System.Windows.Forms.Label();
            this.ddlUpdateType = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.groupTasks.SuspendLayout();
            this.groupAddComment.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(32, 13);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Title";
            // 
            // clbTasks
            // 
            this.clbTasks.FormattingEnabled = true;
            this.clbTasks.Location = new System.Drawing.Point(6, 19);
            this.clbTasks.Name = "clbTasks";
            this.clbTasks.Size = new System.Drawing.Size(352, 124);
            this.clbTasks.TabIndex = 1;
            // 
            // linkStory
            // 
            this.linkStory.AutoSize = true;
            this.linkStory.Location = new System.Drawing.Point(123, 64);
            this.linkStory.Name = "linkStory";
            this.linkStory.Size = new System.Drawing.Size(61, 13);
            this.linkStory.TabIndex = 2;
            this.linkStory.TabStop = true;
            this.linkStory.Text = "Show Story";
            this.linkStory.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkStory_LinkClicked);
            // 
            // linkDescription
            // 
            this.linkDescription.AutoSize = true;
            this.linkDescription.Location = new System.Drawing.Point(190, 64);
            this.linkDescription.Name = "linkDescription";
            this.linkDescription.Size = new System.Drawing.Size(90, 13);
            this.linkDescription.TabIndex = 3;
            this.linkDescription.TabStop = true;
            this.linkDescription.Text = "Show Description";
            this.linkDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkDescription_LinkClicked);
            // 
            // linkComments
            // 
            this.linkComments.AutoSize = true;
            this.linkComments.Location = new System.Drawing.Point(286, 64);
            this.linkComments.Name = "linkComments";
            this.linkComments.Size = new System.Drawing.Size(86, 13);
            this.linkComments.TabIndex = 4;
            this.linkComments.TabStop = true;
            this.linkComments.Text = "Show Comments";
            this.linkComments.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkComments_LinkClicked);
            // 
            // tbNewComment
            // 
            this.tbNewComment.Location = new System.Drawing.Point(6, 19);
            this.tbNewComment.Multiline = true;
            this.tbNewComment.Name = "tbNewComment";
            this.tbNewComment.Size = new System.Drawing.Size(352, 75);
            this.tbNewComment.TabIndex = 5;
            // 
            // groupTasks
            // 
            this.groupTasks.Controls.Add(this.clbTasks);
            this.groupTasks.Location = new System.Drawing.Point(10, 80);
            this.groupTasks.Name = "groupTasks";
            this.groupTasks.Size = new System.Drawing.Size(364, 153);
            this.groupTasks.TabIndex = 6;
            this.groupTasks.TabStop = false;
            this.groupTasks.Text = "Update Tasks";
            // 
            // groupAddComment
            // 
            this.groupAddComment.Controls.Add(this.tbNewComment);
            this.groupAddComment.Location = new System.Drawing.Point(10, 239);
            this.groupAddComment.Name = "groupAddComment";
            this.groupAddComment.Size = new System.Drawing.Size(364, 101);
            this.groupAddComment.TabIndex = 7;
            this.groupAddComment.TabStop = false;
            this.groupAddComment.Text = "Add Comment";
            // 
            // labelState
            // 
            this.labelState.AutoSize = true;
            this.labelState.Location = new System.Drawing.Point(11, 349);
            this.labelState.Name = "labelState";
            this.labelState.Size = new System.Drawing.Size(32, 13);
            this.labelState.TabIndex = 8;
            this.labelState.Text = "State";
            // 
            // ddlState
            // 
            this.ddlState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlState.Location = new System.Drawing.Point(124, 346);
            this.ddlState.Name = "ddlState";
            this.ddlState.Size = new System.Drawing.Size(244, 21);
            this.ddlState.TabIndex = 9;
            // 
            // labelOwner
            // 
            this.labelOwner.AutoSize = true;
            this.labelOwner.Location = new System.Drawing.Point(11, 376);
            this.labelOwner.Name = "labelOwner";
            this.labelOwner.Size = new System.Drawing.Size(38, 13);
            this.labelOwner.TabIndex = 10;
            this.labelOwner.Text = "Owner";
            // 
            // ddlOwner
            // 
            this.ddlOwner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlOwner.FormattingEnabled = true;
            this.ddlOwner.Location = new System.Drawing.Point(124, 373);
            this.ddlOwner.Name = "ddlOwner";
            this.ddlOwner.Size = new System.Drawing.Size(244, 21);
            this.ddlOwner.TabIndex = 11;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(124, 427);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(244, 23);
            this.btnUpdate.TabIndex = 12;
            this.btnUpdate.Text = "Update/Restart";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // labelUpdateType
            // 
            this.labelUpdateType.AutoSize = true;
            this.labelUpdateType.Location = new System.Drawing.Point(11, 403);
            this.labelUpdateType.Name = "labelUpdateType";
            this.labelUpdateType.Size = new System.Drawing.Size(69, 13);
            this.labelUpdateType.TabIndex = 13;
            this.labelUpdateType.Text = "Update Type";
            // 
            // ddlUpdateType
            // 
            this.ddlUpdateType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlUpdateType.FormattingEnabled = true;
            this.ddlUpdateType.Location = new System.Drawing.Point(124, 400);
            this.ddlUpdateType.Name = "ddlUpdateType";
            this.ddlUpdateType.Size = new System.Drawing.Size(244, 21);
            this.ddlUpdateType.TabIndex = 14;
            this.ddlUpdateType.SelectedIndexChanged += new System.EventHandler(this.ddlUpdateType_SelectedIndexChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.lblTitle);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(10, 12);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(364, 49);
            this.flowLayoutPanel1.TabIndex = 15;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 462);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.ddlUpdateType);
            this.Controls.Add(this.labelUpdateType);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.ddlOwner);
            this.Controls.Add(this.labelOwner);
            this.Controls.Add(this.ddlState);
            this.Controls.Add(this.labelState);
            this.Controls.Add(this.groupAddComment);
            this.Controls.Add(this.groupTasks);
            this.Controls.Add(this.linkComments);
            this.Controls.Add(this.linkDescription);
            this.Controls.Add(this.linkStory);
            this.Name = "Form1";
            this.Text = "GrindstonePivotalLink";
            this.groupTasks.ResumeLayout(false);
            this.groupAddComment.ResumeLayout(false);
            this.groupAddComment.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.CheckedListBox clbTasks;
        private System.Windows.Forms.LinkLabel linkStory;
        private System.Windows.Forms.LinkLabel linkDescription;
        private System.Windows.Forms.LinkLabel linkComments;
        private System.Windows.Forms.TextBox tbNewComment;
        private System.Windows.Forms.GroupBox groupTasks;
        private System.Windows.Forms.GroupBox groupAddComment;
        private System.Windows.Forms.Label labelState;
        private System.Windows.Forms.ComboBox ddlState;
        private System.Windows.Forms.Label labelOwner;
        private System.Windows.Forms.ComboBox ddlOwner;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Label labelUpdateType;
        private System.Windows.Forms.ComboBox ddlUpdateType;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}

