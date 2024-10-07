using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EmailViewer.Services
{
    public class RecentEmailsManager
    {
        private const string RecentEmailsFile = "recentEmails.json";
        private const int MaxRecentEmails = 5;
        private List<string> _recentEmails;

        public RecentEmailsManager()
        {
            _recentEmails = LoadRecentEmails();
        }

        public IReadOnlyList<string> RecentEmails => _recentEmails.AsReadOnly();

        public void AddEmail(string emailPath)
        {
            _recentEmails.Remove(emailPath);
            _recentEmails.Insert(0, emailPath);

            if (_recentEmails.Count > MaxRecentEmails)
            {
                _recentEmails = _recentEmails.Take(MaxRecentEmails).ToList();
            }

            SaveRecentEmails();
        }

        private List<string> LoadRecentEmails()
        {
            if (File.Exists(RecentEmailsFile))
            {
                string json = File.ReadAllText(RecentEmailsFile);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            return new List<string>();
        }

        private void SaveRecentEmails()
        {
            string json = JsonConvert.SerializeObject(_recentEmails);
            File.WriteAllText(RecentEmailsFile, json);
        }
    }
}