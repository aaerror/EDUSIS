namespace Domain.Cursantes.Calificaciones;

/// <summary>
/// Dueño de las reglas que deciden cómo le fue a un alumno: qué instancia cuenta como rendida, cómo
/// se promedian y cuándo una materia está aprobada.
/// </summary>
/// <remarks>
/// <para>
/// Estas reglas no pueden vivir en <see cref="Cursante"/>. El traslado de división se modela como
/// cerrar la inscripción actual y abrir otra, y <see cref="Calificacion"/> es entidad hija del
/// cursante, así que las instancias de una misma materia en un mismo ciclo lectivo quedan
/// repartidas entre <strong>dos</strong> agregados: ninguno de los dos tiene la colección completa
/// delante. Tampoco corresponden a <see cref="Calificacion"/>, que no conoce a sus hermanas —ella
/// decide sobre sí misma, en <see cref="Calificacion.EstaAprobado"/>—.
/// </para>
/// <para>
/// Son reglas ENTRE agregados y necesitan un dueño explícito en el dominio, igual que
/// <c>TopesCurriculares</c>: la capa de aplicación aporta las calificaciones (las obtiene del
/// repositorio) y esta política decide. Así el cálculo queda donde se puede consultar y la regla
/// donde pertenece.
/// </para>
/// <para>
/// Estaban como métodos privados de <c>ServicioAlumno</c>, en la capa de aplicación, cuando se
/// escribió el resumen académico del alumno.
/// </para>
/// </remarks>
public static class RendimientoAcademico
{
	/// <summary>
	/// Las instancias que el alumno efectivamente rindió, que son las únicas que tienen nota.
	/// </summary>
	/// <remarks>
	/// <c>Rindio</c> por sí solo alcanzaría —el constructor de <see cref="Calificacion"/> exige la
	/// nota cuando rindió—, pero el segundo término deja la consulta correcta si esa invariante
	/// cambia, y es el que justifica el acceso a <c>Nota</c> en <see cref="Promediar"/>.
	///
	/// Qué cuenta como "rendida" es un hecho del dominio y no un filtro de presentación: la
	/// inasistencia a examen se guarda con <c>Nota</c> en nulo a propósito
	/// (<c>Calificacion.Inasistencia</c>), y cualquier capa que lo decidiera por su cuenta podría
	/// decidirlo distinto.
	/// </remarks>
	public static IEnumerable<Calificacion> Rendidas(IEnumerable<Calificacion> calificaciones) =>
		calificaciones.Where(x => x.Rindio && x.Nota is not null);

	/// <summary>
	/// Las instancias a las que el alumno no se presentó. No promedian.
	/// </summary>
	public static IEnumerable<Calificacion> AusenciasAExamen(IEnumerable<Calificacion> calificaciones) =>
		calificaciones.Where(x => !x.Rindio);

	/// <summary>
	/// El promedio de las notas rendidas, a dos decimales, o <c>null</c> si no rindió ninguna.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Nulo y no cero: un alumno sin notas no tiene cero de promedio, y quien lo informe tiene que
	/// poder decir "sin calificaciones" en lugar del peor promedio posible.
	/// </para>
	/// <para>
	/// Promedia sólo lo rendido. Contar la inasistencia a examen obligaría a inventarle un número
	/// que nadie le puso, y el dominio la guarda sin nota justamente para no tener que hacerlo.
	/// </para>
	/// </remarks>
	public static decimal? Promediar(IEnumerable<Calificacion> calificaciones)
	{
		var notas = Rendidas(calificaciones).Select(x => x.Nota!.Value).ToList();

		if (notas.Count is 0)
		{
			return null;
		}

		return Math.Round((decimal)notas.Average(), 2, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// La materia está aprobada: el promedio de sus instancias rendidas alcanza
	/// <see cref="Calificacion.NOTA_APROBACION"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>El criterio lo fija la institución</strong>, y acá está tomado uno: el que no
	/// privilegia ninguna instancia sobre otra. Los otros dos razonables dan resultados distintos
	/// sobre los mismos datos, así que conviene que esté escrito cuál se eligió. Con un 4 y después
	/// un 7: por promedio da 5,5 y <strong>no</strong> aprueba; por <em>última nota</em> —el
	/// recuperatorio pisa al aplazo— aprueba; por <em>cualquier instancia aprobada</em> también.
	/// </para>
	/// <para>
	/// Recibe las calificaciones y no un promedio ya calculado para que cambiar de criterio sea
	/// cambiar este método y nada más: con un <c>decimal?</c> por parámetro, los otros dos criterios
	/// obligarían a tocar también la firma y a todos los llamadores.
	/// </para>
	/// <para>
	/// El umbral sale de <see cref="Calificacion.NOTA_APROBACION"/>, que es público precisamente
	/// porque el boletín tiene que imprimir el mismo número que decide acá.
	/// </para>
	/// </remarks>
	public static bool EstaAprobada(IEnumerable<Calificacion> calificaciones)
	{
		var promedio = Promediar(calificaciones);

		return promedio is not null && promedio >= (decimal)Calificacion.NOTA_APROBACION;
	}
}
