using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using pm_api.Data;
using pm_api.DTOs.Scheduler;
using pm_api.Services;

namespace pm_api.Controllers
{
    [ApiController]
    [Route("api/Projects/{projectId:int}/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ISchedulerService _scheduler;

        public ScheduleController(AppDbContext db, ISchedulerService scheduler)
        {
            _db = db;
            _scheduler = scheduler;
        }

        private int? CurrentUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(sub, out var id)) return null;
            return id;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ComputeSchedule(int projectId, [FromBody] SchedulerRequestDto request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            // ensure project belongs to user
            var exists = await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (!exists) return NotFound();

            var result = await _scheduler.ComputeScheduleAsync(request);
            return Ok(result);
        }
    }
}
