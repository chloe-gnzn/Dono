using System.Security.Claims;
using BloodMatch.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace BloodMatch.Web.Auth;

/// <summary>
/// Central authorization policy names and role routing helpers.
/// </summary>
public static class AuthPolicies
{
    public const string RequesterOnly = nameof(RequesterOnly);
    public const string DonorOnly = nameof(DonorOnly);
    public const string ModeratorOnly = nameof(ModeratorOnly);
    public const string AdminOnly = nameof(AdminOnly);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(RequesterOnly, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(nameof(UserRole.Requester)));

        options.AddPolicy(DonorOnly, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(nameof(UserRole.Donor)));

        // Administrators can also access moderator review queues.
        options.AddPolicy(ModeratorOnly, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(nameof(UserRole.Moderator), nameof(UserRole.Admin)));

        options.AddPolicy(AdminOnly, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(nameof(UserRole.Admin)));
    }

    public static UserRole GetRole(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return UserRole.Guest;
        }

        foreach (var role in new[] { UserRole.Admin, UserRole.Moderator, UserRole.Donor, UserRole.Requester })
        {
            if (principal.IsInRole(role.ToString()))
            {
                return role;
            }
        }

        return UserRole.Guest;
    }

    public static string GetDefaultHomeRoute(UserRole role) => role switch
    {
        UserRole.Requester => "/requester",
        UserRole.Donor => "/donor",
        UserRole.Moderator => "/moderator",
        UserRole.Admin => "/admin/moderators",
        _ => "/"
    };
}
