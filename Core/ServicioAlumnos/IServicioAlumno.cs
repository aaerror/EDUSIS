using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;

namespace Core.ServicioAlumnos;

public interface IServicioAlumno
{
	Task<PersonaConDetallesResponse> BuscarPorIDAsync(PersonaRequest request);
	Task<PersonaResponse?> BuscarPorDNIAsync(DocumentoRequest request);
	Task<PersonaResponse?> BuscarPorNombreCompletoAsync(NombreCompletoRequest request);

	Task<Guid> RegistrarAlumnoAsync(RegistrarAlumnoRequest request);

	Task<bool> EsDocumentoInvalidoAsync(DocumentoRequest request);


	Task ActualizarContacto(CambiarContactoRequest request);
	Task ActualizarDomicilio(CambiarDomicilioRequest request);
	Task ActualizarSexo(CambiarSexoRequest request);


	Task ModificarNombreCompleto(Guid personaId, string nuevoApellido, string nuevoNombre);

	Task ActualizarDireccion(Guid personaId, DireccionRequest domicilioRequest);

	/// <summary>
	/// Qué destruye el borrado del alumno, sin borrar nada. Se consulta ANTES de
	/// <see cref="EliminarAlumnoAsync"/> para que la confirmación informe cifras.
	/// </summary>
	Task<ImpactoEliminacionAlumnoResponse> ConsultarImpactoEliminacionAsync(EliminarAlumnoRequest request);

	/// <summary>
	/// Borrado físico del alumno y de toda su trayectoria: inscripciones, calificaciones y registros
	/// de asistencia. No se puede deshacer.
	/// </summary>
	Task EliminarAlumnoAsync(EliminarAlumnoRequest request);
}