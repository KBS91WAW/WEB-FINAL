using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Api.Models
{
    public class PortfolioUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(2048)]
        public string ProfileImageUrl { get; set; } = string.Empty;

        public List<Project> Projects { get; set; } = new();
        public List<Skill> Skills { get; set; } = new();
    }
}
