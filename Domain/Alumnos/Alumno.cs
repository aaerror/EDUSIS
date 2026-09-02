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

	public Alumno(string legajo, DatosPersonales datosPersonales, Domicilio unDomicilio, string email, string telefono)
		: base(datosPersonales, unDomicilio, email, telefono)
	{
		Legajo = legajo;
		Periodo = RangoFechas.Create(DateTime.Today.Date);

		AgregarEvento(new AlumnoInscriptoDomainEvent(Id));
	}

	#region Insititucional
	public bool EstaActivo() =>
		Periodo.EstaVigente();

	public void Desinscribir()
	{
		if (Periodo.HaFinalizado())
		{
			throw new AlumnoInactivoException();
		}

		Periodo = Periodo.ActualizarFechaFin(DateTime.Today.Date);
		AgregarEvento(new AlumnoDesinscriptoDomainEvent(Id));
	}
	#endregion
}