using System;
using System.Windows;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailViewer.Data;
using EmailViewer.Models;
using EmailViewer.Utilities;
using BCrypt.Net;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth;
using Google.Apis.Util.Store;
using Google.Apis.Util;
using Microsoft.EntityFrameworkCore;

namespace EmailViewer
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        private static string ClientId => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        private static string ClientSecret => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        public User LoggedInUser { get; private set; }

        // Initialize RateLimiter: 3 attempts per 5 minutes
        private readonly RateLimiter _rateLimiter = new RateLimiter(TimeSpan.FromMinutes(5), 3);

        public LoginWindow()
        {
            InitializeComponent();
        }



        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == EmailTextBox.Text);
            if (user != null && BCrypt.Net.BCrypt.Verify(PasswordBox.Password, user.PasswordHash))
            {
                LoginSuccessful(user);
            }
            else
            {
                MessageBox.Show("Invalid email or password");
            }
        }

        private async void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_rateLimiter.ShouldAllow("GoogleLogin"))
            {
                Logger.Log("Too many login attempts. Please try again later.");
                MessageBox.Show("Too many login attempts. Please try again later.");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
                {
                    Logger.Log("Google Client ID or Client Secret is missing. Please check your environment variables.");
                    MessageBox.Show("Google Client ID or Client Secret is missing. Please check your application settings.");
                    return;
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = new[] { "email", "profile", "https://www.googleapis.com/auth/calendar.events" },
                    DataStore = new FileDataStore("GoogleAuth")
                });

                await flow.DeleteTokenAsync("user", CancellationToken.None);

                var credential = await new Google.Apis.Auth.OAuth2.AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync("user", CancellationToken.None);

                if (credential == null || string.IsNullOrEmpty(credential.Token.IdToken))
                {
                    Logger.Log("Failed to obtain Google credential.");
                    MessageBox.Show("Failed to authenticate with Google. Please try again.");
                    return;
                }

                var userInfo = await GoogleJsonWebSignature.ValidateAsync(credential.Token.IdToken);
                Logger.Log($"Successfully authenticated Google user: {userInfo.Email}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = userInfo.Email,
                        GoogleId = userInfo.Subject,
                    };
                    _context.Users.Add(user);
                }
                else if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = userInfo.Subject;
                }

                try
                {
                    await _context.SaveChangesAsync();
                    Logger.Log($"User processed for Google account: {userInfo.Email}");
                }
                catch (DbUpdateException dbEx)
                {
                    Logger.Log($"Database update error: {dbEx.Message}");
                    Logger.Log($"Inner exception: {dbEx.InnerException?.Message}");
                    MessageBox.Show("An error occurred while saving user data. Please try again.");
                    return;
                }

                LoginSuccessful(user);
            }
            catch (Exception ex)
            {
                Logger.Log($"Google login failed: {ex.Message}");
                Logger.Log($"Exception type: {ex.GetType().Name}");
                Logger.Log($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Logger.Log($"Inner exception: {ex.InnerException.Message}");
                    Logger.Log($"Inner exception type: {ex.InnerException.GetType().Name}");
                    Logger.Log($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                MessageBox.Show($"An unexpected error occurred during Google login: {ex.Message}. Please try again.");
            }
        }

        private void LoginSuccessful(User user)
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