using Infrastructure.Documentos.Content;

namespace Infrastructure.Documentos;

public class CertificadoAlumnoRegular : IDocumentoBuilder
{
	private const string DOCUMENT_TITLE = "Certificado de Alumno Regular";
	private Documento _documento;

	private string _nombreCompleto;
	private string _dni;
	private DateTime _fechaIngreso;
	private string _grado;
	private string _nivelEducativo;

	#region Body
	private string _tituloDocumento = string.Empty;
	private string _subtituloDocumento = string.Empty;
	#endregion


	public CertificadoAlumnoRegular(string nombreCompleto, string dni, DateTime fechaIngreso, string grado, string nivelEducativo)
	{
		if (string.IsNullOrWhiteSpace(nombreCompleto))
		{
			throw new ArgumentException("Datos incompletos para generar un certificado de alumno regular.", nameof(nombreCompleto));
		}

		if (string.IsNullOrWhiteSpace(dni))
		{
			throw new ArgumentException("Datos incompletos para generar un certificado de alumno regular.", nameof(dni));
		}

		if (fechaIngreso > DateTime.Today.Date)
		{
			throw new ArgumentException("La fecha de ingreso es posterior al día de hoy", nameof(fechaIngreso));
		}

		_nombreCompleto = nombreCompleto;
		_dni = dni;
		_fechaIngreso = fechaIngreso;
		_grado = grado;
		_nivelEducativo = nivelEducativo;

		_documento = Documento.Create(DOCUMENT_TITLE);
	}

	public IDocumentoBuilder AgregarTituloDocumento(string titulo, string? subtitulo)
	{
		_documento = Documento.Create(_tituloDocumento, _subtituloDocumento);

		return this;
	}

	public IDocumentoBuilder AgregarContenido(IContenido contenido)
	{
		_documento.AgregarContenido(contenido);

		return this;
	}

	public Documento EnsamblarDocumento()
	{
		var parrafo = new Parrafo()
			.ConParrafo($"\n\rSe deja constancia que {_nombreCompleto}, D.N.I. {_dni} es alumno/a regular del Instituto Maria Saleme desde el {_fechaIngreso.ToString("D")}, cursando el {_grado} año del {_nivelEducativo}.\n\rA pedido del interesado/a se extiende la presente constancia en la ciudad de Córdoba a los {DateTime.Today.Date.Day} días del mes de {DateTime.Today.Date.ToString("MMMM")} de {DateTime.Today.Date.ToString("yyyy")} para ser presentado ante quién corresponda.")
			.CrearContenido();

		_documento.AgregarContenido(parrafo);

		return _documento;
	}
/*
	IDocumento CrearContenido()
	{
		_contenido.

		var parrafo = new Parrafo()
			.ConTitulo(TITULO)
			.ConSubtitulo("")
			.ConParrafo($"\tSe deja constancia que {_nombreCompleto}, D.N.I. {_dni} es alumno/a regular del Instituto Maria Saleme desde el {_fechaIngreso.ToString("D")}, cursando el {_grado} año del {_nivelEducativo}.\n\r\n\r\tA pedido del interesado/a se extiende la presente constancia en la ciudad de Córdoba a los {DateTime.Today.Date.Day} días del mes de {DateTime.Today.Date.ToString("MMMM")} de {DateTime.Today.Date.ToString("yyyy")} para ser presentado ante quién corresponda.")
			.CrearContenido();

		return parrafo;
	}*/

	/*public IReadOnlyCollection<IBlockElement> RecuperarContenido()
	{
		string contenido = $"\tSe deja constancia que {_nombreCompleto}, D.N.I. {_dni} es alumno/a regular del Instituto Maria Saleme desde el {_fechaIngreso.ToString("D")}, cursando el {_grado} año del {_nivelEducativo}.\n\r\n\r\tA pedido del interesado/a se extiende la presente constancia en la ciudad de Córdoba a los {DateTime.Today.Date.Day} días del mes de {DateTime.Today.Date.ToString("MMMM")} de {DateTime.Today.Date.ToString("yyyy")} para ser presentado ante quién corresponda.";

		var text = new Text(contenido)
			.SetFontFamily(StandardFontFamilies.HELVETICA)
			.SetFontSize(12);

		var body = new Paragraph(text)
			.SetTextAlignment(TextAlignment.JUSTIFIED);

		return Contenido.Create(TITULO, "")
			.AgregarParrafo(contenido)
			.CrearContenido()
			.RecuperarContenido();
	}*/
}
