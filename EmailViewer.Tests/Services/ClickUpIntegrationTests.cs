using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmailViewer.Services;
using Moq;
using Newtonsoft.Json;
using System.Threading;
using Xunit;
using System.Collections.Generic;

namespace EmailViewer.Tests.Services
{
    public class ClickUpIntegrationTests
    {
        private readonly Mock<ClickUpIntegration> _mockClickUpIntegration;
        private readonly string _workspaceId = "2457355";

        public ClickUpIntegrationTests()
        {
            _mockClickUpIntegration = new Mock<ClickUpIntegration>(
                (Func<string, string>)(emailPath => "1"),
                "fake_api_key",
                new HttpClient()
            );
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsListOfUsers()
        {
            // Arrange
            var mockUsers = new List<ClickUpUser>
            {
                new ClickUpUser { Id = "1", Username = "testuser", Email = "testuser@example.com" }
            };
            _mockClickUpIntegration.Setup(c => c.GetUsersAsync(_workspaceId)).ReturnsAsync(mockUsers);

            // Act
            var result = await _mockClickUpIntegration.Object.GetUsersAsync(_workspaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("1", result[0].Id);
            Assert.Equal("testuser", result[0].Username);
            Assert.Equal("testuser@example.com", result[0].Email);
        }

        [Fact]
        public async Task GetSpacesWithFoldersAndListsAsync_ReturnsSpacesWithFoldersAndLists()
        {
            // Arrange
            var mockSpaces = new List<ClickUpSpace>
            {
                new ClickUpSpace { Id = "test_space_id", Name = "Test Space" }
            };
            var mockFolders = new List<ClickUpFolder>
            {
                new ClickUpFolder { Id = "test_folder_id", Name = "Test Folder" }
            };
            var mockLists = new List<ClickUpList>
            {
                new ClickUpList { Id = "test_list_id", Name = "Test List" }
            };

            _mockClickUpIntegration.Setup(c => c.GetSpacesAsync(_workspaceId)).ReturnsAsync(mockSpaces);
            _mockClickUpIntegration.Setup(c => c.GetFoldersAsync("test_space_id")).ReturnsAsync(mockFolders);
            _mockClickUpIntegration.Setup(c => c.GetListsAsync("test_space_id")).ReturnsAsync(mockLists);

            // Act
            var result = await _mockClickUpIntegration.Object.GetSpacesWithFoldersAndListsAsync(_workspaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test_space_id", result[0].Space.Id);
            Assert.Equal("Test Space", result[0].Space.Name);
            Assert.Single(result[0].Folders);
            Assert.Equal("test_folder_id", result[0].Folders[0].Id);
            Assert.Single(result[0].ListsNotInFolders);
            Assert.Equal("test_list_id", result[0].ListsNotInFolders[0].Id);
        }

        [Fact]
        public async Task CreateTaskAsync_ReturnsTaskId()
        {
            // Arrange
            var taskDetails = new TaskDetails
            {
                TaskDescription = "Test Task",
                Description = "Test Description",
                Status = "open",
                Date = DateTime.Now,
                AssignedTo = "1",
                ListId = "test_list_id"
            };

            _mockClickUpIntegration.Setup(c => c.CreateTaskAsync(It.IsAny<TaskDetails>(), It.IsAny<string>()))
                .ReturnsAsync("test_task_id");

            // Act
            var result = await _mockClickUpIntegration.Object.CreateTaskAsync(taskDetails, "test_email_path");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_task_id", result);
        }

        [Fact]
        public async Task GetUsersAsync_EmptyUserList_ReturnsEmptyList()
        {
            _mockClickUpIntegration.Setup(c => c.GetUsersAsync(_workspaceId)).ReturnsAsync(new List<ClickUpUser>());

            var result = await _mockClickUpIntegration.Object.GetUsersAsync(_workspaceId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersAsync_ApiError_ThrowsException()
        {
            _mockClickUpIntegration.Setup(c => c.GetUsersAsync(_workspaceId)).ThrowsAsync(new Exception("API Error"));

            await Assert.ThrowsAsync<Exception>(() => _mockClickUpIntegration.Object.GetUsersAsync(_workspaceId));
        }

        [Fact]
        public async Task GetSpacesWithFoldersAndListsAsync_MultipleSpaces_ReturnsCorrectStructure()
        {
            var mockSpaces = new List<ClickUpSpace>
        {
            new ClickUpSpace { Id = "space1", Name = "Space 1" },
            new ClickUpSpace { Id = "space2", Name = "Space 2" }
        };
            _mockClickUpIntegration.Setup(c => c.GetSpacesAsync(_workspaceId)).ReturnsAsync(mockSpaces);
            _mockClickUpIntegration.Setup(c => c.GetFoldersAsync(It.IsAny<string>())).ReturnsAsync(new List<ClickUpFolder>());
            _mockClickUpIntegration.Setup(c => c.GetListsAsync(It.IsAny<string>())).ReturnsAsync(new List<ClickUpList>());

            var result = await _mockClickUpIntegration.Object.GetSpacesWithFoldersAndListsAsync(_workspaceId);

            Assert.Equal(2, result.Count);
            Assert.Equal("space1", result[0].Space.Id);
            Assert.Equal("space2", result[1].Space.Id);
        }

        [Fact]
        public async Task CreateTaskAsync_InvalidTaskDetails_ThrowsException()
        {
            var invalidTaskDetails = new TaskDetails(); // Assume this is invalid due to missing required fields

            _mockClickUpIntegration.Setup(c => c.CreateTaskAsync(It.IsAny<TaskDetails>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid task details"));

            await Assert.ThrowsAsync<ArgumentException>(() => _mockClickUpIntegration.Object.CreateTaskAsync(invalidTaskDetails, "test_email_path"));
        }

        [Fact]
        public async Task GetFoldersAsync_ReturnsCorrectFolders()
        {
            var mockFolders = new List<ClickUpFolder>
        {
            new ClickUpFolder { Id = "folder1", Name = "Folder 1" },
            new ClickUpFolder { Id = "folder2", Name = "Folder 2" }
        };
            _mockClickUpIntegration.Setup(c => c.GetFoldersAsync("test_space_id")).ReturnsAsync(mockFolders);

            var result = await _mockClickUpIntegration.Object.GetFoldersAsync("test_space_id");

            Assert.Equal(2, result.Count);
            Assert.Equal("folder1", result[0].Id);
            Assert.Equal("folder2", result[1].Id);
        }
    }
}