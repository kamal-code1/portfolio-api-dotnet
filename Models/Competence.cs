using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Competence
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Categorie { get; set; } = string.Empty;

    [Range(0, 100)]
    public int Niveau { get; set; } = 0;

    public string? Icone { get; set; }
    public int Ordre { get; set; } = 0;
}
