using System.Windows;
using EmailViewer.Data;
using EmailViewer.Models;

namespace EmailViewer
{
    public partial class FirstTimeSetupWindow : Window
    {
        private readonly AppDbContext _context; // Use the same context passed through the flow
        public User User { get; private set; } // This is the existing user passed from the login window
        public string ConfigPassword { get; private set; }
        public string ClickUpApiKey { get; private set; }

        // Constructor accepting the existing User
        public FirstTimeSetupWindow(User user)
        {
            InitializeComponent();

            try
            {
                _context = new AppDbContext();
                User = user;
                _context.Users.Attach(User);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing FirstTimeSetupWindow: {ex.Message}");
                Application.Current.Shutdown(); // Optionally force shutdown if critical
            }
        }

        // Click event handler for the Save button
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input fields
            if (string.IsNullOrWhiteSpace(OneDriveRootPathTextBox.Text) ||
                string.IsNullOrWhiteSpace(DefaultRootPathTextBox.Text) ||
                string.IsNullOrWhiteSpace(ClickUpApiKeyTextBox.Text))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            // Prompt for configuration password
            var passwordWindow = new PasswordPromptWindow("Enter a password to secure your configuration");
            if (passwordWindow.ShowDialog() == true)
            {
                ConfigPassword = passwordWindow.Password;
            }
            else
            {
                return; // User cancelled password entry, abort saving
            }

            // Update the existing user with the information from the form
            User.OneDriveRootPath = OneDriveRootPathTextBox.Text;
            User.DefaultRootPath = DefaultRootPathTextBox.Text;
            User.ClickUpWorkspaceId = ClickUpWorkspaceId.Text;

            // Save the user information to the database
            _context.SaveChanges();

            ClickUpApiKey = ClickUpApiKeyTextBox.Text;

            // Indicate success and close the window
            MessageBox.Show("Setup completed successfully!");
            DialogResult = true;
            Close();
        }
    }
}
