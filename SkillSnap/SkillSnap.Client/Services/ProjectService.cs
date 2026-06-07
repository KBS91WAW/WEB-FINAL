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
            try
            {
                return await _http.GetFromJsonAsync<List<Project>>("api/projects") ?? new List<Project>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching projects: {ex.Message}");
                return new List<Project>();
            }
        }

        /// <summary>Adds a new project. Requires Admin role; attaches Bearer token.</summary>
        public async Task<bool> AddProjectAsync(Project newProject)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/projects");
                request.Content = JsonContent.Create(newProject);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding project: {ex.Message}");
                return false;
            }
        }

        /// <summary>Updates an existing project. Requires Admin role.</summary>
        public async Task<bool> UpdateProjectAsync(int id, Project project)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/projects/{id}");
                request.Content = JsonContent.Create(project);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating project: {ex.Message}");
                return false;
            }
        }

        /// <summary>Deletes a project by id. Requires Admin role.</summary>
        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                var token = await _auth.GetTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/projects/{id}");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting project: {ex.Message}");
                return false;
            }
        }
    }
}
