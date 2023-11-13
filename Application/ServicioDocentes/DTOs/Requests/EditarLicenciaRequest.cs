using Core.ServicioDocentes.DTOs.Responses;

namespace Core.ServicioDocentes.DTOs.Requests;

public class EditarLicenciaRequest
{
    public Guid DocenteID { get; init; }
    public LicenciaResponse Licencia { get; init; }
    public int Articulo { get; init; }
    public int Dias { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Observacion { get; init; }


    public EditarLicenciaRequest(Guid docenteID, LicenciaResponse licencia, int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        DocenteID = docenteID;
        Licencia = licencia;
        Articulo = articulo;
        Dias = dias;
        FechaInicio = fechaInicio;
        Observacion = observacion;
    }
}
