namespace Core.ServicioCursos.DTOs.Requests;

public record CrearSituacionRevistaRequest
{
    public Guid CursoId { get; init; }
    public Guid MateriaId { get; init; }
    public Guid ProfesorId { get; init; }
    public int Cargo { get; init; }
    public DateTime FechaAlta { get; init; }
    public bool EnFunciones { get; init; }


    public CrearSituacionRevistaRequest(Guid cursoId, Guid materiaId, Guid profesorId, int cargo, DateTime fechaAlta, bool enFunciones)
    {
        CursoId = cursoId;
        MateriaId = materiaId;
        ProfesorId = profesorId;
        Cargo = cargo;
        FechaAlta = fechaAlta;
        EnFunciones = enFunciones;
    }
}
