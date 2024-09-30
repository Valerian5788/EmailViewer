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
        private string _configPassword;
        private AppDbContext _context;
        private User _currentUser;
        private string _clickUpApiKey;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _context = new AppDbContext();

            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secretkeys.env");
            DotNetEnv.Env.Load(envFilePath);
            VerifyEnvironmentVariables();

            if (IsFirstTimeSetup())
            {
                ShowFirstTimeSetupWindow();
            }
            else
            {
                if (!PromptForConfigPassword())
                {
                    Shutdown();
                    return;
                }

                if (!AttemptAutoLogin())
                {
                    ShowLoginWindow();
                }
                else
                {
                    LoadSecureConfiguration();
                    StartMainApplication();
                }
            }
        }

        private bool IsFirstTimeSetup()
        {
            return !File.Exists("config.enc") || !_context.Users.Any();
        }

        private void ShowFirstTimeSetupWindow()
        {
            var setupWindow = new FirstTimeSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                _currentUser = setupWindow.User;
                _configPassword = setupWindow.ConfigPassword;
                _clickUpApiKey = setupWindow.ClickUpApiKey;
                SaveSecureConfiguration();
                StartMainApplication();
            }
            else
            {
                Shutdown();
            }
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

        private void ShowLoginWindow()
        {
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                _currentUser = loginWindow.LoggedInUser;
                StartMainApplication();
            }
            else
            {
                Shutdown();
            }
        }

        private void StartMainApplication()
        {
            MainWindow mainWindow = new MainWindow(_currentUser);
            mainWindow.Show();
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