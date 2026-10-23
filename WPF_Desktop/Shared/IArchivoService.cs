using Core.Shared.Documentos;

namespace WPF_Desktop.Shared;

/// <summary>
/// Puerto de la capa de presentación para entregarle al usuario un documento generado: le
/// pregunta dónde guardarlo, lo escribe y lo abre.
/// </summary>
/// <remarks>
/// Los casos de uso devuelven el PDF en memoria justamente para que esta decisión viva acá: es
/// la única capa que puede abrir un cuadro de diálogo. Es un puerto y no una llamada directa a
/// <c>SaveFileDialog</c> para que los ViewModels se puedan probar sin abrir ventanas.
/// </remarks>
internal interface IArchivoService
{
	/// <summary>
	/// Pide la ruta, escribe el documento y lo abre con la aplicación asociada.
	/// </summary>
	/// <returns>
	/// La ruta donde quedó guardado, o <c>null</c> si el usuario canceló el cuadro de guardado.
	/// </returns>
	string? GuardarYAbrir(DocumentoGenerado documento);
}
