using MimeKit;
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
                    OpenEmailFromId(emailId);
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

            recentEmailsManager = new RecentEmailsManager();
            emailSearcher = new EmailSearcher();
            noteManager = new NoteManager();
            availableTags = new ObservableCollection<string> { "Urgent", "To Do", "To Treat" };
            selectedTags = new ObservableCollection<string>();
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
                    clickUpIntegration = new ClickUpIntegration(GetOrCreateEmailId, _ClickUpApiKey);
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

            LoadEmailIdMap();
            LoadRecentEmails();

            try
            {
                emailIndexer = new EmailIndexer();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing EmailIndexer: {ex.Message}");
                MessageBox.Show($"Error initializing search functionality: {ex.Message}", "Indexer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

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

        private void OpenEmailFromId(string emailId)
        {
            if (emailIdMap.TryGetValue(emailId, out string emailPath))
            {
                DisplayEmail(emailPath);
            }
            else
            {
                MessageBox.Show($"Email with ID {emailId} not found.");
            }
        }

        private void LoadEmailIdMap()
        {
            if (File.Exists(EMAIL_ID_MAP_FILE))
            {
                string json = File.ReadAllText(EMAIL_ID_MAP_FILE);
                emailIdMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }

        private void SaveEmailIdMap()
        {
            string json = JsonConvert.SerializeObject(emailIdMap);
            File.WriteAllText(EMAIL_ID_MAP_FILE, json);
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
            foreach (var path in currentEmailPaths)
            {
                var message = MimeMessage.Load(path);
                emailIndexer.IndexEmail(path, message.Subject, message.From.ToString(), message.TextBody, message.Date.DateTime);
            }

            searchResultsListView.ItemsSource = currentEmailPaths.Select(path => new EmailSearcher.SearchResult
            {
                FilePath = path,
                Subject = Path.GetFileNameWithoutExtension(path),
                Client = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))),
                Project = Path.GetFileName(Path.GetDirectoryName(path)),
                Sender = MimeMessage.Load(path).From.ToString(),
                Date = MimeMessage.Load(path).Date.DateTime
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
                System.Diagnostics.Debug.WriteLine($"Attempting to display email: {filePath}");
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

                string emailId = GetOrCreateEmailId(filePath);
                recentEmailsManager.AddEmail(filePath);
                LoadRecentEmails();

                currentEmailPath = filePath;
                LoadNotesForCurrentEmail();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying email: {ex.Message}");
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
                MessageBox.Show("Veuillez sélectionner un email d'abord.");
                return;
            }
            try
            {
                Logger.Log($"Current Email Path: {currentEmailPath}");

                var users = await clickUpIntegration.GetUsersAsync(currentUser.ClickUpWorkspaceId);
                var spaces = await clickUpIntegration.GetSpacesAsync(currentUser.ClickUpWorkspaceId);

                var taskWindow = new TaskCreationWindow(currentEmailPath, users, currentUser.ClickUpWorkspaceId, clickUpIntegration);
                if (taskWindow.ShowDialog() == true)
                {
                    Logger.Log($"Task Details: {JsonConvert.SerializeObject(taskWindow.TaskDetails)}");

                    string taskId = await clickUpIntegration.CreateTaskAsync(taskWindow.TaskDetails, currentEmailPath);
                    Logger.Log($"Task created successfully. Task ID: {taskId}");
                    MessageBox.Show($"Tâche créée avec succès dans ClickUp! ID de la tâche: {taskId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating task: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
                MessageBox.Show($"Erreur détaillée lors de la création de la tâche : {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
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

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveEmailIdMap();
            emailIndexer?.Dispose();
        }

        private async Task<CalendarService> InitializeCalendarServiceAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(currentUser.GoogleId))
                {
                    Logger.Log("No Google ID found for the current user");
                    return null;
                }

                var clientSecrets = new ClientSecrets
                {
                    ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
                    ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                };

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    new[] { CalendarService.Scope.CalendarEvents },
                    currentUser.GoogleId,
                    CancellationToken.None,
                    new FileDataStore("Calendar.ApiClient"));

                return new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Email Viewer",
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing Google Calendar service: {ex.Message}");
                return null;
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
                    var calendarService = await InitializeCalendarServiceAsync();
                    if (calendarService == null)
                    {
                        MessageBox.Show("Failed to initialize calendar service. Please check your Google authentication.");
                        return;
                    }

                    var newEvent = new Event
                    {
                        Summary = eventWindow.EventTitle,
                        Description = eventWindow.EventDescription,
                        Start = new EventDateTime { DateTime = eventWindow.StartDateTime },
                        End = new EventDateTime { DateTime = eventWindow.EndDateTime },
                    };

                    var createdEvent = await calendarService.Events.Insert(newEvent, "primary").ExecuteAsync();
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
                string subject = message.Subject ?? "No Subject";
                string body = message.TextBody ?? "";

                // Limit the body length to avoid excessively long URLs
                if (body.Length > 500)
                {
                    body = body.Substring(0, 500) + "...";
                }

                string encodedSubject = Uri.EscapeDataString(subject);
                string encodedBody = Uri.EscapeDataString(body);

                // Use the email date as the default event date
                string date = message.Date.ToString("yyyyMMdd");

                string url = $"https://www.google.com/calendar/render?action=TEMPLATE&text={encodedSubject}&details={encodedBody}&dates={date}/{date}";

                // Use ProcessStartInfo to open the default browser
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding event to calendar: {ex.Message}");
                Logger.Log($"Error in QuickAddToCalendar_Click: {ex}");
            }
        }

    }
}
