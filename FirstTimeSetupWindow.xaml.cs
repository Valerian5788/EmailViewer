using System.Windows;
using EmailViewer.Data;
using EmailViewer.Models;

namespace EmailViewer
{
    public partial class FirstTimeSetupWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        public User User { get; private set; }
        public string ConfigPassword { get; private set; }
        public string ClickUpApiKey { get; private set; }

        public FirstTimeSetupWindow()
        {
            InitializeComponent();
            User = new User();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
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
                return; // User cancelled password entry
            }

            // Save non-sensitive information to User object
            User.OneDriveRootPath = OneDriveRootPathTextBox.Text;
            User.DefaultRootPath = DefaultRootPathTextBox.Text;

            _context.Users.Add(User);
            _context.SaveChanges();

            // Store ClickUp API key for later use in secure storage
            ClickUpApiKey = ClickUpApiKeyTextBox.Text;

            MessageBox.Show("Setup completed successfully!");
            DialogResult = true;
            Close();
        }
    }
}