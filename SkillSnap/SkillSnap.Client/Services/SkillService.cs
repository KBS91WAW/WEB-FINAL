using System.Net.Http.Headers;
using System.Net.Http.Json;
using SkillSnap.Client.Models;

namespace SkillSnap.Client.Services
{
    /// <summary>Handles API calls for Skill resources, attaching the JWT token when available.</summary>
    public class SkillService
    {
        private readonly HttpClient _http;
        private readonly AuthService _auth;

        public SkillService(HttpClient http, AuthService auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>Fetches all skills from the API.</summary>
        public async Task<List<Skill>> GetSkillsAsync()
        {
            return await _http.GetFromJsonAsync<List<Skill>>("api/skills") ?? new List<Skill>();
        }

        /// <summary>Adds a new skill. Requires Admin role; attaches Bearer token.</summary>
        public async Task AddSkillAsync(Skill newSkill)
        {
            var token = await _auth.GetTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/skills");
            request.Content = JsonContent.Create(newSkill);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _http.SendAsync(request);
        }
    }
}
