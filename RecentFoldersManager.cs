using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EmailViewer
{
    public class RecentFoldersManager
    {
        private const string RecentFoldersFile = "recentFolders.json";
        private const int MaxRecentFolders = 5;
        private List<string> _recentFolders;

        public RecentFoldersManager()
        {
            _recentFolders = LoadRecentFolders();
        }

        public IReadOnlyList<string> RecentFolders => _recentFolders.AsReadOnly();

        public void AddFolder(string folderPath)
        {
            _recentFolders.Remove(folderPath);
            _recentFolders.Insert(0, folderPath);

            if (_recentFolders.Count > MaxRecentFolders)
            {
                _recentFolders = _recentFolders.Take(MaxRecentFolders).ToList();
            }

            SaveRecentFolders();
        }

        private List<string> LoadRecentFolders()
        {
            if (File.Exists(RecentFoldersFile))
            {
                string json = File.ReadAllText(RecentFoldersFile);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            return new List<string>();
        }

        private void SaveRecentFolders()
        {
            string json = JsonConvert.SerializeObject(_recentFolders);
            File.WriteAllText(RecentFoldersFile, json);
        }
    }
}