using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioDivisiones.DTOs.Responses;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

/// <summary>
/// La ficha de la división elegida, lista para mostrar. Es el envoltorio de
/// <see cref="ResumenDivisionResponse"/>: los <c>Response</c> de <c>Core</c> no se bindean.
/// </summary>
/// <remarks>
/// <para>
/// Los textos se arman acá y no en el XAML por el mismo motivo que en
/// <c>ResumenAcademicoViewModel</c>: dependen de los números ("1 baja", "3 bajas") y un
/// <c>StringFormat</c> no pluraliza. Los armadores son <c>internal static</c> y puros para probarlos
/// sin construir la pantalla.
/// </para>
/// <para>
/// <strong>LOS PENDIENTES SON UNA LISTA Y NO UN TABLERO.</strong> Cátedras sin docente, horas sin
/// asignar, planillas sin cerrar y la licencia del preceptor son cosas que alguien tiene que
/// resolver, y lo que importa es cuáles hay: una grilla de seis contadores en cero obliga a leerlos
/// todos para enterarse de que no pasa nada. Sólo se nombra lo que existe, y sin pendientes se dice
/// en una línea.
/// </para>
/// </remarks>
internal partial class ResumenDivisionViewModel : ObservableObject
{
	private const string GUION = "—";

	/// <summary>El período al que se refiere todo lo que informa la ficha.</summary>
	public string CicloLectivo { get; } = string.Empty;

	/// <summary>
	/// El ciclo es el año en curso. En <c>false</c> la ficha no muestra el estado de hoy, que
	/// <c>Core</c> tampoco informa.
	/// </summary>
	public bool EsCicloEnCurso { get; }

	/// <summary>"28 / 35".</summary>
	public string Matricula { get; } = GUION;

	/// <summary>"2 recursantes · 1 baja en el ciclo".</summary>
	public string DetalleMatricula { get; } = string.Empty;

	/// <summary>"Sin abrir", "Abierta" o "Cerrada"; vacío fuera del año en curso.</summary>
	public string PlanillaDeHoy { get; } = string.Empty;

	public string PorcentajeAsistencia { get; } = GUION;
	public string DetalleAsistencia { get; } = string.Empty;

	public string Promedio { get; } = GUION;
	public string DetalleRendimiento { get; } = string.Empty;

	/// <summary>Lo que hay que resolver en la división, uno por línea.</summary>
	public IReadOnlyList<string> Pendientes { get; } = new List<string>();
	public bool TienePendientes => Pendientes.Count > 0;

	/// <summary>"Pérez, Ana · 5 faltas · 88,1%".</summary>
	public IReadOnlyList<string> AlumnosConMasFaltas { get; } = new List<string>();
	public bool TieneAlumnosConFaltas => AlumnosConMasFaltas.Count > 0;


	public ResumenDivisionViewModel(ResumenDivisionResponse resumen)
	{
		if (resumen is null)
		{
			return;
		}

		CicloLectivo = resumen.CicloLectivo;
		EsCicloEnCurso = resumen.EsCicloEnCurso;

		Matricula = $"{ resumen.Inscriptos } / { resumen.Cupo }";
		DetalleMatricula = DescribirMatricula(resumen);

		PlanillaDeHoy = resumen.PlanillaDeHoy switch
		{
			EstadoPlanillaDelDia.SinAbrir => "Sin abrir",
			EstadoPlanillaDelDia.Abierta => "Abierta",
			EstadoPlanillaDelDia.Cerrada => "Cerrada",
			_ => string.Empty
		};

		PorcentajeAsistencia = resumen.PorcentajeAsistencia is null
			? GUION
			: resumen.PorcentajeAsistencia.Value.ToString("0.#", CultureInfo.CurrentCulture) + "%";
		DetalleAsistencia = DescribirAsistencia(resumen);

		Promedio = resumen.Promedio is null
			? GUION
			: resumen.Promedio.Value.ToString("0.00", CultureInfo.CurrentCulture);
		DetalleRendimiento = DescribirRendimiento(resumen);

		Pendientes = DescribirPendientes(resumen);
		AlumnosConMasFaltas = resumen.AlumnosConMasFaltas
			.Select(x => $"{ x.NombreCompleto } · { Cantidades.Contar(x.Faltas, "falta", "faltas") } · { x.PorcentajeAsistencia.ToString("0.#", CultureInfo.CurrentCulture) }%")
			.ToList();
	}

	#region Armadores de texto
	/// <summary>"2 recursantes · 1 baja en el ciclo", sólo con lo que hay.</summary>
	internal static string DescribirMatricula(ResumenDivisionResponse resumen)
	{
		var partes = new List<string>();

		if (resumen.Recursantes > 0)
		{
			partes.Add(Cantidades.Contar(resumen.Recursantes, "recursante", "recursantes"));
		}

		if (resumen.Bajas > 0)
		{
			partes.Add(Cantidades.Contar(resumen.Bajas, "baja", "bajas") + " en el ciclo");
		}

		if (resumen.Inscriptos >= resumen.Cupo)
		{
			partes.Add("cupo completo");
		}

		return string.Join(" · ", partes);
	}

	/// <summary>"sobre 40 planillas · 12 faltas · 5 tardanzas".</summary>
	/// <remarks>
	/// La tardanza se aclara porque el porcentaje no la descuenta (<c>PorcentajeDeAsistencia</c>): sin
	/// la aclaración, "92% con 5 tardanzas" se lee como si las tardanzas ya estuvieran restadas.
	/// </remarks>
	internal static string DescribirAsistencia(ResumenDivisionResponse resumen)
	{
		if (resumen.Planillas is 0)
		{
			return "Todavía no se tomó asistencia en este ciclo.";
		}

		var partes = new List<string>
		{
			"sobre " + Cantidades.Contar(resumen.Planillas, "planilla", "planillas"),
			Cantidades.Contar(resumen.Faltas, "falta", "faltas")
		};

		if (resumen.Tardanzas > 0)
		{
			partes.Add(Cantidades.Contar(resumen.Tardanzas, "tardanza", "tardanzas") + ", que no descuentan");
		}

		return string.Join(" · ", partes);
	}

	/// <summary>"3 alumnos con materias desaprobadas · 2 ausencias a examen".</summary>
	/// <remarks>
	/// Dice "promedio de los alumnos" cuando no hay excepciones que nombrar para que el número no se
	/// lea como la media de todas las notas: cada alumno pesa una vez
	/// (<c>RendimientoAcademico.PromediarAlumnos</c>).
	/// </remarks>
	internal static string DescribirRendimiento(ResumenDivisionResponse resumen)
	{
		if (resumen.Promedio is null)
		{
			return "Todavía no hay notas de cierre en este ciclo.";
		}

		var partes = new List<string> { "promedio de los alumnos" };

		if (resumen.AlumnosConMateriasDesaprobadas > 0)
		{
			partes.Add(Cantidades.Contar(resumen.AlumnosConMateriasDesaprobadas, "alumno", "alumnos") + " con materias desaprobadas");
		}

		if (resumen.AusenciasAExamen > 0)
		{
			partes.Add(Cantidades.Contar(resumen.AusenciasAExamen, "ausencia a examen", "ausencias a examen"));
		}

		return string.Join(" · ", partes);
	}

	/// <summary>
	/// Lo que hay que resolver, de lo que impide funcionar a lo que sólo está incompleto.
	/// </summary>
	/// <remarks>
	/// El orden es de gravedad: sin preceptor disponible no se toma asistencia mañana; una cátedra
	/// sin docente es una materia que no se dicta; una planilla sin cerrar, horas sin asignar o una
	/// materia sin cátedra son tareas administrativas pendientes. Las planillas sin cerrar se
	/// informan también en un ciclo pasado, porque siguen abiertas.
	/// </remarks>
	internal static IReadOnlyList<string> DescribirPendientes(ResumenDivisionResponse resumen)
	{
		var pendientes = new List<string>();

		if (resumen.PreceptorDeLicencia is true)
		{
			pendientes.Add("El preceptor está de licencia.");
		}

		if (resumen.CatedrasSinDocente is > 0)
		{
			pendientes.Add(Cantidades.Contar(resumen.CatedrasSinDocente.Value, "cátedra", "cátedras") + " sin docente en funciones.");
		}

		if (resumen.CatedrasConSuplente is > 0)
		{
			pendientes.Add(Cantidades.Contar(resumen.CatedrasConSuplente.Value, "cátedra atendida", "cátedras atendidas") + " por un suplente.");
		}

		if (resumen.PlanillasSinCerrar > 0)
		{
			pendientes.Add(Cantidades.Contar(resumen.PlanillasSinCerrar, "planilla anterior", "planillas anteriores") + " sin cerrar.");
		}

		if (resumen.HorasSinAsignar is > 0)
		{
			pendientes.Add(Cantidades.Contar(resumen.HorasSinAsignar.Value, "hora", "horas") + " de cátedra sin horario.");
		}

		if (resumen.MateriasSinCatedra is { Count: > 0 } materias)
		{
			pendientes.Add($"Sin cátedra: { string.Join(", ", materias) }.");
		}

		return pendientes;
	}
	#endregion
}
