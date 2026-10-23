using Core.Shared.Documentos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Doble de <see cref="IDatosInstitucionales"/> con valores fijos.
/// </summary>
/// <remarks>
/// En producción estos datos salen de la sección <c>&lt;institucion&gt;</c> de <c>App.config</c>,
/// que no existe en una prueba unitaria. Los valores no se parecen a los de ningún
/// establecimiento real a propósito: si una aserción los da por ciertos, se ve que lo hace.
/// </remarks>
public sealed class DatosInstitucionalesDePrueba : IDatosInstitucionales
{
	public string Nombre => "Establecimiento de Prueba";

	public string CUE => "0000000";

	public string CueAnexo => "000000000";

	public string Direccion => "Calle de Prueba 1";

	public string Localidad => "Localidad de Prueba";

	public string CodigoPostal => "0000";

	public string Departamento => "Departamento de Prueba";

	public string Jurisdiccion => "Jurisdicción de Prueba";

	public string Telefono => "(000) 0000000";

	public string Sector => "Estatal";

	public string Ambito => "Urbano";

	public string Modalidad => "Educación común";

	public string NivelesEnsenanza => "Secundaria";
}
