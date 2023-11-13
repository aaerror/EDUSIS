using Core.Shared.DTOs.Personas;

namespace Core.ServicioDocentes.DTOs.Requests;

public record RegistrarDocenteRequest
{
    public string Legajo { get; init; }
    public string CUIL { get; init; }
    public RegistrarPuestoDocenteRequest Puesto { get; init; }
    public InformacionPersonalDTO InformacionPersonalDTO { get; init; }
    public ContactoDTO ContactoDTO { get; init; }
    public DomicilioDTO DomicilioDTO { get; init; }


    public RegistrarDocenteRequest(string legajo, string cuil, RegistrarPuestoDocenteRequest puesto, InformacionPersonalDTO informacionPersonalDTO, ContactoDTO contactoDTO, DomicilioDTO domicilioDTO)
    {
        Legajo = legajo;
        CUIL = cuil;
        Puesto = puesto;
        InformacionPersonalDTO = informacionPersonalDTO;
        ContactoDTO = contactoDTO;
        DomicilioDTO = domicilioDTO;
    }
}
