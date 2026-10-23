using iText.IO.Font.Constants;
using iText.Kernel.Font;

namespace Infrastructure.Documentos.Content;

/// <summary>
/// Las tres variantes tipográficas del documento, creadas una vez por documento.
/// </summary>
/// <remarks>
/// Los bloques de contenido reciben estas fuentes en lugar de pedir una familia por nombre
/// —<c>SetFontFamily("Helvetica-Bold")</c>—, porque resolver por nombre depende de que el
/// <c>FontProvider</c> tenga registrado ese alias y, cuando no lo tiene, iText cae en silencio a
/// la fuente por omisión: el documento sale sin negritas y nada avisa.
/// <para>
/// Se crean por documento y no en campos estáticos: un <c>PdfFont</c> compartido entre
/// documentos es una fuente de errores conocida en iText.
/// </para>
/// </remarks>
public sealed record Fuentes(PdfFont Regular, PdfFont Negrita, PdfFont Italica)
{
	public static Fuentes Estandar() =>
		new(Regular: PdfFontFactory.CreateFont(StandardFonts.HELVETICA),
			Negrita: PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD),
			Italica: PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE));
}
