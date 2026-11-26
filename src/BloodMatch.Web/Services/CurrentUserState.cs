using BloodMatch.Web.Models;

namespace BloodMatch.Web.Services;

/// <summary>
/// Holds who is "signed in" for the current Blazor circuit so the layout can
/// adapt its navigation by role.
///
/// TEMPORARY: until Supabase auth lands (commits 8-9) this is set manually from
/// the /dev/components page. Auth will populate it from the real session.
/// </summary>
public sealed class CurrentUserState
{
    public UserRole Role { get; private set; } = UserRole.Guest;
    public string? DisplayName { get; private set; }
    public bool IsAuthenticated => Role != UserRole.Guest;

    /// <summary>Raised after Role or DisplayName changes.</summary>
    public event Action? Changed;

    public void Set(UserRole role, string? displayName = null)
    {
        Role = role;
        DisplayName = displayName;
        Changed?.Invoke();
    }

    public void Clear() => Set(UserRole.Guest);
}
