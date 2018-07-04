using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace ShareWithJira.Data
{
    public class HttpJiraClient
    {
        private readonly Uri _baseApiUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _author;

        public HttpJiraClient(Uri baseApiUrl, string username, string password, string author)
        {
            _baseApiUrl = baseApiUrl;
            _username = username;
            _password = password;
            _author = author;
        }

        public bool TestMode { get; set; }

        public async Task AddIssueAttachmentAsync(string issueId, string fileName, Stream content)
        {
            if (TestMode)
                return;
            string attachmentsUrl = _baseApiUrl.AbsoluteUri + $"/issue/{issueId}/attachments";
            using (var client = CreateDefaultClient())
            {
                client.DefaultRequestHeaders.Add("X-Atlassian-Token", "no-check");
                var multiPartContent = new MultipartFormDataContent("-----" + DateTime.Now.ToString())
                {
                    { new StreamContent(content), "\"file\"", fileName },
                };
                var response = await client.PostAsync(attachmentsUrl, multiPartContent);
                if (!response.IsSuccessStatusCode)
                    throw new WebException($"Error received when adding attachment to issue {issueId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task<string> FindUserAsync(string userQuery)
        {
            if (TestMode)
                return "tester";
            string findUserUrl = _baseApiUrl.AbsoluteUri + $"/groupuserpicker?query={userQuery}";
            using (var client = CreateDefaultClient())
            {
                string result = await client.GetStringAsync(findUserUrl);
                var data = JsonObject.Parse(result);
                var users = data.GetNamedObject("users").GetNamedArray("users");
                if (users.Count == 0)
                    return null;
                return users[0].GetObject().GetNamedValue("name").GetString();
            }
        }

        public async Task AddCommentAsync(string issueId, string comment, bool preventDuplicates = false)
        {
            if (TestMode)
                return;

            // Check for existing comment.
            if (preventDuplicates && (await GetCommentsAsync(issueId)).Any(c => c == comment))
                return;

            string commentUrl = _baseApiUrl.AbsoluteUri + string.Format("/issue/{0}/comment", issueId);
            using (var client = CreateDefaultClient())
            {
                var obj = new JsonObject
                {
                    { "body", JsonValue.CreateStringValue(comment) },
                };
                client.DefaultRequestHeaders.Add("X-Atlassian-Token", "no-check");
                var response = await client.PostAsync(commentUrl, new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    throw new WebException($"Error received when adding comment to issue {issueId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        private async Task<IEnumerable<string>> GetCommentsAsync(string issueId)
        {
            string commentUrl = _baseApiUrl.AbsoluteUri + string.Format("/issue/{0}/comment", issueId);
            using (var client = CreateDefaultClient())
            {
                string result = await client.GetStringAsync(commentUrl);
                var data = JsonObject.Parse(result);
                var comments = data.GetNamedArray("comments");
                return comments
                    .Select(comment => comment.GetObject().GetNamedString("body"));
            }
        }

        private HttpClient CreateDefaultClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}