using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using EmailViewer.Services;

namespace EmailViewer
{
    public partial class TaskCreationWindow : Window
    {
        public TaskDetails TaskDetails { get; private set; }
        private List<ClickUpUser> users;
        private List<ClickUpSpaceWithFoldersAndLists> spacesWithFoldersAndLists;
        private ClickUpIntegration clickUpIntegration;

        public TaskCreationWindow(string documentPath, List<ClickUpUser> users, string workspaceId, ClickUpIntegration clickUpIntegration)
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Now;
            DocumentTextBox.Text = documentPath;

            this.users = users;
            this.clickUpIntegration = clickUpIntegration;

            // Populate ComboBoxes
            RequestedByComboBox.ItemsSource = users;
            RequestedByComboBox.DisplayMemberPath = "Username";
            RequestedByComboBox.SelectedValuePath = "Id";

            AssignedToComboBox.ItemsSource = users;
            AssignedToComboBox.DisplayMemberPath = "Username";
            AssignedToComboBox.SelectedValuePath = "Id";

            LoadSpacesAndFoldersAndLists(workspaceId);

            SpaceComboBox.SelectionChanged += SpaceComboBox_SelectionChanged;
            FolderComboBox.SelectionChanged += FolderComboBox_SelectionChanged;
        }

        private async void LoadSpacesAndFoldersAndLists(string workspaceId)
        {
            try
            {
                spacesWithFoldersAndLists = await clickUpIntegration.GetSpacesWithFoldersAndListsAsync(workspaceId);
                SpaceComboBox.ItemsSource = spacesWithFoldersAndLists.Select(s => s.Space);
                SpaceComboBox.DisplayMemberPath = "Name";
                SpaceComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading spaces, folders, and lists: {ex.Message}");
            }
        }

        private void SpaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpaceComboBox.SelectedItem is ClickUpSpace selectedSpace)
            {
                var spaceWithFoldersAndLists = spacesWithFoldersAndLists.FirstOrDefault(s => s.Space.Id == selectedSpace.Id);
                if (spaceWithFoldersAndLists != null)
                {
                    FolderComboBox.ItemsSource = new List<object> { new { Id = "", Name = "No Folder" } }
                        .Concat(spaceWithFoldersAndLists.Folders.Cast<object>());
                    FolderComboBox.DisplayMemberPath = "Name";
                    FolderComboBox.SelectedValuePath = "Id";
                    FolderComboBox.SelectedIndex = 0; // Select "No Folder" by default

                    UpdateListComboBox(spaceWithFoldersAndLists.ListsNotInFolders);
                }
            }
        }

        private async void FolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderComboBox.SelectedItem is ClickUpFolder selectedFolder)
            {
                var lists = await clickUpIntegration.GetListsInFolderAsync(selectedFolder.Id);
                UpdateListComboBox(lists);
            }
            else if (FolderComboBox.SelectedIndex == 0) // "No Folder" selected
            {
                var selectedSpace = SpaceComboBox.SelectedItem as ClickUpSpace;
                if (selectedSpace != null)
                {
                    var spaceWithFoldersAndLists = spacesWithFoldersAndLists.FirstOrDefault(s => s.Space.Id == selectedSpace.Id);
                    if (spaceWithFoldersAndLists != null)
                    {
                        UpdateListComboBox(spaceWithFoldersAndLists.ListsNotInFolders);
                    }
                }
            }
        }

        private void UpdateListComboBox(List<ClickUpList> lists)
        {
            ListIdComboBox.ItemsSource = lists;
            ListIdComboBox.DisplayMemberPath = "Name";
            ListIdComboBox.SelectedValuePath = "Id";
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestedByComboBox.SelectedItem == null || AssignedToComboBox.SelectedItem == null || ListIdComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select 'Requested By', 'Assigned To', and 'List'.");
                return;
            }

            TaskDetails = new TaskDetails
            {
                Date = DatePicker.SelectedDate ?? DateTime.Now,
                RequestedBy = ((ClickUpUser)RequestedByComboBox.SelectedItem).Id,
                TaskDescription = TaskDescriptionTextBox.Text,
                Document = DocumentTextBox.Text,
                AssignedTo = ((ClickUpUser)AssignedToComboBox.SelectedItem).Id,
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Description = DescriptionTextBox.Text,
                ListId = ((ClickUpList)ListIdComboBox.SelectedItem).Id
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class TaskDetails
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string RequestedBy { get; set; }
        public string TaskDescription { get; set; }
        public string Document { get; set; }
        public string AssignedTo { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string ListId { get; set; }

        public TaskDetails()
        {
            Id = Guid.NewGuid();
        }
    }
}