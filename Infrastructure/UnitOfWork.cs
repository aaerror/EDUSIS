using Domain.Alumnos;
using Domain.Curriculas;
using Domain.Cursos;
using Domain.Docentes;
using Domain.Licencias;
using Domain.Usuarios;
using Infrastructure.Extensions;
using Infrastructure.Repository;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
	private readonly EdusisDBContext _context;
	private readonly IMediator _mediator;
	private IDbContextTransaction _transaction;

	#region Repositories
	public IAlumnoRepository Alumnos { get; private set; }
	public IDocenteRepository Docentes { get; private set; }
	public ILicenciaRepository Licencias { get; private set; }
	public ICursoRepository Cursos { get; private set; }
	public ICurriculaRepository Curriculas { get; private set; }
	public IUsuarioRepository Usuarios { get; private set; }
	#endregion

	public UnitOfWork(EdusisDBContext context, IMediator mediator)
	{
		_context = context;
		_mediator = mediator;

		Alumnos = new AlumnoRepository(_context);
		Docentes = new DocenteRepository(_context);
		Licencias = new LicenciaRepository(_context);
		Cursos = new CursoRepository(_context);
		Curriculas = new CurriculaRepository(_context);
		Usuarios = new UsuarioRepository(_context);
	}

	#region Transactions
	public void BeginTransaction() =>
		_transaction = _context.Database.BeginTransaction();

	public async Task BeginTransactionAsync() =>
		_transaction = await _context.Database.BeginTransactionAsync();

	public void CommitTransaction() =>
		_transaction.Commit();

	public async Task CommitTransactionAsync() =>
		await _transaction.CommitAsync();

	public void RollbackTransaction() =>
		_transaction.Rollback();

	public async Task RollbackTransactionAsync() =>
		await _transaction.RollbackAsync();
	#endregion

	public void Dispose()
	{
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	public async Task<int> GuardarCambiosAsync()
	{
		await MediatrExtension.DispatchDomainEventsAsync(_mediator, _context);

		return await _context.SaveChangesAsync();
	}
}