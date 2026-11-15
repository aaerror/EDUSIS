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

	/// <summary>Nota del primer cuatrimestre, o una raya si no rindió.</summary>
	/// <remarks>
	/// Las cuatro columnas de nota son texto ya formateado y no <c>decimal?</c>: la celda muestra
	/// una raya cuando no hay nota, y la raya y no un "0,00" porque el alumno que faltó al examen
	/// no tiene cero —un cero en la columna se lee como que rindió y le fue mal—.
	///
	/// Cada una es la nota VIGENTE del período: si hubo recuperatorio, es la del recuperatorio.
	/// </remarks>
	public string PrimerCuatrimestre { get; } = GUION;

	/// <inheritdoc cref="PrimerCuatrimestre"/>
	public string SegundoCuatrimestre { get; } = GUION;

	/// <inheritdoc cref="PrimerCuatrimestre"/>
	public string Final { get; } = GUION;

	/// <summary>
	/// Nota de recuperación, de los dos períodos en una sola columna.
	/// </summary>
	/// <inheritdoc cref="PrimerCuatrimestre"/>
	public string Recuperacion { get; } = GUION;

	/// <summary>La nota con la que cierra la materia: la que decide la condición.</summary>
	public string NotaDeCierre { get; } = GUION;

	/// <summary>Rótulo de la insignia de estado: lo lee la grilla, que no decide vocabulario.</summary>
	public string Estado => Aprobada ? "Aprobada" : "Desaprobada";

	/// <summary>
	/// Cuántas veces se la evaluó, contando las ausencias. Una materia con una nota y dos ausencias
	/// no es lo mismo que una con una nota sola, y las notas por sí solas no lo distinguen.
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

			PrimerCuatrimestre = Formatear(_materiaResponse.PrimerCuatrimestre);
			SegundoCuatrimestre = Formatear(_materiaResponse.SegundoCuatrimestre);
			Final = Formatear(_materiaResponse.Final);
			Recuperacion = Formatear(_materiaResponse.Recuperacion);
			NotaDeCierre = Formatear(_materiaResponse.NotaDeCierre);
		}
	}

	private static string Formatear(decimal? nota) =>
		nota is null ? GUION : nota.Value.ToString("0.00", CultureInfo.CurrentCulture);

	private static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";
}
