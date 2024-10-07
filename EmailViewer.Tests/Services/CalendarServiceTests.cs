using System;
using System.Threading.Tasks;
using EmailViewer.Models;
using EmailViewer.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Moq;
using Xunit;
using EmailViewer.Helpers;

namespace EmailViewer.Tests.Services
{
    public class CalendarServiceTests
    {
        private readonly Mock<CalendarService> _mockCalendarService;
        private readonly User _testUser;
        private readonly EmailViewerCalendarService _calendarService;
        private readonly Mock<Func<string, bool>> _mockUrlOpener;

        public CalendarServiceTests()
        {
            _mockCalendarService = new Mock<CalendarService>();
            _testUser = new User { GoogleId = "test_google_id" };
            _mockUrlOpener = new Mock<Func<string, bool>>();
            _calendarService = new EmailViewerCalendarService(_testUser, _mockUrlOpener.Object);
        }

        [Fact]
        public async Task CreateEventAsync_NullCalendarService_ThrowsInvalidOperationException()
        {
            // Arrange
            var calendarService = new EmailViewerCalendarService(new User { GoogleId = null });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                calendarService.CreateEventAsync("Test", "Description", DateTime.Now, DateTime.Now.AddHours(1))
            );
        }

        [Fact]
        public void QuickAddToCalendar_ValidInput_ReturnsTrue()
        {
            // Arrange
            var subject = "Test Subject";
            var body = "Test Body";
            var emailDate = DateTime.Now;
            _mockUrlOpener.Setup(u => u(It.IsAny<string>())).Returns(true);

            // Act
            var result = _calendarService.QuickAddToCalendar(subject, body, emailDate);

            // Assert
            Assert.True(result);
            _mockUrlOpener.Verify(u => u(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void QuickAddToCalendar_LongBody_TruncatesBodyAndReturnsTrue()
        {
            // Arrange
            var subject = "Test Subject";
            var body = new string('a', 1000); // Create a string with 1000 'a' characters
            var emailDate = DateTime.Now;
            _mockUrlOpener.Setup(u => u(It.Is<string>(s => s.Contains("aaaaa...")))).Returns(true);

            // Act
            var result = _calendarService.QuickAddToCalendar(subject, body, emailDate);

            // Assert
            Assert.True(result);
            _mockUrlOpener.Verify(u => u(It.Is<string>(s => s.Contains("aaaaa..."))), Times.Once);
        }
    }
}