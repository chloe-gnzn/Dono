using BloodMatch.Web.Auth;
using BloodMatch.Web.Models;
using Microsoft.AspNetCore.Components;

namespace BloodMatch.Web.Services;

/// <summary>
/// Computes post-authentication redirect targets based on user role and safe local return URLs.
/// </summary>
public sealed class RoleRedirectService
{
    public string ResolvePostLoginTarget(UserRole role, string? returnUrl)
    {
        if (IsSafeLocalUrl(returnUrl))
        {
            return returnUrl!;
        }

        return AuthPolicies.GetDefaultHomeRoute(role);
    }

    public void RedirectAfterLogin(NavigationManager nav, UserRole role, string? returnUrl, bool replace = false)
    {
        var target = ResolvePostLoginTarget(role, returnUrl);
        nav.NavigateTo(target, replace: replace);
    }

    public static bool IsSafeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Only allow relative paths starting with '/' (not protocol-relative '//' or '/\').
        return url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");
    }
}
