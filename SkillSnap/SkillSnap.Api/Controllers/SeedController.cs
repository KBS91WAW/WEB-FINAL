using Microsoft.AspNetCore.Mvc;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;

namespace SkillSnap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public SeedController(SkillSnapContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Seed()
        {
            if (_context.PortfolioUsers.Any())
            {
                return BadRequest("Sample data already exists.");
            }

            var user = new PortfolioUser
            {
                Name = "Jordan Developer",
                Bio = "Full-stack developer passionate about learning new tech.",
                ProfileImageUrl = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Ccircle cx='60' cy='60' r='60' fill='%236366f1'/%3E%3Ctext x='60' y='60' font-size='40' font-weight='bold' fill='white' text-anchor='middle' dominant-baseline='middle'%3EJD%3C/text%3E%3C/svg%3E",
                Projects = new List<Project>
                {
                    new Project { Title = "Task Tracker", Description = "Manage tasks effectively", ImageUrl = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Crect width='120' height='120' fill='%238b5cf6'/%3E%3Ctext x='60' y='60' font-size='40' font-weight='bold' fill='white' text-anchor='middle' dominant-baseline='middle'%3E✓%3C/text%3E%3C/svg%3E" },
                    new Project { Title = "Weather App", Description = "Forecast weather using APIs", ImageUrl = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Crect width='120' height='120' fill='%233b82f6'/%3E%3Ctext x='60' y='60' font-size='40' font-weight='bold' fill='white' text-anchor='middle' dominant-baseline='middle'%3E☀%3C/text%3E%3C/svg%3E" }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "C#", Level = "Advanced" },
                    new Skill { Name = "Blazor", Level = "Intermediate" }
                }
            };

            _context.PortfolioUsers.Add(user);
            _context.SaveChanges();
            return Ok("Sample data inserted.");
        }
    }
}
