using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using GrindstonePivotalCommon;

namespace GrindstonePivotalSyncInstaller
{
    [RunInstaller(true)]
    public class GrindstonePivotalSyncInstaller : Installer
    {
        private const string ASSEMBLY_PATH = "assemblypath";

        private const string GPS_EMAIL_KEY = "gps.email";
        private const string GPS_PASSWORD_KEY = "gps.password";
        private const string GPS_AUTO_CLOSE_KEY = "gps.autoclose";
        private const string GPS_AUTO_SUBMIT_KEY = "gps.autosubmit";
        private const string GPS_PROFILE_NAME_KEY = "gps.profilename";
        private const string GPS_SHOW_TASKS_FOR_KEY = "gps.showtasksfor";

        private string m_email, m_password, m_profileName, m_showTasksFor;
        private bool m_autoClose, m_autoSubmit;

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            string source = Path.GetFileName(Context.Parameters[ASSEMBLY_PATH]);

            const string log = "Application";
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, log);

            string installPath = Directory.GetParent(Context.Parameters[ASSEMBLY_PATH]).ToString();
            Config config = PivotalUtils.GetConfig(installPath + @"\config.json");

            m_email = Context.Parameters[GPS_EMAIL_KEY];
            m_password = Context.Parameters[GPS_PASSWORD_KEY];
            m_autoClose = (Context.Parameters[GPS_AUTO_CLOSE_KEY] == "1");
            m_autoSubmit = (Context.Parameters[GPS_AUTO_SUBMIT_KEY] == "1");
            m_profileName = (String.IsNullOrEmpty(Context.Parameters[GPS_PROFILE_NAME_KEY]) ? "Default" : Context.Parameters[GPS_PROFILE_NAME_KEY]);
            m_showTasksFor = (String.IsNullOrEmpty(Context.Parameters[GPS_SHOW_TASKS_FOR_KEY]) ? string.Empty : string.Concat(Context.Parameters[GPS_SHOW_TASKS_FOR_KEY].ToString().Replace(" ", ""), "-Current"));

            config.Email = m_email;
            config.Password = m_password;
            config.AutoClose = m_autoClose;
            config.AutoSubmit = m_autoSubmit;
            config.ProfileName = m_profileName;
            config.ShowTasksFor = m_showTasksFor;

            PivotalUtils.WriteConfig(config, installPath + @"\config.json");
        }
    }
}
