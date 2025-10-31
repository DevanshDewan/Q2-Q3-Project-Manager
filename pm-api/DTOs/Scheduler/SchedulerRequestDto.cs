using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace pm_api.DTOs.Scheduler
{
    // A single task item in the scheduler request.
    public class SchedulerTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public int? EstimatedHours { get; set; }

        public DateTime? DueDate { get; set; }

        // List of titles this task depends on (by title)
        public List<string> Dependencies { get; set; } = new();
    }

    // Request DTO for schedule endpoint
    public class SchedulerRequestDto
    {
        [Required]
        public List<SchedulerTaskDto> Tasks { get; set; } = new();
    }
}
