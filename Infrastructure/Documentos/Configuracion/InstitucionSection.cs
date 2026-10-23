using System.Configuration;

namespace Infrastructure.Documentos.Configuracion;

/// <summary>
/// Sección <c>&lt;institucion&gt;</c> de <c>App.config</c>: identificación del establecimiento
/// que emite la documentación.
/// </summary>
/// <remarks>
/// Es una sección propia y no un puñado de claves en <c>appSettings</c> porque estos trece datos
/// son una sola cosa —el establecimiento— y conviene que se declaren juntos y con nombres sin
/// prefijo. Que <c>IsRequired</c> esté en los campos que el encabezado imprime no es casual: un
/// certificado sin nombre, CUE o domicilio no sirve para el trámite, y es mejor que falte la
/// configuración de entrada que emitir un documento incompleto.
/// </remarks>
public sealed class InstitucionSection : ConfigurationSection
{
	public const string NOMBRE_SECCION = "institucion";

	[ConfigurationProperty("nombre", IsRequired = true)]
	public string Nombre => (string) this["nombre"];

	[ConfigurationProperty("cue", IsRequired = true)]
	public string CUE => (string) this["cue"];

	[ConfigurationProperty("cueAnexo", IsRequired = false, DefaultValue = "")]
	public string CueAnexo => (string) this["cueAnexo"];

	[ConfigurationProperty("direccion", IsRequired = true)]
	public string Direccion => (string) this["direccion"];

	[ConfigurationProperty("localidad", IsRequired = true)]
	public string Localidad => (string) this["localidad"];

	[ConfigurationProperty("codigoPostal", IsRequired = false, DefaultValue = "")]
	public string CodigoPostal => (string) this["codigoPostal"];

	[ConfigurationProperty("departamento", IsRequired = false, DefaultValue = "")]
	public string Departamento => (string) this["departamento"];

	[ConfigurationProperty("jurisdiccion", IsRequired = true)]
	public string Jurisdiccion => (string) this["jurisdiccion"];

	[ConfigurationProperty("telefono", IsRequired = false, DefaultValue = "")]
	public string Telefono => (string) this["telefono"];

	[ConfigurationProperty("sector", IsRequired = false, DefaultValue = "")]
	public string Sector => (string) this["sector"];

	[ConfigurationProperty("ambito", IsRequired = false, DefaultValue = "")]
	public string Ambito => (string) this["ambito"];

	[ConfigurationProperty("modalidad", IsRequired = false, DefaultValue = "")]
	public string Modalidad => (string) this["modalidad"];

	[ConfigurationProperty("nivelesEnsenanza", IsRequired = false, DefaultValue = "")]
	public string NivelesEnsenanza => (string) this["nivelesEnsenanza"];
}
