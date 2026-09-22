using System.Text.RegularExpressions;
using Domain.Alumnos.DomainEvents;
using Domain.Alumnos.Exceptions;
using Domain.Personas;
using Domain.Personas.Domicilios;
using Domain.Shared;

namespace Domain.Alumnos;

public sealed class Alumno : Persona
{
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