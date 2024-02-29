namespace Core.ServicioCusos.DTOs.Responses;

public record DivisionResponse
{
    public Guid CursoID { get; init; }
    public string CursoDescripcion { get; init; }
    public string NivelEducativo { get; init; }
    public Guid DivisionID { get; init; }
    public string DivisionDescripcion { get; init; }
    public int Alumnos {  get; init; }


    public DivisionResponse(Guid cursoID, string cursoDescripcion, string nivelEducativo, Guid divisionID, string divisionDescripcion, int alumnos)
    {
        CursoID = cursoID;
        CursoDescripcion = cursoDescripcion;
        NivelEducativo = nivelEducativo;
        DivisionID = divisionID;
        DivisionDescripcion = divisionDescripcion;
        Alumnos = alumnos;
    }
}
