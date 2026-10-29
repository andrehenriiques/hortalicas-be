using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

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
    }
}
