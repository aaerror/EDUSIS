using Domain.Shared;

namespace Domain.Personas;

public interface IPersonaRepository<TEntity> : IRepository<TEntity> where TEntity : Persona
{
    bool EsDocumentoValido(string documento);
}
