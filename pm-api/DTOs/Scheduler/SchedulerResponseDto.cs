using System.Collections.Generic;

namespace pm_api.DTOs.Scheduler
{
    public class SchedulerResponseDto
    {
        // If the scheduler can compute an order, list of task titles in recommended order
        public List<string> RecommendedOrder { get; set; } = new();

        // Diagnostics lines: missing dependencies, cycles, or other notes
        public List<string> Diagnostics { get; set; } = new();
    }
}
