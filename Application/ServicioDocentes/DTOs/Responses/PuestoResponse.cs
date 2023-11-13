namespace Core.ServicioDocentes.DTOs.Responses;

public record PuestoResponse
{
    public int PosicionIndex { get; init; }
    public string PosicionDescripcion { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }


    public PuestoResponse(int posicionIndex, string posicionDescripcion, DateTime fechaInicio, DateTime? fechaFin)
    {
        PosicionIndex = posicionIndex;
        PosicionDescripcion = posicionDescripcion;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }
}
