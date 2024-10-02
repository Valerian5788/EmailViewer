using System.Windows;
using EmailViewer.Data;
using EmailViewer.Models;

namespace EmailViewer
{
    public partial class ProfileWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        public User User { get; private set; }

        public ProfileWindow(User user)
        {
            InitializeComponent();
            User = user;
            LoadUserData();
        }

        private void LoadUserData()
        {
            EmailTextBox.Text = User.Email;
            OneDrivePathTextBox.Text = User.OneDriveRootPath;
            DefaultRootPathTextBox.Text = User.DefaultRootPath;

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            User.OneDriveRootPath = OneDrivePathTextBox.Text;
            User.DefaultRootPath = DefaultRootPathTextBox.Text;

            _context.SaveChanges();

            MessageBox.Show("Profile updated successfully");
            DialogResult = true;
            Close();
        }
    }
}