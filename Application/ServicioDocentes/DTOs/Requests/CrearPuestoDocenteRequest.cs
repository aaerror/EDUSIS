namespace Core.ServicioDocentes.DTOs.Requests;

public record CrearPuestoDocenteRequest
{
    public Guid DocenteID { get; init; }
    public int Posicion { get; init; }
    public DateTime FechaInicio { get; init; }


    public CrearPuestoDocenteRequest(Guid docenteID, int posicion, DateTime fechaInicio)
    {
        DocenteID = docenteID;
        Posicion = posicion;
        FechaInicio = fechaInicio;
    }
}
