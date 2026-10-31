using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioAsistencias.DTOs.Responses;
using Domain.Asistencias;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

/// <summary>
/// Una fila de la planilla de asistencia del día.
/// </summary>
/// <remarks>
/// <para>
/// Existe por la regla de siempre: los <c>Response</c> de <c>Core</c> no se bindean —son el contrato
/// de la capa de aplicación, no el modelo de presentación—, así que cada registro de la planilla
/// viaja a la grilla envuelto en este ViewModel, con los valores ya formateados para la vista
/// (<see cref="Minutos"/> con su sufijo, <see cref="Estado"/> como texto).
/// </para>
/// <para>
/// Acá además hace falta <b>unir dos fuentes</b>: <c>PlanillaAsistenciaResponse</c> identifica a cada
/// cursante sólo por su <c>CursanteID</c> y no trae nombres, y una grilla de asistencia sin nombres
/// no sirve. El nombre lo resuelve el ViewModel padre contra <c>ListarCursantesAsync</c> y lo pasa por
/// constructor: así la pantalla se arma sin tocar <c>Core</c> ni agregarle un campo al
/// <c>Response</c> que sólo esta vista necesita.
/// </para>
/// </remarks>
internal partial class RegistroAsistenciaViewModel : ObservableObject
{
	[ObservableProperty]
	private Guid _cursanteID;

	[ObservableProperty]
	private string _nombreCompleto = string.Empty;

	[ObservableProperty]
	private string _estado = string.Empty;

	/// <summary>Los minutos de la tardanza, ya formateados. Vacío en los registros que no son tardanza.</summary>
	[ObservableProperty]
	private string _minutos = string.Empty;

	[ObservableProperty]
	private string _observacion = string.Empty;

	/// <summary>
	/// La fila cuenta como falta —ausencia o inasistencia—. Lo lee la grilla para marcarla con la
	/// insignia de alerta: es lo que el preceptor busca de un vistazo en la planilla del día.
	/// </summary>
	[ObservableProperty]
	private bool _esFalta;


	public RegistroAsistenciaViewModel(RegistroAsistenciaResponse registro, string nombreCompleto)
	{
		CursanteID = Guid.Empty;
		NombreCompleto = nombreCompleto ?? string.Empty;
		Estado = string.Empty;
		Minutos = string.Empty;
		Observacion = string.Empty;
		EsFalta = false;

		if (registro is not null)
		{
			CursanteID = registro.CursanteID;
			Estado = registro.Tipo.ToString();
			Minutos = registro.Minutos is null ? string.Empty : $"{ (int)registro.Minutos.Value.TotalMinutes } min";
			Observacion = registro.Observacion ?? string.Empty;
			EsFalta = registro.Tipo is TipoAsistencia.Ausencia or TipoAsistencia.Inasistencia;
		}
	}
}
