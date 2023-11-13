namespace Core.ServicioDocentes.DTOs.Requests;

public record EditarEstadoLicenciaRequest
{
    public Guid DocenteID { get; init; }
    public int Articulo { get; init; }
    public int Dias { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Observacion { get; init; }
   


    public EditarEstadoLicenciaRequest(Guid docenteID, int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        DocenteID = docenteID;
        Articulo = articulo;
        Dias = dias;
        FechaInicio = fechaInicio;
        Observacion = observacion;
    }
}