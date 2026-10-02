using System.Windows;

namespace WPF_Desktop.Shared;

/// <summary>Única clase de la capa que envuelve <c>MessageBox.Show</c>.</summary>
internal class DialogService : IDialogService
{
	public void MostrarInformacion(string mensaje, string titulo)
	{
		MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
	}

	public void MostrarAdvertencia(string mensaje, string titulo)
	{
		MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
	}

	public void MostrarError(string mensaje, string titulo)
	{
		MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public bool Confirmar(string mensaje, string titulo)
	{
		return MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
	}
}
