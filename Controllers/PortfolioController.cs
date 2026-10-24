using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Data;
using PortfolioAPI.Models;
using System.Security.Claims;

namespace PortfolioAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly AppDbContext _context;

    public PortfolioController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================
    // PROJETS
    // ============================================

    /// <summary>
    /// GET /api/projets - Récupérer tous les projets actifs
    /// </summary>
    [HttpGet("projets")]
    public async Task<IActionResult> GetProjets()
    {
        var projets = await _context.Projets
            .Include(p => p.Technologies)
            .Where(p => p.Actif)
            .OrderBy(p => p.Ordre)
            .Select(p => new {
                p.Id,
                p.Titre,
                p.Description,
                p.Lien,
                p.DateCreation,
                Technologies = p.Technologies.Select(t => t.Nom).ToList()
            })
            .ToListAsync();

        return Ok(new {
            success = true,
            data = projets,
            message = "Projets récupérés avec succès"
        });
    }

    /// <summary>
    /// POST /api/projets - Créer un nouveau projet (JWT requis)
    /// </summary>
    [HttpPost("projets")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProjet([FromBody] CreateProjetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Données invalides" });

        var projet = new Projet
        {
            Titre = request.Titre,
            Description = request.Description,
            Lien = request.Lien,
            LienGithub = request.LienGithub,
            ImageUrl = request.ImageUrl,
            Actif = true,
            Ordre = request.Ordre,
            DateCreation = DateTime.UtcNow,
            Technologies = request.Technologies?.Select(t => new ProjetTech { Nom = t }).ToList() ?? new List<ProjetTech>()
        };

        _context.Projets.Add(projet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProjets), new { id = projet.Id }, new {
            success = true,
            data = new {
                projet.Id,
                projet.Titre,
                projet.Description,
                projet.Lien,
                Technologies = projet.Technologies.Select(t => t.Nom).ToList()
            },
            message = "Projet créé avec succès"
        });
    }

    /// <summary>
    /// DELETE /api/projets/{id} - Supprimer un projet (JWT requis)
    /// </summary>
    [HttpDelete("projets/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProjet(int id)
    {
        var projet = await _context.Projets.FindAsync(id);
        if (projet == null)
            return NotFound(new { success = false, message = "Projet non trouvé" });

        _context.Projets.Remove(projet);
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = "Projet supprimé avec succès"
        });
    }

    // ============================================
    // COMPÉTENCES
    // ============================================

    /// <summary>
    /// GET /api/competences - Récupérer toutes les compétences
    /// </summary>
    [HttpGet("competences")]
    public async Task<IActionResult> GetCompetences()
    {
        var competences = await _context.Competences
            .OrderBy(c => c.Ordre)
            .ToListAsync();

        return Ok(new {
            success = true,
            data = competences,
            message = "Compétences récupérées avec succès"
        });
    }

    /// <summary>
    /// GET /api/competences/grouped - Récupérer les compétences groupées par catégorie
    /// </summary>
    [HttpGet("competences/grouped")]
    public async Task<IActionResult> GetCompetencesGrouped()
    {
        var competences = await _context.Competences
            .OrderBy(c => c.Ordre)
            .ToListAsync();

        var grouped = competences
            .GroupBy(c => c.Categorie)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => new {
                    c.Id,
                    c.Nom,
                    c.Niveau,
                    c.Icone
                }).ToList()
            );

        return Ok(new {
            success = true,
            data = grouped,
            message = "Compétences groupées récupérées avec succès"
        });
    }

    // ============================================
    // EXPÉRIENCES
    // ============================================

    /// <summary>
    /// GET /api/experiences - Récupérer toutes les expériences
    /// </summary>
    [HttpGet("experiences")]
    public async Task<IActionResult> GetExperiences()
    {
        var experiences = await _context.Experiences
            .OrderByDescending(e => e.DateDebut)
            .ToListAsync();

        return Ok(new {
            success = true,
            data = experiences,
            message = "Expériences récupérées avec succès"
        });
    }

    /// <summary>
    /// POST /api/experiences - Créer une nouvelle expérience (JWT requis)
    /// </summary>
    [HttpPost("experiences")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateExperience([FromBody] Experience experience)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Données invalides" });

        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExperiences), new { id = experience.Id }, new {
            success = true,
            data = experience,
            message = "Expérience créée avec succès"
        });
    }

    // ============================================
    // FORMATIONS
    // ============================================

    /// <summary>
    /// GET /api/formations - Récupérer toutes les formations
    /// </summary>
    [HttpGet("formations")]
    public async Task<IActionResult> GetFormations()
    {
        var formations = await _context.Formations
            .OrderByDescending(f => f.AnneeDebut)
            .ToListAsync();

        return Ok(new {
            success = true,
            data = formations,
            message = "Formations récupérées avec succès"
        });
    }

    /// <summary>
    /// POST /api/formations - Créer une nouvelle formation (JWT requis)
    /// </summary>
    [HttpPost("formations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFormation([FromBody] Formation formation)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Données invalides" });

        _context.Formations.Add(formation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFormations), new { id = formation.Id }, new {
            success = true,
            data = formation,
            message = "Formation créée avec succès"
        });
    }

    // ============================================
    // MESSAGES
    // ============================================

    /// <summary>
    /// POST /api/messages - Envoyer un message de contact (public)
    /// </summary>
    [HttpPost("messages")]
    public async Task<IActionResult> PostMessage([FromBody] Message message)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Données invalides" });

        message.DateEnvoi = DateTime.UtcNow;
        message.Lu = false;
        
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMessages), new { id = message.Id }, new {
            success = true,
            data = new {
                message.Id,
                message.Nom,
                message.Email,
                message.Sujet,
                message.Lu,
                message.DateEnvoi
            },
            message = "Message envoyé avec succès !"
        });
    }

    /// <summary>
    /// GET /api/messages - Récupérer tous les messages (JWT requis)
    /// </summary>
    [HttpGet("messages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await _context.Messages
            .OrderByDescending(m => m.DateEnvoi)
            .ToListAsync();

        return Ok(new {
            success = true,
            data = messages,
            message = "Messages récupérés avec succès"
        });
    }

    /// <summary>
    /// PUT /api/messages/{id}/lu - Marquer un message comme lu (JWT requis)
    /// </summary>
    [HttpPut("messages/{id}/lu")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkMessageAsRead(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return NotFound(new { success = false, message = "Message non trouvé" });

        message.Lu = true;
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = "Message marqué comme lu"
        });
    }

    // ============================================
    // UTILISATEURS / ADMIN
    // ============================================

    /// <summary>
    /// GET /api/utilisateurs - Liste des utilisateurs (JWT requis)
    /// </summary>
    [HttpGet("utilisateurs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUtilisateurs()
    {
        var utilisateurs = await _context.Utilisateurs
            .Select(u => new {
                u.Id,
                u.NomComplet,
                u.Email,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new {
            success = true,
            data = utilisateurs,
            message = "Utilisateurs récupérés avec succès"
        });
    }

    // ============================================
    // PROFIL ADMIN CONNECTÉ
    // ============================================

    /// <summary>
    /// GET /api/portfolio/me - Profil de l'admin connecté
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { success = false, message = "Token invalide" });

        var user = await _context.Utilisateurs.FindAsync(userId);
        if (user == null)
            return NotFound(new { success = false, message = "Utilisateur non trouvé" });

        return Ok(new {
            success = true,
            data = new {
                user.Id,
                user.NomComplet,
                user.Email,
                user.Role
            },
            message = "Profil récupéré avec succès"
        });
    }
}

// DTO pour la création d'un projet
public class CreateProjetRequest
{
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public string? LienGithub { get; set; }
    public string? ImageUrl { get; set; }
    public int Ordre { get; set; }
    public List<string>? Technologies { get; set; }
}
