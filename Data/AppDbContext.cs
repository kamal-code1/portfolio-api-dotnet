using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Models;

namespace PortfolioAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs { get; set; }
    public DbSet<Projet> Projets { get; set; }
    public DbSet<ProjetTech> ProjetTechs { get; set; }
    public DbSet<Competence> Competences { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<Formation> Formations { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration de la relation Projet -> ProjetTech
        modelBuilder.Entity<ProjetTech>()
            .HasOne(pt => pt.Projet)
            .WithMany(p => p.Technologies)
            .HasForeignKey(pt => pt.ProjetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
