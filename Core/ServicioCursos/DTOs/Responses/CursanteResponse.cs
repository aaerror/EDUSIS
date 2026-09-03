namespace Core.ServicioCursos.DTOs.Responses;

public record CursanteResponse
{
    public Guid AlumnoID { get; init; }
    public string NombreCompleto { get; init; }
    public string Documento { get; init; }
    public string Edad { get; init; }


    public CursanteResponse(Guid alumnoID, string nombreCompleto, string documento, string edad)
    {
        AlumnoID = alumnoID;
        NombreCompleto = nombreCompleto;
        Documento = documento;
        Edad = edad;
    }
}
