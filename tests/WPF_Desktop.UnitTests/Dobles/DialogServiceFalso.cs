using WPF_Desktop.Shared;

namespace WPF_Desktop.UnitTests.Dobles;

/// <summary>
/// Doble escrito a mano de <see cref="IDialogService"/>. No se usa NSubstitute porque la interfaz es
/// <c>internal</c> y Castle DynamicProxy no puede generar un proxy de ella sin
/// <c>InternalsVisibleTo("DynamicProxyGenAssembly2")</c> en <c>WPF_Desktop</c>.
/// </summary>
internal class DialogServiceFalso : IDialogService
{
	public List<(string Mensaje, string Titulo)> Informaciones { get; } = new();

	public List<(string Mensaje, string Titulo)> Advertencias { get; } = new();

	public List<(string Mensaje, string Titulo)> Errores { get; } = new();

	public List<(string Mensaje, string Titulo)> Confirmaciones { get; } = new();

	/// <summary>Lo que devolverá <see cref="Confirmar"/>.</summary>
	public bool RespuestaDeConfirmacion { get; set; } = true;

	public void MostrarInformacion(string mensaje, string titulo)
	{
		Informaciones.Add((mensaje, titulo));
	}

	public void MostrarAdvertencia(string mensaje, string titulo)
	{
		Advertencias.Add((mensaje, titulo));
	}

	public void MostrarError(string mensaje, string titulo)
	{
		Errores.Add((mensaje, titulo));
	}

	public bool Confirmar(string mensaje, string titulo)
	{
		Confirmaciones.Add((mensaje, titulo));
		return RespuestaDeConfirmacion;
	}
}
