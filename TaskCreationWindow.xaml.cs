using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace EmailViewer
{
    public partial class TaskCreationWindow : Window
    {
        public TaskDetails TaskDetails { get; private set; }
        private List<ClickUpUser> users;
        private List<ClickUpSpace> spaces;
        private ClickUpIntegration clickUpIntegration;

        public TaskCreationWindow(string documentPath, List<ClickUpUser> users, List<ClickUpSpace> spaces, ClickUpIntegration clickUpIntegration)
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Now;
            DocumentTextBox.Text = documentPath;

            this.users = users;
            this.spaces = spaces;
            this.clickUpIntegration = clickUpIntegration;

            // Populate ComboBoxes
            RequestedByComboBox.ItemsSource = users;
            RequestedByComboBox.DisplayMemberPath = "Username";
            RequestedByComboBox.SelectedValuePath = "Id";

            AssignedToComboBox.ItemsSource = users;
            AssignedToComboBox.DisplayMemberPath = "Username";
            AssignedToComboBox.SelectedValuePath = "Id";

            SpaceComboBox.ItemsSource = spaces;
            SpaceComboBox.DisplayMemberPath = "Name";
            SpaceComboBox.SelectedValuePath = "Id";

            SpaceComboBox.SelectionChanged += SpaceComboBox_SelectionChanged;
        }

        private async void SpaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpaceComboBox.SelectedItem is ClickUpSpace selectedSpace)
            {
                var lists = await clickUpIntegration.GetListsAsync(selectedSpace.Id);
                ListIdComboBox.ItemsSource = lists;
                ListIdComboBox.DisplayMemberPath = "Name";
                ListIdComboBox.SelectedValuePath = "Id";
            }
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