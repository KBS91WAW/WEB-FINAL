using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace SkillSnap.Client.Services
{
    /// <summary>Manages authentication: registration, login, logout, and token storage.</summary>
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private readonly UserSessionService _session;

        public AuthService(HttpClient http, IJSRuntime js, UserSessionService session)
        {
            _http = http;
            _js = js;
            _session = session;
        }

        /// <summary>Registers a new user with the given email and password.</summary>
        public async Task<bool> RegisterAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new { email, password });
            return response.IsSuccessStatusCode;
        }

        /// <summary>Logs in a user, stores the JWT in localStorage, and populates the session.</summary>
        public async Task<string?> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result?.Token is not null)
            {
                await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                PopulateSession(result.Token);
            }

            return result?.Token;
        }

        /// <summary>Removes the JWT from localStorage and clears the session.</summary>
        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            _session.Clear();
        }

        /// <summary>Returns the stored JWT from localStorage, or null if not logged in.</summary>
        public async Task<string?> GetTokenAsync()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", "authToken");
        }

        /// <summary>Restores the session from localStorage on page load.</summary>
        public async Task InitializeAsync()
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                PopulateSession(token);
        }

        private void PopulateSession(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return;

            var jwt = handler.ReadJwtToken(token);
            var userId = jwt.Subject
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? string.Empty;
            var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? string.Empty;
            _session.SetUser(userId, role);
        }

        private record TokenResponse(string Token);
    }
}
