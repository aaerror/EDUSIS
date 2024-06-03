namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarSituacionRevistaRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }
    public Guid ProfesorID { get; init; }
    public int Cargo { get; init; }
    public DateTime FechaAlta { get; init; }
    public bool EnFunciones { get; init; }


    public RegistrarSituacionRevistaRequest(Guid cursoID, Guid materiaID, Guid profesorID, int cargo, DateTime fechaAlta, bool enFunciones)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        ProfesorID = profesorID;
        Cargo = cargo;
        FechaAlta = fechaAlta;
        EnFunciones = enFunciones;
    }
}
