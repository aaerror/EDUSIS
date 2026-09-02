using Domain.Docentes;
using Domain.Docentes.Puestos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Docente"/> (<see cref="IDocenteRepository"/>) sobre <c>List&lt;Docente&gt;</c>.</summary>
public sealed class DocenteRepositorioFake : RepositorioDePersonasEnMemoria<Docente>, IDocenteRepository
{
	/// <summary>CUILs que la prueba fuerza como inválidos, además de los ya presentes en el repo.</summary>
	public HashSet<string> CuilsInvalidos { get; } = new();

	/// <summary>Legajos que la prueba fuerza como inválidos, además de los ya presentes en el repo.</summary>
	public HashSet<string> LegajosInvalidos { get; } = new();

	public Task<Docente?> BuscarDocentePorIDConPuestosAsync(Guid docenteID) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.Id.Equals(docenteID)));

	public Task<Docente?> BuscarDocentePorIDConLicenciasAsync(Guid docenteID) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.Id.Equals(docenteID)));

	public Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto)
	{
		IReadOnlyCollection<Docente> coincidencias = _entidades
			.Where(x => $"{x.DatosPersonales.Apellido} {x.DatosPersonales.Nombre}"
				.Contains(nombreCompleto, StringComparison.OrdinalIgnoreCase))
			.ToList();

		return Task.FromResult(coincidencias);
	}

	public Task<bool> EsCuilInvalidoAsync(string cuil) =>
		Task.FromResult(CuilsInvalidos.Contains(cuil) || _entidades.Any(x => x.CUIL == cuil));

	public Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		Task.FromResult(LegajosInvalidos.Contains(legajo) || _entidades.Any(x => x.Legajo == legajo));

	public Task<IReadOnlyCollection<Puesto>> PuestosPorDocenteAsync(Guid docenteID)
	{
		var docente = _entidades.FirstOrDefault(x => x.Id.Equals(docenteID));
		IReadOnlyCollection<Puesto> puestos = docente is null
			? Array.Empty<Puesto>()
			: docente.Puestos.ToList();

		return Task.FromResult(puestos);
	}
}
