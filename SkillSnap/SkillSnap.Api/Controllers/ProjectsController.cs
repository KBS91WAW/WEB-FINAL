using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;

namespace SkillSnap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly SkillSnapContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(SkillSnapContext context, IMemoryCache cache, ILogger<ProjectsController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var sw = Stopwatch.StartNew();

            if (!_cache.TryGetValue("projects", out List<Project>? projects))
            {
                _logger.LogInformation("Cache miss for 'projects'. Querying database.");
                projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.PortfolioUser)
                    .ToListAsync();
                _cache.Set("projects", projects, TimeSpan.FromMinutes(5));
            }
            else
            {
                _logger.LogInformation("Cache hit for 'projects'.");
            }

            sw.Stop();
            _logger.LogInformation("GetProjects completed in {ElapsedMs}ms.", sw.ElapsedMilliseconds);

            return Ok(projects);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Project>> AddProject(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _cache.Remove("projects");
            return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
        }
    }
}
