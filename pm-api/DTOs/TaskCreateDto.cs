using System.ComponentModel.DataAnnotations;

namespace pm_api.DTOs
{
    public class TaskCreateDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        public DateTime? DueDate { get; set; }
    }
}
