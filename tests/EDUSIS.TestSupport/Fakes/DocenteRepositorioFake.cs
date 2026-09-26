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

	public Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto)
	{
		IReadOnlyCollection<Docente> coincidencias = _entidades
			.Where(x =>
				x.DatosPersonales.Nombre.Trim().ToLower().Contains(nombreCompleto.Trim().ToLower()) ||
				x.DatosPersonales.Apellido.Trim().ToLower().Contains(nombreCompleto.Trim().ToLower()))
			.ToList();

		return Task.FromResult(coincidencias);
	}

	public Task<IReadOnlyCollection<Docente>> BuscarActivosAsync()
	{
		IReadOnlyCollection<Docente> activos = _entidades
			.Where(x => x.Activo)
			.ToList();

		return Task.FromResult(activos);
	}

	public Task<IReadOnlyCollection<Docente>> BuscarSegunPosicionAsync(Posicion posicion)
	{
		IReadOnlyCollection<Docente> coincidencias = _entidades
			.Where(x => x.Puestos.Any(p => p.Posicion == posicion && p.Estado == EstadoPuesto.Activo))
			.ToList();

		return Task.FromResult(coincidencias);
	}

	public Task<bool> ExisteDocenteConLegajoAsync(Guid docenteID, string legajo) =>
		Task.FromResult(_entidades.Any(x => x.Id.Equals(docenteID) && x.Legajo == legajo));

	public Task<bool> EsCuilInvalidoAsync(string cuil) =>
		Task.FromResult(CuilsInvalidos.Contains(cuil) || _entidades.Any(x => x.CUIL == cuil));

	public Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		Task.FromResult(LegajosInvalidos.Contains(legajo) || _entidades.Any(x => x.Legajo == legajo));
}
