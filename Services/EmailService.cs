using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using MimeKit;
using Newtonsoft.Json;
using EmailViewer.Helpers;

namespace EmailViewer.Services
{
    public class EmailService
    {
        private readonly EmailIndexer emailIndexer;
        private readonly RecentEmailsManager recentEmailsManager;
        private Dictionary<string, string> emailIdMap;
        private const string EMAIL_ID_MAP_FILE = "emailIdMap.json";

        public EmailService(EmailIndexer emailIndexer, RecentEmailsManager recentEmailsManager)
        {
            this.emailIndexer = emailIndexer;
            this.recentEmailsManager = recentEmailsManager;
            LoadEmailIdMap();
        }

        public List<EmailSearcher.SearchResult> LoadEmailsFromDirectory(string directoryPath)
        {
            var emailPaths = Directory.GetFiles(directoryPath, "*.eml").ToList();
            var results = new List<EmailSearcher.SearchResult>();

            foreach (var path in emailPaths)
            {
                var message = MimeMessage.Load(path);
                emailIndexer.IndexEmail(path, message.Subject, message.From.ToString(), message.TextBody, message.Date.DateTime);

                results.Add(new EmailSearcher.SearchResult
                {
                    FilePath = path,
                    Subject = Path.GetFileNameWithoutExtension(path),
                    Client = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))),
                    Project = Path.GetFileName(Path.GetDirectoryName(path)),
                    Sender = message.From.ToString(),
                    Date = message.Date.DateTime
                });
            }

            return results;
        }

        public EmailContent DisplayEmail(string filePath)
        {
            try
            {
                var message = MimeMessage.Load(filePath);
                var emailContent = new EmailContent
                {
                    From = message.From.ToString(),
                    Subject = message.Subject,
                    Date = message.Date.ToString("g"),
                    Body = message.TextBody ?? "",
                    FilePath = filePath
                };

                string emailId = GetOrCreateEmailId(filePath);
                recentEmailsManager.AddEmail(filePath);

                return emailContent;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error displaying email: {ex.Message}");
                throw;
            }
        }

        public string GetOrCreateEmailId(string emailPath)
        {
            string emailId = emailIdMap.FirstOrDefault(x => x.Value == emailPath).Key;
            if (string.IsNullOrEmpty(emailId))
            {
                emailId = Guid.NewGuid().ToString("N");
                emailIdMap[emailId] = emailPath;
                SaveEmailIdMap();
            }
            return emailId;
        }

        public void OpenEmailFromId(string emailId)
        {
            if (emailIdMap.TryGetValue(emailId, out string emailPath))
            {
                DisplayEmail(emailPath);
            }
            else
            {
                throw new KeyNotFoundException($"Email with ID {emailId} not found.");
            }
        }

        public void LoadEmailIdMap()
        {
            if (File.Exists(EMAIL_ID_MAP_FILE))
            {
                string json = File.ReadAllText(EMAIL_ID_MAP_FILE);
                emailIdMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            else
            {
                emailIdMap = new Dictionary<string, string>();
            }
        }

        public void SaveEmailIdMap()
        {
            string json = JsonConvert.SerializeObject(emailIdMap);
            File.WriteAllText(EMAIL_ID_MAP_FILE, json);
        }
    }

    public class EmailContent
    {
        public string From { get; set; }
        public string Subject { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
        public string FilePath { get; set; }
    }
}