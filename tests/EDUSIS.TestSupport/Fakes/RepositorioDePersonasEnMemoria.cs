using Domain.Personas;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Base en memoria para los repos de agregados que heredan de <see cref="Persona"/>
/// (<see cref="Domain.Alumnos.Alumno"/>, <see cref="Domain.Docentes.Docente"/>). Implementa
/// <see cref="IPersonaRepository{TEntity}"/>.
/// </summary>
public abstract class RepositorioDePersonasEnMemoria<TEntity> : RepositorioEnMemoria<TEntity>, IPersonaRepository<TEntity>
	where TEntity : Persona
{
	/// <summary>
	/// Documentos que la prueba declara como inválidos de antemano. Además, cualquier documento
	/// ya presente en el repo se considera inválido (regla de unicidad).
	/// </summary>
	public HashSet<string> DocumentosInvalidos { get; } = new();

	public Task<bool> EsDocumentoInvalidoAsync(string documento) =>
		Task.FromResult(DocumentosInvalidos.Contains(documento)
			|| _entidades.Any(x => x.DatosPersonales.Documento == documento));

	public Task<bool> ExisteIDAsync(Guid id) =>
		Task.FromResult(_entidades.Any(x => x.Id.Equals(id)));
}
