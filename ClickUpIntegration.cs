using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmailViewer
{
    public class ClickUpIntegration
    {
        private readonly HttpClient _httpClient;
        private const string API_KEY = "pk_2667038_JP6JYQUO0SU5EEBAAT3TV9XXTV0MBLZJ";
        private const string BASE_URL = "https://api.clickup.com/api/v2";

        public ClickUpIntegration()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", API_KEY);
        }

        public async Task<string> CreateTaskAsync(string listId, TaskDetails taskDetails, string emailPath)
        {
            if (!int.TryParse(taskDetails.AssignedTo, out int assigneeId))
            {
                Logger.Log($"Invalid assignee ID: {taskDetails.AssignedTo}");
                throw new ArgumentException("Invalid assignee ID", nameof(taskDetails.AssignedTo));
            }

            string documentPathUrl;
            string emailLinkUrl;

            Logger.Log($"Attempting to generate OneDrive links for document: {taskDetails.Document} and email: {emailPath}");

            try
            {
                documentPathUrl = OneDriveIntegration.GetOneDriveLink(taskDetails.Document);
                Logger.Log($"Successfully generated OneDrive document URL: {documentPathUrl}");

                // Decode the email path
                string decodedEmailPath = Uri.UnescapeDataString(emailPath.Replace("file://", ""));
                emailLinkUrl = OneDriveIntegration.GetOneDriveLink(decodedEmailPath);
                Logger.Log($"Successfully generated OneDrive email URL: {emailLinkUrl}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error generating OneDrive links: {ex.Message}");
                Logger.Log($"Exception details: {ex}");
                Logger.Log("Using fake URLs instead.");

                documentPathUrl = $"https://example.com/onedrive/document/{Uri.EscapeDataString(taskDetails.Document)}";
                emailLinkUrl = $"https://example.com/onedrive/email/{Guid.NewGuid()}";

                Logger.Log($"Using fake document URL: {documentPathUrl}");
                Logger.Log($"Using fake email URL: {emailLinkUrl}");
            }

            var taskData = new
            {
                name = taskDetails.TaskDescription,
                description = $"Requested by: {taskDetails.RequestedBy}\n\n{taskDetails.Description}",
                status = MapStatus(taskDetails.Status),
                due_date = ((DateTimeOffset)taskDetails.Date).ToUnixTimeMilliseconds(),
                assignees = new[] { assigneeId },
                custom_fields = new[]
                {
            new
            {
                id = "050935e2-46c9-4a3d-a0c7-6d317d8d90d7",
                value = documentPathUrl
            },
            new
            {
                id = "17629225-7613-40a3-8dfb-989dffbc7999",
                value = emailLinkUrl
            }
        }
            };

            var json = JsonConvert.SerializeObject(taskData, Formatting.Indented);
            Logger.Log($"JSON being sent to ClickUp:\n{json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BASE_URL}/list/{listId}/task", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            Logger.Log($"Response from ClickUp:\nStatus Code: {response.StatusCode}\nContent: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {responseContent}");
            }

            var taskInfo = JsonConvert.DeserializeAnonymousType(responseContent, new { id = "" });

            return taskInfo.id;
        }

        private string MapStatus(string status)
        {
            // Map your application's status to ClickUp status
            // You'll need to adjust this based on your ClickUp workspace's statuses
            switch (status)
            {
                case "À faire": return "to do";
                case "En cours": return "in progress";
                case "Fini":
                case "Terminé": return "complete";
                case "Bloqué": return "blocked";
                default: return "open";
            }
        }
    }
}