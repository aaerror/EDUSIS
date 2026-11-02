using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioAsistencias.DTOs.Responses;
using System;
using System.Globalization;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

/// <summary>
/// Una fila del historial: la planilla de un día ya cargado, con su estado y los cuatro contadores.
/// </summary>
/// <remarks>
/// Guarda <see cref="Fecha"/> sin formatear además del texto porque la fila es navegable: elegirla
/// lleva la pantalla a la planilla de ese día, y para eso hace falta el <c>DateTime</c>, no su
/// leyenda. El resto de los campos sí viajan ya formateados, que es el trato de siempre con la vista.
/// </remarks>
internal partial class PlanillaResumenViewModel : ObservableObject
{
	/// <summary>La fecha sin formatear: es lo que viaja al <c>DatePicker</c> cuando se abre esta fila.</summary>
	[ObservableProperty]
	private DateTime _fecha;

	/// <summary>El día con su nombre —"vie 24/10"—, porque en un historial de asistencia importa qué día de la semana fue.</summary>
	[ObservableProperty]
	private string _dia = string.Empty;

	[ObservableProperty]
	private string _estado = string.Empty;

	/// <summary>Lo lee la grilla para pintar la insignia: cerrada es neutra, abierta es la que todavía espera algo.</summary>
	[ObservableProperty]
	private bool _cerrada;

	[ObservableProperty]
	private int _alumnos;

	[ObservableProperty]
	private int _presentes;

	[ObservableProperty]
	private int _ausencias;

	[ObservableProperty]
	private int _inasistencias;

	[ObservableProperty]
	private int _tardanzas;

	/// <summary>
	/// Ausencias más inasistencias: las dos son inasistencia a clase. Es la columna que se mira para
	/// saber si ese día pasó algo.
	/// </summary>
	[ObservableProperty]
	private int _faltas;


	public PlanillaResumenViewModel(PlanillaResumenResponse planilla)
	{
		if (planilla is null)
		{
			return;
		}

		Fecha = planilla.Fecha;
		Dia = planilla.Fecha.ToString("ddd dd/MM", CultureInfo.CurrentCulture);
		Cerrada = planilla.Cerrada;
		Estado = planilla.Cerrada ? "Cerrada" : "Abierta";
		Alumnos = planilla.Registros;
		Presentes = planilla.Presentes;
		Ausencias = planilla.Ausencias;
		Inasistencias = planilla.Inasistencias;
		Tardanzas = planilla.Tardanzas;
		Faltas = planilla.Ausencias + planilla.Inasistencias;
	}
}
