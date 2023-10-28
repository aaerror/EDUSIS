namespace Core.ServicioCursos.DTOs.Requests;

public record CrearSituacionRevistaProfesorRequest
{
    public Guid CursoId { get; init; }
    public Guid MateriaId { get; init; }
    public Guid ProfesorId { get; init; }
    public int Cargo { get; init; }
    public DateTime FechaAlta { get; init; }


    public CrearSituacionRevistaProfesorRequest(Guid cursoId, Guid materiaId, Guid profesorId, int cargo, DateTime fechaAlta)
    {
        CursoId = cursoId;
        MateriaId = materiaId;
        ProfesorId = profesorId;
        Cargo = cargo;
        FechaAlta = fechaAlta;
    }
}
