using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursantes;

public class CicloLectivo : ValueObject
{
	public string Periodo { get; private set; }


	#region CONSTRUCTOR
	private CicloLectivo(string periodo)
	{
		if (string.IsNullOrWhiteSpace(periodo))
		{
			throw new ArgumentNullException(nameof(periodo), "Se debe especificar el ciclo lectivo.");
		}

		if (!Regex.IsMatch(periodo, @"^(\d){4}$", RegexOptions.None))
		{
			throw new FormatException($"El formato del ciclo lectivo es incorrecto: {periodo}.");
		}

		Periodo = periodo;
	}

	public static CicloLectivo Crear(string periodo)
	{
		return new(periodo);
	}
	#endregion

	public bool AñoEnCurso() => int.Parse(Periodo) == DateTime.Now.Year;

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Periodo;
	}
}