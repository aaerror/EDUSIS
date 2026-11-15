using System.ComponentModel;

namespace Domain.Cursantes.Calificaciones;

/// <summary>
/// El tramo del ciclo lectivo al que pertenece una calificación.
/// </summary>
/// <remarks>
/// <para>
/// El ciclo se divide en dos cuatrimestres —son dos y no tres porque el receso escolar se come
/// al menos dos meses—, cada uno con su nota. Después el docente cierra la materia con una nota
/// final, y para quien no la alcanzó quedan dos períodos de recuperación.
/// </para>
/// <para>
/// Cada uno de estos tramos admite además un recuperatorio, que no es un valor de este enum sino
/// <see cref="Calificacion.EsRecuperatorio"/>: "recuperatorio" no es un momento del ciclo sino el
/// segundo intento DENTRO de un momento, y hay cuatro posibles en un mismo ciclo. Reemplazó a un
/// enum <c>Instancia { Parcial, Recuperatorio }</c> que colapsaba los dos ejes en uno y por eso no
/// podía decir de qué cuatrimestre era un recuperatorio.
/// </para>
/// <para>
/// El orden de declaración es cronológico y se usa para la lista desplegable de la pantalla de
/// calificaciones. La precedencia con la que se decide la nota de cierre NO sale de acá sino de
/// <see cref="RendimientoAcademico"/>, que la declara explícita: atar una regla de negocio al
/// orden en que están escritos los miembros de un enum la rompe en silencio el día que alguien
/// agrega un valor en el medio.
/// </para>
/// </remarks>
public enum Periodo
{
	[Description("1° cuatrimestre")]
	PrimerCuatrimestre,

	[Description("2° cuatrimestre")]
	SegundoCuatrimestre,

	[Description("Nota final")]
	Final,

	[Description("1ª recuperación")]
	PrimeraRecuperacion,

	[Description("2ª recuperación")]
	SegundaRecuperacion,
}
