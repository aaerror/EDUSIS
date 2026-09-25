using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Vista no genérica de un repositorio en memoria, para que <see cref="UnitOfWorkFake"/> recorra
/// todos los fakes sin repetir código por agregado.
/// </summary>
internal interface IRepositorioEnMemoria
{
	IEnumerable<Entity> Entidades { get; }

	/// <summary>Devuelve las afectadas acumuladas desde el último guardado y reinicia el contador.</summary>
	int ConsumirAfectadas();
}

/// <summary>
/// Implementación en memoria de <see cref="IRepository{TEntity}"/> sobre una <see cref="List{T}"/>.
/// Es la base de los repos fake por agregado. No usa EF: los métodos se ejecutan sobre
/// <see cref="_entidades"/> en memoria.
/// </summary>
public abstract class RepositorioEnMemoria<TEntity> : IRepository<TEntity>, IRepositorioEnMemoria
	where TEntity : Entity
{
	protected readonly List<TEntity> _entidades = new();

	/// <summary>
	/// Nº de altas / bajas / modificaciones registradas desde el último
	/// <see cref="UnitOfWorkFake.GuardarCambiosAsync"/>. La unidad de trabajo lo consume y lo
	/// reinicia para calcular las "entidades afectadas simuladas".
	/// </summary>
	public int Afectadas { get; private set; }

	/// <summary>Contenido actual del repo, sólo lectura, para aserciones directas en las pruebas.</summary>
	public IReadOnlyCollection<TEntity> Elementos => _entidades.AsReadOnly();

	IEnumerable<Entity> IRepositorioEnMemoria.Entidades => _entidades;

	int IRepositorioEnMemoria.ConsumirAfectadas()
	{
		var afectadas = Afectadas;
		Afectadas = 0;
		return afectadas;
	}

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

	public Task<TEntity?> BuscarPorIDAsync(Guid id) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.Id.Equals(id)));

	public Task<IReadOnlyCollection<TEntity>> BuscarTodosAsync() =>
		Task.FromResult((IReadOnlyCollection<TEntity>)_entidades.ToList());

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

	public Task EliminarAsync(Guid id)
	{
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
}
