using Core.Shared.DTOs.Personas;

namespace Core.ServicioAlumnos.DTOs.Requests;

public record RegistrarAlumnoRequest
{
    public InformacionPersonalDTO InformacionPersonalDTO { get; init; }
    public DomicilioDTO DomicilioDTO { get; init; }
    public ContactoDTO ContactoDTO { get; init; }


    public RegistrarAlumnoRequest(InformacionPersonalDTO informacionPersonalDTO, DomicilioDTO domicilioDTO, ContactoDTO contactoDTO)
    {
        InformacionPersonalDTO = informacionPersonalDTO;
        DomicilioDTO = domicilioDTO;
        ContactoDTO = contactoDTO;
    }
}
