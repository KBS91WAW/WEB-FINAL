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
            bool cacheHit = _cache.TryGetValue("projects", out List<Project>? projects);

            if (!cacheHit)
            {
                _logger.LogInformation("[Cache MISS] 'projects' — querying database.");
                projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.PortfolioUser)
                    .ToListAsync();
                _cache.Set("projects", projects, TimeSpan.FromMinutes(5));
            }
            else
            {
                _logger.LogInformation("[Cache HIT]  'projects' — served from memory.");
            }

            sw.Stop();
            _logger.LogInformation("GetProjects completed in {ElapsedMs}ms. Items={Count}",
                sw.ElapsedMilliseconds, projects?.Count ?? 0);

            Response.Headers["X-Cache"] = cacheHit ? "HIT" : "MISS";
            Response.Headers["X-Cache-Items"] = (projects?.Count ?? 0).ToString();
            Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
            return Ok(projects);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Project>> AddProject(Project project)
        {
            if (project.PortfolioUserId == 0)
            {
                var portfolioUser = await _context.PortfolioUsers.FirstOrDefaultAsync();
                if (portfolioUser is null)
                    return BadRequest("No portfolio user exists. Seed sample data first.");
                project.PortfolioUserId = portfolioUser.Id;
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            InvalidateCache("projects", "POST");
            project.PortfolioUser = null;
            return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, Project project)
        {
            var existing = await _context.Projects.FindAsync(id);
            if (existing is null) return NotFound();

            existing.Title = project.Title;
            existing.Description = project.Description;
            existing.ImageUrl = project.ImageUrl;

            await _context.SaveChangesAsync();
            InvalidateCache("projects", "PUT");
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var existing = await _context.Projects.FindAsync(id);
            if (existing is null) return NotFound();

            _context.Projects.Remove(existing);
            await _context.SaveChangesAsync();
            InvalidateCache("projects", "DELETE");
            return NoContent();
        }

        private void InvalidateCache(string key, string operation)
        {
            _cache.Remove(key);
            _logger.LogInformation("[Cache INVALIDATED] key='{Key}' by {Operation}.", key, operation);
            Response.Headers["X-Cache-Invalidated"] = key;
        }
    }
}
