namespace Core.ServicioDocentes.DTOs.Requests;

public record EliminarPuestoDocenteRequest
{
    public Guid DocenteID { get; init; }
    public int Puesto { get; init; }
    public DateTime FechaInicio { get; init; }


    public EliminarPuestoDocenteRequest(Guid docenteID, int puesto, DateTime fechaInicio)
    {
        DocenteID = docenteID;
        Puesto = puesto;
        FechaInicio = fechaInicio;
    }
}
