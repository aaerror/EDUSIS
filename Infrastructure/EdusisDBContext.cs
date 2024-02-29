using Domain.Alumnos;
using Domain.Cursos;
using Domain.Cursos.Divisiones;
using Domain.Docentes;
using Domain.Personas;
using Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class EdusisDBContext : DbContext
{
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Docente> Docentes { get; set; }
    public DbSet<Curso> Cursos { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseSqlServer(@"Data Source=localhost; Integrated Security=True; Encrypt=True; TrustServerCertificate=True; Initial Catalog=EdusisDB;");
    }
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /*modelBuilder.Entity<CicloLectivo>(builder =>
        {
            builder.ToTable("ciclo_lectivo");

            builder.Property<int>("ciclo_lectivo_id");

            // PK_CICLO-LECTIVO
            builder.HasKey("ciclo_lectivo_id")
                   .HasName("PK_CICLO-LECTIVO");

            builder.Property(x => x.Periodo)
                   .HasColumnName("periodo")
                   .HasColumnType("varchar")
                   .HasMaxLength(4)
                   .IsRequired();
        });*/

        modelBuilder.ApplyConfiguration(new PersonasConfiguration());
        modelBuilder.ApplyConfiguration(new AlumnosConfiguration());
        modelBuilder.ApplyConfiguration(new DocentesConfigurations());
        modelBuilder.ApplyConfiguration(new CursosConfigurations());
        modelBuilder.ApplyConfiguration(new MateriasConfiguration());
        modelBuilder.ApplyConfiguration(new DivisionesConfigurations());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>()
                            .HaveMaxLength(50);
    }
}