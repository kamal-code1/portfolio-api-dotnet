using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PortfolioAPI.Models;
using PortfolioAPI.Services;

namespace PortfolioAPI.Tests;

/// <summary>
/// Démarre l'API en mémoire avec une base SQLite temporaire et un faux service d'e-mail.
/// </summary>
public class PortfolioApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Test-Admin-Password-123";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"portfolio-tests-{Guid.NewGuid():N}.db");

    public FakeEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting est appliqué avant la lecture de la configuration dans Program.cs
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", "cle-de-test-uniquement-pour-les-tests-0123456789");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

public class FakeEmailSender : IEmailSender
{
    public List<ContactRequest> Sent { get; } = new();
    public bool Fail { get; set; }

    public Task SendContactAsync(ContactRequest contact, CancellationToken ct)
    {
        if (Fail)
            throw new InvalidOperationException("Échec simulé");
        Sent.Add(contact);
        return Task.CompletedTask;
    }
}
