namespace WPF_Desktop.Store;

/// <summary>
/// Estado compartido que pertenece al USUARIO de la sesión y no al proceso. El ciclo de sesión de
/// <c>App</c> llama a <see cref="Limpiar"/> sobre todas las implementaciones antes de volver a la
/// pantalla de acceso, así que el usuario que entra después no hereda nada del anterior.
/// </summary>
/// <remarks>
/// <para>
/// Hace falta porque los <c>*Store</c> son <c>Singleton</c> —uno por proceso— mientras que la
/// sesión dura menos que el proceso desde que existe el cambio de usuario. Hasta entonces la
/// diferencia era invisible: el único final de sesión era el apagado, y el apagado limpia todo.
/// </para>
/// <para>
/// Es una interfaz y no una lista de llamadas en <c>App</c> por una razón concreta: los stores se
/// registran con un segundo <c>AddSingleton&lt;IEstadoDeSesion&gt;</c> que resuelve HACIA la
/// instancia ya registrada —el mismo patrón que <c>ISesionUsuario</c>/<c>ISesionActual</c>—, y
/// <c>App</c> pide <c>IEnumerable&lt;IEstadoDeSesion&gt;</c> sin nombrar a ninguno. Un store nuevo
/// entra al barrido agregando su propia línea al lado de la que ya escribió, no editando un método
/// en otra capa que nadie recuerda que existe.
/// </para>
/// <para>
/// Los <c>*NavigationStore</c> NO la implementan. Lo que guardan no es un valor del usuario sino el
/// ViewModel activo de una ventana, y su reinicio está atado al ciclo de vida de esa ventana:
/// <c>App</c> los pone en cero por separado, con el orden explicado ahí.
/// </para>
/// <para>
/// Sobre el <c>null!</c> que aparece en varias implementaciones: el valor de construcción de esas
/// propiedades ES <c>null</c> —nadie eligió todavía un curso, una división ni una materia—, pero
/// están declaradas como referencia no anulable con <c>Nullable</c> habilitado, así que el
/// compilador pide el perdón explícito. Declararlas <c>CursoSeleccionado?</c> es lo correcto y es la
/// dirección que marca UI-34 de <c>docs/todos.md</c> para los centinelas de este drill-down;
/// hacerlo arrastra una guarda de nulidad a cada ViewModel que las lee, y eso es un cambio aparte.
/// </para>
/// </remarks>
internal interface IEstadoDeSesion
{
	/// <summary>
	/// Devuelve el store a su valor inicial: el mismo que tendría recién construido.
	/// </summary>
	void Limpiar();
}
