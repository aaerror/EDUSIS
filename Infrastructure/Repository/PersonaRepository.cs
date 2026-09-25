using Domain.Personas;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal abstract class PersonaRepository<TEntity> : Repository<TEntity>, IPersonaRepository<TEntity>
	where TEntity : Persona
{
	protected EdusisDBContext DbContext => (EdusisDBContext)Context;

	public PersonaRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> EsDocumentoInvalidoAsync(string documento) =>
		await DbContext.Set<TEntity>().AnyAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));

	public async Task<bool> ExisteIDAsync(Guid id) =>
		await DbContext.Set<TEntity>().AnyAsync(x => x.Id.Equals(id));
}
