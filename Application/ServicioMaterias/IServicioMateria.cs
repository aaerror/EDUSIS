using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;

namespace Core.ServicioMaterias
{
    public interface IServicioMateria
    {
        MateriaResponse BuscarMateria(BuscarMateriaRequest request);
        IReadOnlyCollection<MateriaResponse> BuscarMateriaSegunCurso(BuscarMateriaSegunCursoRequest request);

        bool EsNombreDuplicadoMateria(NombreDuplicadoRequest request);

        Task RegistrarMateria(RegistrarMateriaRequest request);
        void ModificarMateria(ModificarMateriaRequest request);
        void EliminarMateria(EliminarMateriaRequest request);

        IReadOnlyCollection<SituacionRevistaResponse> HistoricoSituacionRevista(HistoricoSituacionRevistaRequest request);
        SituacionRevistaResponse RegistrarDocenteEnMateria(RegistrarSituacionRevistaRequest request);
        void EstablecerDocenteEnFunciones(RegistrarDocenteEnFuncionesRequest request);
        void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request);

        void RegistrarHorario(RegistrarHorarioEnMateria request);
        IReadOnlyCollection<HorarioResponse> HorariosDeMateria(BuscarHorariosRequest request);


    }
}