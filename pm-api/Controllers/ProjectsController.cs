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
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProjectsController(AppDbContext db) { _db = db; }

        private int? CurrentUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(sub, out var id)) return null;
            return id;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetMyProjects()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var projects = await _db.Projects
                .Where(p => p.OwnerId == userId)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(projects);
        }

        [HttpGet("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetProject(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var project = await _db.Projects.Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (project == null) return NotFound();

            return Ok(new
            {
                project.Id,
                project.Title,
                project.Description,
                project.CreatedAt,
                tasks = project.Tasks.Select(t => new { t.Id, t.Title, t.DueDate, t.IsCompleted })
            });
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var project = new Project
            {
                Title = dto.Title,
                Description = dto.Description,
                OwnerId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new { project.Id, project.Title, project.Description, project.CreatedAt });
        }

        [HttpPut("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] ProjectUpdateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
            if (project == null) return NotFound();

            project.Title = dto.Title;
            project.Description = dto.Description;

            _db.Projects.Update(project);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
            if (project == null) return NotFound();

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
