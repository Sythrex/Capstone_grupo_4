using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.HasKey(x => x.Id);

            e.Property(x => x.Rut)
                .IsRequired()
                .HasMaxLength(12); // admite 12345678-9 o 12.345.678-9 (si quieres 12 + separadores)

            e.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(120);

            e.Property(x => x.Correo)
                .HasMaxLength(150);

            e.Property(x => x.Telefono)
                .HasMaxLength(20);
        });
    }
}
