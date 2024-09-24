using System.Windows;
using System.Linq;
using EmailViewer.Data;
using EmailViewer.Models;
using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;

namespace EmailViewer
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        public User LoggedInUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            AttemptAutoLogin();
        }

        private void AttemptAutoLogin()
        {
            string token = AuthManager.LoadAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                var user = _context.Users.FirstOrDefault(u => u.RememberMeToken == token);
                if (user != null)
                {
                    LoggedInUser = user;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == EmailTextBox.Text);
            if (user != null && BCrypt.Net.BCrypt.Verify(PasswordBox.Password, user.PasswordHash))
            {
                LoggedInUser = user;
                if (RememberMeCheckBox.IsChecked == true)
                {
                    user.RememberMeToken = Guid.NewGuid().ToString();
                    _context.SaveChanges();
                    AuthManager.SaveAuthToken(user.RememberMeToken);
                }
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid email or password");
            }
        }



        private void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement Google login here
            // You'll need to use a library like Google.Apis.Auth for this
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            if (registerWindow.ShowDialog() == true)
            {
                MessageBox.Show("Registration successful. Please log in.");
            }
        }

        private void OpenMainWindow(User user)
        {
            var mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close();
        }
    }
}