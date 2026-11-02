namespace WPF_Desktop.Shared;

/// <summary>
/// Única vía por la que la capa de presentación le habla al usuario fuera de su propia
/// pantalla. Ningún ViewModel llama a <c>MessageBox.Show</c>: no se puede doblar en una prueba
/// y bloquea el hilo de la interfaz hasta que alguien lo despacha a mano.
/// </summary>
/// <remarks>
/// Lo que decide la implementación, no el ViewModel: qué interrumpe y qué no. Informar y
/// advertir salen como aviso al pie —el usuario sigue trabajando—; el error y la confirmación
/// abren un diálogo, porque uno no se puede perder de vista y la otra exige una decisión.
/// </remarks>
internal interface IDialogService
{
	void MostrarInformacion(string mensaje, string titulo);

	void MostrarAdvertencia(string mensaje, string titulo);

	void MostrarError(string mensaje, string titulo);

	/// <summary>Pregunta Sí/No al usuario; devuelve <c>true</c> si aceptó.</summary>
	bool Confirmar(string mensaje, string titulo);

	/// <summary>
	/// Confirma una acción concreta, con su verbo y su consecuencia. Es la forma preferida:
	/// <see cref="Confirmar"/> no tiene dónde poner lo que la acción se lleva puesto.
	/// </summary>
	bool ConfirmarAccion(SolicitudDeDialogo solicitud);
}
