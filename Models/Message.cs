using System.ComponentModel.DataAnnotations;

namespace PortfolioAPI.Models;

public class Message
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Sujet { get; set; }

    [Required]
    public string Contenu { get; set; } = string.Empty;

    public bool Lu { get; set; } = false;
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;
}
