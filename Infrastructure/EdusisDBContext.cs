using Domain.Alumno;
using Domain.Personas;
using Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class EdusisDBContext : DbContext
{
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Alumno> Alumnos { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseSqlServer(@"Data Source=localhost; Integrated Security=True; Encrypt=True; TrustServerCertificate=True; Initial Catalog=EdusisDB;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new AlumnosConfiguration());
    }
}