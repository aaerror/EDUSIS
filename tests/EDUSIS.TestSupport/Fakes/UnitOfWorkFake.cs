using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Doble en memoria de <see cref="IUnitOfWork"/> para las pruebas de los servicios de <c>Core</c>
/// (US2). Reemplaza la persistencia por repos respaldados por <c>List&lt;T&gt;</c> y expone
/// contadores de verificación (contracts/test-support-api.md §4.1):
/// <list type="bullet">
///   <item><see cref="CantidadDeGuardados"/> — cuántas veces se llamó a <see cref="GuardarCambiosAsync"/>.</item>
///   <item><see cref="EventosPublicados"/> — eventos de dominio drenados al guardar (si <see cref="PublicarEventos"/>).</item>
///   <item><see cref="LlamadasDeTransaccion"/> — secuencia de Begin/Commit/Rollback para aserciones de orden.</item>
/// </list>
/// </summary>
public sealed class UnitOfWorkFake : IUnitOfWork
{
	private readonly List<IDomainEvent> _eventosPublicados = new();
	private readonly List<string> _llamadasDeTransaccion = new();

	/// <summary>
	/// Cuando es <see langword="true"/> (por defecto), <see cref="GuardarCambiosAsync"/> recorre
	/// los agregados de todos los repos, lee sus <c>Eventos</c> y los drena con
	/// <c>LiberarEventos()</c>, tal como hace <c>MediatrExtension</c> en producción.
	/// </summary>
	public bool PublicarEventos { get; set; } = true;

	public int CantidadDeGuardados { get; private set; }

	public IReadOnlyList<IDomainEvent> EventosPublicados => _eventosPublicados;

	public IReadOnlyList<string> LlamadasDeTransaccion => _llamadasDeTransaccion;

	#region Repositorios
	public AlumnoRepositorioFake AlumnosFake { get; } = new();
	public DocenteRepositorioFake DocentesFake { get; } = new();
	public LicenciaRepositorioFake LicenciasFake { get; } = new();
	public CursoRepositorioFake CursosFake { get; } = new();
	public CurriculaRepositorioFake CurriculasFake { get; } = new();
	public UsuarioRepositorioFake UsuariosFake { get; } = new();

	public Domain.Alumnos.IAlumnoRepository Alumnos => AlumnosFake;
	public Domain.Docentes.IDocenteRepository Docentes => DocentesFake;
	public Domain.Licencias.ILicenciaRepository Licencias => LicenciasFake;
	public Domain.Cursos.ICursoRepository Cursos => CursosFake;
	public Domain.Curriculas.ICurriculaRepository Curriculas => CurriculasFake;
	public Domain.Usuarios.IUsuarioRepository Usuarios => UsuariosFake;

	private IEnumerable<Entity> TodasLasEntidades()
	{
		foreach (var alumno in AlumnosFake.Elementos) yield return alumno;
		foreach (var docente in DocentesFake.Elementos) yield return docente;
		foreach (var licencia in LicenciasFake.Elementos) yield return licencia;
		foreach (var curso in CursosFake.Elementos) yield return curso;
		foreach (var curricula in CurriculasFake.Elementos) yield return curricula;
		foreach (var usuario in UsuariosFake.Elementos) yield return usuario;
	}
	#endregion

	#region GuardarCambiosAsync
	public Task<int> GuardarCambiosAsync()
	{
		CantidadDeGuardados++;

		if (PublicarEventos)
		{
			foreach (var entidad in TodasLasEntidades().ToList())
			{
				var eventos = entidad.Eventos;
				if (eventos is null || eventos.Count == 0)
				{
					continue;
				}

				_eventosPublicados.AddRange(eventos);
				entidad.LiberarEventos();
			}
		}

		var afectadas = AlumnosFake.Afectadas
			+ DocentesFake.Afectadas
			+ LicenciasFake.Afectadas
			+ CursosFake.Afectadas
			+ CurriculasFake.Afectadas
			+ UsuariosFake.Afectadas;

		AlumnosFake.Afectadas = 0;
		DocentesFake.Afectadas = 0;
		LicenciasFake.Afectadas = 0;
		CursosFake.Afectadas = 0;
		CurriculasFake.Afectadas = 0;
		UsuariosFake.Afectadas = 0;

		return Task.FromResult(afectadas);
	}
	#endregion

	#region Transacciones (no-op, sólo registran la secuencia)
	public void BeginTransaction() => _llamadasDeTransaccion.Add(nameof(BeginTransaction));

	public Task BeginTransactionAsync()
	{
		_llamadasDeTransaccion.Add(nameof(BeginTransactionAsync));
		return Task.CompletedTask;
	}

	public void CommitTransaction() => _llamadasDeTransaccion.Add(nameof(CommitTransaction));

	public Task CommitTransactionAsync()
	{
		_llamadasDeTransaccion.Add(nameof(CommitTransactionAsync));
		return Task.CompletedTask;
	}

	public void RollbackTransaction() => _llamadasDeTransaccion.Add(nameof(RollbackTransaction));

	public Task RollbackTransactionAsync()
	{
		_llamadasDeTransaccion.Add(nameof(RollbackTransactionAsync));
		return Task.CompletedTask;
	}
	#endregion

	public void Dispose()
	{
	}
}
