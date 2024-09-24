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

namespace EmailViewer
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        private string ClientId => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        private string ClientSecret => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
        public User LoggedInUser { get; private set; }

        // Initialize RateLimiter: 3 attempts per 5 minutes
        private readonly RateLimiter _rateLimiter = new RateLimiter(TimeSpan.FromMinutes(5), 3);

        public LoginWindow()
        {
            InitializeComponent();
            AttemptAutoLogin();
        }

        private void AttemptAutoLogin()
        {
            string token = AuthManager.LoadAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                var user = _context.Users.FirstOrDefault(u => u.RememberMeToken == token);
                if (user != null)
                {
                    LoggedInUser = user;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_rateLimiter.ShouldAllow(EmailTextBox.Text))
            {
                MessageBox.Show("Too many login attempts. Please try again later.");
                return;
            }

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
                MessageBox.Show("Too many login attempts. Please try again later.");
                return;
            }

            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = new[] { "email", "profile" },
                    DataStore = new FileDataStore("GoogleAuth")
                });

                var token = await flow.LoadTokenAsync("user", CancellationToken.None);

                if (token == null || token.IsExpired(SystemClock.Default))
                {
                    var credential = await new Google.Apis.Auth.OAuth2.AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync("user", CancellationToken.None);
                    token = credential.Token;
                }

                var userInfo = await GoogleJsonWebSignature.ValidateAsync(token.IdToken);
                var user = _context.Users.FirstOrDefault(u => u.Email == userInfo.Email);

                if (user == null)
                {
                    user = new User { Email = userInfo.Email };
                    _context.Users.Add(user);
                    _context.SaveChanges();
                }

                LoginSuccessful(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Google login failed: {ex.Message}");
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