using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;

namespace Core.ServicioMaterias;

public interface IServicioMateria
{
    IReadOnlyCollection<MateriaResponse> ListarMateriasSegunCurso(ListarMateriasSegunCursoRequest request);

    Task RegistrarMateria(RegistrarMateriaRequest request);
    void ModificarMateria(ModificarMateriaRequest request);
    void EliminarMateria(EliminarMateriaRequest request);

    IReadOnlyCollection<SituacionRevistaResponse> ListarCargosDocenteSegunMateria(ListarCargosDocentesSegunMateriaRequest request);
    SituacionRevistaResponse RegistrarDocenteEnMateria(RegistrarDocenteEnMateriaRequest request);
    void EstablecerDocenteDeAula(EstablecerDocenteDeAulaRequest request);
    void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request);

    void RegistrarHorarioEnMateria(RegistrarHorarioEnMateriaRequest request);
    IReadOnlyCollection<HorarioResponse> ListarHorariosDeMateria(ListarHorariosRequest request);


}