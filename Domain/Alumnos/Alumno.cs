using System.Text.RegularExpressions;
using Domain.Alumnos.DomainEvents;
using Domain.Alumnos.Exceptions;
using Domain.Personas;
using Domain.Personas.Domicilios;
using Domain.Shared;

namespace Domain.Alumnos;

public sealed class Alumno : Persona
{
	private const string PREFIJO_LEGAJO = "AL";
	private const int CANTIDAD_DIGITOS_LEGAJO = 4;
	private const int MAXIMO_LEGAJO = 9999;

	public string Legajo { get; private set; }
	public RangoFechas Periodo { get; private set; }


	private Alumno()
		: base() {}

	public Alumno(
		string legajo,
		DatosPersonales datosPersonales,
		Domicilio unDomicilio,
		string email,
		string telefono)
			: base(datosPersonales, unDomicilio, email, telefono)
	{
		if (!Regex.IsMatch(legajo, @"^([A-Z]{2}\d{4})$"))
		{
			throw new FormatException($"Verificar el legajo del docente: {nameof(legajo)}.");
		}

		Legajo = legajo;
		Periodo = RangoFechas.Create(DateTime.Today.Date);

		AgregarEvento(new AlumnoInscriptoDomainEvent(Id));
	}

	#region Legajo
	/// <summary>
	/// Deriva el legajo que le corresponde al próximo alumno a partir del último existente.
	/// Sin legajos previos arranca en <c>AL0001</c>.
	/// </summary>
	public static string SiguienteLegajo(string? ultimoLegajo)
	{
		var ultimoNumero = string.IsNullOrWhiteSpace(ultimoLegajo)
			? 0
			: int.Parse(ultimoLegajo.Trim()[^CANTIDAD_DIGITOS_LEGAJO..]);

		var siguienteNumero = ultimoNumero + 1;

		if (siguienteNumero > MAXIMO_LEGAJO)
		{
			throw new LegajosAgotadosException();
		}

		return $"{PREFIJO_LEGAJO}{siguienteNumero.ToString($"D{CANTIDAD_DIGITOS_LEGAJO}")}";
	}
	#endregion

	#region Insititucional
	public bool EstaActivo() =>
		Periodo.HaIniciado() && !Periodo.FechaFin.HasValue;

	public void Desinscribir()
	{
		if (!EstaActivo())
		{
			throw new AlumnoInactivoException();
		}

		Periodo = Periodo.ActualizarFechaFin(DateTime.Today.Date);
		AgregarEvento(new AlumnoDesinscriptoDomainEvent(Id));
	}
	#endregion
}