namespace WPF_Desktop.ViewModels.Shared.Messages;

/// <summary>
/// La sesión termina. <see cref="VolverAlAcceso"/> dice en qué: en la pantalla de acceso, para que
/// entre otro usuario, o en el apagado del proceso.
/// </summary>
/// <remarks>
/// <para>
/// Va por el mensajero y no por un <c>*Store</c> porque el receptor —<c>App</c>— ya existe cuando se
/// produce el dato, que es el criterio que fija <c>CLAUDE.md</c> para elegir entre los dos. No es un
/// parámetro de navegación: nadie lo tiene que leer en su constructor.
/// </para>
/// <para>
/// El receptor es <c>App</c> y no la propia ventana por una razón de responsabilidad: la decisión de
/// qué pasa después de cerrar <c>MainWindow</c> —relogin o apagado— es del ciclo de vida de la
/// aplicación, no de una pantalla. <c>MainViewModel</c> declara la intención y no sabe cómo se
/// cumple; si supiera, tendría que conocer <c>StartupWindow</c>, el orden del <c>ShutdownMode</c> y
/// el barrido de estado, que son exactamente las tres cosas que están documentadas en <c>App</c>.
/// </para>
/// <para>
/// Es un <c>record</c> con dos instancias estáticas en lugar de un <c>bool</c> suelto para que el
/// sitio de llamada se lea solo: <c>Send(FinDeSesionMessage.CambioDeUsuario)</c> dice qué se pidió,
/// mientras que <c>Send(new FinDeSesionMessage(true))</c> obliga a ir a buscar qué significa el
/// <c>true</c>. Mismo criterio que las fábricas estáticas de <c>SolicitudDeDialogo</c>.
/// </para>
/// </remarks>
internal sealed record FinDeSesionMessage(bool VolverAlAcceso)
{
	/// <summary>Cerrar la sesión y volver a la pantalla de acceso, con el proceso vivo.</summary>
	public static FinDeSesionMessage CambioDeUsuario { get; } =
		new(VolverAlAcceso: true);

	/// <summary>Cerrar la sesión y terminar la aplicación.</summary>
	public static FinDeSesionMessage SalidaDeLaAplicacion { get; } =
		new(VolverAlAcceso: false);
}
