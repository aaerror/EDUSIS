namespace Core.ServicioCusos.DTOs.Responses;

public record DivisionResponse
{
    /*public Guid CursoID { get; init; }
    public string CursoDescripcion { get; init; }
    public string NivelEducativo { get; init; }*/
    public Guid DivisionID { get; init; }
    public string Descripcion { get; init; }
    public string Preceptor { get; init; }
    public int Alumnos { get; init; }


    public DivisionResponse(Guid divisionID, string descripcion, string preceptor, int alumnos)
    {
        DivisionID = divisionID;
        Descripcion = descripcion;
        Preceptor = preceptor;
        Alumnos = alumnos;
    }
}
