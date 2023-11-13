namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteConPuestosResponse
{
    public Guid DocenteID { get; init; }
    public string Legajo { get; init; }
    public string Documento { get; init; }
    public string NombreCompleto { get; init; }
    public IReadOnlyCollection<PuestoResponse> Puestos { get; init; }


    public DocenteConPuestosResponse(Guid docenteID, string legajo, string documento, string nombreCompleto, List<PuestoResponse> puestos)
    {
        DocenteID = docenteID;
        Legajo = legajo;
        Documento = documento;
        NombreCompleto = nombreCompleto;
        Puestos = puestos.AsReadOnly();
    }
}
