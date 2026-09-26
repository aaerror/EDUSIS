using Domain.Shared;

namespace Domain.Cursos;

public interface ICursoRepository : IRepository<Curso>
{
	Task<bool> ExisteCursoAsync(Grado grado, NivelEducativo nivelEducativo);
}