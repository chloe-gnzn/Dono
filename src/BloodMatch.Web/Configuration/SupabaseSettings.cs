namespace BloodMatch.Web.Configuration;

/// <summary>
/// Supabase connection settings bound from appsettings and user-secrets ("Supabase" section).
/// </summary>
public sealed class SupabaseSettings
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// Server-only privileged key. Never pass this to client-side code or Razor output.
    /// Store locally via `dotnet user-secrets set "Supabase:ServiceRoleKey" "..."`.
    /// </summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    public bool HasClientConfig =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrWhiteSpace(AnonKey);

    public bool HasServiceRoleConfig =>
        HasClientConfig && !string.IsNullOrWhiteSpace(ServiceRoleKey);
}
