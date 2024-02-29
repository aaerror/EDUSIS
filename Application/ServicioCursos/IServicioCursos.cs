using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCusos.DTOs.Responses;

namespace Core.ServicioCursos
{
    public interface IServicioCursos
    {
        void RegistrarCurso(CrearCursoRequest request);
        IReadOnlyCollection<CursoResponse> BuscarCursos();

        #region Divisiones
        IReadOnlyCollection<DivisionResponse> BuscarDivisiones(Guid unCurso);
        IReadOnlyCollection<CursanteResponse> BuscarListado(BuscarListadoRequest request);
        void InscribirAlumnoEnDivision(CrearCursanteRequest request);
        void AgregarDivisionAlCurso(Guid unCurso);
        void QuitarDivisiosDelCurso(EliminarDivisionRequest request);
        #endregion

        #region Calificación
        void RegistrarCalificacion(CrearCalificationRequest request);
        #endregion

        #region Materias
        IReadOnlyCollection<MateriaResponse> BuscarMaterias(Guid unCurso);
        void RegistrarMateriaEnCurso(Guid unCurso, CrearMateriaRequest request);
        void ActualizarMateria(EditarMateriaRequest request);
        void QuitarMateriaDelCurso(EliminarMateriaRequest request);
        void AsignarHorarioAMateriaDelCurso(CrearHorarioRequest request);
        #endregion

        #region Situacion Revista
        SituacionRevistaResponse InscribirDocenteEnMateria(CrearSituacionRevistaRequest request);
        void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request);
        void EstablecerDocenteEnFunciones(CrearDocenteEnFuncionesRequest request);
        #endregion
    }
}