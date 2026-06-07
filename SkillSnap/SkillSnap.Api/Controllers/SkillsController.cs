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
    public class SkillsController : ControllerBase
    {
        private readonly SkillSnapContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SkillsController> _logger;

        public SkillsController(SkillSnapContext context, IMemoryCache cache, ILogger<SkillsController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSkills()
        {
            var sw = Stopwatch.StartNew();

            if (!_cache.TryGetValue("skills", out List<Skill>? skills))
            {
                _logger.LogInformation("Cache miss for 'skills'. Querying database.");
                skills = await _context.Skills
                    .AsNoTracking()
                    .Include(s => s.PortfolioUser)
                    .ToListAsync();
                _cache.Set("skills", skills, TimeSpan.FromMinutes(5));
            }
            else
            {
                _logger.LogInformation("Cache hit for 'skills'.");
            }

            sw.Stop();
            _logger.LogInformation("GetSkills completed in {ElapsedMs}ms.", sw.ElapsedMilliseconds);

            return Ok(skills);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Skill>> AddSkill(Skill skill)
        {
            if (skill.PortfolioUserId == 0)
            {
                var portfolioUser = await _context.PortfolioUsers.FirstOrDefaultAsync();
                if (portfolioUser is null)
                    return BadRequest("No portfolio user exists. Seed sample data first.");
                skill.PortfolioUserId = portfolioUser.Id;
            }

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            _cache.Remove("skills");
            skill.PortfolioUser = null;
            return CreatedAtAction(nameof(GetSkills), new { id = skill.Id }, skill);
        }
    }
}
