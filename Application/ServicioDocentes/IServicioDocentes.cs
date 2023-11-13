using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Domain.Docentes;

namespace Core.ServicioDocentes
{
    public interface IServicioDocentes
    {
        DocenteConDetalleResponse BuscarDocenteConDetalle(Guid docenteID);
        DocenteInfoResponse BuscarDocentePorDNI(string documento);
        DocenteConPuestosResponse BuscarDocenteConPuestos(Guid docenteID);
        bool EsCuilInvalido(string cuil);
        bool EsDocumentoInvalido(string documento);
        bool EsLegajoInvalido(string legajo);
        void RegistrarDocente(RegistrarDocenteRequest request);
        void ModificarContacto(CambiarContactoRequest request);
        void ModificarDomicilio(CambiarDomicilioRequest request);
        void ModificarSexo(CambiarSexoRequest request);

        IReadOnlyCollection<LicenciaResponse> BuscarLicencias(Guid docenteID);
        LicenciaResponse RegistrarLicencia(RegistrarLicenciaDocenteRequest request);
        LicenciaResponse ModificarLicencia(EditarLicenciaRequest request);
        void AprobarLicencia(EditarEstadoLicenciaRequest request);
        void CancelarLicencia(EditarEstadoLicenciaRequest request);

        IReadOnlyCollection<PuestoResponse> BuscarPuestos(Guid docenteID);
        PuestoResponse AsignarPuestoDocente(CrearPuestoDocenteRequest request);
        PuestoResponse QuitarPuestoDocente(EliminarPuestoDocenteRequest request);
        void QuitarDocente(Guid docenteId);
    }
}