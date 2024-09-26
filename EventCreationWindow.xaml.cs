using System;
using System.Windows;

namespace EmailViewer
{
    public partial class EventCreationWindow : Window
    {
        public string EventTitle { get; private set; }
        public string EventDescription { get; private set; }
        public DateTime StartDateTime { get; private set; }
        public DateTime EndDateTime { get; private set; }

        public EventCreationWindow(string subject, string body)
        {
            InitializeComponent();
            EventTitleTextBox.Text = subject;
            DescriptionTextBox.Text = body;
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            EventTitle = EventTitleTextBox.Text;
            EventDescription = DescriptionTextBox.Text;

            if (DateTime.TryParse($"{StartDatePicker.SelectedDate:d} {StartTimeTextBox.Text}", out DateTime startDateTime))
            {
                StartDateTime = startDateTime;
            }
            else
            {
                MessageBox.Show("Invalid start date/time.");
                return;
            }

            if (DateTime.TryParse($"{EndDatePicker.SelectedDate:d} {EndTimeTextBox.Text}", out DateTime endDateTime))
            {
                EndDateTime = endDateTime;
            }
            else
            {
                MessageBox.Show("Invalid end date/time.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}