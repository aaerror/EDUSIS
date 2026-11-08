using Domain.Alumnos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Alumno"/> (<see cref="IAlumnoRepository"/>) sobre <c>List&lt;Alumno&gt;</c>.</summary>
public sealed class AlumnoRepositorioFake : RepositorioDePersonasEnMemoria<Alumno>, IAlumnoRepository
{
	/// <summary>Legajos que la prueba fuerza como inválidos. Por defecto todo legajo libre es válido.</summary>
	public HashSet<string> LegajosInvalidos { get; } = new();

	public Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		Task.FromResult(LegajosInvalidos.Contains(legajo) || _entidades.Any(x => x.Legajo == legajo));

	public Task<string?> BuscarUltimoLegajoAsync() =>
		Task.FromResult<string?>(_entidades.Select(x => x.Legajo).OrderByDescending(x => x).FirstOrDefault());

	/// <remarks>
	/// <c>Contains</c> con <c>OrdinalIgnoreCase</c> imita al <c>LIKE '%…%'</c> del repositorio real
	/// sobre una colación insensible a mayúsculas. Lo que el fake NO puede imitar es la colación
	/// insensible a <strong>tildes</strong> de SQL Server: acá "perez" no encuentra a "Pérez" y en
	/// la base sí. Es la misma ceguera de LINQ to Objects que registra CORE-23; la cubre
	/// <c>AlumnoRepositorioTests</c> contra base real.
	/// </remarks>
	public Task<IReadOnlyCollection<Alumno>> BuscarSegunNombreCompletoAsync(string termino)
	{
		var patron = termino.Trim();

		IReadOnlyCollection<Alumno> encontrados = _entidades
			.Where(x => x.DatosPersonales.Apellido.Contains(patron, StringComparison.OrdinalIgnoreCase) ||
						x.DatosPersonales.Nombre.Contains(patron, StringComparison.OrdinalIgnoreCase))
			.OrderBy(x => x.DatosPersonales.Apellido)
			.ThenBy(x => x.DatosPersonales.Nombre)
			.ToList();

		return Task.FromResult(encontrados);
	}

	public Task<Alumno?> BuscarPorDocumentoAsync(string documento) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.DatosPersonales.Documento == documento));
}
