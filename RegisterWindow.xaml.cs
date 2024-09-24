using System.Windows;
using System.Linq;
using EmailViewer.Data;
using EmailViewer.Models;
using BCrypt.Net;

namespace EmailViewer
{
    public partial class RegisterWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match");
                return;
            }

            if (_context.Users.Any(u => u.Email == EmailTextBox.Text))
            {
                MessageBox.Show("Email already in use");
                return;
            }

            var user = new User
            {
                Email = EmailTextBox.Text,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordBox.Password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            DialogResult = true;
            Close();
        }
    }
}