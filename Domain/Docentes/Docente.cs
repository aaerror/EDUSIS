using Domain.Docentes.DomainEvents;
using Domain.Docentes.Exceptions;
using Domain.Docentes.Puestos;
using Domain.Licencias;
using Domain.Personas;
using Domain.Personas.Domicilios;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Docentes;

public sealed class Docente : Persona
{
	private List<Puesto> _puestos = new();
	private List<Licencia> _licencias = new();

	public string Legajo { get; private set; }
	public string CUIL { get; private set; }
	public RangoFechas Periodo { get; private set; }
	public Puesto? Puesto { get; private set; }
	public bool Activo { get; private set; }

	public IReadOnlyCollection<Puesto> Puestos => _puestos.AsReadOnly();
	public IReadOnlyCollection<Licencia> Licencias => _licencias.AsReadOnly();


	#region CONSTRUCTOR
	private Docente()
		: base() { }

	private Docente(string legajo,
					string cuil,
					DateTime fechaAlta,
					DateTime? fechaBaja,
					DatosPersonales datosPersonales,
					Domicilio domicilio,
					string email,
					string telefono)
		: base(datosPersonales, domicilio, email, telefono)
	{
		if (datosPersonales.Edad() < 18)
		{
			throw new DocenteMenorDeEdadException();
		}

		if ((datosPersonales.Sexo.Equals(Sexo.Masculino) && datosPersonales.Edad() > 64) || (datosPersonales.Sexo.Equals(Sexo.Femenino) && datosPersonales.Edad() > 59))
		{
			throw new DocenteEnEdadJubilatoriaException();
		}

		if (fechaAlta.Date > DateTime.Today.Date)
		{
			throw new ArgumentException($"La fecha de alta ({fechaAlta.Date.ToString("D")}) debe ser anterior al día de hoy ({DateTime.Now.Date.ToString("D")}).");
		}

		ValidarLegajo(legajo);
		ValidarCuil(cuil);

		Legajo = legajo;
		CUIL = cuil;
		Periodo = fechaBaja.HasValue ? RangoFechas.Create(fechaAlta, fechaBaja.Value) : RangoFechas.Create(fechaAlta);
		Activo = Periodo.HaIniciado();
	}

	public Docente(string legajo, string cuil, DateTime fechaAlta, DatosPersonales datosPersonales, Domicilio domicilio, string email, string telefono)
		: this(legajo, cuil, fechaAlta, null, datosPersonales, domicilio, email, telefono) { }
	#endregion

	private void ValidarLegajo(string legajoDocente)
	{
		if (!Regex.IsMatch(legajoDocente, @"^(\d{6})$"))
		{
			throw new FormatException($"Verificar el legajo del docente: { nameof(legajoDocente) }.");
		}
	}

	private void ValidarCuil(string cuil)
	{
		//@"^\d{2}-\d{8}-\d{1}$
		if (!Regex.IsMatch(cuil, @"\b(20|23|24|27|30|33|34)(\D)?[0-9]{8}(\D)?[0-9]"))
		{
			throw new FormatException($"Verificar el CUIL del docente: { nameof(cuil) }.");
		}
	}

	#region Institucional
	public void Desafectar()
	{
		if (Periodo.HaFinalizado())
		{
			throw new DocenteInactivoException();
		}

		Periodo = Periodo.ActualizarFechaFin(DateTime.Today.Date);
		Activo = Periodo.EstaVigente();
		AgregarEvento(new DocenteDesafectadoDomainEvent(Id));
	}
	#endregion

	#region PuestoDocente
	private Puesto? BuscarPuestoDocente(Guid puestoID) =>
		_puestos.Where(x => x.Id.Equals(puestoID))
				.FirstOrDefault();

	private bool EsPosicionAsignada(Posicion unaPosicion) =>
		_puestos.Any(x => x.Posicion.Equals(unaPosicion) && x.Estado.Equals(EstadoPuesto.Activo));

	public IEnumerable<Puesto> PuestoDocentesAsignados() =>
		_puestos.Where(x => x.Periodo.EstaVigente())
				.AsEnumerable();

	public void AsignarCargoDocente(string unaPosicion, string unEstado, DateTime fechaInicio, DateTime? fechaFin)
	{
		var posicion = Enum.Parse<Posicion>(unaPosicion);

		if (!Activo)
		{
			throw new DocenteInactivoException();
		}

		if (EsPosicionAsignada(posicion))
		{
			throw new PuestoDocenteAsignadoException(nameof(posicion));
		}

		if (Periodo.FechaInicio > fechaInicio.Date)
		{
			throw new ArgumentException($"La fecha de inicio en el cargo docente debe ser la misma a la fecha de alta del docente en la institución o posterior.");
		}


		var nuevoPuesto = new Puesto(Id, unEstado, unaPosicion, fechaInicio, fechaFin);
		_puestos.Add(nuevoPuesto);
	}

	public void ModificarCargoDocente(Guid puestoID, string unaPosicion, DateTime fechaInicio, DateTime? fechaFin)
	{
		var puesto = BuscarPuestoDocente(puestoID);
		if (puesto is null)
		{
			throw new ArgumentException("Puesto docente no encontrado.", nameof(puestoID));
		}

		var estado = puesto.Estado;
		if (!estado.Equals(EstadoPuesto.Pendiente))
		{
			throw new ArgumentException("El puesto docente que desea modificar no se encuentra pendiente.");
		}

		var result = Enum.TryParse<Posicion>(unaPosicion, out var posicion);
		if (!result)
		{
			throw new ArgumentException("Posición inválida.", nameof(unaPosicion));
		}

		var asignada = EsPosicionAsignada(posicion);
		if (asignada)
		{
			throw new PuestoDocenteAsignadoException(nameof(posicion));
		}

		puesto.ActualizarPuestoDocente(unaPosicion, fechaInicio, fechaFin);
	}

	public void CambiarEventualidadCargoDocente(Guid posicionID)
	{
		if (!Activo)
		{
			throw new DocenteInactivoException();
		}

		var puesto = BuscarPuestoDocente(posicionID);
		puesto.EstablecerComoPuestoFijo();
	}

	public void RescindirCargoDocente(Guid puestoID, DateTime? fechaFinalizacion)
	{
		if (!Activo)
		{
			throw new DocenteInactivoException();
		}

		var puestoDocente = BuscarPuestoDocente(puestoID);
		if (puestoDocente is null)
		{
			throw new PuestoDocenteNoEncontradoException();
		}

		if (puestoDocente.Estado.Equals(EstadoPuesto.Inactivo))
		{
			throw new PuestoDocenteSinAsignarException();
		}

		if (fechaFinalizacion.HasValue)
		{
			puestoDocente.Rescindir(fechaFinalizacion.Value);
		}
		else
		{
			puestoDocente.Rescindir();
		}
	}

	public void EliminarCargoDocente(Guid puestoID)
	{
		if (!Activo)
		{
			throw new DocenteInactivoException();
		}

		var puestoDocente = BuscarPuestoDocente(puestoID);
		if (puestoDocente is null)
		{
			throw new PuestoDocenteNoEncontradoException();
		}

		if (!puestoDocente.Estado.Equals(EstadoPuesto.Pendiente))
		{
			throw new ArgumentException("El puesto docente no se encuentra pendiente.");
		}

		_puestos.Remove(puestoDocente);
	}
	#endregion
}