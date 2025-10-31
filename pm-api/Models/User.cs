using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pm_api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [JsonIgnore]
        public List<Project> Projects { get; set; } = new();
    }
}
