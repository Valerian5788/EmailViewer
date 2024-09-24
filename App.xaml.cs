using DotNetEnv;
using System;
using System.IO;
using System.Windows;

namespace EmailViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secretkeys.env");
            DotNetEnv.Env.Load(envFilePath);

            // Optionally, verify that the variables are loaded
            string apiKey = Environment.GetEnvironmentVariable("CLICKUP_APIKEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Failed to load CLICKUP_APIKEY from environment variables.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            string emailId = null;

            Logger.Log($"Application starting with {e.Args.Length} arguments");

            if (e.Args.Length > 0)
            {
                Logger.Log($"First argument: {e.Args[0]}");

                if (e.Args[0].StartsWith("emailviewer:"))
                {
                    emailId = ParseEmailId(e.Args[0]);
                    Logger.Log($"Parsed email ID from URL: {emailId}");
                }
                else if (e.Args[0] == "--emailId" && e.Args.Length > 1)
                {
                    emailId = e.Args[1];
                    Logger.Log($"Email ID from command line argument: {emailId}");
                }
                else
                {
                    Logger.Log("Unrecognized argument format");
                }
            }

            MainWindow mainWindow;

            if (!string.IsNullOrEmpty(emailId))
            {
                Logger.Log($"Creating MainWindow with email ID: {emailId}");
                mainWindow = new MainWindow(emailId);
            }
            else
            {
                Logger.Log("Creating MainWindow without email ID");
                mainWindow = new MainWindow();
            }

            mainWindow.Show();
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
    }
}