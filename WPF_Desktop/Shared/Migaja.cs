using CommunityToolkit.Mvvm.Input;

namespace WPF_Desktop.Shared;

/// <summary>
/// Un tramo de la ruta que se dibuja arriba de cada pantalla: "CURSOS / DIVISIONES / CURSANTES".
/// </summary>
/// <remarks>
/// <para>
/// Tres clases de tramo, y las tres se construyen con las fábricas de abajo:
/// <see cref="Hacia"/> es un enlace —el comando con el que la pantalla ya sabía volver a ese
/// nivel—, <see cref="Actual"/> es dónde estoy parado, y <see cref="Contexto"/> es un nivel
/// intermedio al que esta pantalla no puede saltar directo.
/// </para>
/// <para>
/// El tramo de contexto existe porque la navegación de la aplicación sube de a un nivel: desde
/// Horarios se vuelve a Cátedras, y de ahí a Diseño curricular, pero no hay atajo de Horarios a
/// Diseño curricular. En lugar de inventar ese atajo —que obligaría a inyectar servicios de
/// navegación de más en los ViewModels del fondo— la ruta muestra el nivel apagado: dice dónde
/// estoy sin prometer un clic que no existe.
/// </para>
/// <para>
/// Por qué hacía falta: el drill-down baja cuatro niveles (Cursos → División → Cursantes;
/// Cursos → Diseño curricular → Cátedras → Horarios) y la única pista de ubicación era el
/// nombre de la sección en la barra de título. Siete pantallas traían su propio botón "Volver",
/// con siete redacciones, y otras siete no traían ninguno: desde Cursantes no se subía.
/// </para>
/// </remarks>
internal record Migaja(string Texto, IRelayCommand? Ir = null, bool EsActual = false)
{
	public bool EsNavegable => Ir is not null;

	/// <summary>Nivel intermedio sin salto directo: se dibuja apagado y sin clic.</summary>
	public bool EsContexto => Ir is null && !EsActual;

	/// <summary>Un nivel al que esta pantalla sabe volver.</summary>
	public static Migaja Hacia(string texto, IRelayCommand ir) =>
		new(texto, ir);

	/// <summary>Esta pantalla. Siempre el último tramo.</summary>
	public static Migaja Actual(string texto) =>
		new(texto, null, true);

	/// <summary>Un nivel intermedio, sin salto directo desde acá.</summary>
	public static Migaja Contexto(string texto) =>
		new(texto);
}
