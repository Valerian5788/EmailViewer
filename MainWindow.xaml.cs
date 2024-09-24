using MimeKit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using EmailViewer.Models;

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

        // Default constructor for XAML
        public MainWindow() : this(null, null) { }

        // Constructor with email ID parameter
        public MainWindow(string emailId) : this(null, emailId) { }

        public MainWindow(User user, string emailId = null)
        {
            InitializeComponent();
            currentUser = user;
            CommonInitialization();
 
            if (!string.IsNullOrEmpty(emailId))
            {
                OpenEmailFromId(emailId);
            }
            Logger.Log(emailId == null ? "J'ai ouvert sans parametre" : "J'ai ouvert avec parametre");
        }



        private void CommonInitialization()
        {
            recentEmailsManager = new RecentEmailsManager();
            emailSearcher = new EmailSearcher();
            noteManager = new NoteManager();
            availableTags = new ObservableCollection<string> { "Urgent", "To Do", "To Treat" };
            selectedTags = new ObservableCollection<string>();
            noteTagsComboBox.ItemsSource = availableTags;
            selectedTagsItemsControl.ItemsSource = selectedTags;
            Closing += MainWindow_Closing;
            clickUpIntegration = new ClickUpIntegration(GetOrCreateEmailId);

            // Use user settings if available
            if (currentUser != null)
            {
                oneDriveBasePath = currentUser.OneDriveRootPath;
                rootPath = currentUser.DefaultRootPath;
                // Use other user properties as needed
            }
            else
            {
                // Fallback to default values or load from environment variables
                oneDriveBasePath = @"C:\Users\User\OneDrive"; // Replace with your actual path
                rootPath = Environment.GetEnvironmentVariable("DEFAULT_ROOT_PATH");
            }

            try
            {
                OneDriveIntegration.SetOneDriveRootPath(oneDriveBasePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting OneDrive root path: {ex.Message}");
                Logger.Log($"Error setting OneDrive root path: {ex.Message}");
            }

            LoadEmailIdMap();
            LoadRecentEmails();
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

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            string newTag = noteTagsComboBox.Text.Trim();
            if (!string.IsNullOrEmpty(newTag) && !selectedTags.Contains(newTag))
            {
                selectedTags.Add(newTag);
                if (!availableTags.Contains(newTag))
                {
                    availableTags.Add(newTag);
                }
                noteTagsComboBox.Text = "";
            }
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                selectedTags.Remove(tag);
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
                noteTitleTextBox.Text = selectedNote.Title;
                noteContentTextBox.Text = selectedNote.Content;
                selectedTags.Clear();
                foreach (var tag in selectedNote.Tags)
                {
                    selectedTags.Add(tag);
                }
            }
        }

        private void AddUpdateNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmailPath))
            {
                MessageBox.Show("Please select an email first.");
                return;
            }

            string title = noteTitleTextBox.Text;
            string content = noteContentTextBox.Text;
            List<string> tags = selectedTags.ToList();

            if (notesListView.SelectedItem is Note selectedNote)
            {
                noteManager.UpdateNote(selectedNote.Id, title, content, tags);
            }
            else
            {
                noteManager.AddNote(currentEmailPath, title, content, tags);
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
            List<string> tags = selectedTags.ToList(); // Use selectedTags instead of noteTagsTextBox

            var searchResults = noteManager.SearchNotes(searchTerm, tags);
            notesListView.ItemsSource = searchResults;
        }

        private void ClearNoteInputs()
        {
            noteTitleTextBox.Clear();
            noteContentTextBox.Clear();
            selectedTags.Clear();
            notesListView.SelectedItem = null;
        }

        //private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(currentEmailPath))
        //    {
        //        MessageBox.Show("Veuillez sélectionner un email d'abord.");
        //        return;
        //    }

        //    try
        //    {
        //        // Comment out OneDrive-related code
        //        /*
        //        // Ensure the email is in the OneDrive folder
        //        if (!currentEmailPath.StartsWith(oneDriveBasePath))
        //        {
        //            MessageBox.Show("L'email doit être dans un dossier OneDrive synchronisé.");
        //            return;
        //        }

        //        // Generate a relative path for the email within OneDrive
        //        string relativePath = currentEmailPath.Substring(oneDriveBasePath.Length).TrimStart('\\', '/');
        //        string oneDriveLink = $"https://onedrive.live.com/edit.aspx?resid=YOUR_RESOURCE_ID&cid=YOUR_CID&path=/{Uri.EscapeDataString(relativePath)}";
        //        */

        //        // For testing, use the local file path instead of OneDrive link
        //        string localEmailLink = $"file://{Uri.EscapeDataString(currentEmailPath)}";
        //        Console.WriteLine($"Local Email Link: {localEmailLink}");

        //        var taskWindow = new TaskCreationWindow(currentEmailPath);
        //        if (taskWindow.ShowDialog() == true)
        //        {
        //            string clickUpListId = "901506764736";
        //            Console.WriteLine($"ClickUp List ID: {clickUpListId}");
        //            Console.WriteLine($"Task Details: {JsonConvert.SerializeObject(taskWindow.TaskDetails)}");

        //            string taskId = await clickUpIntegration.CreateTaskAsync(clickUpListId, taskWindow.TaskDetails, localEmailLink);

        //            MessageBox.Show($"Tâche créée avec succès dans ClickUp! ID de la tâche: {taskId}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Erreur détaillée lors de la création de la tâche : {ex.Message}\n\nStack Trace: {ex.StackTrace}");
        //    }
        //}

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
                var taskWindow = new TaskCreationWindow(currentEmailPath);
                if (taskWindow.ShowDialog() == true)
                {
                    string clickUpListId = Environment.GetEnvironmentVariable("CLICKUP_LISTID");
                    Logger.Log($"ClickUp List ID: {clickUpListId}");
                    Logger.Log($"Task Details: {JsonConvert.SerializeObject(taskWindow.TaskDetails)}");

                    // Get the email ID
                    string emailId = GetOrCreateEmailId(currentEmailPath);

                    string taskId = await clickUpIntegration.CreateTaskAsync(clickUpListId, taskWindow.TaskDetails, currentEmailPath);
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




        private string SelectExcelFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                Title = "Sélectionner le fichier Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        private void CreateExcelTask(TaskDetails taskDetails, string excelFilePath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                excelApp = new Excel.Application();
                excelApp.DisplayAlerts = false; // Disable alerts
                workbook = excelApp.Workbooks.Open(excelFilePath, ReadOnly: false, Editable: true);
                worksheet = workbook.Worksheets["To Do"]; // Use the "To Do" sheet

                // Find the next available row starting from row 5
                int newRow = 5;
                while (!string.IsNullOrWhiteSpace(Convert.ToString(worksheet.Cells[newRow, 1].Value)))
                {
                    newRow++;
                }

                worksheet.Cells[newRow, 1] = taskDetails.Date;
                worksheet.Cells[newRow, 2] = taskDetails.RequestedBy;
                worksheet.Cells[newRow, 3] = taskDetails.TaskDescription;
                worksheet.Cells[newRow, 4] = taskDetails.Document;
                worksheet.Cells[newRow, 5] = taskDetails.AssignedTo;
                worksheet.Cells[newRow, 6] = ""; // Leave column F empty
                worksheet.Cells[newRow, 7] = taskDetails.Id.ToString(); // Add GUID to column G
                worksheet.Cells[newRow, 8] = taskDetails.Status;

                workbook.Save();
                MessageBox.Show("Tâche créée avec succès !");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création de la tâche : {ex.Message}");
            }
            finally
            {
                if (excelApp != null) excelApp.DisplayAlerts = true; // Re-enable alerts
                if (worksheet != null) Marshal.ReleaseComObject(worksheet);
                if (workbook != null)
                {
                    workbook.Close(true);
                    Marshal.ReleaseComObject(workbook);
                }
                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveEmailIdMap();
            // The RecentFoldersManager will save the folders when the application closes
        }
    }
}