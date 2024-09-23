using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace EmailViewer
{
    public class ClickUpIntegration
    {
        private readonly HttpClient _httpClient;
        private const string API_KEY = "pk_2667038_JP6JYQUO0SU5EEBAAT3TV9XXTV0MBLZJ";
        private const string BASE_URL = "https://api.clickup.com/api/v2";
        private Func<string, string> getOrCreateEmailId; // Function to get or create email ID

        public ClickUpIntegration(Func<string, string> getOrCreateEmailIdFunc)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", API_KEY);
            this.getOrCreateEmailId = getOrCreateEmailIdFunc;
        }

        public async Task<string> CreateTaskAsync(string listId, TaskDetails taskDetails, string emailPath)
        {
            if (!int.TryParse(taskDetails.AssignedTo, out int assigneeId))
            {
                Logger.Log($"Invalid assignee ID: {taskDetails.AssignedTo}");
                throw new ArgumentException("Invalid assignee ID", nameof(taskDetails.AssignedTo));
            }

            // Get or create email ID using the provided function
            string emailId = getOrCreateEmailId(emailPath);

            // Generate the custom URL
            string customUrl = $"emailviewer:open?id={Uri.EscapeDataString(emailId)}";

            var taskData = new
            {
                name = taskDetails.TaskDescription,
                description = $"Requested by: {taskDetails.RequestedBy}\n\n{taskDetails.Description}\n\nOpen Email: {customUrl}",
                status = MapStatus(taskDetails.Status),
                due_date = ((DateTimeOffset)taskDetails.Date).ToUnixTimeMilliseconds(),
                assignees = new[] { assigneeId },
                // ... any other task data ...
            };

            var json = JsonConvert.SerializeObject(taskData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BASE_URL}/list/{listId}/task", content);
            var responseContent = await response.Content.ReadAsStringAsync();

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
            switch (status)
            {
                case "À faire": return "to do";
                case "En cours": return "in progress";
                case "Fini": return "finished";
                case "Terminé": return "achevé";
                case "Infos": return "infos";
                default: return "open";
            }
        }
    }
}