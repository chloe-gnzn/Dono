using System.Security.Cryptography;
using BloodMatch.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BloodMatch.Web.Services;

/// <summary>
/// Persists the Supabase session tokens in ASP.NET Core ProtectedLocalStorage
/// so interactive Blazor Server circuits can restore sign-in across reloads.
/// </summary>
public sealed class CircuitSessionStore(
    ProtectedLocalStorage localStorage,
    ILogger<CircuitSessionStore> logger)
{
    private const string StorageKey = "bloodmatch.supabase.session";

    public async Task<StoredSupabaseSession?> LoadAsync()
    {
        try
        {
            var result = await localStorage.GetAsync<StoredSupabaseSession>(StorageKey);
            return result.Success ? result.Value : null;
        }
        catch (InvalidOperationException)
        {
            // Expected during static SSR prerendering before JS interop is available.
            return null;
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(ex, "Stored session could not be unprotected; clearing stale entry.");
            await ClearAsync();
            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to read session from ProtectedLocalStorage.");
            return null;
        }
    }

    public async Task SaveAsync(StoredSupabaseSession session)
    {
        try
        {
            await localStorage.SetAsync(StorageKey, session);
        }
        catch (InvalidOperationException)
        {
            // Ignore during prerender.
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await localStorage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException)
        {
            // Ignore during prerender.
        }
    }
}
