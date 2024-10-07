using System;
using System.Windows;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailViewer.Data;
using EmailViewer.Models;
using BCrypt.Net;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth;
using Google.Apis.Util.Store;
using Google.Apis.Util;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using EmailViewer.Services;
using EmailViewer.Helpers;

namespace EmailViewer
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        private static string ClientId => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        private static string ClientSecret => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
        private readonly UserService _userService;

        public User LoggedInUser { get; private set; }

        // Initialize RateLimiter: 3 attempts per 5 minutes
        private readonly RateLimiter _rateLimiter = new RateLimiter(TimeSpan.FromMinutes(5), 3);

        public LoginWindow()
        {
            InitializeComponent();
            _userService = new UserService(new AppDbContext());
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = await _userService.AuthenticateUser(EmailTextBox.Text, PasswordBox.Password);
                if (user != null)
                {
                    LoginSuccessful(user);
                }
                else
                {
                    MessageBox.Show("Invalid email or password");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during login: {ex.Message}");
            }
        }

        private async void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = await _userService.AuthenticateGoogleUser();
                GoogleLoginSuccessful(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during Google login: {ex.Message}");
            }
        }

        private void LoginSuccessful(User user)
        {
            LoggedInUser = user;
            if (RememberMeCheckBox.IsChecked == true)
            {
                _userService.SaveRememberMeToken(user);
            }
            DialogResult = true;
            Close();
        }

        private void GoogleLoginSuccessful(User user)
        {
            LoggedInUser = user;
            _userService.SaveRememberMeToken(user);
            DialogResult = true;
            Close();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            if (registerWindow.ShowDialog() == true)
            {
                MessageBox.Show("Registration successful. Please log in.");
            }
        }
    }
}