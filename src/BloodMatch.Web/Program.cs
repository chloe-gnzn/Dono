using BloodMatch.Web.Components;
using BloodMatch.Web.Configuration;
using BloodMatch.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App with global Interactive Server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor authentication state & protected browser storage for circuit sessions.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Supabase configuration bound from appsettings and dotnet user-secrets.
builder.Services.AddOptions<SupabaseSettings>()
    .BindConfiguration(SupabaseSettings.SectionName);

builder.Services.AddHttpClient("SupabaseHealth");

// User-scoped Supabase client (one per Blazor Server circuit so user sessions stay isolated).
builder.Services.AddScoped<Supabase.Client>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<SupabaseSettings>>().Value;
    var url = string.IsNullOrWhiteSpace(settings.Url) ? "https://placeholder.supabase.co" : settings.Url.TrimEnd('/');
    var anonKey = string.IsNullOrWhiteSpace(settings.AnonKey) ? "placeholder-anon-key" : settings.AnonKey;

    return new Supabase.Client(url, anonKey, new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false
    });
});

// Server-only privileged client (never exposed to browser or UI components).
builder.Services.AddSingleton<ISupabaseServiceClient, SupabaseServiceClient>();

builder.Services.AddScoped<SupabaseHealthService>();
builder.Services.AddHealthChecks()
    .AddCheck<SupabaseHealthCheck>("supabase");

// Authentication services
builder.Services.AddScoped<CurrentUserState>();
builder.Services.AddScoped<CircuitSessionStore>();
builder.Services.AddScoped<SupabaseAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<SupabaseAuthenticationStateProvider>());
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/healthz");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
