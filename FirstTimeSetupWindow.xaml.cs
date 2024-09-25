using System.Windows;
using EmailViewer.Data;
using EmailViewer.Models;

namespace EmailViewer
{
    public partial class FirstTimeSetupWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        public User User { get; private set; }

        public FirstTimeSetupWindow(User user)
        {
            InitializeComponent();
            User = user;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OneDriveRootPathTextBox.Text) ||
                string.IsNullOrWhiteSpace(DefaultRootPathTextBox.Text) ||
                string.IsNullOrWhiteSpace(ClickUpApiKeyTextBox.Text) ||
                string.IsNullOrWhiteSpace(ClickUpListIdTextBox.Text) ||
                string.IsNullOrWhiteSpace(ClickUpUserIdTextBox.Text))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            User.OneDriveRootPath = OneDriveRootPathTextBox.Text;
            User.DefaultRootPath = DefaultRootPathTextBox.Text;
            User.ClickUpApiKey = ClickUpApiKeyTextBox.Text;
            User.ClickUpListId = ClickUpListIdTextBox.Text;
            User.ClickUpUserId = ClickUpUserIdTextBox.Text;

            _context.SaveChanges();

            MessageBox.Show("Setup completed successfully!");
            DialogResult = true;
            Close();
        }
    }
}