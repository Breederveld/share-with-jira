using Windows.Storage;

namespace ShareWithJira.Data
{
    public static class SettingsStore
    {
        public static bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(JiraUrlPrefix)
                    && !string.IsNullOrWhiteSpace(JiraUser)
                    && !string.IsNullOrWhiteSpace(JiraPassword);
            }
        }

        public static string JiraUrlPrefix
        {
            get { return (string)ApplicationData.Current.LocalSettings.Values["JiraUrlPrefix"]; }
            set { ApplicationData.Current.LocalSettings.Values["JiraUrlPrefix"] = value; }
        }

        public static string JiraUser
        {
            get { return (string)ApplicationData.Current.LocalSettings.Values["JiraUser"]; }
            set { ApplicationData.Current.LocalSettings.Values["JiraUser"] = value; }
        }

        public static string JiraPassword
        {
            get { return (string)ApplicationData.Current.LocalSettings.Values["JiraPassword"]; }
            set { ApplicationData.Current.LocalSettings.Values["JiraPassword"] = value; }
        }
    }
}