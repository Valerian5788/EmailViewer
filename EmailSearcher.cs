using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;

namespace EmailViewer
{
    public class EmailSearcher
    {
        public class SearchResult
        {
            public string FilePath { get; set; }
            public string Client { get; set; }
            public string Project { get; set; }
            public string Subject { get; set; }
            public string Sender { get; set; }
            public DateTime Date { get; set; }
        }

        public List<SearchResult> Search(string rootPath, string searchTerm, string client = null, string project = null, DateTime? startDate = null, DateTime? endDate = null, string sender = null)
        {
            var results = new List<SearchResult>();
            var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);

            foreach (var dir in directories)
            {
                var emailFiles = Directory.GetFiles(dir, "*.eml");
                foreach (var file in emailFiles)
                {
                    var email = MimeMessage.Load(file);
                    var clientName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(file)));
                    var projectName = Path.GetFileName(Path.GetDirectoryName(file));

                    if (MatchesSearchCriteria(email, searchTerm, client, project, startDate, endDate, sender, clientName, projectName))
                    {
                        results.Add(new SearchResult
                        {
                            FilePath = file,
                            Client = clientName,
                            Project = projectName,
                            Subject = email.Subject,
                            Sender = email.From.ToString(),
                            Date = email.Date.DateTime
                        });
                    }
                }
            }

            return results;
        }

        private bool MatchesSearchCriteria(MimeMessage email, string searchTerm, string client, string project, DateTime? startDate, DateTime? endDate, string sender, string clientName, string projectName)
        {
            return (string.IsNullOrEmpty(searchTerm) || email.TextBody.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || email.Subject.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(client) || clientName.Equals(client, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(project) || projectName.Equals(project, StringComparison.OrdinalIgnoreCase))
                && (!startDate.HasValue || email.Date.DateTime >= startDate.Value)
                && (!endDate.HasValue || email.Date.DateTime <= endDate.Value)
                && (string.IsNullOrEmpty(sender) || email.From.ToString().Contains(sender, StringComparison.OrdinalIgnoreCase));
        }
    }
}