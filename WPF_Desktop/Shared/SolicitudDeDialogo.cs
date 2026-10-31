namespace WPF_Desktop.Shared;

/// <summary>
/// Lo que se le muestra al usuario cuando la aplicación necesita una decisión, o cuando algo
/// falló y no puede pasar desapercibido.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Consecuencia"/> es la razón de ser de este tipo. Los <c>MessageBox</c> que esto
/// reemplaza preguntaban "¿Eliminar la división?" y nada más: quien contesta no sabe si se
/// lleva puestos a los cursantes, a las planillas de asistencia o a nada. Una confirmación que
/// no dice qué se pierde no es una confirmación, es un trámite.
/// </para>
/// <para>
/// <see cref="VerboDeAccion"/> va en el botón, y es el mismo verbo del botón que abrió el
/// diálogo: si la pantalla dice "Dar de baja", el diálogo confirma con "Dar de baja" y no con
/// un "Sí" que no dice a qué.
/// </para>
/// <para>
/// Las fábricas estáticas de abajo son la forma normal de construirlo. Dejan una sola línea en
/// el ViewModel y evitan que cada pantalla elija por su cuenta el rótulo y los colores.
/// </para>
/// </remarks>
internal record SolicitudDeDialogo
{
	/// <summary>Línea de rótulo: CONFIRMAR, ERROR, ATENCIÓN.</summary>
	public required string Encabezado { get; init; }

	public required string Titulo { get; init; }

	/// <summary>El cuerpo. Puede quedar vacío cuando el título ya alcanza.</summary>
	public string Mensaje { get; init; } = string.Empty;

	/// <summary>Qué va a pasar si se confirma. Vacío oculta el bloque.</summary>
	public string Consecuencia { get; init; } = string.Empty;

	public required string VerboDeAccion { get; init; }

	/// <summary>Pinta de alerta la regla de acento. Es la severidad, no la acción.</summary>
	public bool EsAlerta { get; init; }

	/// <summary>El botón de acción va en rojo: lo que hace no se puede deshacer.</summary>
	public bool EsDestructiva { get; init; }

	/// <summary>Sin "Cancelar": no hay nada que decidir, sólo que acusar recibo.</summary>
	public bool SoloAcuse { get; init; }

	public bool TieneCancelar => !SoloAcuse;

	/// <summary>
	/// Una acción que destruye algo. El título dice qué se va a hacer, en el mismo verbo del
	/// botón, y la consecuencia dice qué más se lleva puesto.
	/// </summary>
	public static SolicitudDeDialogo Destructiva(string titulo, string consecuencia, string verbo) =>
		new()
		{
			Encabezado = "CONFIRMAR",
			Titulo = titulo,
			Consecuencia = consecuencia,
			VerboDeAccion = verbo,
			EsAlerta = true,
			EsDestructiva = true
		};

	/// <summary>
	/// Una decisión que no destruye nada pero conviene revisar antes: un dato que viaja al
	/// expediente, un estado que cambia para otra persona.
	/// </summary>
	public static SolicitudDeDialogo Revisar(string titulo, string mensaje, string verbo) =>
		new()
		{
			Encabezado = "CONFIRMAR",
			Titulo = titulo,
			Mensaje = mensaje,
			VerboDeAccion = verbo
		};

	/// <summary>
	/// Algo salió bien pero no se puede perder de vista: unas credenciales iniciales, un número
	/// de legajo recién asignado. No hay decisión que tomar, pero tampoco puede irse solo a los
	/// cinco segundos como un aviso al pie.
	/// </summary>
	public static SolicitudDeDialogo Aviso(string titulo, string mensaje) =>
		new()
		{
			Encabezado = "AVISO",
			Titulo = titulo,
			Mensaje = mensaje,
			VerboDeAccion = "Entendido",
			SoloAcuse = true
		};

	/// <summary>Algo falló. No hay decisión que tomar: hay que enterarse.</summary>
	public static SolicitudDeDialogo Error(string titulo, string mensaje) =>
		new()
		{
			Encabezado = "ERROR",
			Titulo = titulo,
			Mensaje = mensaje,
			VerboDeAccion = "Entendido",
			EsAlerta = true,
			SoloAcuse = true
		};
}
