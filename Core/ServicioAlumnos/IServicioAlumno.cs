using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;

namespace Core.ServicioAlumnos;

public interface IServicioAlumno
{
	Task<PersonaConDetallesResponse> BuscarPorIDAsync(PersonaRequest request);
	Task<PersonaResponse?> BuscarPorDNIAsync(DocumentoRequest request);

	/// <summary>
	/// Los alumnos que coinciden con el término: documento exacto si son sólo dígitos, coincidencia
	/// parcial en nombre o apellido si no. Una colección vacía significa que no hubo coincidencias.
	/// </summary>
	/// <remarks>
	/// Reemplaza a <c>BuscarPorNombreCompletoAsync</c>, que pedía el nombre completo exacto con su
	/// coma y sus tildes y devolvía un solo alumno. Esa forma es la que dejaba a la pantalla de
	/// alumnos sin listado y con el documento como único camino —limitación que su propio XAML
	/// documentaba— mientras Docentes y Cursos sí tenían índice.
	/// </remarks>
	Task<IReadOnlyCollection<PersonaResponse>> BuscarAlumnosAsync(BuscarAlumnoRequest request);

	/// <summary>
	/// Qué cursa el alumno, cómo le va y cuánto asiste, todo referido a un solo ciclo lectivo.
	/// </summary>
	Task<ResumenAcademicoResponse> ConsultarResumenAcademicoAsync(PersonaRequest request);

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
