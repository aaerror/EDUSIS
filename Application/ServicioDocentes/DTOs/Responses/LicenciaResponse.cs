namespace Core.ServicioDocentes.DTOs.Responses;

public record LicenciaResponse
{
    public int Articulo { get; init; }
    public int Estado { get; init; }
    public int Dias { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public string Observacion { get; init; }


    public LicenciaResponse(int articulo, int estado, int dias, DateTime fechaInicio, DateTime? fechaFin, string observacion)
    {
        Articulo = articulo;
        Estado = estado;
        Dias = dias;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        Observacion = observacion;
    }
}
