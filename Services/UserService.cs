using System;
using System.Threading;
using System.Threading.Tasks;
using EmailViewer.Data;
using EmailViewer.Models;
using EmailViewer.Helpers;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth;
using Google.Apis.Util.Store;
using BCrypt.Net;

namespace EmailViewer.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly RateLimiter _rateLimiter;
        private static string ClientId => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        private static string ClientSecret => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        public UserService(AppDbContext context)
        {
            _context = context;
            _rateLimiter = new RateLimiter(TimeSpan.FromMinutes(5), 3);
        }

        public async Task<User> AuthenticateUser(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public async Task<User> AuthenticateGoogleUser()
        {
            if (!_rateLimiter.ShouldAllow("GoogleLogin"))
            {
                Logger.Log("Too many login attempts. Please try again later.");
                throw new Exception("Too many login attempts. Please try again later.");
            }

            try
            {
                if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
                {
                    Logger.Log("Google Client ID or Client Secret is missing. Please check your environment variables.");
                    throw new Exception("Google Client ID or Client Secret is missing. Please check your application settings.");
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

                var credential = await new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync("user", CancellationToken.None);

                if (credential == null || string.IsNullOrEmpty(credential.Token.IdToken))
                {
                    Logger.Log("Failed to obtain Google credential.");
                    throw new Exception("Failed to authenticate with Google. Please try again.");
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
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()) // Generate a random password hash
                    };
                    _context.Users.Add(user);
                }
                else if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = userInfo.Subject;
                }

                await _context.SaveChangesAsync();
                Logger.Log($"User processed for Google account: {userInfo.Email}");

                return user;
            }
            catch (Exception ex)
            {
                Logger.Log($"Google login failed: {ex.Message}");
                throw;
            }
        }

        public async Task<User> RegisterUser(string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                throw new Exception("Email already in use");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task UpdateUserProfile(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public void SaveRememberMeToken(User user)
        {
            user.RememberMeToken = Guid.NewGuid().ToString();
            _context.SaveChanges();
            AuthManager.SaveAuthToken(user.RememberMeToken);
        }

        public async Task<User> GetUserByRememberMeToken(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.RememberMeToken == token);
        }

        public void ClearRememberMeToken()
        {
            AuthManager.ClearAuthToken();
        }
    }
}