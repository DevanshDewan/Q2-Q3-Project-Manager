using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pm_api.Data;
using pm_api.DTOs;
using pm_api.Models;
using pm_api.Services;
using System.Security.Cryptography;
using System.Text;

namespace pm_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwt;

        public AuthController(AppDbContext db, IJwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existing != null) return Conflict(new { message = "Email already registered" });

            var hash = HashPassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hash,
                FullName = dto.FullName
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new { user.Id, user.Email, user.FullName });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }

        // simple salted hash using SHA256 — fine for the assignment
        private static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var sha = SHA256.Create();
            var combined = salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
            var hash = sha.ComputeHash(combined);
            var payload = salt.Concat(hash).ToArray();
            return Convert.ToBase64String(payload);
        }

        private static bool VerifyPassword(string password, string stored)
        {
            try
            {
                var payload = Convert.FromBase64String(stored);
                var salt = payload[..16];
                var storedHash = payload[16..];

                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
                return hash.SequenceEqual(storedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
