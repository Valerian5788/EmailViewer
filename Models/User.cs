using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailViewer.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        [MaxLength(255)]
        public string? GoogleId { get; set; }

        [MaxLength(500)]
        public string? OneDriveRootPath { get; set; }

        [MaxLength(500)]
        public string? DefaultRootPath { get; set; }

        [MaxLength(255)]
        public string? ClickUpApiKey { get; set; }

        [MaxLength(255)]
        public string? ClickUpListId { get; set; }

        [MaxLength(255)]
        public string? ClickUpUserId { get; set; }

        [MaxLength(255)]
        public string? RememberMeToken { get; set; }
    }
}