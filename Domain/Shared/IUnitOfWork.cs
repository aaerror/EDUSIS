using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Catedras;
using Domain.Curriculas;
using Domain.Materias;
using Domain.Cursantes;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Docentes;
using Domain.Licencias;
using Domain.Usuarios;

namespace Domain.Shared;

public interface IUnitOfWork : IDisposable
{
	IAlumnoRepository Alumnos { get; }

	IDocenteRepository Docentes { get; }

	ILicenciaRepository Licencias { get; }

	ICursoRepository Cursos { get; }

	IDivisionRepository Divisiones { get; }

	ICursanteRepository Cursantes { get; }

	ICurriculaRepository Curriculas { get; }

	IMateriaRepository Materias { get; }

	ICatedraRepository Catedras { get; }

	IUsuarioRepository Usuarios { get; }

	IPlanillaAsistenciaRepository PlanillasAsistencia { get; }

	Task<int> GuardarCambiosAsync();

	#region Transactions
	void BeginTransaction();

	Task BeginTransactionAsync();

	void CommitTransaction();

	Task CommitTransactionAsync();

	void RollbackTransaction();

	Task RollbackTransactionAsync();
	#endregion
}