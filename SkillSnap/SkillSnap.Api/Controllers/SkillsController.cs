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
            bool cacheHit = _cache.TryGetValue("skills", out List<Skill>? skills);

            if (!cacheHit)
            {
                _logger.LogInformation("[Cache MISS] 'skills' — querying database.");
                skills = await _context.Skills
                    .AsNoTracking()
                    .Include(s => s.PortfolioUser)
                    .ToListAsync();
                _cache.Set("skills", skills, TimeSpan.FromMinutes(5));
            }
            else
            {
                _logger.LogInformation("[Cache HIT]  'skills' — served from memory.");
            }

            sw.Stop();
            _logger.LogInformation("GetSkills completed in {ElapsedMs}ms. Items={Count}",
                sw.ElapsedMilliseconds, skills?.Count ?? 0);

            Response.Headers["X-Cache"] = cacheHit ? "HIT" : "MISS";
            Response.Headers["X-Cache-Items"] = (skills?.Count ?? 0).ToString();
            Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
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
            InvalidateCache("skills", "POST");
            skill.PortfolioUser = null;
            return CreatedAtAction(nameof(GetSkills), new { id = skill.Id }, skill);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(int id, Skill skill)
        {
            var existing = await _context.Skills.FindAsync(id);
            if (existing is null) return NotFound();

            existing.Name = skill.Name;
            existing.Level = skill.Level;

            await _context.SaveChangesAsync();
            InvalidateCache("skills", "PUT");
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var existing = await _context.Skills.FindAsync(id);
            if (existing is null) return NotFound();

            _context.Skills.Remove(existing);
            await _context.SaveChangesAsync();
            InvalidateCache("skills", "DELETE");
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
