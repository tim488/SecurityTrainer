using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

/// <summary>Shared helpers for per-lab tests: a client logged in as a lab user, and services.</summary>
public abstract class LabTestBase(WebApplicationFactory<Program> factory, string slug) : IClassFixture<WebApplicationFactory<Program>>
{
    protected WebApplicationFactory<Program> Factory => factory;
    protected string Slug => slug;
    protected string Api => $"/api/labs/{slug}";
    protected string Flag => Get<FlagService>().GetFlag(slug);
    protected ILab Lab => Get<LabRegistry>().Find(slug)!;

    protected HttpClient Client(string? user = DemoAuth.Player, string? language = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (user is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Get<DemoAuth>().GetToken(slug, user));
        if (language is not null)
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        return client;
    }

    protected T Get<T>() where T : notnull => factory.Services.GetRequiredService<T>();
}
