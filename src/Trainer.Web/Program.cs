using Trainer.Web.Components;
using Trainer.Web.Core;

// This app is DELIBERATELY VULNERABLE. It binds to http://localhost:5000 only
// (see appsettings.json "Kestrel"). Never deploy it or expose it to a network.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<FlagService>();
builder.Services.AddSingleton<DemoAuth>();
builder.Services.AddSingleton<MissionRegistry>();
builder.Services.AddSingleton<RequestSender>();
builder.Services.AddScoped<ProgressStore>();

// The request console calls the app itself. Redirects are never followed and
// cookies are never sent, so each request is exactly what the player typed.
builder.Services.AddHttpClient(RequestSender.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
// Friendly "not found" pages for the UI only. Mission APIs must return their real status codes
// (a re-executed POST would otherwise fail antiforgery and show up as 400 in the console).
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseAntiforgery();

app.MapStaticAssets();

app.Services.GetRequiredService<MissionRegistry>().MapEndpoints(app);

// Mission pages used to live under /labs/; keep old links and bookmarks working.
app.MapGet("/labs/{slug}", (string slug) => Results.Redirect($"/missions/{Uri.EscapeDataString(slug)}", permanent: true));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
