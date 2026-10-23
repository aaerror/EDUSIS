using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using Core.Shared.Documentos;

namespace WPF_Desktop.Shared;

/// <summary>Única clase de la capa que abre el cuadro de guardado y lanza el visor de PDF.</summary>
internal class ArchivoService : IArchivoService
{
	public string? GuardarYAbrir(DocumentoGenerado documento)
	{
		var dialogo = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = ".pdf",
				FileName = documento.NombreSugerido,
				Filter = "Documento PDF (*.pdf)|*.pdf",
				OverwritePrompt = true,
				Title = "Guardar documento"
			};

		if (dialogo.ShowDialog() != true)
		{
			return null;
		}

		File.WriteAllBytes(dialogo.FileName, documento.Contenido);

		// UseShellExecute delega en el visor de PDF asociado. Sin él, Process.Start intentaría
		// EJECUTAR el .pdf como si fuera un programa y fallaría.
		Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });

		return dialogo.FileName;
	}
}
