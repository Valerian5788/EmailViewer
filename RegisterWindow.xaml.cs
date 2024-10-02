using System.Windows;
using System.Linq;
using EmailViewer.Data;
using EmailViewer.Models;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace EmailViewer
{
    public partial class RegisterWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (await _context.Users.AnyAsync(u => u.Email == EmailTextBox.Text))
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
                await _context.SaveChangesAsync();

                // Show FirstTimeSetupWindow
                var firstTimeSetupWindow = new FirstTimeSetupWindow(user);
                if (firstTimeSetupWindow.ShowDialog() == true)
                {
                    user = firstTimeSetupWindow.User;
                    await _context.SaveChangesAsync();

                    MessageBox.Show("Registration and setup completed successfully!");

                    // Create and show the MainWindow
                    var mainWindow = new MainWindow(user);
                    mainWindow.Show();

                    // Close the current window (RegisterWindow)
                    this.Close();
                }
                else
                {
                    // If the user cancels the FirstTimeSetupWindow, we should probably delete the user
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    MessageBox.Show("Registration cancelled.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during registration: {ex.Message}");
                Logger.Log($"Registration error: {ex}");
            }
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