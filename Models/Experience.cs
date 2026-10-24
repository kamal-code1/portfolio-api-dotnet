using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Experience
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Entreprise { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Poste { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string? Lieu { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public bool EnCours { get; set; } = false;
}
