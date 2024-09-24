using System.ComponentModel.DataAnnotations;

namespace EmailViewer.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string GoogleId { get; set; }
        public string OneDriveRootPath { get; set; }
        public string DefaultRootPath { get; set; }
        public string ClickUpApiKey { get; set; }
        public string ClickUpListId { get; set; }
        public string ClickUpUserId { get; set; }
        public string RememberMeToken { get; set; }
    }
}