using Domain.Materias;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class MateriaRepository : Repository<Materia>, IMateriaRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;


	public MateriaRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion) =>
		await _context.Materias.AnyAsync(x => x.CurriculaID == unaCurricula && x.Descripcion.ToLower() == descripcion.Trim().ToLower());

	public async Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion, Guid excluirMateria) =>
		await _context.Materias.AnyAsync(x => x.CurriculaID == unaCurricula && x.Descripcion.ToLower() == descripcion.Trim().ToLower() && x.Id != excluirMateria);

	public async Task<IReadOnlyCollection<Materia>> BuscarMateriasSegunCurriculaAsync(Guid unaCurricula) =>
		await Consulta()
			.Where(x => x.CurriculaID == unaCurricula)
			.ToListAsync();

	public async Task<int> TotalHorasCatedraSegunCurriculaAsync(Guid unaCurricula) =>
		await _context.Materias
			.Where(x => x.CurriculaID == unaCurricula)
			.SumAsync(x => x.HorasCatedra);

	public async Task<int> TotalEspaciosSegunCurriculaAsync(Guid unaCurricula) =>
		await _context.Materias.CountAsync(x => x.CurriculaID == unaCurricula);
}
