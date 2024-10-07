using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using EmailViewer.Helpers;
using EmailViewer.Models;

namespace EmailViewer.Services
{
    public class EmailViewerCalendarService
    {
        private readonly User currentUser;

        public EmailViewerCalendarService(User user)
        {
            currentUser = user;
        }

        public async Task<CalendarService> InitializeCalendarServiceAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(currentUser.GoogleId))
                {
                    Logger.Log("No Google ID found for the current user");
                    return null;
                }

                var clientSecrets = new ClientSecrets
                {
                    ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
                    ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                };

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    new[] { CalendarService.Scope.CalendarEvents },
                    currentUser.GoogleId,
                    CancellationToken.None,
                    new FileDataStore("Calendar.ApiClient"));

                return new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Email Viewer",
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing Google Calendar service: {ex.Message}");
                return null;
            }
        }

        public async Task<Event> CreateEventAsync(string title, string description, DateTime startDateTime, DateTime endDateTime)
        {
            var calendarService = await InitializeCalendarServiceAsync();
            if (calendarService == null)
            {
                throw new InvalidOperationException("Failed to initialize calendar service. Please check your Google authentication.");
            }

            var newEvent = new Event
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime { DateTime = startDateTime },
                End = new EventDateTime { DateTime = endDateTime },
            };

            return await calendarService.Events.Insert(newEvent, "primary").ExecuteAsync();
        }

        public void QuickAddToCalendar(string subject, string body, DateTime emailDate)
        {
            try
            {
                subject = subject ?? "No Subject";
                body = body ?? "";

                // Limit the body length to avoid excessively long URLs
                if (body.Length > 500)
                {
                    body = body.Substring(0, 500) + "...";
                }

                string encodedSubject = Uri.EscapeDataString(subject);
                string encodedBody = Uri.EscapeDataString(body);

                string date = emailDate.ToString("yyyyMMdd");

                string url = $"https://www.google.com/calendar/render?action=TEMPLATE&text={encodedSubject}&details={encodedBody}&dates={date}/{date}";

                // Use ProcessStartInfo to open the default browser
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in QuickAddToCalendar: {ex}");
                throw;
            }
        }
    }
}