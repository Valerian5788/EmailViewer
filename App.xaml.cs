using DotNetEnv;
using System;
using System.IO;
using System.Windows;
using EmailViewer.Models;
using EmailViewer.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EmailViewer
{
    public partial class App : Application
    {
        private MainWindow _mainWindow;
        private string _configPassword;
        private AppDbContext _context;
        private User _currentUser;
        private string _clickUpApiKey;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _context = new AppDbContext();

            // Load environment variables
            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secretkeys.env");
            DotNetEnv.Env.Load(envFilePath);
            VerifyEnvironmentVariables();

            // Create and show login window
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() != true)  // Ensure login window is shown first
            {
                Shutdown(); // Shutdown only if login fails or is canceled
                return;
            }

            // Get the logged-in user
            _currentUser = loginWindow.LoggedInUser;
            if (_currentUser == null)
            {
                MessageBox.Show("Error: User is null after login.");
                Shutdown(); // Shutdown if no user is found after login
                return;
            }

            // Check for first-time setup
            bool isFirstTimeSetup = IsFirstTimeSetup();
            if (isFirstTimeSetup)
            {
                var firstTimeSetupWindow = new FirstTimeSetupWindow(_currentUser);
                if (firstTimeSetupWindow.ShowDialog() != true)  // Ensure first-time setup window is properly initialized
                {
                    Shutdown(); // Shutdown only if the first-time setup is canceled
                    return;
                }

                _currentUser = firstTimeSetupWindow.User;
                _configPassword = firstTimeSetupWindow.ConfigPassword;
                _clickUpApiKey = firstTimeSetupWindow.ClickUpApiKey;
                SaveSecureConfiguration();
            }
            Logger.Log("Before creating main window, the user is : " + _currentUser.Email );
            // Create and show main window
            if (_mainWindow == null)
            {
                Logger.Log("Creating MainWindow instance");
                _mainWindow = new MainWindow(_currentUser);
                _mainWindow.Show();
            }
            else
            {
                Logger.Log("MainWindow instance already exists");
            }
        }



        private bool IsFirstTimeSetup()
        {
            bool configFileExists = File.Exists("config.enc");
            bool usersExist = _context.Users.Any();

            MessageBox.Show($"Config file exists: {configFileExists}\nUsers exist: {usersExist}");

            return !configFileExists || !usersExist;
        }



        private bool PromptForConfigPassword()
        {
            var passwordWindow = new PasswordPromptWindow("Enter your configuration password");
            if (passwordWindow.ShowDialog() == true)
            {
                _configPassword = passwordWindow.Password;
                return true;
            }
            return false;
        }

        private void LoadSecureConfiguration()
        {
            try
            {
                var clickUpApiKey = SecureStorage.GetEncrypted("CLICKUP_APIKEY", _configPassword);
                Environment.SetEnvironmentVariable("CLICKUP_APIKEY", clickUpApiKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}");
                Shutdown();
            }
        }

        private void SaveSecureConfiguration()
        {
            SecureStorage.SaveEncrypted("CLICKUP_APIKEY", _clickUpApiKey, _configPassword);
        }

        private bool AttemptAutoLogin()
        {
            string token = AuthManager.LoadAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                _currentUser = _context.Users.FirstOrDefault(u => u.RememberMeToken == token);
                return _currentUser != null;
            }
            return false;
        }

        

        private void VerifyEnvironmentVariables()
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

            if (string.IsNullOrEmpty(clientId))
            {
                Logger.Log("GOOGLE_CLIENT_ID is not set in the environment variables.");
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                Logger.Log("GOOGLE_CLIENT_SECRET is not set in the environment variables.");
            }
        }
    }
}