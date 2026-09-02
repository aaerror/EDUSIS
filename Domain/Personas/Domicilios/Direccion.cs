using Domain.Shared.Exceptions;
using Domain.Shared;

namespace Domain.Personas.Domicilios;

public class Direccion : ValueObject
{
	public string Calle { get; private set; }
	public string Altura { get; private set; }
	public Vivienda Vivienda { get; private set; }
	public string Observacion { get; private set; } = string.Empty;


	private Direccion(string calle, string altura, Vivienda vivienda, string observacion)
	{
		if (string.IsNullOrWhiteSpace(calle))
		{
			throw new ArgumentNullException(nameof(calle), $"La dirección posee datos incompletos.");
		}

		if (!string.IsNullOrWhiteSpace(observacion))
		{
			if (observacion.Trim().Length > 120)
			{
				throw new ExcesoCaracteresException("La observación no debe superar los 120 caracteres.");
			}
		}

		Vivienda = vivienda;
		Calle = calle.Trim();
		Altura = altura;
		Observacion = string.IsNullOrWhiteSpace(observacion) ? observacion : observacion.Trim();
	}

	public static Direccion Crear(string calle, string altura, string vivienda, string observacion) =>
		new(calle, altura, Enum.Parse<Vivienda>(vivienda), observacion);

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Calle;
		yield return Altura;
		yield return Vivienda;
		yield return Observacion;
	}
}