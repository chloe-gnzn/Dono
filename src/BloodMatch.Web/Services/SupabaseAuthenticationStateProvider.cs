using System.Security.Claims;
using BloodMatch.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;

namespace BloodMatch.Web.Services;

/// <summary>
/// Integrates Blazor's cascading AuthenticationState with Supabase Auth and
/// resolves the authoritative user role from public.profiles.
/// </summary>
public sealed class SupabaseAuthenticationStateProvider(
    Supabase.Client supabase,
    CircuitSessionStore sessionStore,
    CurrentUserState currentUserState,
    ILogger<SupabaseAuthenticationStateProvider> logger) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal AnonymousPrincipal = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = supabase.Auth.CurrentSession;

        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            var stored = await sessionStore.LoadAsync();
            if (stored is not null
                && !string.IsNullOrWhiteSpace(stored.AccessToken)
                && !string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                try
                {
                    session = await supabase.Auth.SetSession(stored.AccessToken, stored.RefreshToken);
                    if (session is not null && !string.IsNullOrWhiteSpace(session.AccessToken))
                    {
                        await PersistSessionAsync(session);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "Stored Supabase session is no longer valid; clearing.");
                    await sessionStore.ClearAsync();
                    session = null;
                }
            }
        }

        if (session?.User is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            currentUserState.Clear();
            return new AuthenticationState(AnonymousPrincipal);
        }

        var (role, displayName) = await ResolveProfileAsync(session.User);
        currentUserState.Set(role, displayName);

        return new AuthenticationState(BuildPrincipal(session.User, role, displayName));
    }

    public async Task<UserRole> MarkAuthenticatedAsync(Session session)
    {
        if (!string.IsNullOrWhiteSpace(session.AccessToken) && !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            await supabase.Auth.SetSession(session.AccessToken, session.RefreshToken);
            await PersistSessionAsync(session);
        }

        var user = session.User ?? supabase.Auth.CurrentUser;
        if (user is null)
        {
            currentUserState.Clear();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(AnonymousPrincipal)));
            return UserRole.Guest;
        }

        var (role, displayName) = await ResolveProfileAsync(user);
        currentUserState.Set(role, displayName);

        var principal = BuildPrincipal(user, role, displayName);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        return role;
    }

    public async Task MarkLoggedOutAsync()
    {
        await sessionStore.ClearAsync();
        currentUserState.Clear();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(AnonymousPrincipal)));
    }

    private Task PersistSessionAsync(Session session)
    {
        var expiresAt = session.CreatedAt == default
            ? DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn > 0 ? session.ExpiresIn : 3600)
            : new DateTimeOffset(DateTime.SpecifyKind(session.CreatedAt, DateTimeKind.Utc))
                .AddSeconds(session.ExpiresIn > 0 ? session.ExpiresIn : 3600);

        return sessionStore.SaveAsync(new StoredSupabaseSession(
            session.AccessToken ?? string.Empty,
            session.RefreshToken ?? string.Empty,
            expiresAt));
    }

    private async Task<(UserRole Role, string DisplayName)> ResolveProfileAsync(User user)
    {
        var fallbackName = user.Email ?? "Member";

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return (UserRole.Requester, fallbackName);
        }

        try
        {
            var profile = await supabase
                .From<UserProfile>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
                .Single();

            if (profile is not null)
            {
                var name = string.IsNullOrWhiteSpace(profile.FullName) ? fallbackName : profile.FullName;
                return (profile.ToUserRole(), name);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load profile row for user {UserId}. Falling back to metadata.", user.Id);
        }

        // Only allow requester/donor fallback from user metadata; elevated roles must come from profiles.
        var metaRole = UserRole.Requester;
        if (user.UserMetadata is not null
            && user.UserMetadata.TryGetValue("role", out var rawRole)
            && string.Equals(rawRole?.ToString(), "donor", StringComparison.OrdinalIgnoreCase))
        {
            metaRole = UserRole.Donor;
        }

        var metaName = fallbackName;
        if (user.UserMetadata is not null
            && user.UserMetadata.TryGetValue("full_name", out var rawName)
            && !string.IsNullOrWhiteSpace(rawName?.ToString()))
        {
            metaName = rawName.ToString()!;
        }

        return (metaRole, metaName);
    }

    private static ClaimsPrincipal BuildPrincipal(User user, UserRole role, string displayName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Supabase"));
    }
}
