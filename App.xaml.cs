using DotNetEnv;
using System;
using System.IO;
using System.Windows;
using EmailViewer.Models;
using EmailViewer.Data;

namespace EmailViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secretkeys.env");
            DotNetEnv.Env.Load(envFilePath);

            // Verify that the variables are loaded
            string apiKey = Environment.GetEnvironmentVariable("CLICKUP_APIKEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Failed to load CLICKUP_APIKEY from environment variables.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            string emailId = ParseEmailIdFromArgs(e.Args);

            // Attempt auto-login
            User loggedInUser = AttemptAutoLogin();

            if (loggedInUser == null)
            {
                // Show login window if auto-login failed
                var loginWindow = new LoginWindow();
                if (loginWindow.ShowDialog() == true)
                {
                    loggedInUser = loginWindow.LoggedInUser;
                }
                else
                {
                    // User cancelled login, exit the application
                    Shutdown();
                    return;
                }
            }

            // Create and show the main window
            MainWindow mainWindow = new MainWindow(loggedInUser, emailId);
            mainWindow.Show();
        }

        private string ParseEmailIdFromArgs(string[] args)
        {
            if (args.Length > 0)
            {
                Logger.Log($"First argument: {args[0]}");

                if (args[0].StartsWith("emailviewer:"))
                {
                    string emailId = ParseEmailId(args[0]);
                    Logger.Log($"Parsed email ID from URL: {emailId}");
                    return emailId;
                }
                else if (args[0] == "--emailId" && args.Length > 1)
                {
                    Logger.Log($"Email ID from command line argument: {args[1]}");
                    return args[1];
                }
                else
                {
                    Logger.Log("Unrecognized argument format");
                }
            }

            return null;
        }

        private string ParseEmailId(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                return System.Web.HttpUtility.ParseQueryString(uri.Query).Get("id");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error parsing email ID: {ex.Message}");
                return null;
            }
        }

        private User AttemptAutoLogin()
        {
            string token = AuthManager.LoadAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.RememberMeToken == token);
                    if (user != null)
                    {
                        Logger.Log("Auto-login successful");
                        return user;
                    }
                }
            }
            Logger.Log("Auto-login failed or no token found");
            return null;
        }
    }
}