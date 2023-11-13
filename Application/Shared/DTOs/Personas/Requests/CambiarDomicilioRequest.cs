namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarDomicilioRequest
{
    public Guid PersonaID { get; init; }
    public string Calle { get; init; }
    public string Altura { get; init; }
    public int Vivienda { get; init; }
    public string Observacion { get; init; }
    public string Localidad { get; init; }
    public string Provincia { get; init; }
    public string Pais { get; init; }


    public CambiarDomicilioRequest(Guid personaID, string calle, string altura, int vivienda, string observacion, string localidad, string provincia, string pais)
    {
        PersonaID = personaID;
        Calle = calle;
        Altura = altura;
        Vivienda = vivienda;
        Observacion = observacion;
        Localidad = localidad;
        Provincia = provincia;
        Pais = pais;
    }
}
