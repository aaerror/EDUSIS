using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.ServicioCursantes.DTOs.Responses;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.ViewModels.Alumnos;

/// <summary>
/// La situación académica del alumno, lista para mostrar. Es el envoltorio de
/// <see cref="ResumenAcademicoResponse"/>: los <c>Response</c> de <c>Core</c> no se bindean.
/// </summary>
/// <remarks>
/// <para>
/// Lo consumen DOS pantallas con distinto nivel de detalle, y por eso es un tipo propio y no un
/// puñado de propiedades en cada ViewModel: la ficha del índice de Alumnos muestra qué cursa, el
/// promedio y el porcentaje de asistencia; el perfil agrega la tabla de materias y la trayectoria
/// completa. Las dos leen el mismo objeto, así que no pueden describir lo mismo de dos maneras.
/// </para>
/// <para>
/// Los textos se arman acá y no en el XAML con <c>StringFormat</c> porque dependen de los números:
/// "1 materia aprobada" y "5 materias aprobadas" no son el mismo formato, y un <c>StringFormat</c>
/// no pluraliza. Es el mismo criterio de <c>GestionAlumnosViewModel.DescribirConsecuencia</c>, y por
/// eso los armadores son <c>internal static</c> y puros: se prueban sin construir la pantalla.
/// </para>
/// <para>
/// TODO LO QUE INFORMA ES DE UN SOLO CICLO LECTIVO, el que nombra <see cref="CicloLectivo"/>, y los
/// rótulos lo dicen: "PROMEDIO 2026", no "PROMEDIO". Un número sin período declarado obliga a quien
/// lo lee a adivinar si son las notas de este año o el acumulado de la escolaridad entera.
/// </para>
/// </remarks>
internal partial class ResumenAcademicoViewModel : ObservableObject
{
	private const string GUION = "—";

	#region Response
	private readonly ResumenAcademicoResponse _resumenResponse;
	#endregion

	/// <summary>El período al que se refieren el promedio, las materias y la asistencia.</summary>
	public string CicloLectivo { get; } = string.Empty;

	/// <summary>
	/// El alumno tiene al menos una inscripción. En <c>false</c> no hay ciclo del que hablar y la
	/// pantalla muestra una sola línea en lugar de tres bloques vacíos.
	/// </summary>
	public bool TieneTrayectoria { get; }

	/// <summary>Tiene una inscripción vigente.</summary>
	public bool EstaCursando { get; }

	/// <summary>Qué cursa: "3° Año B · Secundaria", o el aviso de que no está cursando.</summary>
	public string Cursada { get; } = string.Empty;

	/// <summary>Repite el ciclo. La vista lo marca con insignia, no con un texto más.</summary>
	public bool EsRecursante { get; }

	public bool TienePromedio { get; }

	/// <summary>El promedio con coma decimal, o una raya si todavía no rindió nada.</summary>
	public string Promedio { get; } = GUION;

	public bool TieneCalificaciones { get; }

	/// <summary>Sobre qué se calculó el promedio: cuántas materias y cómo se reparten.</summary>
	public string DetalleCalificaciones { get; } = string.Empty;

	public bool TieneAsistencia { get; }

	/// <summary>El porcentaje con el signo, o una raya si no se le tomó asistencia nunca.</summary>
	public string PorcentajeAsistencia { get; } = GUION;

	/// <summary>Las jornadas y las faltas detrás del porcentaje.</summary>
	public string DetalleAsistencia { get; } = string.Empty;

	public ObservableCollection<MateriaCursadaViewModel> Materias { get; } = new();

	public ObservableCollection<InscripcionHistoricaViewModel> Trayectoria { get; } = new();


	public ResumenAcademicoViewModel(ResumenAcademicoResponse resumenResponse)
	{
		if (resumenResponse is null)
		{
			return;
		}

		_resumenResponse = resumenResponse;

		CicloLectivo = _resumenResponse.CicloLectivo ?? string.Empty;
		TieneTrayectoria = _resumenResponse.CicloLectivo is not null;
		EstaCursando = _resumenResponse.EstaCursando;
		EsRecursante = _resumenResponse.Cursada?.EsRecursante ?? false;

		Cursada = DescribirCursada(_resumenResponse.Cursada);

		TienePromedio = _resumenResponse.Promedio is not null;
		Promedio = _resumenResponse.Promedio is null
			? GUION
			: _resumenResponse.Promedio.Value.ToString("0.00", CultureInfo.CurrentCulture);

		TieneCalificaciones = _resumenResponse.TieneCalificaciones;
		DetalleCalificaciones = DescribirCalificaciones(_resumenResponse);

		TieneAsistencia = _resumenResponse.Asistencia.TieneRegistro;
		PorcentajeAsistencia = _resumenResponse.Asistencia.TieneRegistro
			? _resumenResponse.Asistencia.Porcentaje.ToString("0.#", CultureInfo.CurrentCulture) + "%"
			: GUION;
		DetalleAsistencia = DescribirAsistencia(_resumenResponse.Asistencia);

		foreach (var materia in _resumenResponse.Materias)
		{
			Materias.Add(new MateriaCursadaViewModel(materia));
		}

		// De la más reciente a la más vieja: el perfil se abre para ver dónde está el alumno hoy, no
		// dónde empezó. Core las devuelve en el orden inverso porque el historial de la división se
		// lee en orden de cursada.
		foreach (var inscripcion in _resumenResponse.Trayectoria.OrderByDescending(x => x.FechaInicio))
		{
			Trayectoria.Add(new InscripcionHistoricaViewModel(inscripcion));
		}
	}

	#region Armadores de texto
	/// <summary>
	/// Qué cursa, en una línea: "3° Año B · Secundaria".
	/// </summary>
	/// <remarks>
	/// El grado sale de <c>Grados.Decir</c> y no de concatenarle el símbolo al nombre del enum: el
	/// enum se llama <c>Primero</c>, y "Primero° año" es el defecto que esa clase documenta.
	///
	/// Sin inscripción vigente DICE que no está cursando. Dejarlo vacío haría que un egresado y un
	/// alumno con el perfil a medio cargar se vean igual.
	/// </remarks>
	internal static string DescribirCursada(InscripcionHistoricaResponse cursada)
	{
		if (cursada is null)
		{
			return "No tiene una inscripción vigente.";
		}

		string curso = Grados.Decir(cursada.Grado);
		string nivel = string.IsNullOrWhiteSpace(cursada.NivelEducativo)
			? string.Empty
			: $" · { cursada.NivelEducativo }";

		return $"{ curso } { cursada.Division }{ nivel }".Trim();
	}

	/// <summary>
	/// Sobre qué se calculó el promedio: "6 materias · 5 aprobadas · 1 desaprobada".
	/// </summary>
	/// <remarks>
	/// Las ausencias a examen se nombran aparte y sólo si hubo, porque NO entran al promedio: el
	/// dominio las guarda sin nota (<c>Calificacion.Inasistencia</c>), así que un promedio de 7,42
	/// con tres ausencias encima describe algo distinto que el mismo 7,42 sin ninguna, y el número
	/// solo no lo dice.
	/// </remarks>
	internal static string DescribirCalificaciones(ResumenAcademicoResponse resumen)
	{
		if (!resumen.TieneCalificaciones)
		{
			return "Todavía no se le cargó ninguna calificación en este ciclo.";
		}

		var partes = new List<string>
		{
			Contar(resumen.Materias.Count, "materia", "materias")
		};

		if (resumen.MateriasAprobadas > 0)
		{
			partes.Add(Contar(resumen.MateriasAprobadas, "aprobada", "aprobadas"));
		}

		if (resumen.MateriasDesaprobadas > 0)
		{
			partes.Add(Contar(resumen.MateriasDesaprobadas, "desaprobada", "desaprobadas"));
		}

		if (resumen.AusenciasAExamen > 0)
		{
			partes.Add(Contar(resumen.AusenciasAExamen, "ausencia a examen", "ausencias a examen") + ", que no promedian");
		}

		return string.Join(" · ", partes) + ".";
	}

	/// <summary>
	/// Las jornadas detrás del porcentaje: "118 jornadas · 7 faltas (5 justificadas) · 3 tardanzas".
	/// </summary>
	/// <remarks>
	/// Las faltas se informan sumadas Y repartidas porque la justificada y la injustificada son la
	/// misma ausencia de clase para el porcentaje y decisiones distintas para el preceptor. La
	/// tardanza va aparte y NO descuenta: el alumno estuvo en clase.
	/// </remarks>
	internal static string DescribirAsistencia(AsistenciaAcumuladaResponse asistencia)
	{
		if (!asistencia.TieneRegistro)
		{
			return "Todavía no se le tomó asistencia en este ciclo.";
		}

		var partes = new List<string>
		{
			Contar(asistencia.Jornadas, "jornada", "jornadas")
		};

		if (asistencia.Faltas > 0)
		{
			string falta = Contar(asistencia.Faltas, "falta", "faltas");

			partes.Add(asistencia.Ausencias > 0 && asistencia.Inasistencias > 0
				? $"{ falta } ({ Contar(asistencia.Ausencias, "justificada", "justificadas") })"
				: falta);
		}

		if (asistencia.Tardanzas > 0)
		{
			partes.Add(Contar(asistencia.Tardanzas, "tardanza", "tardanzas") + ", que no descuentan");
		}

		return string.Join(" · ", partes) + ".";
	}

	private static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";
	#endregion
}
