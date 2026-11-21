using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioLicencias.DTOs.Responses;
using Domain.Licencias;
using System;

namespace WPF_Desktop.ViewModels.Licencias;

/// <summary>
/// Una fila de la bandeja de pendientes. Sólo se lee: la decisión se toma desde la bandeja, y lo
/// único que se escribe (la observación) vive en <see cref="LicenciasPendientesViewModel"/>.
/// </summary>
internal sealed class LicenciaPendienteViewModel : ObservableObject
{
	public Guid LicenciaID { get; }
	public Guid DocenteID { get; }
	public string Docente { get; }
	public string Denominacion { get; }
	public int Dias { get; }
	public DateTime FechaInicio { get; }
	public DateTime? FechaFin { get; }
	public string? Observacion { get; }

	/// <summary>Si la decide sólo Secretaría Académica: define qué permiso piden aprobar y rechazar.</summary>
	public bool ResolucionAcademica { get; }


	public LicenciaPendienteViewModel(LicenciaPendienteResponse response)
	{
		var plazo = PlazoLegal.De(response.Articulo);

		LicenciaID = response.LicenciaID;
		DocenteID = response.DocenteID;
		Docente = response.Docente;
		Denominacion = plazo.Denominacion;
		ResolucionAcademica = plazo.ResolucionAcademica;
		Dias = response.Dias;
		FechaInicio = response.FechaInicio;
		FechaFin = response.FechaFin;
		Observacion = response.Observacion;
	}
}
