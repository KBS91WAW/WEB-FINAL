using System.Net.Http.Headers;
using System.Net.Http.Json;
using SkillSnap.Client.Models;

namespace SkillSnap.Client.Services
{
    /// <summary>Handles API calls for Project resources, attaching the JWT token when available.</summary>
    public class ProjectService
    {
        private readonly HttpClient _http;
        private readonly AuthService _auth;

        public ProjectService(HttpClient http, AuthService auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>Fetches all projects from the API.</summary>
        public async Task<List<Project>> GetProjectsAsync()
        {
            return await _http.GetFromJsonAsync<List<Project>>("api/projects") ?? new List<Project>();
        }

        /// <summary>Adds a new project. Requires Admin role; attaches Bearer token.</summary>
        public async Task<bool> AddProjectAsync(Project newProject)
        {
            var token = await _auth.GetTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/projects");
            request.Content = JsonContent.Create(newProject);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}
