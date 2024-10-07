# EmailViewer - Advanced Email Management for CEOs


## Project Overview

EmailViewer is a sophisticated email management application designed specifically for CEOs and business leaders who need to efficiently handle large volumes of emails while integrating with task management and calendar systems. This application is currently under active development and aims to streamline email workflows, enhance productivity, and provide seamless integration with popular business tools.

## Key Features

1. **Secure Email Viewing and Management**
   - View and organize emails from multiple folders
   - Recent emails list for quick access
   - Advanced search functionality using Lucene.NET for fast and efficient email retrieval

2. **Robust User Authentication**
   - Secure login with email and password
   - Google OAuth integration for seamless access
   - Auto-login capability with remember me token
   - Logout functionality for security

3. **Note Management System**
   - Add, edit, and delete notes associated with specific emails
   - Tag notes for easy categorization and retrieval
   - Custom tagging system for personalized organization

4. **Task Creation and Integration**
   - Create tasks directly from emails
   - Integration with ClickUp for advanced task management
   - Custom task creation window with detailed options

5. **Calendar Integration**
   - Quick add events to Google Calendar
   - Create detailed calendar events from email content

6. **Advanced Email Indexing**
   - Utilizes Lucene.NET for fast and efficient email indexing and searching
   - Improves performance when dealing with large volumes of emails

7. **OneDrive Integration**
   - Set OneDrive root path for centralized email storage
   - Ensures consistency across devices and improves data backup

8. **User Profile Management**
   - Store and utilize user-specific settings
   - Customize application behavior based on individual preferences

9. **Logging and Debugging**
   - Comprehensive logging system for tracking important events and errors
   - Facilitates easier troubleshooting and application maintenance

10. **Rate Limiting**
    - Implements rate limiting for login attempts to enhance security

11. **Intuitive User Interface**
    - Collapsible search panel for a cleaner workspace
    - Grid view for search results and notes
    - Rich text display for email content

## Security Measures

EmailViewer takes security seriously, implementing several measures to protect user data:

1. **Encrypted Storage**: Sensitive information like API keys are stored in encrypted form.
2. **Secure Authentication**: Utilizes BCrypt for password hashing and supports OAuth 2.0 for Google integration.
3. **Rate Limiting**: Prevents brute-force attacks by limiting login attempts.
4. **Secure Configuration**: Uses a separate configuration file for sensitive data, which is not tracked in version control.
5. **HTTPS Communication**: All API calls (e.g., to ClickUp) are made over HTTPS.

## Development Status

EmailViewer is currently under active development. Key areas of ongoing work include:

1. Refining the user interface for improved usability
2. Enhancing integration with third-party services
3. Optimizing performance for handling larger email volumes
4. Implementing additional security features
5. Expanding the note-taking and task management capabilities

## Technical Details

EmailViewer is built using the following technologies:

- **Framework**: .NET 8.0 with WPF for the user interface
- **Language**: C#
- **Database**: Microsoft SQL Server (via Entity Framework Core)
- **Email Parsing**: MimeKit
- **Search Engine**: Lucene.NET
- **External APIs**: Google Calendar API, ClickUp API
- **Authentication**: BCrypt.NET, Google OAuth 2.0
- **Serialization**: Newtonsoft.Json

## Getting Started

To get started with EmailViewer, you'll need:

- A ClickUp API Key
- A well-organized folder structure for your emails (hybrid solution coming soon)

## Contributing

As this project is under active development, contributions are welcome. Please contact the project maintainers for more information on how to contribute.

## License

No license is currently specified for this project.

## Contact

- Email: valerianvdc.pro@gmail.com
- LinkedIn: [Valérian Vandercamme](https://www.linkedin.com/in/val%C3%A9rian-vandercamme-72a7681b5/)

---

<sub>EmailViewer - Empowering CEOs with Advanced Email Management</sub>
