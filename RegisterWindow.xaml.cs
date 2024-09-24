using System.Windows;
using System.Linq;
using EmailViewer.Data;
using EmailViewer.Models;
using System.Text.RegularExpressions;

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
            if (!ValidateInput())
                return;

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

            MessageBox.Show("Registration successful!");
            DialogResult = true;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !Regex.IsMatch(EmailTextBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid email address.");
                return false;
            }

            if (PasswordBox.Password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.");
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match");
                return false;
            }

            return true;
        }
    }
}