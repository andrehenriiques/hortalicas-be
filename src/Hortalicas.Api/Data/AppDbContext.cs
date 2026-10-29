using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Estufa> Estufas => Set<Estufa>();
    public DbSet<Canteiro> Canteiros => Set<Canteiro>();
    public DbSet<Hortalica> Hortalicas => Set<Hortalica>();
    public DbSet<Plantio> Plantios => Set<Plantio>();
    public DbSet<Irrigacao> Irrigacoes => Set<Irrigacao>();
    public DbSet<Colheita> Colheitas => Set<Colheita>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nome).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Cpf).IsRequired().HasMaxLength(11);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(254);
            entity.Property(u => u.Telefone).IsRequired().HasMaxLength(11);
            entity.HasIndex(u => u.Cpf).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Estufa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Observacao).HasMaxLength(500);
            entity.HasIndex(e => e.Nome).IsUnique();
            entity.HasMany(e => e.Canteiros).WithOne(c => c.Estufa).HasForeignKey(c => c.EstufaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Canteiro>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.EstufaId, c.Numero }).IsUnique();
            entity.HasMany(c => c.Plantios).WithOne(p => p.Canteiro).HasForeignKey(p => p.CanteiroId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Hortalica>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Nome).IsRequired().HasMaxLength(80);
            entity.Property(h => h.Observacao).HasMaxLength(500);
            entity.HasIndex(h => h.Nome).IsUnique();
        });

        modelBuilder.Entity<Plantio>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Situacao).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.MotivoEncerramento).HasMaxLength(300);
            entity.Property(p => p.Observacao).HasMaxLength(500);
            entity.HasOne(p => p.Hortalica).WithMany().HasForeignKey(p => p.HortalicaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(p => p.Colheitas).WithOne(c => c.Plantio).HasForeignKey(c => c.PlantioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => new { p.CanteiroId, p.Situacao });
        });

        modelBuilder.Entity<Irrigacao>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Observacao).HasMaxLength(500);
            entity.HasOne(i => i.Estufa).WithMany().HasForeignKey(i => i.EstufaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(i => new { i.EstufaId, i.Data });
        });

        modelBuilder.Entity<Colheita>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Lote).IsRequired().HasMaxLength(20);
            entity.Property(c => c.MotivoDescarte).HasMaxLength(300);
            entity.HasIndex(c => c.Lote).IsUnique();
            entity.HasIndex(c => c.Data);
        });
    }
}
