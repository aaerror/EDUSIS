namespace WPF_Desktop.Shared;

/// <summary>
/// Cantidades dichas en castellano, con el sustantivo en el número que corresponde.
/// </summary>
/// <remarks>
/// Un <c>StringFormat</c> no pluraliza: "1 faltas" es lo que sale de bindear el número solo.
/// </remarks>
internal static class Cantidades
{
	/// <summary>"1 falta", "3 faltas", "0 faltas".</summary>
	public static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";
}
