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

            var taskData = new
            {
                name = taskDetails.TaskDescription,
                description = $"Requested by: {taskDetails.RequestedBy}\n\n{taskDetails.Description}",
                status = MapStatus(taskDetails.Status),
                due_date = ((DateTimeOffset)taskDetails.Date).ToUnixTimeMilliseconds(),
                assignees = new[] { assigneeId },
                // Include other custom fields if necessary
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
            string taskId = taskInfo.id;

            // Attach the email file to the created task
            await AttachFileToTask(taskId, emailPath);

            return taskId;
        }

        private async Task AttachFileToTask(string taskId, string filePath)
        {
            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "attachment", Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync($"{BASE_URL}/task/{taskId}/attachment", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"ClickUp API error when attaching file: {response.StatusCode}, {responseContent}");
                }
            }
        }

        private string MapStatus(string status)
        {
            // Map your application's status to ClickUp status
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