using DotNetEnv;
using System;
using System.IO;
using System.Windows;
using EmailViewer.Models;
using EmailViewer.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Newtonsoft.Json;
using EmailViewer.Services;
using EmailViewer.Helpers;

namespace EmailViewer
{
    public partial class App : Application
    {
        private const string MutexName = "EmailViewerSingleInstanceMutex";
        private Mutex _mutex;

        private MainWindow _mainWindow;
        private string _configPassword;
        private AppDbContext _context;
        private User _currentUser;
        private string _clickUpApiKey;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show("An instance of the application is already running.", "Multiple Instances", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _context = new AppDbContext();

            // Load environment variables
            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secretkeys.env");
            DotNetEnv.Env.Load(envFilePath);
            VerifyEnvironmentVariables();

            // Attempt auto-login
            if (AttemptAutoLogin())
            {
                // User was automatically logged in
                ShowMainWindow();
            }
            else
            {
                // Show login window
                var loginWindow = new LoginWindow();
                if (loginWindow.ShowDialog() == true)
                {
                    _currentUser = loginWindow.LoggedInUser;

                    // Check for first-time setup
                    bool isFirstTimeSetup = IsFirstTimeSetup();
                    if (isFirstTimeSetup)
                    {
                        if (!PerformFirstTimeSetup())
                        {
                            Shutdown();
                            return;
                        }
                    }

                    ShowMainWindow();
                }
                else
                {
                    Shutdown();
                    return;
                }
            }

            Logger.Log("Application startup completed successfully");
        }

        private bool PerformFirstTimeSetup()
        {
            var firstTimeSetupWindow = new FirstTimeSetupWindow(_currentUser);
            if (firstTimeSetupWindow.ShowDialog() != true)
            {
                return false;
            }

            _currentUser = firstTimeSetupWindow.User;
            _configPassword = firstTimeSetupWindow.ConfigPassword;
            _clickUpApiKey = firstTimeSetupWindow.ClickUpApiKey;
            SaveSecureConfiguration();
            return true;
        }

        private void ShowMainWindow()
        {
            Logger.Log($"Showing MainWindow for user: {_currentUser.Email}");
            _mainWindow = new MainWindow(_currentUser);
            _mainWindow.Closed += (s, args) => Shutdown();
            _mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }



        private bool IsFirstTimeSetup()
        {
            bool configFileExists = File.Exists("config.enc");
            bool usersExist = _context.Users.Any();

            Logger.Log($"Config file exists: {configFileExists}\nUsers exist: {usersExist}");

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

        public static string LoadKeyFromSecureConfiguration(string configPassword)
        {
            try
            {
                string clickUpApiKey = SecureStorage.GetEncrypted("CLICKUP_APIKEY", configPassword);
                return clickUpApiKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}");
                return null;
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
                if (_currentUser != null)
                {
                    Logger.Log($"Auto-login successful for user: {_currentUser.Email}");
                    return true;
                }
            }
            Logger.Log("Auto-login failed or no token found");
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