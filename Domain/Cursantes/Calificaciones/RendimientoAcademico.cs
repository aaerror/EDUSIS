namespace Domain.Cursantes.Calificaciones;

/// <summary>
/// Dueño de las reglas que deciden cómo le fue a un alumno: qué instancia cuenta como rendida, qué
/// nota queda de cada período, con qué nota cierra una materia y cuándo está aprobada.
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
/// Lo que recibe cada método es el conjunto de calificaciones <strong>de una materia en un ciclo
/// lectivo</strong>: agrupar por materia y por ciclo es trabajo de la capa de aplicación, porque
/// requiere ir al repositorio. Pasarle calificaciones de dos materias juntas no falla, da un
/// número sin sentido.
/// </para>
/// </remarks>
public static class RendimientoAcademico
{
	/// <summary>
	/// Los períodos que pueden cerrar una materia, del más decisivo al menos.
	/// </summary>
	/// <remarks>
	/// Explícito y no derivado del orden de <see cref="Periodo"/>: atar la precedencia al orden en
	/// que están escritos los miembros de un enum la rompe en silencio el día que alguien agrega
	/// un valor en el medio. Los cuatrimestres no están porque no cierran por sí mismos —cierran
	/// promediados, y sólo si no hubo nota final ni recuperación—.
	/// </remarks>
	private static readonly Periodo[] PERIODOS_DE_CIERRE =
	{
		Periodo.SegundaRecuperacion,
		Periodo.PrimeraRecuperacion,
		Periodo.Final,
	};

	private static readonly Periodo[] CUATRIMESTRES =
	{
		Periodo.PrimerCuatrimestre,
		Periodo.SegundoCuatrimestre,
	};

	/// <summary>
	/// Las instancias que el alumno efectivamente rindió, que son las únicas que tienen nota.
	/// </summary>
	/// <remarks>
	/// <c>Rindio</c> por sí solo alcanzaría —el constructor de <see cref="Calificacion"/> exige la
	/// nota cuando rindió—, pero el segundo término deja la consulta correcta si esa invariante
	/// cambia, y es el que justifica el acceso a <c>Nota</c> más abajo.
	///
	/// Qué cuenta como "rendida" es un hecho del dominio y no un filtro de presentación: la
	/// inasistencia a examen se guarda con <c>Nota</c> en nulo a propósito
	/// (<c>Calificacion.Inasistencia</c>), y cualquier capa que lo decidiera por su cuenta podría
	/// decidirlo distinto.
	/// </remarks>
	public static IEnumerable<Calificacion> Rendidas(IEnumerable<Calificacion> calificaciones) =>
		calificaciones.Where(x => x.Rindio && x.Nota is not null);

	/// <summary>
	/// Las instancias a las que el alumno no se presentó. No aportan nota a ningún período.
	/// </summary>
	public static IEnumerable<Calificacion> AusenciasAExamen(IEnumerable<Calificacion> calificaciones) =>
		calificaciones.Where(x => !x.Rindio);

	/// <summary>
	/// La nota que quedó de un período, o <c>null</c> si no rindió nada en él.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>EL RECUPERATORIO REEMPLAZA A LA NOTA DEL PERÍODO.</strong> No promedian ni gana la
	/// más alta: si el alumno rindió el recuperatorio, la nota del período es la del recuperatorio
	/// incluso cuando es peor que la original. Es lo que significa recuperar.
	/// </para>
	/// <para>
	/// Faltar al recuperatorio no borra la nota original: una inasistencia no es una nota, así que
	/// <see cref="Rendidas"/> la descarta y queda vigente lo que se había rendido.
	/// </para>
	/// <para>
	/// Si hubiera más de una instancia del mismo tipo en el mismo período —que el dominio no
	/// prohíbe— vale la más reciente por fecha.
	/// </para>
	/// </remarks>
	public static decimal? NotaDelPeriodo(IEnumerable<Calificacion> calificaciones, Periodo periodo)
	{
		var delPeriodo = Rendidas(calificaciones).Where(x => x.Periodo.Equals(periodo)).ToList();

		var vigente = delPeriodo.Where(x => x.EsRecuperatorio).MaxBy(x => x.Fecha)
					?? delPeriodo.Where(x => !x.EsRecuperatorio).MaxBy(x => x.Fecha);

		return vigente is null ? null : (decimal)vigente.Nota!.Value;
	}

	/// <summary>
	/// El promedio de los cuatrimestres cursados, o <c>null</c> si no rindió ninguno.
	/// </summary>
	/// <remarks>
	/// Promedia los cuatrimestres que tienen nota, no los dos siempre: a mitad de año el segundo
	/// todavía no existe y la nota del primero es toda la información que hay. Es el número que el
	/// docente suele volcar como nota final, pero no es la nota final: eso lo carga él
	/// (<see cref="Periodo.Final"/>) y puede no coincidir.
	/// </remarks>
	public static decimal? PromedioDeCuatrimestres(IEnumerable<Calificacion> calificaciones) =>
		Promediar(CUATRIMESTRES.Select(x => NotaDelPeriodo(calificaciones, x)));

	/// <summary>
	/// La nota de recuperación vigente, o <c>null</c> si no rindió ninguna.
	/// </summary>
	/// <remarks>
	/// Los dos períodos de recuperación dan un solo número porque a la segunda se llega sólo
	/// después de no aprobar la primera: la que vale es la última rendida. Está acá y no en quien
	/// arma la tabla para que el boletín, el reporte y el perfil no puedan elegir distinto.
	/// </remarks>
	public static decimal? NotaDeRecuperacion(IEnumerable<Calificacion> calificaciones)
	{
		var instancias = calificaciones.ToList();

		return NotaDelPeriodo(instancias, Periodo.SegundaRecuperacion)
			?? NotaDelPeriodo(instancias, Periodo.PrimeraRecuperacion);
	}

	/// <summary>
	/// La nota con la que cierra la materia, o <c>null</c> si no hay con qué cerrarla.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es la del período más avanzado que tenga nota, y recién si no hay ninguno el promedio de
	/// los cuatrimestres: segunda recuperación, primera recuperación, nota final, cuatrimestres.
	/// El orden sigue al calendario —a una recuperación se llega después de no alcanzar la nota
	/// final— y por eso lo último que se rindió es lo que vale.
	/// </para>
	/// <para>
	/// La nota final la carga el docente y no la calcula el sistema: cuando existe reemplaza al
	/// promedio de los cuatrimestres, aunque sea distinta. Es una decisión suya, no una cuenta.
	/// </para>
	/// </remarks>
	public static decimal? NotaDeCierre(IEnumerable<Calificacion> calificaciones)
	{
		var instancias = calificaciones.ToList();

		foreach (var periodo in PERIODOS_DE_CIERRE)
		{
			var nota = NotaDelPeriodo(instancias, periodo);

			if (nota is not null)
			{
				return nota;
			}
		}

		return PromedioDeCuatrimestres(instancias);
	}

	/// <summary>
	/// La materia está aprobada: su nota de cierre alcanza
	/// <see cref="Calificacion.NOTA_APROBACION"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Toda la decisión está en <see cref="NotaDeCierre"/>, así que acá no hay más criterio que el
	/// umbral. Sin nota de cierre no está aprobada, y el motivo importa: la nota es nula, no cero.
	/// Un alumno que faltó a todos los exámenes no aprobó, pero tampoco le fue mal.
	/// </para>
	/// <para>
	/// Recibe las calificaciones y no una nota ya calculada para que cambiar el criterio sea
	/// cambiar este archivo y nada más: con un <c>decimal?</c> por parámetro, mover la regla
	/// obligaría a tocar también la firma y a todos los llamadores.
	/// </para>
	/// <para>
	/// El umbral sale de <see cref="Calificacion.NOTA_APROBACION"/>, que es público precisamente
	/// porque el boletín tiene que imprimir el mismo número que decide acá.
	/// </para>
	/// </remarks>
	public static bool EstaAprobada(IEnumerable<Calificacion> calificaciones)
	{
		var cierre = NotaDeCierre(calificaciones);

		return cierre is not null && cierre >= (decimal)Calificacion.NOTA_APROBACION;
	}

	/// <summary>
	/// El promedio general del alumno: la media de las notas de cierre de sus materias.
	/// </summary>
	/// <remarks>
	/// Recibe las calificaciones <strong>ya agrupadas por materia</strong>, una colección por
	/// materia, porque promediar todas las instancias juntas daría otro número y peor: mezclaría
	/// la nota de un cuatrimestre con la de su propio recuperatorio, que la reemplazó, y con las
	/// de los períodos de recuperación. Una materia pesa una vez, con la nota que la cerró.
	/// </remarks>
	public static decimal? PromediarNotasDeCierre(IEnumerable<IEnumerable<Calificacion>> materias) =>
		Promediar(materias.Select(NotaDeCierre));

	/// <summary>
	/// La media de las notas que existen, a dos decimales, o <c>null</c> si no hay ninguna.
	/// </summary>
	/// <remarks>
	/// Redondeo <c>AwayFromZero</c> y no el bancario, que es el predeterminado de
	/// <c>Math.Round</c>: con el bancario 7,125 daría 7,12 y no 7,13, y un promedio que redondea
	/// "para el lado par" no es el que nadie espera leer en un boletín.
	/// </remarks>
	private static decimal? Promediar(IEnumerable<decimal?> notas)
	{
		var presentes = notas.Where(x => x is not null).Select(x => x!.Value).ToList();

		if (presentes.Count is 0)
		{
			return null;
		}

		return Math.Round(presentes.Average(), 2, MidpointRounding.AwayFromZero);
	}
}
