using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioAsistencias.DTOs.Responses;
using System;
using System.Globalization;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

/// <summary>
/// Una fila del resumen: lo que acumuló un alumno en el período consultado.
/// </summary>
/// <remarks>
/// El nombre no viene en <see cref="ResumenCursanteResponse"/> —la planilla identifica a los alumnos
/// por su identificador— y lo resuelve el ViewModel padre contra el listado de cursantes, igual que en
/// la grilla del día. Ver el <c>remarks</c> de <see cref="RegistroAsistenciaViewModel"/>.
/// </remarks>
internal partial class ResumenCursanteViewModel : ObservableObject
{
	[ObservableProperty]
	private Guid _cursanteID;

	[ObservableProperty]
	private string _nombreCompleto = string.Empty;

	/// <summary>Días del período en los que el alumno estuvo en la planilla. Es el divisor del porcentaje.</summary>
	[ObservableProperty]
	private int _jornadas;

	[ObservableProperty]
	private int _presentes;

	[ObservableProperty]
	private int _ausencias;

	[ObservableProperty]
	private int _inasistencias;

	[ObservableProperty]
	private int _tardanzas;

	[ObservableProperty]
	private int _faltas;

	/// <summary>El porcentaje ya formateado con su signo: la grilla lo muestra, no lo calcula.</summary>
	[ObservableProperty]
	private string _asistencia = string.Empty;

	/// <summary>
	/// El alumno tiene al menos una falta en el período. Lo lee la grilla para marcar la fila.
	/// </summary>
	/// <remarks>
	/// Es "tiene faltas" y no "está en riesgo" a propósito. Cuántas faltas se toleran antes de perder
	/// la regularidad es una regla de la institución, y esta pantalla no la conoce: señalar un umbral
	/// inventado sería afirmar algo que el sistema no sabe. El orden de la grilla —de más faltas a
	/// menos, que ya viene resuelto de <c>Core</c>— es lo que pone al alumno del que hay que ocuparse
	/// en la primera fila, y el corte lo hace quien mira.
	/// </remarks>
	[ObservableProperty]
	private bool _tieneFaltas;


	public ResumenCursanteViewModel(ResumenCursanteResponse cursante, string nombreCompleto)
	{
		NombreCompleto = nombreCompleto ?? string.Empty;

		if (cursante is null)
		{
			return;
		}

		CursanteID = cursante.CursanteID;
		Jornadas = cursante.Jornadas;
		Presentes = cursante.Presentes;
		Ausencias = cursante.Ausencias;
		Inasistencias = cursante.Inasistencias;
		Tardanzas = cursante.Tardanzas;
		Faltas = cursante.Faltas;
		Asistencia = $"{ cursante.PorcentajeAsistencia.ToString("0.#", CultureInfo.CurrentCulture) }%";
		TieneFaltas = cursante.Faltas > 0;
	}
}
