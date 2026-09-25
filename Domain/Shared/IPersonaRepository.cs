using Domain.Personas;

namespace Domain.Shared;

/// <summary>
/// Base compartida por IAlumnoRepository e IDocenteRepository.
/// Persona no es una raíz de agregado; este repositorio no es instanciable.
/// </summary>
public interface IPersonaRepository<TEntity> : IRepository<TEntity>
	where TEntity : Persona
{
	Task<bool> EsDocumentoInvalidoAsync(string documento);
	Task<bool> ExisteIDAsync(Guid id);
}