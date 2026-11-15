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

	/// <remarks>
	/// El rollback revierte la base pero no el <c>ChangeTracker</c>: lo guardado dentro de la
	/// transacción queda rastreado como <c>Unchanged</c> (la base ya no lo tiene) y lo que no llegó
	/// a guardarse sigue <c>Added</c>. Como el contexto vive lo que vive la pantalla, el siguiente
	/// guardado de esa pantalla reinsertaría lo pendiente o haría UPDATE sobre filas revertidas.
	/// Se limpia el tracker para que lo próximo se relea de la base.
	/// </remarks>
	public void RollbackTransaction()
	{
		TransaccionActual.Rollback();
		FinalizarTransaccion();
		_context.ChangeTracker.Clear();
	}

	public async Task RollbackTransactionAsync()
	{
		await TransaccionActual.RollbackAsync();
		FinalizarTransaccion();
		_context.ChangeTracker.Clear();
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