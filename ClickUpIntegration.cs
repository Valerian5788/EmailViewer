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
        private readonly string _apiKey;
        private const string BASE_URL = "https://api.clickup.com/api/v2";
        private Func<string, string> getOrCreateEmailId;

        public ClickUpIntegration(Func<string, string> getOrCreateEmailIdFunc, string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
            this.getOrCreateEmailId = getOrCreateEmailIdFunc;
        }

        public async Task<List<ClickUpUser>> GetUsersAsync(string teamId)
        {
            var response = await _httpClient.GetAsync($"{BASE_URL}/team");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {content}");
            }

            var teamsResponse = JsonConvert.DeserializeAnonymousType(content, new { teams = new List<TeamInfo>() });

            // Find the team that matches the provided teamId
            var team = teamsResponse.teams.FirstOrDefault(t => t.id == teamId);
            if (team == null)
            {
                throw new Exception($"Team with ID {teamId} not found in the response");
            }

            return team.members.Select(m => new ClickUpUser
            {
                Id = m.user.id.ToString(),
                Username = m.user.username,
                Email = m.user.email
            }).ToList();
        }

        // Helper classes to deserialize the JSON response
        private class TeamInfo
        {
            public string id { get; set; }
            public List<MemberInfo> members { get; set; }
        }

        private class MemberInfo
        {
            public UserInfo user { get; set; }
        }

        private class UserInfo
        {
            public int id { get; set; }
            public string username { get; set; }
            public string email { get; set; }
        }

        public async Task<List<ClickUpSpaceWithFoldersAndLists>> GetSpacesWithFoldersAndListsAsync(string workspaceId)
        {
            var spaces = await GetSpacesAsync(workspaceId);
            var spacesWithFoldersAndLists = new List<ClickUpSpaceWithFoldersAndLists>();

            foreach (var space in spaces)
            {
                var folders = await GetFoldersAsync(space.Id);
                var listsNotInFolders = await GetListsAsync(space.Id);

                spacesWithFoldersAndLists.Add(new ClickUpSpaceWithFoldersAndLists
                {
                    Space = space,
                    Folders = folders,
                    ListsNotInFolders = listsNotInFolders
                });
            }

            return spacesWithFoldersAndLists;
        }

        public async Task<List<ClickUpFolder>> GetFoldersAsync(string spaceId)
        {
            var response = await _httpClient.GetAsync($"{BASE_URL}/space/{spaceId}/folder");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {content}");
            }
            var foldersResponse = JsonConvert.DeserializeAnonymousType(content, new { folders = new List<ClickUpFolder>() });
            return foldersResponse.folders;
        }

        public async Task<List<ClickUpList>> GetListsInFolderAsync(string folderId)
        {
            var response = await _httpClient.GetAsync($"{BASE_URL}/folder/{folderId}/list");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {content}");
            }
            var listsResponse = JsonConvert.DeserializeAnonymousType(content, new { lists = new List<ClickUpList>() });
            return listsResponse.lists;
        }


        public async Task<List<ClickUpSpace>> GetSpacesAsync(string workspaceId)
        {
            var response = await _httpClient.GetAsync($"{BASE_URL}/team/{workspaceId}/space");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {content}");
            }
            var spacesResponse = JsonConvert.DeserializeAnonymousType(content, new { spaces = new List<ClickUpSpace>() });
            return spacesResponse.spaces;
        }

        public async Task<List<ClickUpList>> GetListsAsync(string spaceId)
        {
            var response = await _httpClient.GetAsync($"{BASE_URL}/space/{spaceId}/list");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ClickUp API error: {response.StatusCode}, {content}");
            }
            var listsResponse = JsonConvert.DeserializeAnonymousType(content, new { lists = new List<ClickUpList>() });
            return listsResponse.lists;
        }

        public async Task<string> CreateTaskAsync(TaskDetails taskDetails, string emailPath)
        {
            string emailId = getOrCreateEmailId(emailPath);
            string customUrl = $"emailviewer:open?id={Uri.EscapeDataString(emailId)}";

            var taskData = new
            {
                name = taskDetails.TaskDescription,
                description = $"Requested by: {taskDetails.RequestedBy}\n\n{taskDetails.Description}\n\nOpen Email: {customUrl}",
                status = MapStatus(taskDetails.Status),
                due_date = ((DateTimeOffset)taskDetails.Date).ToUnixTimeMilliseconds(),
                assignees = new[] { int.Parse(taskDetails.AssignedTo) },
            };

            var json = JsonConvert.SerializeObject(taskData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BASE_URL}/list/{taskDetails.ListId}/task", content);
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
                case "Terminé": return "complete";
                case "Infos": return "info";
                default: return "open";
            }
        }
    }


    public class ClickUpSpaceWithFoldersAndLists
    {
        public ClickUpSpace Space { get; set; }
        public List<ClickUpFolder> Folders { get; set; }
        public List<ClickUpList> ListsNotInFolders { get; set; }
    }

    public class ClickUpFolder
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class ClickUpSpaceWithLists
    {
        public ClickUpSpace Space { get; set; }
        public List<ClickUpList> Lists { get; set; }
    }

    public class ClickUpUser
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class ClickUpList
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ClickUpSpace
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}