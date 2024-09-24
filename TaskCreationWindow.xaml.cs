using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace EmailViewer
{
    public partial class TaskCreationWindow : Window
    {
        public TaskDetails TaskDetails { get; private set; }
        private Dictionary<string, string> userIdMap = new Dictionary<string, string>
        {
            { "David", Environment.GetEnvironmentVariable("CLICKUP_DAVID_USERID") }
            // Add more users here as needed
        };

        public TaskCreationWindow(string documentPath)
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Now;
            DocumentTextBox.Text = documentPath;

            // Populate ComboBoxes
            RequestedByComboBox.ItemsSource = userIdMap.Keys;
            AssignedToComboBox.ItemsSource = userIdMap.Keys;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestedByComboBox.SelectedItem == null || AssignedToComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select both 'Requested By' and 'Assigned To'.");
                return;
            }

            string requestedBy = RequestedByComboBox.SelectedItem.ToString();
            string assignedTo = AssignedToComboBox.SelectedItem.ToString();

            TaskDetails = new TaskDetails
            {
                Date = DatePicker.SelectedDate ?? DateTime.Now,
                RequestedBy = userIdMap[requestedBy],
                TaskDescription = TaskDescriptionTextBox.Text,
                Document = DocumentTextBox.Text,
                AssignedTo = userIdMap[assignedTo],
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Description = DescriptionTextBox.Text
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

        public TaskDetails()
        {
            Id = Guid.NewGuid();
        }
    }
}