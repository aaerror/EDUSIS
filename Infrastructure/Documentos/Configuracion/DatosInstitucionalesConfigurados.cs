using System.Configuration;
using Core.Shared.Documentos;

namespace Infrastructure.Documentos.Configuracion;

/// <summary>
/// Resuelve <see cref="IDatosInstitucionales"/> desde la sección <c>&lt;institucion&gt;</c> de
/// <c>App.config</c>.
/// </summary>
/// <remarks>
/// La sección se lee una sola vez, al construir: el archivo de configuración no cambia mientras
/// la aplicación corre, y releerlo por cada documento sería trabajo por nada.
/// </remarks>
internal sealed class DatosInstitucionalesConfigurados : IDatosInstitucionales
{
	private readonly InstitucionSection _seccion;


	public DatosInstitucionalesConfigurados()
	{
		// Falla acá, con el nombre de la sección en el mensaje, y no más adelante con un
		// NullReferenceException en el encabezado de un PDF a medio armar.
		_seccion = ConfigurationManager.GetSection(InstitucionSection.NOMBRE_SECCION) as InstitucionSection
			?? throw new ConfigurationErrorsException(
				$"Falta la sección <{ InstitucionSection.NOMBRE_SECCION }> en App.config: sin los datos del establecimiento no se puede emitir documentación.");
	}

	public string Nombre => _seccion.Nombre;

	public string CUE => _seccion.CUE;

	public string CueAnexo => _seccion.CueAnexo;

	public string Direccion => _seccion.Direccion;

	public string Localidad => _seccion.Localidad;

	public string CodigoPostal => _seccion.CodigoPostal;

	public string Departamento => _seccion.Departamento;

	public string Jurisdiccion => _seccion.Jurisdiccion;

	public string Telefono => _seccion.Telefono;

	public string Sector => _seccion.Sector;

	public string Ambito => _seccion.Ambito;

	public string Modalidad => _seccion.Modalidad;

	public string NivelesEnsenanza => _seccion.NivelesEnsenanza;
}
