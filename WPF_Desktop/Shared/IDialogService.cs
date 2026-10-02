namespace WPF_Desktop.Shared;

/// <summary>
/// Puerto de la capa de presentación para avisar y preguntar al usuario.
/// Los ViewModels lo consumen en lugar de abrir cuadros de mensaje directamente, de modo que las pruebas
/// puedan reemplazarlo por un doble que registre los mensajes.
/// </summary>
internal interface IDialogService
{
	void MostrarInformacion(string mensaje, string titulo);

	void MostrarAdvertencia(string mensaje, string titulo);

	void MostrarError(string mensaje, string titulo);

	/// <summary>Pregunta Sí/No al usuario; devuelve <c>true</c> si aceptó.</summary>
	bool Confirmar(string mensaje, string titulo);
}
