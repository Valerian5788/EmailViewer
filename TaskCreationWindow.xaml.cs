using System;
using System.Windows;
using System.Windows.Controls;

namespace EmailViewer
{
    public partial class TaskCreationWindow : Window
    {
        public TaskDetails TaskDetails { get; private set; }

        public TaskCreationWindow(string documentPath)
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Now;
            DocumentTextBox.Text = documentPath;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            TaskDetails = new TaskDetails
            {
                Date = DatePicker.SelectedDate ?? DateTime.Now,
                RequestedBy = RequestedByTextBox.Text,
                TaskDescription = TaskDescriptionTextBox.Text,
                Document = DocumentTextBox.Text,
                AssignedTo = AssignedToTextBox.Text,
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
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

        public TaskDetails()
        {
            Id = Guid.NewGuid(); // Generate a new GUID when the task is created
        }
    }
}