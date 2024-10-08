﻿using MimeKit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using EmailViewer.Models;
using Google.Apis.Calendar.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2.Flows;
using EmailViewer.Services;
using EmailViewer.Helpers;

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
        private ObservableCollection<string> availableTags;
        private ObservableCollection<string> selectedTags;
        private ClickUpIntegration clickUpIntegration;
        private string oneDriveBasePath;
        private Dictionary<string, string> emailIdMap = new Dictionary<string, string>();
        private const string EMAIL_ID_MAP_FILE = "emailIdMap.json";
        private User currentUser;
        private EmailIndexer emailIndexer;
        private string _ClickUpApiKey;
        private EmailService emailService;
        private EmailViewerCalendarService calendarService;
        private EnhancedEmailService enhancedEmailService;
        private EnhancedEmailService.EmailThread currentThread;
        private int currentMessageIndex = 0;


        private MainWindow()
        {
            InitializeComponent();
            // This constructor should not be used directly
            throw new InvalidOperationException("MainWindow must be initialized with a User object.");
        }

        public MainWindow(User user, string emailId = null)
        {
            Logger.Log($"Entering MainWindow constructor with user: {user?.Email ?? "null"} and emailId: {emailId ?? "null"}");
            InitializeComponent();
            currentUser = user;
            Logger.Log($"After InitializeComponent, currentUser: {currentUser?.Email ?? "null"}");

            try
            {
                CommonInitialization();

                if (!string.IsNullOrEmpty(emailId))
                {
                    emailService.OpenEmailFromId(emailId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during initialization: {ex.Message}");
                MessageBox.Show($"An error occurred during initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            Logger.Log(emailId == null ? "MainWindow opened without parameter" : "MainWindow opened with parameter");
            Logger.Log($"Exiting MainWindow constructor, currentUser: {currentUser?.Email ?? "null"}");
        }

        private void CommonInitialization()
        {
            Logger.Log($"Entering CommonInitialization, currentUser: {currentUser?.Email ?? "null"}");

            if (currentUser == null)
            {
                throw new InvalidOperationException("User is null. Cannot initialize MainWindow.");
            }

            emailIndexer = new EmailIndexer();
            recentEmailsManager = new RecentEmailsManager();
            emailSearcher = new EmailSearcher();
            emailService = new EmailService(emailIndexer, recentEmailsManager);
            noteManager = new NoteManager();
            availableTags = new ObservableCollection<string> { "Urgent", "To Do", "To Treat" };
            selectedTags = new ObservableCollection<string>();
            calendarService = new EmailViewerCalendarService(currentUser);
            enhancedEmailService = new EnhancedEmailService();
            Closing += MainWindow_Closing;

            // Prompt for configuration password
            var passwordWindow = new PasswordPromptWindow("Enter your configuration password");
            if (passwordWindow.ShowDialog() == true)
            {
                string configPassword = passwordWindow.Password;
                _ClickUpApiKey = App.LoadKeyFromSecureConfiguration(configPassword);

                if (!string.IsNullOrEmpty(_ClickUpApiKey))
                {
                    Logger.Log($"Initializing ClickUpIntegration with API key: {_ClickUpApiKey.Substring(0, 5)}...");
                    clickUpIntegration = new ClickUpIntegration(emailService.GetOrCreateEmailId, _ClickUpApiKey);
                    Logger.Log("ClickUpIntegration initialized successfully");
                }
                else
                {
                    Logger.Log("Failed to retrieve ClickUp API key");
                    MessageBox.Show("Failed to retrieve ClickUp API key. Some features may not work.");
                }
           
            }
            else
            {
                Logger.Log("User cancelled configuration password entry");
                MessageBox.Show("Configuration password is required for full functionality.");
            }

            // Use user settings
            oneDriveBasePath = currentUser.OneDriveRootPath;
            rootPath = currentUser.DefaultRootPath;
            Logger.Log($"Using user settings: OneDriveRootPath = {oneDriveBasePath}, DefaultRootPath = {rootPath}");

            // Automatically open the root folder
            if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
            {
                PopulateFolderTreeView();
            }
            else
            {
                Logger.Log($"Root path is invalid or not set: {rootPath}");
                MessageBox.Show("The root folder path is not set or is invalid. Please update your profile settings.", "Root Folder Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            emailService.LoadEmailIdMap();
            LoadRecentEmails();

            Logger.Log("Exiting CommonInitialization");
        }


        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthManager.ClearAuthToken();
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                currentUser = loginWindow.LoggedInUser;
                // Reinitialize the main window with the new user
                CommonInitialization();
            }
            else
            {
                // User cancelled login, close the application
                Application.Current.Shutdown();
            }
        }

        private void ToggleSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchGrid.Visibility == Visibility.Visible)
            {
                searchGrid.Visibility = Visibility.Collapsed;
                toggleSearchButton.Content = "▼ Show Search";
            }
            else
            {
                searchGrid.Visibility = Visibility.Visible;
                toggleSearchButton.Content = "▲ Hide Search";
            }
        }


        private void LoadRecentEmails()
        {
            recentEmailsListBox.Items.Clear();
            foreach (var emailPath in recentEmailsManager.RecentEmails)
            {
                recentEmailsListBox.Items.Add(Path.GetFileName(emailPath));
            }
        }

        private void DisplayEnhancedEmail(string filePath)
        {
            currentThread = enhancedEmailService.LoadEmailThread(filePath);
            currentMessageIndex = currentThread.Messages.Count - 1; // Start with the most recent message
            DisplayCurrentMessage();
        }

        private void DisplayCurrentMessage()
        {
            var currentMessage = currentThread.Messages[currentMessageIndex];

            fromTextBlock.Text = $"From: {currentMessage.From}";
            subjectTextBlock.Text = $"Subject: {currentMessage.Subject}";
            dateTextBlock.Text = $"Date: {currentMessage.Date}";

            string sanitizedHtml = enhancedEmailService.SanitizeHtml(currentMessage.HtmlBody ?? currentMessage.TextBody);
            emailWebBrowser.NavigateToString(sanitizedHtml);

            UpdateThreadNavigation();
        }

        private void UpdateThreadNavigation()
        {
            previousButton.IsEnabled = currentMessageIndex > 0;
            nextButton.IsEnabled = currentMessageIndex < currentThread.Messages.Count - 1;
            messageCountLabel.Text = $"{currentMessageIndex + 1} / {currentThread.Messages.Count}";
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMessageIndex > 0)
            {
                currentMessageIndex--;
                DisplayCurrentMessage();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMessageIndex < currentThread.Messages.Count - 1)
            {
                currentMessageIndex++;
                DisplayCurrentMessage();
            }
        }

        // Replace the existing DisplayEmail method with this:
        private void DisplayEmail(string filePath)
        {
            try
            {
                DisplayEnhancedEmail(filePath);
                currentEmailPath = filePath;
                LoadNotesForCurrentEmail();
                LoadRecentEmails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading email: {ex.Message}");
            }
        }

        private void SearchResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (searchResultsListView.SelectedItem is EmailSearcher.SearchResult selectedResult)
            {
                DisplayEmail(selectedResult.FilePath);
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
                    var searchResults = emailService.LoadEmailsFromDirectory(selectedPath);
                    searchResultsListView.ItemsSource = searchResults;
                    currentEmailPaths = searchResults.Select(r => r.FilePath).ToList();
                }
            }
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                MessageBox.Show("Please select a root folder first.");
                return;
            }

            string searchTerm = searchTextBox.Text;
            var searchResults = emailIndexer.Search(searchTerm);

            searchResultsListView.ItemsSource = searchResults.Select(result => new EmailSearcher.SearchResult
            {
                FilePath = result.FilePath,
                Subject = result.Subject,
                Sender = result.Sender,
                Date = result.Date,
                Client = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(result.FilePath))),
                Project = Path.GetFileName(Path.GetDirectoryName(result.FilePath))
            }).ToList();
        }


        private void AddNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            var noteWindow = new NoteWindow(currentEmailPath, noteManager);
            if (noteWindow.ShowDialog() == true)
            {
                RefreshNotesView();
            }
        }

        private void NotesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (notesListView.SelectedItem is Note selectedNote)
            {
                var noteWindow = new NoteWindow(currentEmailPath, noteManager, selectedNote);
                if (noteWindow.ShowDialog() == true)
                {
                    RefreshNotesView();
                }
                notesListView.SelectedItem = null;
            }
        }

        private void RefreshNotesView()
        {
            if (!string.IsNullOrEmpty(currentEmailPath))
            {
                var notes = noteManager.GetNotesForEmail(currentEmailPath);
                notesListView.ItemsSource = null;
                notesListView.ItemsSource = notes;
            }
        }

        private void LoadNotesForCurrentEmail()
        {
            RefreshNotesView();
        }



        private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            await clickUpIntegration.ShowTaskCreationWindowAsync(currentEmailPath, currentUser.ClickUpWorkspaceId);
        }


        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow(currentUser);
            if (profileWindow.ShowDialog() == true)
            {
                // Update currentUser with the possibly modified User object
                currentUser = profileWindow.User;
                // You might need to update other parts of your application here
                CommonInitialization();
            }
        }

        private async void CreateDetailedEvent_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            try
            {
                var message = MimeKit.MimeMessage.Load(currentEmailPath);
                var eventWindow = new EventCreationWindow(message.Subject, message.TextBody);

                if (eventWindow.ShowDialog() == true)
                {
                    var createdEvent = await calendarService.CreateEventAsync(
                        eventWindow.EventTitle,
                        eventWindow.EventDescription,
                        eventWindow.StartDateTime,
                        eventWindow.EndDateTime
                    );
                    MessageBox.Show($"Event created: {createdEvent.HtmlLink}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating event: {ex.Message}");
                Logger.Log($"Error creating calendar event: {ex}");
            }
        }

        private void QuickAddToCalendar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            try
            {
                var message = MimeKit.MimeMessage.Load(currentEmailPath);
                calendarService.QuickAddToCalendar(message.Subject, message.TextBody, message.Date.DateTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding event to calendar: {ex.Message}");
                Logger.Log($"Error in QuickAddToCalendar_Click: {ex}");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            emailService.SaveEmailIdMap();
            emailIndexer?.Dispose();
        }

    }
}
