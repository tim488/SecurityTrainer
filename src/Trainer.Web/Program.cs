using Trainer.Web.Components;
using Trainer.Web.Core;

// This app is DELIBERATELY VULNERABLE. It binds to http://localhost:5000 only
// (see appsettings.json "Kestrel"). Never deploy it or expose it to a network.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<FlagService>();
builder.Services.AddSingleton<DemoAuth>();
builder.Services.AddSingleton<LabRegistry>();
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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();

app.Services.GetRequiredService<LabRegistry>().MapEndpoints(app);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
