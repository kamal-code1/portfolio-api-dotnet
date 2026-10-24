using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class ProjetTech
{
    public int Id { get; set; }
    public int ProjetId { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    public Projet Projet { get; set; } = null!;
}
