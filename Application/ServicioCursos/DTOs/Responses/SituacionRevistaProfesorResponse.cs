namespace Core.ServicioCursos.DTOs.Responses;

public record SituacionRevistaProfesorResponse
{
    public Guid Profesor { get; set; }
    public string NombreCompleto { get; set; }
    public string Cargo { get; set; }


    public SituacionRevistaProfesorResponse(Guid profesor, string nombreCompleto, string cargo)
    {
        Profesor = profesor;
        NombreCompleto = nombreCompleto;
        Cargo = cargo;
    }
}
