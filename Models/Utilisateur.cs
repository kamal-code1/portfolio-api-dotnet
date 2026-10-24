using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Utilisateur
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NomComplet { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string MotDePasse { get; set; } = string.Empty;

    public string Role { get; set; } = "Admin";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
