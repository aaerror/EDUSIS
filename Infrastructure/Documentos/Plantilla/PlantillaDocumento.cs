using System.Globalization;
using Core.Shared.Documentos;
using Infrastructure.Documentos.Content;

namespace Infrastructure.Documentos.Plantilla;

/// <summary>
/// Los bloques comunes a toda la documentación: encabezado institucional, título,
/// identificación del estudiante, finalidad, lugar y fecha, y pie de firma y sello.
/// </summary>
/// <remarks>
/// Está en un solo lugar porque los seis documentos y los cinco reportes comparten exactamente
/// estos bloques, y porque son los campos que un documento oficial tiene que llevar: si cada
/// generador los armara por su cuenta, alcanzaría con olvidar uno para emitir un certificado que
/// no sirve para el trámite.
/// </remarks>
internal sealed class PlantillaDocumento
{
	/// <summary>
	/// Las fechas de un documento en español se escriben en español. Sin cultura explícita,
	/// <c>ToString("MMMM")</c> devuelve el mes en el idioma de la máquina que lo emitió.
	/// </summary>
	private static readonly CultureInfo CULTURA = CultureInfo.GetCultureInfo("es-AR");

	private readonly IDatosInstitucionales _institucion;


	public PlantillaDocumento(IDatosInstitucionales institucion)
	{
		_institucion = institucion;
	}

	/// <summary>Identificación del establecimiento emisor, al principio del cuerpo.</summary>
	public IContenido BloqueInstitucional()
	{
		var cue = string.IsNullOrWhiteSpace(_institucion.CueAnexo)
			? _institucion.CUE
			: $"{ _institucion.CUE } · Anexo { _institucion.CueAnexo }";

		var localidad = string.IsNullOrWhiteSpace(_institucion.CodigoPostal)
			? _institucion.Localidad
			: $"{ _institucion.Localidad } (CP { _institucion.CodigoPostal })";

		var jurisdiccion = string.IsNullOrWhiteSpace(_institucion.Departamento)
			? _institucion.Jurisdiccion
			: $"{ _institucion.Departamento }, { _institucion.Jurisdiccion }";

		return Ficha.Crear()
			.Con("Establecimiento", _institucion.Nombre)
			.Con("CUE", cue)
			.Con("Domicilio", _institucion.Direccion)
			.Con("Localidad", $"{ localidad } · { jurisdiccion }")
			.Con("Oferta", Unir(_institucion.Modalidad, _institucion.NivelesEnsenanza))
			.Con("Contacto", Unir(_institucion.Telefono, Unir(_institucion.Sector, _institucion.Ambito)));
	}

	/// <summary>Título del documento, centrado.</summary>
	public IEnumerable<IContenido> Titulo(string titulo, string? subtitulo = null)
	{
		yield return Parrafo.Crear(titulo.ToUpperInvariant())
			.Centrado()
			.EnNegrita()
			.ConTamano(13f)
			.ConEspacio(16f, subtitulo is null ? 18f : 4f);

		if (!string.IsNullOrWhiteSpace(subtitulo))
		{
			yield return Parrafo.Crear(subtitulo)
				.Centrado()
				.EnItalica()
				.ConTamano(10f)
				.ConEspacio(0f, 18f);
		}
	}

	/// <summary>Identificación del estudiante y su trayectoria escolar.</summary>
	public IContenido BloqueAlumno(IdentificacionAlumno alumno)
	{
		var trayectoria = string.IsNullOrWhiteSpace(alumno.Division)
			? $"{ alumno.NivelEducativo } · { alumno.Curso }"
			: $"{ alumno.NivelEducativo } · { alumno.Curso } división { alumno.Division }";

		return Ficha.Crear()
			.Con("Estudiante", alumno.NombreCompleto)
			.Con("Documento", $"DNI { alumno.Documento }")
			.Con("Legajo", alumno.Legajo)
			.Con("Trayectoria", trayectoria);
	}

	/// <summary>
	/// Cierre del documento: finalidad —si el trámite la requiere—, lugar y fecha de expedición,
	/// y el espacio de firma y sello.
	/// </summary>
	public IEnumerable<IContenido> Cierre(MetadatosEmision emision)
	{
		if (!string.IsNullOrWhiteSpace(emision.Finalidad))
		{
			yield return Parrafo.Crear($"Se extiende la presente a los efectos de: { emision.Finalidad }.")
				.ConSangria()
				.ConEspacio(6f, 10f);
		}

		yield return Parrafo.Crear($"{ emision.Localidad }, { EnLetras(emision.Fecha) }.")
			.ALaDerecha()
			.ConEspacio(16f, 0f);

		yield return Firma.Crear();
	}

	/// <summary>Fecha en palabras: "18 de octubre de 2026".</summary>
	public static string EnLetras(DateTime fecha) =>
		$"{ fecha.Day } de { fecha.ToString("MMMM", CULTURA) } de { fecha.Year }";

	/// <summary>Fecha corta, para los rangos y las tablas.</summary>
	public static string Corta(DateTime fecha) =>
		fecha.ToString("dd/MM/yyyy", CULTURA);

	/// <summary>Une dos datos con un separador, descartando los que no estén.</summary>
	private static string Unir(string? izquierda, string? derecha)
	{
		var partes = new[] { izquierda, derecha }
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x!.Trim());

		return string.Join(" · ", partes);
	}
}
