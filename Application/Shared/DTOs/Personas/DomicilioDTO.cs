namespace Core.Shared.DTOs.Personas;

public record DomicilioDTO
{
    public string Calle { get; init; }
    public string Altura { get; init; }
    public int Vivienda { get; init; }
    public string Observacion { get; init; }
    public string Localidad { get; init; }
    public string Provincia { get; init; }
    public string Pais { get; init; }


    public DomicilioDTO(string calle, string altura, int vivienda, string observacion, string localidad, string provincia, string pais)
    {
        Calle = calle;
        Altura = altura;
        Vivienda = vivienda;
        Observacion = observacion;
        Localidad = localidad;
        Provincia = provincia;
        Pais = pais;
    }
}
