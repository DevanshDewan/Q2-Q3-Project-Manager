using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace pm_api.Dtos
{
    // Single task DTO used by request
    public class SchedulerTaskDto
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = null!;

        public int? EstimatedHours { get; set; }

        public DateTime? DueDate { get; set; }

        public List<string>? Dependencies { get; set; } = new();
    }

    // Alias name used by some files: ScheduleTaskDto (in case some code references it)
    public class ScheduleTaskDto : SchedulerTaskDto { }

    // Primary request DTO - controller expects SchedulerRequestDto
    public class SchedulerRequestDto
    {
        [Required]
        [MinLength(1)]
        public List<SchedulerTaskDto> Tasks { get; set; } = new();
    }

    // Some code expects ScheduleRequestDto (note singular/plural/name differences).
    // Create it here as identical to SchedulerRequestDto so both names compile.
    public class ScheduleRequestDto : SchedulerRequestDto { }

    // Response DTO that your controller/service should return
    public class SchedulerResponseDto
    {
        public List<string> RecommendedOrder { get; set; } = new();
        public List<string> Diagnostics { get; set; } = new();
    }

    // Backwards-compatible alias: ScheduleResultDto
    public class ScheduleResultDto : SchedulerResponseDto { }
}
