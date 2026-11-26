using BloodMatch.Web.Components;
using BloodMatch.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App with global Interactive Server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Per-circuit user state used by the layout (temporary until auth is added).
builder.Services.AddScoped<CurrentUserState>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
