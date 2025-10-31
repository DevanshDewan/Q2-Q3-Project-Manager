using System.ComponentModel.DataAnnotations;

namespace pm_api.DTOs
{
    public class ProjectUpdateDto
    {
        [Required, MinLength(3), MaxLength(100)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
