using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Projet
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Lien { get; set; }
    public string? LienGithub { get; set; }
    public string? ImageUrl { get; set; }
    public bool Actif { get; set; } = true;
    public int Ordre { get; set; } = 0;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public ICollection<ProjetTech> Technologies { get; set; } = new List<ProjetTech>();
}
