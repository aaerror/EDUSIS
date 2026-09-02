using System.Linq.Expressions;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Implementación en memoria de <see cref="IRepository{TEntity}"/> sobre una <see cref="List{T}"/>.
/// Es la base de los repos fake por agregado (contracts/test-support-api.md §4.2). No usa EF ni
/// expresiones traducibles: los predicados de <see cref="BuscarAsync"/> se compilan y evalúan en
/// memoria.
/// </summary>
public abstract class RepositorioEnMemoria<TEntity> : IRepository<TEntity>
	where TEntity : Entity
{
	protected readonly List<TEntity> _entidades = new();

	/// <summary>
	/// Nº de altas / bajas / modificaciones registradas desde el último
	/// <see cref="UnitOfWorkFake.GuardarCambiosAsync"/>. La unidad de trabajo lo consume y lo
	/// reinicia para calcular las "entidades afectadas simuladas".
	/// </summary>
	public int Afectadas { get; internal set; }

	/// <summary>Contenido actual del repo, sólo lectura, para aserciones directas en las pruebas.</summary>
	public IReadOnlyCollection<TEntity> Elementos => _entidades.AsReadOnly();

	#region Sembrado
	public void Sembrar(params TEntity[] entidades) =>
		_entidades.AddRange(entidades);
	#endregion

	#region IRepository<TEntity>
	public Task AgregarAsync(TEntity entity)
	{
		_entidades.Add(entity);
		Afectadas++;
		return Task.CompletedTask;
	}

	public Task AgregarRangoAsync(IEnumerable<TEntity> entities)
	{
		foreach (var entity in entities)
		{
			_entidades.Add(entity);
			Afectadas++;
		}

		return Task.CompletedTask;
	}

	public Task<TEntity?> BuscarPorIDAsync(params object[] ids)
	{
		var id = ExtraerId(ids);
		return Task.FromResult(_entidades.FirstOrDefault(x => x.Id.Equals(id)));
	}

	public Task<IEnumerable<TEntity>> BuscarTodosAsync() =>
		Task.FromResult(_entidades.AsEnumerable());

	public Task<IEnumerable<TEntity>> BuscarAsync(Expression<Func<TEntity, bool>> predicate) =>
		Task.FromResult(_entidades.Where(predicate.Compile()));

	public void Modificar(TEntity entity)
	{
		if (!_entidades.Contains(entity))
		{
			_entidades.Add(entity);
		}

		Afectadas++;
	}

	public void ModificarRango(IEnumerable<TEntity> entities)
	{
		foreach (var entity in entities)
		{
			Modificar(entity);
		}
	}

	public Task Eliminar(params object[] ids)
	{
		var id = ExtraerId(ids);
		var entidad = _entidades.FirstOrDefault(x => x.Id.Equals(id));
		if (entidad is not null)
		{
			_entidades.Remove(entidad);
			Afectadas++;
		}

		return Task.CompletedTask;
	}

	public void EliminarRango(IEnumerable<TEntity> entities)
	{
		foreach (var entity in entities)
		{
			if (_entidades.Remove(entity))
			{
				Afectadas++;
			}
		}
	}
	#endregion

	/// <summary>
	/// La superficie real de <c>BuscarPorIDAsync</c>/<c>Eliminar</c> es <c>params object[]</c>:
	/// EF acepta claves compuestas (p. ej. <c>Licencia</c> = LicenciaID + DocenteID). El fake
	/// resuelve por el primer <see cref="Guid"/> recibido, que siempre es el <c>Id</c> del agregado.
	/// </summary>
	private protected static Guid ExtraerId(object[] ids)
	{
		if (ids is null || ids.Length == 0)
		{
			return Guid.Empty;
		}

		return ids[0] switch
		{
			Guid guid => guid,
			string texto when Guid.TryParse(texto, out var parseado) => parseado,
			_ => Guid.Empty
		};
	}
}
