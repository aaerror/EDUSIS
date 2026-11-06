using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Catedras;
using Domain.Curriculas;
using Domain.Cursantes;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Docentes;
using Domain.Licencias;
using Domain.Materias;
using Domain.Personas;
using Domain.Usuarios;
using Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class EdusisDBContext : DbContext
{
	public DbSet<Persona> Personas { get; set; }
	public DbSet<Alumno> Alumnos { get; set; }
	public DbSet<Docente> Docentes { get; set; }
	public DbSet<Licencia> Licencias { get; set; }
	public DbSet<Curso> Cursos { get; set; }
	public DbSet<Curricula> Curriculas { get; set; }
	public DbSet<Rol> Rol { get; set; }
	public DbSet<Usuario> Usuarios { get; set; }
	public DbSet<Division> Divisiones { get; set; }
	public DbSet<Cursante> Cursantes { get; set; }
	public DbSet<Materia> Materias { get; set; }
	public DbSet<Catedra> Catedras { get; set; }
	public DbSet<PlanillaAsistencia> PlanillasAsistencia { get; set; }


	public EdusisDBContext(DbContextOptions options)
		: base(options) {}

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

		// Catálogo shared-type: CicloLectivo es VO de Cursante, no entidad de dominio (ver CicloLectivoConfiguration).
		modelBuilder.ConfigurarCatalogoCicloLectivo();

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(EdusisDBContext).Assembly);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Properties<string>()
							.HaveMaxLength(50);
	}

	/// <summary>
	/// <c>rol</c> son <strong>datos de referencia</strong>: existen desde la migración y nunca se
	/// insertan en tiempo de ejecución. Antes de guardar, cualquier <see cref="Rol"/> que el
	/// <c>ChangeTracker</c> haya marcado como <c>Added</c> se degrada a <c>Unchanged</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hace falta porque la atribución rol → permisos vive en código: los <see cref="Rol"/> son
	/// instancias estáticas de <c>Enumeration</c>, no entidades traídas de la base. Cuando un caso de
	/// uso hace <c>new Usuario(..., new List&lt;Rol&gt; { Rol.Docente })</c>, EF alcanza ese
	/// <c>Rol</c> por la navegación, lo ve desconectado y emite un <c>INSERT INTO rol</c> que choca
	/// contra <c>PK_ROL</c> — el rol ya está ahí.
	/// </para>
	/// <para>
	/// <c>Tools/EDUSIS.Seed</c> resolvía esto con un <c>AttachRange</c> de los seis roles antes de
	/// usarlos, y la predicción #2 de <c>docs/plan/rbac-y-seed.md</c> anticipaba el choque. El
	/// problema de esa solución es que obliga a recordarla: el primer caso de uso que asignó un rol
	/// sin pasar por el seeder —<c>RegistrarUsuarioAsync</c>, al empezar a usar el rol del request—
	/// volvió a chocar, y lo hizo contra la base, donde sólo lo ve una prueba end-to-end.
	/// </para>
	/// <para>
	/// Resolverlo acá lo resuelve para todos: el consumidor sigue construyendo con la instancia
	/// estática, que es lo natural, y la capa que conoce la persistencia es la que sabe que esa tabla
	/// no se escribe. No afecta a <c>usuario_rol</c>, que es la relación y sí se inserta.
	/// </para>
	/// </remarks>
	private void NoInsertarDatosDeReferencia()
	{
		foreach (var entrada in ChangeTracker.Entries<Rol>().Where(x => x.State == EntityState.Added))
		{
			entrada.State = EntityState.Unchanged;
		}
	}

	public override int SaveChanges()
	{
		NoInsertarDatosDeReferencia();
		return base.SaveChanges();
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		NoInsertarDatosDeReferencia();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}
}