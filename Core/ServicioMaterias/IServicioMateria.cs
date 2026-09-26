using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;

namespace Core.ServicioMaterias;

public interface IServicioMateria
{
	Task<IReadOnlyCollection<MateriaResponse>> ListarMateriasSegunCurriculaAsync(ListarMateriasSegunCurriculaRequest request);
	Task<Guid> RegistrarMateriaAsync(RegistrarMateriaRequest request);
	Task ModificarMateriaAsync(ModificarMateriaRequest request);
	Task EliminarMateriaAsync(EliminarMateriaRequest request);
}
