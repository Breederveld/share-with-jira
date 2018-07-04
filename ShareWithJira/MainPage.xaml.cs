using Windows.UI.Xaml.Controls;

namespace ShareWithJira
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void btSaveSettings_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            settings.ApplySettings();
        }
    }
}
