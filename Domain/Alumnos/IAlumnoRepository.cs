using Domain.Personas;
using Domain.Shared;

namespace Domain.Alumnos;

public interface IAlumnoRepository : IPersonaRepository<Alumno>
{
	Task<bool> EsLegajoInvalidoAsync(string legajo);
	Task<string?> BuscarUltimoLegajoAsync();

	/// <summary>
	/// Alumnos cuyo nombre <strong>o</strong> apellido contiene el término, ordenados por apellido
	/// y nombre. Una colección vacía significa que no hubo coincidencias, que no es un error.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Reemplaza a <c>BuscarPorNombreCompletoAsync</c>, que hacía <c>LIKE</c> exacto sobre
	/// <c>Apellido + ", " + Nombre</c> y devolvía <strong>un</strong> alumno. Para encontrar a
	/// alguien había que tipear "Pérez, Juan" con la coma, el espacio y la tilde en su lugar: un
	/// contrato que la pantalla de alumnos no podía usar, y por eso no lo usaba —buscaba sólo por
	/// documento— y lo decía en un comentario de su propio XAML.
	/// </para>
	/// <para>
	/// Devuelve colección y no una entidad porque el apellido <strong>no es único</strong>: dos
	/// hermanos comparten apellido, y un contrato que devuelve uno solo tiene que elegir
	/// arbitrariamente a cuál de los dos esconde. Es la misma forma que
	/// <c>IDocenteRepository.BuscarSegunNombreCompletoAsync</c>, que ya resolvía esto para docentes.
	/// </para>
	/// </remarks>
	Task<IReadOnlyCollection<Alumno>> BuscarSegunNombreCompletoAsync(string termino);

	Task<Alumno?> BuscarPorDocumentoAsync(string documento);
}
