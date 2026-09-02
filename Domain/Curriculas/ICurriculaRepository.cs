using Domain.Shared;

namespace Domain.Curriculas;

public interface ICurriculaRepository : IRepository<Curricula>
 {
	Task<Curricula?> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula);
	Task<IEnumerable<Curricula>> CurriculasSegunCursoAsync(Guid unCurso);
}