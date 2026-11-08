using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioAlumnos.DTOs.Responses;
using System.Globalization;
using System;

namespace WPF_Desktop.ViewModels.Alumnos;

/// <summary>
/// Una fila de la tabla de materias del perfil. Es el envoltorio de
/// <see cref="MateriaCursadaResponse"/>: los <c>Response</c> de <c>Core</c> no se bindean.
/// </summary>
internal partial class MateriaCursadaViewModel : ObservableObject
{
	#region Response
	private readonly MateriaCursadaResponse _materiaResponse;
	#endregion

	[ObservableProperty]
	private Guid _materiaID = Guid.Empty;

	[ObservableProperty]
	private string _materia = string.Empty;

	[ObservableProperty]
	private int _rendidas;

	[ObservableProperty]
	private int _ausenciasAExamen;

	[ObservableProperty]
	private bool _aprobada;

	/// <summary>
	/// El promedio de la materia con coma decimal, o una raya si no rindió ninguna instancia.
	/// </summary>
	/// <remarks>
	/// La raya y no un "0,00": el alumno que faltó a los dos exámenes no tiene cero de promedio, y
	/// un cero en la columna se lee como que rindió y le fue mal.
	/// </remarks>
	public string Promedio { get; } = GUION;

	/// <summary>Rótulo de la insignia de estado: lo lee la grilla, que no decide vocabulario.</summary>
	public string Estado => Aprobada ? "Aprobada" : "Desaprobada";

	/// <summary>
	/// Cuántas veces se la evaluó, contando las ausencias. Una materia con una nota y dos ausencias
	/// no es lo mismo que una con una nota sola, y el promedio por sí solo no lo distingue.
	/// </summary>
	public string Instancias =>
		AusenciasAExamen is 0
			? Contar(Rendidas, "instancia rendida", "instancias rendidas")
			: $"{ Contar(Rendidas, "instancia rendida", "instancias rendidas") } · { Contar(AusenciasAExamen, "ausencia", "ausencias") }";

	private const string GUION = "—";


	public MateriaCursadaViewModel(MateriaCursadaResponse materiaResponse)
	{
		if (materiaResponse is not null)
		{
			_materiaResponse = materiaResponse;

			MateriaID = _materiaResponse.MateriaID;
			Materia = _materiaResponse.Materia;
			Rendidas = _materiaResponse.Rendidas;
			AusenciasAExamen = _materiaResponse.AusenciasAExamen;
			Aprobada = _materiaResponse.Aprobada;

			Promedio = _materiaResponse.Promedio is null
				? GUION
				: _materiaResponse.Promedio.Value.ToString("0.00", CultureInfo.CurrentCulture);
		}
	}

	private static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";
}
