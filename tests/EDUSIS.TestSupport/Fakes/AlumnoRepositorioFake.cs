using Domain.Alumnos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Alumno"/> (<see cref="IAlumnoRepository"/>) sobre <c>List&lt;Alumno&gt;</c>.</summary>
public sealed class AlumnoRepositorioFake : RepositorioDePersonasEnMemoria<Alumno>, IAlumnoRepository
{
	/// <summary>Legajos que la prueba fuerza como inválidos. Por defecto todo legajo libre es válido.</summary>
	public HashSet<string> LegajosInvalidos { get; } = new();

	public bool EsLegajoValido(string legajo) =>
		!LegajosInvalidos.Contains(legajo) && _entidades.All(x => x.Legajo != legajo);

	public Task<Alumno?> BuscarPorNombreCompletoAsync(string nombreCompleto) =>
		Task.FromResult(_entidades.FirstOrDefault(x =>
			string.Equals(
				$"{x.DatosPersonales.Apellido} {x.DatosPersonales.Nombre}",
				nombreCompleto,
				StringComparison.OrdinalIgnoreCase)));

	public Task<Alumno?> BuscarPorDocumentoAsync(string documento) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.DatosPersonales.Documento == documento));
}
