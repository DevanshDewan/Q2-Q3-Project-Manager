using System.ComponentModel.DataAnnotations;

namespace pm_api.DTOs
{
    public class RegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        public string Password { get; set; } = null!;

        [MaxLength(100)]
        public string? FullName { get; set; }
    }
}
