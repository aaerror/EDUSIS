using Domain.Shared;

namespace Domain.Personas.Domicilios;

public class Domicilio : ValueObject
{
	public Direccion Direccion { get; private set; }
	public Ubicacion Ubicacion { get; private set; }


	#region CONSTRUCTOR
	private Domicilio() {}

	private Domicilio(string calle, string altura, string vivienda, string observacion, string localidad, string provincia, string pais)
	{
		Direccion = Direccion.Crear(calle, altura, vivienda, observacion);
		Ubicacion = Ubicacion.Crear(localidad, provincia, pais);
	}

	private Domicilio(Direccion unaDireccion, Ubicacion unaUbicacion)
	{
		if (unaDireccion is null)
		{
			throw new ArgumentNullException(nameof(unaDireccion), "Dirección del domicilio inexistente.");
		}

		if (unaUbicacion is null)
		{
			throw new ArgumentNullException(nameof(unaUbicacion), "Ubicación del domicilio inexistente.");
		}

		Direccion = unaDireccion;
		Ubicacion = unaUbicacion;
	}

	public static Domicilio Crear(string calle, string altura, string vivienda, string observacion, string localidad, string provincia, string pais) =>
		new(calle, altura, vivienda, observacion, localidad, provincia, pais);
	#endregion

	internal Domicilio CambiarDireccion(Direccion unaDireccion)
	{
		return new(unaDireccion, Ubicacion);
	}

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Direccion;
		yield return Ubicacion;
	}
}