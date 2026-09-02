using Domain.Alumnos;
using Domain.Curriculas;
using Domain.Cursos;
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

	ICurriculaRepository Curriculas { get; }

	IUsuarioRepository Usuarios { get; }

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