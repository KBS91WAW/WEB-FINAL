namespace SkillSnap.Client.Services
{
    public class UserSessionService
    {
        public string? UserId { get; private set; }
        public string? Role { get; private set; }
        public string? CurrentProjectTitle { get; private set; }

        public event Action? OnChange;

        public void SetUser(string userId, string role)
        {
            UserId = userId;
            Role = role;
            NotifyStateChanged();
        }

        public void SetCurrentProject(string projectTitle)
        {
            CurrentProjectTitle = projectTitle;
            NotifyStateChanged();
        }

        public void Clear()
        {
            UserId = null;
            Role = null;
            CurrentProjectTitle = null;
            NotifyStateChanged();
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        public bool IsAdmin => Role == "Admin";

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
