using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortfolioAPI.Data;
using PortfolioAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PortfolioAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/auth/login - Connexion et obtention du JWT
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.MotDePasse))
            return BadRequest(new { 
                success = false, 
                message = "Email et mot de passe requis" 
            });

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (utilisateur == null)
            return Unauthorized(new { 
                success = false, 
                message = "Email ou mot de passe incorrect" 
            });

        // Vérifier le mot de passe avec BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.MotDePasse, utilisateur.MotDePasse))
            return Unauthorized(new { 
                success = false, 
                message = "Email ou mot de passe incorrect" 
            });

        // Générer le token JWT
        // Présence et longueur vérifiées au démarrage (Program.cs)
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "portfolio-api";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "portfolio-client";
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
            new Claim(ClaimTypes.Name, utilisateur.NomComplet),
            new Claim(ClaimTypes.Email, utilisateur.Email),
            new Claim(ClaimTypes.Role, utilisateur.Role ?? "User")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new {
            success = true,
            message = "Connexion réussie.",
            data = new {
                token = tokenString,
                nomComplet = utilisateur.NomComplet,
                email = utilisateur.Email,
                expiration = DateTime.UtcNow.AddMinutes(expirationMinutes)
            }
        });
    }

    /// <summary>
    /// POST /api/auth/register - Inscription d'un nouvel utilisateur (Optionnel)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.MotDePasse))
            return BadRequest(new { 
                success = false, 
                message = "Email et mot de passe requis" 
            });

        // Vérifier si l'email existe déjà
        if (await _context.Utilisateurs.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { 
                success = false, 
                message = "Cet email est déjà utilisé" 
            });

        // Hacher le mot de passe
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.MotDePasse);

        var utilisateur = new Utilisateur
        {
            NomComplet = request.NomComplet ?? string.Empty,
            Email = request.Email,
            MotDePasse = hashedPassword,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Utilisateurs.Add(utilisateur);
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = "Compte créé avec succès",
            data = new {
                utilisateur.Id,
                utilisateur.NomComplet,
                utilisateur.Email
            }
        });
    }
}

// DTOs pour les requêtes
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string? NomComplet { get; set; }
    public string Email { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
}
