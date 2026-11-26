using System.Diagnostics;
using System.Net.Http.Headers;
using BloodMatch.Web.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BloodMatch.Web.Services;

public sealed record SupabaseHealthReport(
    bool IsHealthy,
    bool ClientConfigured,
    bool ServiceRoleConfigured,
    bool AuthReachable,
    bool RestReachable,
    bool SeedTableAccessible,
    long LatencyMs,
    string Summary,
    string? ErrorDetail = null)
{
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Runs configuration and live HTTP/PostgREST connectivity checks against Supabase.
/// </summary>
public sealed class SupabaseHealthService(
    IOptionsSnapshot<SupabaseSettings> options,
    IHttpClientFactory httpClientFactory,
    ILogger<SupabaseHealthService> logger)
{
    private readonly SupabaseSettings _settings = options.Value;

    public async Task<SupabaseHealthReport> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        if (!_settings.HasClientConfig)
        {
            return new SupabaseHealthReport(
                IsHealthy: false,
                ClientConfigured: false,
                ServiceRoleConfigured: _settings.HasServiceRoleConfig,
                AuthReachable: false,
                RestReachable: false,
                SeedTableAccessible: false,
                LatencyMs: sw.ElapsedMilliseconds,
                Summary: "Supabase Url or AnonKey is missing.",
                ErrorDetail: "Configure Supabase:Url and Supabase:AnonKey in user-secrets or appsettings.Development.json.");
        }

        var baseUrl = _settings.Url.TrimEnd('/');
        var client = httpClientFactory.CreateClient("SupabaseHealth");
        client.Timeout = TimeSpan.FromSeconds(8);

        var authOk = false;
        var restOk = false;
        var seedOk = false;
        string? errorDetail = null;

        try
        {
            using var authReq = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/auth/v1/health");
            authReq.Headers.Add("apikey", _settings.AnonKey);
            using var authResp = await client.SendAsync(authReq, cancellationToken);
            authOk = authResp.IsSuccessStatusCode;

            using var restReq = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/rest/v1/blood_types?select=code&limit=1");
            restReq.Headers.Add("apikey", _settings.AnonKey);
            if (!_settings.AnonKey.StartsWith("sb_", StringComparison.OrdinalIgnoreCase))
            {
                restReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AnonKey);
            }
            using var restResp = await client.SendAsync(restReq, cancellationToken);

            restOk = restResp.IsSuccessStatusCode;
            if (restOk)
            {
                var body = await restResp.Content.ReadAsStringAsync(cancellationToken);
                seedOk = body.Contains("code", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var errBody = await restResp.Content.ReadAsStringAsync(cancellationToken);
                errorDetail = string.IsNullOrWhiteSpace(errBody)
                    ? $"PostgREST returned HTTP {(int)restResp.StatusCode} ({restResp.ReasonPhrase})."
                    : $"PostgREST returned HTTP {(int)restResp.StatusCode} ({restResp.ReasonPhrase}): {errBody}";
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Supabase connectivity check failed.");
            errorDetail = ex.Message;
        }

        sw.Stop();
        var healthy = authOk && restOk;
        var summary = healthy
            ? (seedOk ? "Connected to Supabase Auth and PostgREST (seed data verified)." : "Connected to Supabase Auth and PostgREST.")
            : "Supabase connectivity check failed.";

        return new SupabaseHealthReport(
            IsHealthy: healthy,
            ClientConfigured: true,
            ServiceRoleConfigured: _settings.HasServiceRoleConfig,
            AuthReachable: authOk,
            RestReachable: restOk,
            SeedTableAccessible: seedOk,
            LatencyMs: sw.ElapsedMilliseconds,
            Summary: summary,
            ErrorDetail: errorDetail);
    }
}

public sealed class SupabaseHealthCheck(SupabaseHealthService healthService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var report = await healthService.CheckConnectivityAsync(cancellationToken);
        var data = new Dictionary<string, object>
        {
            ["clientConfigured"] = report.ClientConfigured,
            ["serviceRoleConfigured"] = report.ServiceRoleConfigured,
            ["authReachable"] = report.AuthReachable,
            ["restReachable"] = report.RestReachable,
            ["seedTableAccessible"] = report.SeedTableAccessible,
            ["latencyMs"] = report.LatencyMs
        };

        if (report.IsHealthy)
        {
            return HealthCheckResult.Healthy(report.Summary, data);
        }

        return report.ClientConfigured
            ? HealthCheckResult.Unhealthy(report.ErrorDetail ?? report.Summary, data: data)
            : HealthCheckResult.Degraded(report.Summary, data: data);
    }
}
