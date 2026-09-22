using Domain.Personas.Domicilios;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="Domicilio"/>. Estado por defecto válido con acentos y
/// <c>ñ</c>. <see cref="Build"/> usa la factoría pública <see cref="Domicilio.Crear"/>.
/// </summary>
public sealed class DomicilioBuilder
{
	#region ESTADO POR DEFECTO
	private string _calle = "Pasaje Ñuñorco";
	private string _altura = "1234";
	private Vivienda _vivienda = Vivienda.Casa;
	private string _observacion = "Portón verde, timbre a la derecha";
	private string _localidad = "Yerba Buena";
	private string _provincia = "Tucumán";
	private string _pais = "Argentina";
	#endregion

	#region CONFIGURACIÓN
	public DomicilioBuilder ConCalle(string calle)
	{
		_calle = calle;
		return this;
	}

	public DomicilioBuilder ConAltura(string altura)
	{
		_altura = altura;
		return this;
	}

	public DomicilioBuilder ConVivienda(Vivienda vivienda)
	{
		_vivienda = vivienda;
		return this;
	}

	public DomicilioBuilder ConObservacion(string observacion)
	{
		_observacion = observacion;
		return this;
	}

	public DomicilioBuilder ConLocalidad(string localidad)
	{
		_localidad = localidad;
		return this;
	}

	public DomicilioBuilder ConProvincia(string provincia)
	{
		_provincia = provincia;
		return this;
	}

	public DomicilioBuilder ConPais(string pais)
	{
		_pais = pais;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Domicilio Build() =>
		Domicilio.Crear(_calle, _altura, _vivienda.ToString(), _observacion, _localidad, _provincia, _pais);
	#endregion
}
