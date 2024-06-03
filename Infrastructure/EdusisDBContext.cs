using Domain.Alumnos;
using Domain.Cursos;
using Domain.Docentes;
using Domain.Materias;
using Domain.Personas;
using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure;

public class EdusisDBContext : DbContext
{
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Docente> Docentes { get; set; }
    public DbSet<Curso> Cursos { get; set; }
    public DbSet<Materia> Materias { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }


    public EdusisDBContext(DbContextOptions options)
        : base(options) { }

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

        //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        /*modelBuilder.ApplyConfiguration(new PersonasConfiguration());
        modelBuilder.ApplyConfiguration(new AlumnosConfiguration());
        modelBuilder.ApplyConfiguration(new DocentesConfigurations());
        modelBuilder.ApplyConfiguration(new CursosConfigurations());
        modelBuilder.ApplyConfiguration(new MateriasConfiguration());
        modelBuilder.ApplyConfiguration(new DivisionesConfigurations());
        modelBuilder.ApplyConfiguration(new UsuariosConfiguration());*/

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>()
                            .HaveMaxLength(50);
    }
}