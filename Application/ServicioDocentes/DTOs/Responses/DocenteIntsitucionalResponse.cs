namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteIntsitucionalResponse
{
    public Guid DocenteID { get; init; }
    public string Documento { get; init; }
    public DateTime FechaAlta { get; init; }
    public DateTime? FechaBaja { get; init; }
    public string Legajo { get; init; }
    public string CUIL { get; init; }
    public bool EstaActivo { get; init; }


    public DocenteIntsitucionalResponse(Guid docenteId,
                                        string documento,
                                        DateTime fechaAlta,
                                        DateTime? fechaBaja,
                                        string legajo,
                                        string cuil,
                                        bool estaActivo)
    {
        DocenteID = docenteId;
        Documento = documento;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
        Legajo = legajo;
        CUIL = cuil;
        EstaActivo = estaActivo;
    }
}
