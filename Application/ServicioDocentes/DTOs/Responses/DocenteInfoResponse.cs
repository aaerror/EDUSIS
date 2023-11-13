namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteInfoResponse
{
    public Guid DocenteID { get; init; }
    public string NombreCompleto { get; init; }
    public DocenteIntsitucionalResponse Institucional { get; init; }

    public DocenteInfoResponse(Guid docenteID, string nombreCompleto, DocenteIntsitucionalResponse institucional)
    {
        DocenteID = docenteID;
        NombreCompleto = nombreCompleto;
        Institucional = institucional;
    }
}
