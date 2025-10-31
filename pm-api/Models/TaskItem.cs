using System.ComponentModel.DataAnnotations;

namespace pm_api.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}
