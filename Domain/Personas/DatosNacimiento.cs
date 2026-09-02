using Domain.Personas.Exceptions;
using Domain.Shared;

namespace Domain.Personas;

public class DatosNacimiento : ValueObject
{
	public DateTime FechaNacimiento { get; private set; }
	public int Edad => DateTime.Today.Year - FechaNacimiento.Date.Year;


	#region CONSTRUCTOR
	private DatosNacimiento(DateTime fechaNacimiento)
	{
		if (fechaNacimiento.Date >= DateTime.Today.Date)
		{
			throw new FechaNacimientoInvalidaException();
		}

		FechaNacimiento = fechaNacimiento.Date;
	}

	public static DatosNacimiento Crear(DateTime fechaNacimiento) =>
		new(fechaNacimiento);
	#endregion

	public bool EsMayorEdad() =>
		Edad > 18;

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return FechaNacimiento;
	}
}