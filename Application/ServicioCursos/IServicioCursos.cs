using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;

namespace Core.ServicioCursos
{
    public interface IServicioCursos
    {
        void RegistrarCurso(CrearCursoRequest request);
        List<CursoResponse> BuscarCursos();

        void AgregarDivisionAlCurso(Guid unCurso);
        void QuitarDivisiosDelCurso(Guid unCurso, Guid unaDivision);

        IReadOnlyCollection<MateriaResponse> BuscarMaterias(Guid unCurso);
        
        void RegistrarMateriaEnCurso(Guid unCurso, CrearMateriaRequest request);
        void ActualizarMateria(EditarMateriaRequest request);
        void QuitarMateriaDelCurso(EliminarMateriaRequest request);
        void AsignarHorarioAMateriaDelCurso(CrearHorarioRequest request);

        SituacionRevistaResponse InscribirDocenteEnMateria(CrearSituacionRevistaRequest request);
        void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request);
        void EstablecerDocenteEnFunciones(CrearDocenteEnFuncionesRequest request);
    }
}