namespace Core.ServicioAlumnos.DTOs.Requests;

/// <summary>
/// Un único término para buscar un alumno: el documento, o parte del nombre o del apellido.
/// </summary>
/// <remarks>
/// Es un campo y no dos porque quien busca ya sabe qué dato tiene a mano, y obligarlo a declarar
/// primero en qué campo lo va a escribir es un paso que no aporta nada. Qué se hace con el término
/// —documento exacto si son sólo dígitos, coincidencia parcial en nombre y apellido si no— lo
/// decide <c>ServicioAlumno.BuscarAlumnosAsync</c>: es política de aplicación, no de la pantalla,
/// así que se prueba sin armar un ViewModel.
/// </remarks>
public record BuscarAlumnoRequest(string Termino);
