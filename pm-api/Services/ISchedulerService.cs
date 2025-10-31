using System.Threading.Tasks;
using pm_api.DTOs.Scheduler;

namespace pm_api.Services
{
    public interface ISchedulerService
    {
        /// <summary>
        /// Compute a recommended order of task titles given the request.
        /// Returns SchedulerResponseDto with RecommendedOrder and Diagnostics.
        /// </summary>
        Task<SchedulerResponseDto> ComputeScheduleAsync(SchedulerRequestDto request);
    }
}
