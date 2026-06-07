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
            try
            {
                return await _http.GetFromJsonAsync<List<Skill>>("api/skills") ?? new List<Skill>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching skills: {ex.Message}");
                return new List<Skill>();
            }
        }

        /// <summary>Adds a new skill. Requires Admin role; attaches Bearer token.</summary>
        public async Task<bool> AddSkillAsync(Skill newSkill)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/skills");
                request.Content = JsonContent.Create(newSkill);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding skill: {ex.Message}");
                return false;
            }
        }

        /// <summary>Updates an existing skill. Requires Admin role.</summary>
        public async Task<bool> UpdateSkillAsync(int id, Skill skill)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/skills/{id}");
                request.Content = JsonContent.Create(skill);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating skill: {ex.Message}");
                return false;
            }
        }

        /// <summary>Deletes a skill by id. Requires Admin role.</summary>
        public async Task<bool> DeleteSkillAsync(int id)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/skills/{id}");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting skill: {ex.Message}");
                return false;
            }
        }
    }
}
