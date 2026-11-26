using BloodMatch.Web.Models;

namespace BloodMatch.Web.Services;

public sealed class CurrentUserState
{
    public UserRole Role { get; private set; } = UserRole.Guest;
    public string? DisplayName { get; private set; }
    public bool IsAuthenticated => Role != UserRole.Guest;

    public event Action? Changed;

    public void Set(UserRole role, string? displayName = null)
    {
        Role = role;
        DisplayName = displayName;
        Changed?.Invoke();
    }

    public void Clear() => Set(UserRole.Guest);
}
