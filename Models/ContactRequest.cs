using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

/// <summary>
/// Données du formulaire de contact public (DTO : empêche l'écriture de Id, Lu, DateEnvoi).
/// </summary>
public class ContactRequest
{
    [Required(ErrorMessage = "Le nom est requis."), StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères.")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'adresse e-mail est requise."), EmailAddress(ErrorMessage = "Adresse e-mail invalide."), StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le sujet est requis."), StringLength(150, MinimumLength = 3, ErrorMessage = "Le sujet doit contenir entre 3 et 150 caractères.")]
    public string Sujet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le message est requis."), StringLength(5000, MinimumLength = 10, ErrorMessage = "Le message doit contenir entre 10 et 5000 caractères.")]
    public string Contenu { get; set; } = string.Empty;

    /// <summary>Champ piège invisible : rempli uniquement par les robots.</summary>
    public string? Website { get; set; }
}
