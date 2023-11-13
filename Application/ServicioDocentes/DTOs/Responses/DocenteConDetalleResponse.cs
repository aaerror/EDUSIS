using Core.Shared.DTOs.Personas;

namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteConDetalleResponse
{
    public Guid DocenteID { get; init; }
    public InformacionPersonalDTO InformacionPersonalDTO { get; init; }
    public DomicilioDTO DomicilioDTO { get; init; }
    public ContactoDTO ContactoDTO { get; init; }
    public DocenteIntsitucionalResponse DocenteIntsitucionalResponse { get; init; }
    public IReadOnlyCollection<PuestoResponse> Puestos { get; init; }


    public DocenteConDetalleResponse(Guid docenteID,
                                     InformacionPersonalDTO informacionPersonal,
                                     DomicilioDTO domicilio,
                                     ContactoDTO contacto,
                                     IReadOnlyCollection<PuestoResponse> puestos)
    {
        DocenteID = docenteID;
        InformacionPersonalDTO = informacionPersonal;
        DomicilioDTO = domicilio;
        ContactoDTO = contacto;
        Puestos = puestos;
    }
}
