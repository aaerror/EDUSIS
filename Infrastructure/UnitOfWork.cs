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
using Domain.Usuarios;
using Infrastructure.Extensions;
using Infrastructure.Repository;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
	private readonly EdusisDBContext _context;
	private readonly IMediator _mediator;
	private IDbContextTransaction? _transaction;

	#region Repositories
	public IAlumnoRepository Alumnos { get; private set; }

	public IDocenteRepository Docentes { get; private set; }

	public ILicenciaRepository Licencias { get; private set; }

	public ICursoRepository Cursos { get; private set; }

	public IDivisionRepository Divisiones { get; private set; }

	public ICursanteRepository Cursantes { get; private set; }

	public ICurriculaRepository Curriculas { get; private set; }

	public IMateriaRepository Materias { get; private set; }

	public ICatedraRepository Catedras { get; private set; }

	public IUsuarioRepository Usuarios { get; private set; }

	public IPlanillaAsistenciaRepository PlanillasAsistencia { get; private set; }
	#endregion

	public UnitOfWork(EdusisDBContext context, IMediator mediator)
	{
		_context = context;
		_mediator = mediator;

		Alumnos = new AlumnoRepository(_context);
		Docentes = new DocenteRepository(_context);
		Licencias = new LicenciaRepository(_context);
		Cursos = new CursoRepository(_context);
		Divisiones = new DivisionRepository(_context);
		Cursantes = new CursanteRepository(_context);
		Curriculas = new CurriculaRepository(_context);
		Materias = new MateriaRepository(_context);
		Catedras = new CatedraRepository(_context);
		Usuarios = new UsuarioRepository(_context);
		PlanillasAsistencia = new PlanillaAsistenciaRepository(_context);
	}

	#region Transactions
	private IDbContextTransaction TransaccionActual =>
		_transaction ?? throw new InvalidOperationException("No hay una transacción iniciada.");

	private void FinalizarTransaccion()
	{
		_transaction?.Dispose();
		_transaction = null;
	}

	private void VerificarSinTransaccion()
	{
		if (_transaction is not null)
		{
			throw new InvalidOperationException("Ya hay una transacción iniciada.");
		}
	}

	public void BeginTransaction()
	{
		VerificarSinTransaccion();
		_transaction = _context.Database.BeginTransaction();
	}

	public async Task BeginTransactionAsync()
	{
		VerificarSinTransaccion();
		_transaction = await _context.Database.BeginTransactionAsync();
	}

	public void CommitTransaction()
	{
		TransaccionActual.Commit();
		FinalizarTransaccion();
	}

	public async Task CommitTransactionAsync()
	{
		await TransaccionActual.CommitAsync();
		FinalizarTransaccion();
	}

	public void RollbackTransaction()
	{
		TransaccionActual.Rollback();
		FinalizarTransaccion();
	}

	public async Task RollbackTransactionAsync()
	{
		await TransaccionActual.RollbackAsync();
		FinalizarTransaccion();
	}
	#endregion

	public void Dispose()
	{
		_transaction?.Dispose();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	public async Task<int> GuardarCambiosAsync()
	{
		await MediatrExtension.DispatchDomainEventsAsync(_mediator, _context);

		return await _context.SaveChangesAsync();
	}
}