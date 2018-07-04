using ShareWithJira.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace ShareWithJira
{
    public sealed partial class SharePage : Page
    {
        private const string TEST_PREFIX = "test-user";

        public SharePage()
        {
            InitializeComponent();
            tbJiraId.Focus(FocusState.Keyboard);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var shareOperation = e.Parameter as ShareOperation;
            if (shareOperation == null)
                return;

            // Ensure valid settings.
            while (!SettingsStore.IsValid)
            {
                var settingsControl = new SettingsControl();
                ContentDialog settingsDialog = new ContentDialog
                {
                    Title = "Enter Jira settings",
                    Content = settingsControl,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };

                ContentDialogResult result = await settingsDialog.ShowAsync();
                if (result == ContentDialogResult.None)
                {
                    Window.Current.Close();
                    return;
                }

                settingsControl.ApplySettings();
            }

            // Share on click.
            btSendToJira.Click += async (sender, _) =>
            {
                string jiraId = tbJiraId.Text;
                string comment = tbComment.Text;

                shareOperation.ReportStarted();

                await Task.Run(() => UploadToJiraAsync(shareOperation, jiraId, comment))
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            shareOperation.ReportError(t.Exception.Message);
                        else
                            shareOperation.ReportCompleted();
                    });
            };
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var users = await User.FindAllAsync();
            return users
                .Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated
                    && p.Type == UserType.LocalUser)
                .FirstOrDefault();
        }

        private async Task<string> GetUserNameAsync(User user)
        {
            var userName = await user.GetPropertyAsync(KnownUserProperties.AccountName) as string;
            var props = await user.GetPropertiesAsync(new[] { KnownUserProperties.AccountName, KnownUserProperties.FirstName, KnownUserProperties.LastName });
            if (!string.IsNullOrEmpty(userName))
                return userName;
            string first = await user.GetPropertyAsync(KnownUserProperties.FirstName) as string;
            string last = await user.GetPropertyAsync(KnownUserProperties.LastName) as string;
            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
                return null;
            return $"{first} {last}";
        }

        private async Task UploadToJiraAsync(ShareOperation shareOperation, string jiraId, string comment)
        {
            // Read all images.
            var imageStreams = await GetImageStreamsAsync(shareOperation);
            var files = (await Task.WhenAll(imageStreams.AsParallel()
                .Select(async kv =>
                {
                    var originalStream = await kv.Value;
                    var memoryStream = new MemoryStream();
                    await originalStream.AsStream().CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    originalStream.Dispose();
                    return new
                    {
                        kv.Key,
                        Value = memoryStream
                    };
                })))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            shareOperation.ReportSubmittedBackgroundTask();

            // Send images to Jira.
            var jiraClient = new HttpJiraClient(new Uri($"https://{SettingsStore.JiraUrlPrefix}.atlassian.net/rest/api/2"),
                SettingsStore.JiraUser, SettingsStore.JiraPassword, SettingsStore.JiraUser);
            if (SettingsStore.JiraUrlPrefix.ToUpper() == TEST_PREFIX.ToUpper())
                jiraClient.TestMode = true;
            foreach (var file in files)
                await jiraClient.AddIssueAttachmentAsync(jiraId, file.Key, file.Value);

            if (!string.IsNullOrWhiteSpace(comment))
                await jiraClient.AddCommentAsync(jiraId, comment, true);
        }

        private async Task<IDictionary<string, Task<IRandomAccessStreamWithContentType>>> GetImageStreamsAsync(ShareOperation shareOperation)
        {
            if (shareOperation.Data.Contains(StandardDataFormats.Bitmap))
            {
                var image = await shareOperation.Data.GetBitmapAsync();
                var content = await image.OpenReadAsync();
                return new Dictionary<string, Task<IRandomAccessStreamWithContentType>>
                {
                    { "Bitmap", Task.FromResult(content) }
                };
            }
            if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
            {
                var items = await shareOperation.Data.GetStorageItemsAsync();
                return items
                    .Where(item => item.IsOfType(StorageItemTypes.File))
                    .Cast<StorageFile>()
                    .ToDictionary(item => item.Name, async file => await file.OpenReadAsync());
            }
            else
            {
                return null;
            }
        }

        private void tbJiraId_KeyDown(object sender, KeyRoutedEventArgs evt)
        {
            if ((int)evt.Key == 189)
            {
                var inputScope = new InputScope();
                inputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
                tbJiraId.InputScope = inputScope;
            }
            else if (evt.Key == VirtualKey.Back)
            {
                if (!tbJiraId.Text.Contains("-"))
                {
                    var inputScope = new InputScope();
                    inputScope.Names.Add(new InputScopeName(InputScopeNameValue.Url));
                    tbJiraId.InputScope = inputScope;
                }
            }
        }
    }
}