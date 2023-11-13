namespace Core.ServicioDocentes.DTOs.Requests;

public record RegistrarPuestoDocenteRequest
{
    public int Posicion {  get; init; }
    public DateTime FechaInicio { get; init; }


    public RegistrarPuestoDocenteRequest(int posicion, DateTime fechaInicio)
    {
        Posicion = posicion;
        FechaInicio = fechaInicio;
    }
}
