using System;
using System.IO;
using Xunit;
using EmailViewer.Services;

namespace EmailViewer.Tests.Services
{
    public class EmailIndexerTests : IDisposable
    {
        private readonly string _tempIndexPath;

        public EmailIndexerTests()
        {
            _tempIndexPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [Fact]
        public void IndexEmail_ShouldAddDocumentToIndex()
        {
            using var indexer = new EmailIndexer(_tempIndexPath);
            var filePath = "test.eml";
            var subject = "Test Subject";
            var sender = "test@example.com";
            var body = "This is a test email body.";
            var date = DateTime.Now;

            indexer.IndexEmail(filePath, subject, sender, body, date);

            var searchResults = indexer.Search(subject);
            Assert.Single(searchResults);
            Assert.Equal(filePath, searchResults[0].FilePath);
            Assert.Equal(subject, searchResults[0].Subject);
            Assert.Equal(sender, searchResults[0].Sender);
            Assert.Equal(date.Date, searchResults[0].Date.Date);
        }

        [Fact]
        public void Search_ShouldReturnMatchingResults()
        {
            using var indexer = new EmailIndexer(_tempIndexPath);
            indexer.IndexEmail("1.eml", "Meeting Notes", "alice@example.com", "Discussed project timeline", DateTime.Now);
            indexer.IndexEmail("2.eml", "Vacation Request", "bob@example.com", "Requesting time off next week", DateTime.Now);
            indexer.IndexEmail("3.eml", "Project Update", "charlie@example.com", "Progress on the current sprint", DateTime.Now);

            var results = indexer.Search("project");

            Assert.Equal(2, results.Length);
            Assert.Contains(results, r => r.Subject == "Meeting Notes");
            Assert.Contains(results, r => r.Subject == "Project Update");
        }

        [Fact]
        public void Search_ShouldLimitResults()
        {
            using var indexer = new EmailIndexer(_tempIndexPath);
            for (int i = 0; i < 20; i++)
            {
                indexer.IndexEmail($"{i}.eml", $"Test Email {i}", "test@example.com", $"Content {i}", DateTime.Now);
            }

            var results = indexer.Search("Test", 5);

            Assert.Equal(5, results.Length);
        }

        [Fact]
        public void Dispose_ShouldCleanUpResources()
        {
            var indexer = new EmailIndexer(_tempIndexPath);
            indexer.IndexEmail("test.eml", "Test", "test@example.com", "Test body", DateTime.Now);

            indexer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => indexer.Search("test"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempIndexPath))
            {
                Directory.Delete(_tempIndexPath, true);
            }
        }
    }
}