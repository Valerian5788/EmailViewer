using System;
using System.IO;
using Xunit;
using EmailViewer.Services;
using MimeKit;

namespace EmailViewer.Tests.Services
{
    public class EmailSearcherTests : IDisposable
    {
        private readonly EmailSearcher _emailSearcher;
        private readonly string _testRootPath;

        public EmailSearcherTests()
        {
            _emailSearcher = new EmailSearcher();
            _testRootPath = Path.Combine(Path.GetTempPath(), "EmailSearcherTests");
            Directory.CreateDirectory(_testRootPath);
        }

        [Fact]
        public void Search_WithValidKeyword_ReturnsMatchingResults()
        {
            // Arrange
            CreateTestEmailFile("Test Subject", "This is a test email body", "sender@example.com", DateTime.Now);

            // Act
            var results = _emailSearcher.Search(_testRootPath, "test");

            // Assert
            Assert.Single(results);
            Assert.Contains("Test Subject", results[0].Subject);
        }

        [Fact]
        public void Search_WithDateRange_ReturnsEmailsWithinRange()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-1);
            var endDate = DateTime.Now.AddDays(1);
            CreateTestEmailFile("Email 1", "Body 1", "sender1@example.com", DateTime.Now);
            CreateTestEmailFile("Email 2", "Body 2", "sender2@example.com", DateTime.Now.AddDays(-2));

            // Act
            var results = _emailSearcher.Search(_testRootPath, "", startDate: startDate, endDate: endDate);

            // Assert
            Assert.Single(results);
            Assert.Equal("Email 1", results[0].Subject);
        }

        [Fact]
        public void Search_WithSender_ReturnsEmailsFromSpecificSender()
        {
            // Arrange
            CreateTestEmailFile("Email 1", "Body 1", "sender1@example.com", DateTime.Now);
            CreateTestEmailFile("Email 2", "Body 2", "sender2@example.com", DateTime.Now);

            // Act
            var results = _emailSearcher.Search(_testRootPath, "", sender: "sender1@example.com");

            // Assert
            Assert.Single(results);
            Assert.Equal("sender1@example.com", results[0].Sender);
        }

        [Fact]
        public void Search_WithNonExistentKeyword_ReturnsEmptyList()
        {
            // Arrange
            CreateTestEmailFile("Test Subject", "This is a test email body", "sender@example.com", DateTime.Now);

            // Act
            var results = _emailSearcher.Search(_testRootPath, "nonexistent");

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void Search_WithSpecialCharacters_HandlesQueryCorrectly()
        {
            // Arrange
            CreateTestEmailFile("Subject with $pecial Ch@racters", "Body with $pecial Ch@racters", "sender@example.com", DateTime.Now);

            // Act
            var results = _emailSearcher.Search(_testRootPath, "$pecial Ch@racters");

            // Assert
            Assert.Single(results);
            Assert.Contains("$pecial Ch@racters", results[0].Subject);
        }

        [Fact]
        public void Search_WithLargeDataset_PerformsWithinAcceptableTime()
        {
            // Arrange
            const int emailCount = 10000;
            for (int i = 0; i < emailCount; i++)
            {
                CreateTestEmailFile($"Subject {i}", $"Body {i}", $"sender{i}@example.com", DateTime.Now.AddDays(-i));
            }

            // Act
            var startTime = DateTime.Now;
            var results = _emailSearcher.Search(_testRootPath, "Subject 5000");
            var duration = DateTime.Now - startTime;

            // Assert
            Assert.Single(results);
            Assert.True(duration.TotalSeconds < 5, $"Search took {duration.TotalSeconds} seconds, which exceeds the 5 second threshold");
        }

        private void CreateTestEmailFile(string subject, string body, string sender, DateTime date)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sender Name", sender));
            message.To.Add(new MailboxAddress("Recipient Name", "recipient@example.com"));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
            message.Date = date;

            var fileName = $"{Guid.NewGuid()}.eml";
            var filePath = Path.Combine(_testRootPath, "Client", "Project", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = File.Create(filePath))
            {
                message.WriteTo(stream);
            }
        }

        public void Dispose()
        {
            Directory.Delete(_testRootPath, true);
        }
    }
}