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
using Domain.Shared;
using Domain.Usuarios;

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
	private readonly IRepositorioEnMemoria[] _todosLosRepos;

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
	public DivisionRepositorioFake DivisionesFake { get; } = new();
	public CursanteRepositorioFake CursantesFake { get; } = new();
	public CurriculaRepositorioFake CurriculasFake { get; } = new();
	public MateriaRepositorioFake MateriasFake { get; } = new();
	public CatedraRepositorioFake CatedrasFake { get; } = new();
	public UsuarioRepositorioFake UsuariosFake { get; } = new();
	public PlanillaAsistenciaRepositorioFake PlanillasAsistenciaFake { get; } = new();

	public IAlumnoRepository Alumnos => AlumnosFake;
	public IDocenteRepository Docentes => DocentesFake;
	public ILicenciaRepository Licencias => LicenciasFake;
	public ICursoRepository Cursos => CursosFake;
	public IDivisionRepository Divisiones => DivisionesFake;
	public ICursanteRepository Cursantes => CursantesFake;
	public ICurriculaRepository Curriculas => CurriculasFake;
	public IMateriaRepository Materias => MateriasFake;
	public ICatedraRepository Catedras => CatedrasFake;
	public IUsuarioRepository Usuarios => UsuariosFake;
	public IPlanillaAsistenciaRepository PlanillasAsistencia => PlanillasAsistenciaFake;

	public UnitOfWorkFake()
	{
		_todosLosRepos = new IRepositorioEnMemoria[]
		{
			AlumnosFake,
			DocentesFake,
			LicenciasFake,
			CursosFake,
			DivisionesFake,
			CursantesFake,
			CurriculasFake,
			MateriasFake,
			CatedrasFake,
			UsuariosFake,
			PlanillasAsistenciaFake
		};
	}
	#endregion

	#region GuardarCambiosAsync
	public Task<int> GuardarCambiosAsync()
	{
		CantidadDeGuardados++;

		// Una sola pasada basta: el fake no ejecuta handlers, así que nadie puede encolar eventos
		// nuevos durante el drenado (la cascada real se prueba en DespachoDeEventosTests).
		if (PublicarEventos)
		{
			var entidades = _todosLosRepos
				.SelectMany(repo => repo.Entidades)
				.Where(x => x.Eventos is not null && x.Eventos.Any())
				.ToList();

			_eventosPublicados.AddRange(entidades.SelectMany(x => x.Eventos));
			entidades.ForEach(x => x.LiberarEventos());
		}

		var afectadas = _todosLosRepos.Sum(repo => repo.ConsumirAfectadas());

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
