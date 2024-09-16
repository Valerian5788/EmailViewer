using MimeKit;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace EmailViewer
{
    public partial class MainWindow : Window
    {
        private List<string> currentEmailPaths = new List<string>();
        private RecentEmailsManager recentEmailsManager;
        private EmailSearcher emailSearcher;
        private NoteManager noteManager;
        private string rootPath;
        private string currentEmailPath;

        public MainWindow()
        {
            InitializeComponent();
            recentEmailsManager = new RecentEmailsManager();
            emailSearcher = new EmailSearcher();
            noteManager = new NoteManager();
            Closing += MainWindow_Closing;
            LoadRecentEmails();
        }

        private void LoadRecentEmails()
        {
            recentEmailsListBox.Items.Clear();
            foreach (var emailPath in recentEmailsManager.RecentEmails)
            {
                recentEmailsListBox.Items.Add(Path.GetFileName(emailPath));
            }
        }

        private void OpenRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Folder Selection.",
                Title = "Select Root Folder"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                rootPath = Path.GetDirectoryName(openFileDialog.FileName);
                PopulateFolderTreeView();
            }
        }

        private void PopulateFolderTreeView()
        {
            folderTreeView.Items.Clear();

            var rootItem = new TreeViewItem
            {
                Header = new TextBlock { Text = "Root" },
                Tag = rootPath
            };
            folderTreeView.Items.Add(rootItem);

            PopulateTreeViewItem(rootItem, rootPath);
            rootItem.IsExpanded = true;
        }

        private void PopulateTreeViewItem(TreeViewItem item, string path)
        {
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(directory);
                var subItem = new TreeViewItem
                {
                    Header = new TextBlock { Text = directoryInfo.Name },
                    Tag = directory
                };
                item.Items.Add(subItem);

                // Check if this directory contains .eml files
                if (Directory.GetFiles(directory, "*.eml").Any())
                {
                    var emailsItem = new TreeViewItem
                    {
                        Header = new TextBlock { Text = "Emails" },
                        Tag = directory
                    };
                    subItem.Items.Add(emailsItem);
                }

                PopulateTreeViewItem(subItem, directory);
            }
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as TreeViewItem;
            if (selectedItem != null && selectedItem.Tag is string selectedPath)
            {
                if (selectedItem.Header is TextBlock textBlock && textBlock.Text == "Emails")
                {
                    LoadEmailsFromDirectory(selectedPath);
                }
            }
        }

        private void LoadEmailsFromDirectory(string directoryPath)
        {
            currentEmailPaths = Directory.GetFiles(directoryPath, "*.eml").ToList();
            searchResultsListView.ItemsSource = currentEmailPaths.Select(path => new EmailSearcher.SearchResult
            {
                FilePath = path,
                Subject = Path.GetFileNameWithoutExtension(path),
                Client = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))),
                Project = Path.GetFileName(Path.GetDirectoryName(path)),
                // You might want to load the actual email to get the sender and date
                Sender = "Unknown",
                Date = File.GetCreationTime(path)
            }).ToList();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                MessageBox.Show("Please select a root folder first.");
                return;
            }

            string searchTerm = searchTextBox.Text;
            string senderFilter = senderTextBox.Text;
            DateTime? startDate = startDatePicker.SelectedDate;
            DateTime? endDate = endDatePicker.SelectedDate;

            var searchResults = emailSearcher.Search(rootPath, searchTerm, null, null, startDate, endDate, senderFilter);
            searchResultsListView.ItemsSource = searchResults;
        }

        private void SearchResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (searchResultsListView.SelectedItem is EmailSearcher.SearchResult selectedResult)
            {
                DisplayEmail(selectedResult.FilePath);
            }
        }

        private void DisplayEmail(string filePath)
        {
            try
            {
                var message = MimeMessage.Load(filePath);
                emailContentRichTextBox.Document.Blocks.Clear();

                Paragraph headerPara = new Paragraph();
                headerPara.Inlines.Add(new Bold(new Run("From: ")));
                headerPara.Inlines.Add(new Run(message.From.ToString() + Environment.NewLine));
                headerPara.Inlines.Add(new Bold(new Run("Subject: ")));
                headerPara.Inlines.Add(new Run(message.Subject + Environment.NewLine));
                headerPara.Inlines.Add(new Bold(new Run("Date: ")));
                headerPara.Inlines.Add(new Run(message.Date.ToString("g") + Environment.NewLine));

                emailContentRichTextBox.Document.Blocks.Add(headerPara);

                Paragraph bodyPara = new Paragraph(new Run(message.TextBody ?? ""));
                bodyPara.FontWeight = FontWeights.Normal;
                bodyPara.Margin = new Thickness(0, 10, 0, 0);

                emailContentRichTextBox.Document.Blocks.Add(bodyPara);

                recentEmailsManager.AddEmail(filePath);
                LoadRecentEmails();

                currentEmailPath = filePath;
                LoadNotesForCurrentEmail();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading email: {ex.Message}");
            }
        }

        private void RecentEmailsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentEmailsListBox.SelectedItem is string selectedFileName)
            {
                string fullPath = recentEmailsManager.RecentEmails.FirstOrDefault(path => Path.GetFileName(path) == selectedFileName);
                if (fullPath != null)
                {
                    DisplayEmail(fullPath);
                }
            }
        }

        private void LoadNotesForCurrentEmail()
        {
            if (!string.IsNullOrEmpty(currentEmailPath))
            {
                var notes = noteManager.GetNotesForEmail(currentEmailPath);
                notesListView.ItemsSource = notes;
            }
        }

        private void NotesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (notesListView.SelectedItem is Note selectedNote)
            {
                noteContentTextBox.Text = selectedNote.Content;
                noteTagsTextBox.Text = string.Join(", ", selectedNote.Tags);
            }
        }

        private void AddUpdateNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            string content = noteContentTextBox.Text;
            List<string> tags = noteTagsTextBox.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            if (notesListView.SelectedItem is Note selectedNote)
            {
                noteManager.UpdateNote(selectedNote.Id, content, tags);
            }
            else
            {
                noteManager.AddNote(currentEmailPath, content, tags);
            }

            LoadNotesForCurrentEmail();
            ClearNoteInputs();
        }

        private void DeleteNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (notesListView.SelectedItem is Note selectedNote)
            {
                noteManager.DeleteNote(selectedNote.Id);
                LoadNotesForCurrentEmail();
                ClearNoteInputs();
            }
            else
            {
                MessageBox.Show("Please select a note to delete.");
            }
        }

        private void SearchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = noteSearchTextBox.Text;
            List<string> tags = noteTagsTextBox.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            var searchResults = noteManager.SearchNotes(searchTerm, tags);
            notesListView.ItemsSource = searchResults;
        }

        private void ClearNoteInputs()
        {
            noteContentTextBox.Clear();
            noteTagsTextBox.Clear();
            notesListView.SelectedItem = null;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The RecentFoldersManager will save the folders when the application closes
        }
    }
}