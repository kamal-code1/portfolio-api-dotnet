using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Formation
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Etablissement { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int AnneeDebut { get; set; }
    public int? AnneeFin { get; set; }
}
