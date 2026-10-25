using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortfolioAPI.Data;
using PortfolioAPI.Models;
using PortfolioAPI.Services;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Base de données : PostgreSQL, ou SQLite si la chaîne commence par "Data Source="
// Secrets fournis par user-secrets ou variables d'environnement (voir README)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection manquant (variable ConnectionStrings__DefaultConnection).");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

// Configuration JSON pour accepter camelCase (comme React)
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configuration JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key manquant ou trop court (32 octets minimum, variable Jwt__Key).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "portfolio-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "portfolio-client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:5173",
            "https://kamal-portfolio.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Envoi des messages de contact par e-mail
builder.Services.AddHttpClient("resend", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddScoped<IEmailSender, EmailSender>();

// Hébergement derrière un proxy (Render) : IP client et schéma HTTPS réels
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Limitation par IP : formulaire de contact et authentification
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { success = false, message = "Trop de requêtes. Réessayez dans quelques minutes." }, ct);

    void AddIpPolicy(string name, int permits, TimeSpan window) =>
        options.AddPolicy(name, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));

    AddIpPolicy("contact", 5, TimeSpan.FromMinutes(15));
    AddIpPolicy("auth", 10, TimeSpan.FromMinutes(15));
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// En-têtes de sécurité pour le site statique et l'API
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (!context.Request.Path.StartsWithSegments("/swagger"))
        h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

// Seed Data - Initialiser les données
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    
    // Seed des compétences si vide
    if (!context.Competences.Any())
    {
        context.Competences.AddRange(
            new Competence { Nom = "ASP.NET Core", Categorie = "Backend", Niveau = 90, Ordre = 1 },
            new Competence { Nom = "C# / .NET", Categorie = "Backend", Niveau = 92, Ordre = 2 },
            new Competence { Nom = "React", Categorie = "Frontend", Niveau = 88, Ordre = 3 },
            new Competence { Nom = "TypeScript", Categorie = "Frontend", Niveau = 82, Ordre = 4 },
            new Competence { Nom = "PostgreSQL", Categorie = "Database", Niveau = 87, Ordre = 5 },
            new Competence { Nom = "Git/GitHub", Categorie = "DevOps", Niveau = 85, Ordre = 6 }
        );
        context.SaveChanges();
    }

    // Seed des projets si vide
    if (!context.Projets.Any())
    {
        var aquaFeed = new Projet
        {
            Titre = "AquaFeed Smart",
            Description = "Plateforme IoT de supervision et d'irrigation automatisée (testée en simulation)",
            LienGithub = "https://github.com/kamal-code1/aquafeed-smart",
            Actif = true,
            Ordre = 1
        };
        aquaFeed.Technologies.Add(new ProjetTech { Nom = "React", Projet = aquaFeed });
        aquaFeed.Technologies.Add(new ProjetTech { Nom = "Node.js", Projet = aquaFeed });
        aquaFeed.Technologies.Add(new ProjetTech { Nom = "TypeScript", Projet = aquaFeed });
        aquaFeed.Technologies.Add(new ProjetTech { Nom = "PostgreSQL", Projet = aquaFeed });

        var portfolioApi = new Projet
        {
            Titre = "PortfolioAPI",
            Description = "API REST ASP.NET Core 8 de ce portfolio : EF Core, PostgreSQL, JWT",
            LienGithub = "https://github.com/kamal-code1/portfolio-api-dotnet",
            Actif = true,
            Ordre = 2
        };
        portfolioApi.Technologies.Add(new ProjetTech { Nom = "ASP.NET Core", Projet = portfolioApi });
        portfolioApi.Technologies.Add(new ProjetTech { Nom = "EF Core", Projet = portfolioApi });
        portfolioApi.Technologies.Add(new ProjetTech { Nom = "PostgreSQL", Projet = portfolioApi });
        portfolioApi.Technologies.Add(new ProjetTech { Nom = "JWT", Projet = portfolioApi });

        context.Projets.AddRange(aquaFeed, portfolioApi);
        context.SaveChanges();
    }

    // Seed de l'utilisateur admin si vide
    // Créé uniquement si Admin:Email et Admin:Password sont configurés
    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPassword = builder.Configuration["Admin:Password"];
    if (!context.Utilisateurs.Any() && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        context.Utilisateurs.Add(new Utilisateur
        {
            NomComplet = "Kamal Drissi El-Bouzaidi",
            Email = adminEmail,
            MotDePasse = hashedPassword,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }
}

app.Run();

// Rend la classe Program accessible aux tests d'intégration (WebApplicationFactory)
public partial class Program { }
