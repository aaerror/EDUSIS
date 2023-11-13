namespace Core.ServicioDocentes.DTOs.Requests;

public record RegistrarLicenciaDocenteRequest
{
    public Guid DocenteID { get; init; }
    public int Articulo { get; init; }
    public int Dias { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Observacion { get; init;}


    public RegistrarLicenciaDocenteRequest(Guid docenteID, int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        DocenteID = docenteID;
        Articulo = articulo;
        Dias = dias;
        FechaInicio = fechaInicio;
        Observacion = observacion;
    }
}
