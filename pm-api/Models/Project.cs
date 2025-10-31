using System.ComponentModel.DataAnnotations;

namespace pm_api.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int OwnerId { get; set; }

        public User? Owner { get; set; }

        public List<TaskItem> Tasks { get; set; } = new();
    }
}
