using Domain.Shared;

namespace Domain.Personas;

public interface IPersonaRepository<TEntity> : IRepository<TEntity>
	where TEntity : Persona
{
	Task<bool> EsDocumentoInvalidoAsync(string documento);
	Task<bool> ExisteIDAsync(Guid id);
}