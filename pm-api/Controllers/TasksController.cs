using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using pm_api.Data;
using pm_api.DTOs;
using pm_api.Models;

namespace pm_api.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId:int}/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TasksController(AppDbContext db) { _db = db; }

        private int? CurrentUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(sub, out var id)) return null;
            return id;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetTasks(int projectId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (!projectExists) return NotFound();

            var tasks = await _db.Tasks
                .Where(t => t.ProjectId == projectId)
                .Select(t => new { t.Id, t.Title, t.DueDate, t.IsCompleted })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CreateTask(int projectId, [FromBody] TaskCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return NotFound();

            var task = new TaskItem
            {
                Title = dto.Title,
                DueDate = dto.DueDate,
                ProjectId = projectId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { projectId = projectId, id = task.Id }, new { task.Id, task.Title, task.DueDate, task.IsCompleted });
        }

        [HttpGet("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetTask(int projectId, int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var task = await _db.Tasks.Include(t => t.Project)
                        .FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project!.OwnerId == userId);

            if (task == null) return NotFound();

            return Ok(new { task.Id, task.Title, task.DueDate, task.IsCompleted });
        }

        [HttpPut("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateTask(int projectId, int id, [FromBody] TaskUpdateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var task = await _db.Tasks.Include(t => t.Project)
                        .FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project!.OwnerId == userId);

            if (task == null) return NotFound();

            task.Title = dto.Title;
            task.DueDate = dto.DueDate;
            task.IsCompleted = dto.IsCompleted;

            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteTask(int projectId, int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var task = await _db.Tasks.Include(t => t.Project)
                        .FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project!.OwnerId == userId);

            if (task == null) return NotFound();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
