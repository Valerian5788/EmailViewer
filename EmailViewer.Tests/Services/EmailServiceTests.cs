using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmailViewer.Tests;
using EmailViewer.Services;
using MimeKit;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace EmailViewer.Tests.Services
{
    public class EmailServiceTests : IDisposable
    {
        private readonly Mock<EmailIndexer> _mockEmailIndexer;
        private readonly Mock<RecentEmailsManager> _mockRecentEmailsManager;
        private readonly EmailService _emailService;
        private readonly string _testDirectoryPath;

        public EmailServiceTests()
        {
            _mockEmailIndexer = new Mock<EmailIndexer>();
            _mockRecentEmailsManager = new Mock<RecentEmailsManager>();
            _emailService = new EmailService(_mockEmailIndexer.Object, _mockRecentEmailsManager.Object);

            // Create a temporary directory for test emails
            _testDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDirectoryPath);
        }

        public void Dispose()
        {
            // Clean up the temporary directory after tests
            if (Directory.Exists(_testDirectoryPath))
            {
                Directory.Delete(_testDirectoryPath, true);
            }
        }

        [Fact]
        public void LoadEmailsFromDirectory_ShouldReturnCorrectResults()
        {
            // Arrange
            var testFile1 = Path.Combine(_testDirectoryPath, "test1.eml");
            var testFile2 = Path.Combine(_testDirectoryPath, "test2.eml");

            CreateTestEmailFile(testFile1, "sender1@example.com", "recipient1@example.com", "Test Subject 1", "Test Body 1");
            CreateTestEmailFile(testFile2, "sender2@example.com", "recipient2@example.com", "Test Subject 2", "Test Body 2");

            // Act
            var results = _emailService.LoadEmailsFromDirectory(_testDirectoryPath);

            // Log results
            foreach (var result in results)
            {
                LoggerTest.Log($"SearchResult: FilePath={result.FilePath}, Subject={result.Subject}, Sender={result.Sender}, Date={result.Date}, Client={result.Client}, Project={result.Project}");
            }

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.FilePath == testFile1);
            Assert.Contains(results, r => r.FilePath == testFile2);
            Assert.Contains(results, r => r.Subject == "Test Subject 1" || r.Subject == "test1");
            Assert.Contains(results, r => r.Subject == "Test Subject 2" || r.Subject == "test2");
            Assert.Contains(results, r => r.Sender == "sender1@example.com");
            Assert.Contains(results, r => r.Sender == "sender2@example.com");
        }

        private void CreateTestEmailFile(string filePath, string from, string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            message.WriteTo(filePath);
        }

        [Fact]
        public void LoadEmailIdMap_ShouldLoadExistingMap()
        {
            // Arrange
            var mockFileContent = JsonConvert.SerializeObject(new Dictionary<string, string> { { "test-id", "test-path" } });
            File.WriteAllText("emailIdMap.json", mockFileContent);

            // Act
            _emailService.LoadEmailIdMap();

            // Assert
            var emailIdMap = (Dictionary<string, string>)typeof(EmailService).GetField("emailIdMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_emailService);
            Assert.Single(emailIdMap);
            Assert.Equal("test-path", emailIdMap["test-id"]);

            // Cleanup
            File.Delete("emailIdMap.json");
        }

        [Fact]
        public void SaveEmailIdMap_ShouldSaveMapToFile()
        {
            // Arrange
            var emailIdMap = new Dictionary<string, string> { { "test-id", "test-path" } };
            typeof(EmailService).GetField("emailIdMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_emailService, emailIdMap);

            // Act
            _emailService.SaveEmailIdMap();

            // Assert
            Assert.True(File.Exists("emailIdMap.json"));
            var savedContent = File.ReadAllText("emailIdMap.json");
            Assert.Contains("test-id", savedContent);
            Assert.Contains("test-path", savedContent);

            // Cleanup
            File.Delete("emailIdMap.json");
        }

        // ... [Other test methods remain unchanged]
    }
}