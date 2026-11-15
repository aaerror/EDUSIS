using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.DTOs.Cursadas;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

/// <summary>
/// Una fila del historial de divisiones del alumno. Es el envoltorio de
/// <see cref="InscripcionHistoricaResponse"/>: los <c>Response</c> de <c>Core</c> no se bindean.
/// </summary>
/// <remarks>
/// El historial existe porque el traslado de división se modela como cerrar la inscripción actual
/// y abrir una nueva en el destino, así que un alumno trasladado tiene más de una inscripción en
/// el mismo ciclo lectivo. La fila con <c>EstaActiva</c> en <c>true</c> es la vigente; las demás
/// son registro histórico y la vista las marca con <c>Insignia.Alerta</c>.
/// </remarks>
internal partial class InscripcionHistoricaViewModel : ObservableObject
{
	#region Response
	private readonly InscripcionHistoricaResponse _inscripcionResponse;
	#endregion

	[ObservableProperty]
	private Guid _cursanteID = Guid.Empty;

	[ObservableProperty]
	private Guid _divisionID = Guid.Empty;

	[ObservableProperty]
	private string _division = string.Empty;

	[ObservableProperty]
	private string _grado = string.Empty;

	[ObservableProperty]
	private string _nivelEducativo = string.Empty;

	[ObservableProperty]
	private string _cicloLectivo = string.Empty;

	[ObservableProperty]
	private DateTime _fechaInicio;

	[ObservableProperty]
	private DateTime? _fechaFin;

	[ObservableProperty]
	private bool _esRecursante;

	[ObservableProperty]
	private bool _estaActiva;

	/// <summary>Rótulo de la insignia de estado: lo lee la grilla, que no decide vocabulario.</summary>
	public string Estado => EstaActiva ? "Vigente" : "Cerrada";


	public InscripcionHistoricaViewModel(InscripcionHistoricaResponse inscripcionResponse)
	{
		if (inscripcionResponse is not null)
		{
			_inscripcionResponse = inscripcionResponse;

			CursanteID = _inscripcionResponse.CursanteID;
			DivisionID = _inscripcionResponse.DivisionID;
			Division = _inscripcionResponse.Division;
			Grado = _inscripcionResponse.Grado;
			NivelEducativo = _inscripcionResponse.NivelEducativo;
			CicloLectivo = _inscripcionResponse.CicloLectivo;
			FechaInicio = _inscripcionResponse.FechaInicio;
			FechaFin = _inscripcionResponse.FechaFin;
			EsRecursante = _inscripcionResponse.EsRecursante;
			EstaActiva = _inscripcionResponse.EstaActiva;
		}
	}
}
