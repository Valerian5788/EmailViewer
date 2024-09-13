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
        private RecentFoldersManager recentFoldersManager;

        public MainWindow()
        {
            InitializeComponent();
            recentFoldersManager = new RecentFoldersManager();
            fileListBox.SelectionChanged += FileListBox_SelectionChanged;
            LoadRecentFolders();
            Closing += MainWindow_Closing;
        }

        private void LoadRecentFolders()
        {
            recentFoldersListBox.Items.Clear();
            foreach (var folder in recentFoldersManager.RecentFolders)
            {
                recentFoldersListBox.Items.Add(folder);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The RecentFoldersManager will save the folders when the application closes
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Email files (*.eml)|*.eml|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                recentFoldersManager.AddFolder(folderPath);
                LoadRecentFolders();
                LoadEmailsFromDirectory(folderPath);
                SelectAndDisplayEmail(openFileDialog.FileName);
            }
        }

        private void RecentFolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentFoldersListBox.SelectedItem is string selectedFolder)
            {
                LoadEmailsFromDirectory(selectedFolder);
                if (currentEmailPaths.Any())
                {
                    SelectAndDisplayEmail(currentEmailPaths.First());
                }
            }
        }

        private void LoadEmailsFromDirectory(string directoryPath)
        {
            currentEmailPaths = Directory.GetFiles(directoryPath, "*.eml").ToList();
            fileListBox.Items.Clear();

            foreach (string emailPath in currentEmailPaths)
            {
                fileListBox.Items.Add(Path.GetFileName(emailPath));
            }
        }

        private void SelectAndDisplayEmail(string emailPath)
        {
            int index = currentEmailPaths.IndexOf(emailPath);
            if (index >= 0)
            {
                fileListBox.SelectedIndex = index;
                DisplayEmail(emailPath);
            }
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = fileListBox.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < currentEmailPaths.Count)
            {
                DisplayEmail(currentEmailPaths[selectedIndex]);
            }
        }

        private void DisplayEmail(string filePath)
        {
            try
            {
                var message = MimeMessage.Load(filePath);
                emailContentRichTextBox.Document.Blocks.Clear();

                // Display main email
                AddEmailContent(message, false);

                // Display replies if any
                if (message.References.Any() || message.InReplyTo != null)
                {
                    // Create a dummy reply message
                    var dummyReply = new MimeMessage
                    {
                        Subject = "Re: " + message.Subject,
                        Date = message.Date.AddHours(1)
                    };
                    dummyReply.From.Add(new MailboxAddress("Reply Sender", "reply@example.com"));
                    dummyReply.Body = new TextPart("plain") { Text = "This is a placeholder for a reply message." };

                    AddEmailContent(dummyReply, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading email: {ex.Message}");
            }
        }

        private void AddEmailContent(MimeMessage email, bool isReply)
        {
            Paragraph headerPara = new Paragraph();
            headerPara.Inlines.Add(new Bold(new Run("From: ")));
            headerPara.Inlines.Add(new Run(email.From.ToString() + Environment.NewLine));
            headerPara.Inlines.Add(new Bold(new Run("Subject: ")));
            headerPara.Inlines.Add(new Run(email.Subject + Environment.NewLine));
            headerPara.Inlines.Add(new Bold(new Run("Date: ")));
            headerPara.Inlines.Add(new Run(email.Date.ToString("g") + Environment.NewLine));

            if (isReply)
            {
                headerPara.Background = Brushes.LightGray;
                headerPara.Inlines.InsertBefore(headerPara.Inlines.FirstInline, new Bold(new Run("--- Reply ---" + Environment.NewLine)));
            }

            emailContentRichTextBox.Document.Blocks.Add(headerPara);

            Paragraph bodyPara = new Paragraph(new Run(email.TextBody ?? ""));
            bodyPara.FontWeight = FontWeights.Normal;
            bodyPara.Margin = new Thickness(0, 10, 0, 20);

            if (isReply)
            {
                bodyPara.Background = Brushes.LightGray;
            }

            emailContentRichTextBox.Document.Blocks.Add(bodyPara);
        }

        private void SaveNoteButton_Click(object sender, RoutedEventArgs e)
        {
            // We'll implement this later
            MessageBox.Show("Note saving functionality will be implemented soon!");
        }
    }
}