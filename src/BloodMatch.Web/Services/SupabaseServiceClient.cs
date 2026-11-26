using BloodMatch.Web.Configuration;
using Microsoft.Extensions.Options;

namespace BloodMatch.Web.Services;

/// <summary>
/// Server-only Supabase client authenticated with the service_role key.
/// Bypasses RLS — inject ONLY into server-side services, never directly into UI components.
/// </summary>
public interface ISupabaseServiceClient
{
    bool IsConfigured { get; }
    Supabase.Client GetClient();
}

public sealed class SupabaseServiceClient : ISupabaseServiceClient
{
    private readonly SupabaseSettings _settings;
    private readonly Lazy<Supabase.Client> _client;

    public SupabaseServiceClient(IOptions<SupabaseSettings> options)
    {
        _settings = options.Value;
        _client = new Lazy<Supabase.Client>(CreateClient);
    }

    public bool IsConfigured => _settings.HasServiceRoleConfig;

    public Supabase.Client GetClient()
    {
        if (!_settings.HasServiceRoleConfig)
        {
            throw new InvalidOperationException(
                "Supabase service-role settings are not configured. Set Supabase:Url, Supabase:AnonKey, and Supabase:ServiceRoleKey via user-secrets or environment variables.");
        }

        return _client.Value;
    }

    private Supabase.Client CreateClient()
    {
        var opts = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false
        };

        return new Supabase.Client(_settings.Url.TrimEnd('/'), _settings.ServiceRoleKey, opts);
    }
}
