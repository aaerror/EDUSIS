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
using Domain.Shared.Exceptions;
using Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
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

		// Después del despacho: los handlers también agregan y modifican entidades internas.
		MarcarRaicesConCambiosInternos();

		try
		{
			return await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException ex)
		{
			// El tipo de EF no sale de Infrastructure: la UI muestra ex.Message, y el de EF está en
			// inglés y habla de filas afectadas.
			var agregados = string.Join(", ", ex.Entries.Select(x => x.Metadata.ClrType.Name).Distinct());
			throw new ConflictoDeConcurrenciaException(agregados, ex);
		}
	}

	#region Concurrencia del agregado
	/// <summary>
	/// Marca como modificada la raíz de cada entidad interna que cambió, para que su UPDATE
	/// verifique y avance la versión del agregado entero.
	/// </summary>
	/// <remarks>
	/// <para>
	/// La columna <c>version</c> sólo vive en la raíz. Sin esto, agregar una calificación a un
	/// cursante —un INSERT en <c>calificacion</c>— no toca la fila de <c>cursante</c>, y dos
	/// estaciones que editan el mismo agregado por sus partes internas no se enteran una de la otra:
	/// la concurrencia quedaría limitada a los campos propios de la raíz.
	/// </para>
	/// <para>
	/// Sólo cuenta la relación de PERTENENCIA: la raíz navega hacia la entidad interna
	/// (<c>Cursante.Calificaciones</c>, <c>Catedra.Horarios</c>) o la entidad es la tabla de unión de
	/// una colección de la raíz (<c>usuario_rol</c>). Una clave ajena hacia otro agregado
	/// (<c>Calificacion → Materia</c>) no marca a ese otro agregado.
	/// </para>
	/// </remarks>
	private void MarcarRaicesConCambiosInternos()
	{
		_context.ChangeTracker.DetectChanges();

		var internasConCambios = _context.ChangeTracker.Entries()
			.Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
					 && !TieneVersion(x.Metadata))
			.ToList();

		foreach (var interna in internasConCambios)
		{
			foreach (var claveAjena in interna.Metadata.GetForeignKeys())
			{
				var esPertenencia = claveAjena.PrincipalToDependent is not null || interna.Metadata.IsPropertyBag;
				if (!esPertenencia || !TieneVersion(claveAjena.PrincipalEntityType))
				{
					continue;
				}

				var raiz = BuscarPrincipal(interna, claveAjena);
				if (raiz is { State: EntityState.Unchanged })
				{
					raiz.State = EntityState.Modified;
				}
			}
		}
	}

	private static bool TieneVersion(IEntityType tipo) =>
		tipo.FindProperty(ConfiguracionVersionExtensions.PROPIEDAD) is not null;

	/// <summary>La entrada rastreada de la raíz a la que apunta la clave ajena, o <c>null</c> si no se cargó.</summary>
	private EntityEntry? BuscarPrincipal(EntityEntry interna, IForeignKey claveAjena)
	{
		// Una entidad borrada ya no tiene valor actual confiable en la clave ajena: se usa el original.
		var valores = claveAjena.Properties
			.Select(propiedad => interna.State == EntityState.Deleted
				? interna.OriginalValues[propiedad]
				: interna.CurrentValues[propiedad])
			.ToArray();

		if (valores.Any(valor => valor is null))
		{
			return null;
		}

		return _context.ChangeTracker.Entries()
			.FirstOrDefault(candidata =>
				claveAjena.PrincipalEntityType.IsAssignableFrom(candidata.Metadata)
			 && claveAjena.PrincipalKey.Properties
					.Select(propiedad => candidata.Property(propiedad.Name).CurrentValue)
					.SequenceEqual(valores));
	}
	#endregion
}