using ShareWithJira.Data;
using Windows.UI.Xaml.Controls;

namespace ShareWithJira
{
    public sealed partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
            ResetSettings();
        }

        public void ResetSettings()
        {
            tbJiraPrefix.Text = SettingsStore.JiraUrlPrefix ?? string.Empty;
            tbUser.Text = SettingsStore.JiraUser ?? string.Empty;
            tbPassword.Password = SettingsStore.JiraPassword ?? string.Empty;
        }

        public void ApplySettings()
        {
            SettingsStore.JiraUrlPrefix = tbJiraPrefix.Text;
            SettingsStore.JiraUser = tbUser.Text;
            SettingsStore.JiraPassword = tbPassword.Password;
        }
    }
}