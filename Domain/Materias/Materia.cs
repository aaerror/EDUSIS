using Domain.Materias.DomainEvents;
using Domain.Shared;

namespace Domain.Materias;

public sealed class Materia : Entity
{
	public Guid CurriculaID { get; private set; }
	public string Descripcion { get; private set; } = string.Empty;
	public int HorasCatedra { get; private set; }


	#region CONSTRUCTOR
	private Materia()
		: base() {}

	private Materia(Guid materiaID)
		: base(materiaID) {}

	private Materia(
		Guid materiaID,
		Guid curriculaID,
		string descripcion,
		int horasCatedra)
			: this(materiaID)
	{
		if (Guid.Empty.Equals(curriculaID))
		{
			var msg = "Se debe especificar la currícula de la materia.";
			throw new ArgumentNullException(nameof(curriculaID), msg);
		}

		if (string.IsNullOrWhiteSpace(descripcion))
		{
			var msg = "Se debe especificar el nombre de la materia.";
			throw new ArgumentNullException(nameof(descripcion), msg);
		}

		if (horasCatedra < 1)
		{
			var msg = "La materia debe tener al menos una hora cátedra.";
			throw new ArgumentException(msg, nameof(horasCatedra));
		}

		CurriculaID = curriculaID;
		Descripcion = descripcion;
		HorasCatedra = horasCatedra;

		AgregarEvento(new MateriaRegistradaEvent(Id));
	}

	public Materia(
		Guid curriculaID,
		string descripcion,
		int horasCatedra)
			: this(Guid.NewGuid(), curriculaID, descripcion, horasCatedra) {}
	#endregion

	private void EditarDescripcion(string descripcion)
	{
		if (string.IsNullOrWhiteSpace(descripcion))
		{
			var msg = "Se debe especificar el nombre de la materia.";
			throw new ArgumentNullException(nameof(descripcion), msg);
		}

		Descripcion = descripcion;
	}

	private void EditarCargaHoraria(int horasCatedra)
	{
		// TODO: Comprobar que la carga horaria sea divisor de 5 o 10
		if (horasCatedra <= 0)
		{
			var msg = "La cantidad de horas cátedras debe ser mayor a cero.";
			throw new ArgumentException(msg, nameof(horasCatedra));
		}

		HorasCatedra = horasCatedra;
	}

	public void ModificarMateria(string descripcion, int horasCatedra)
	{
		EditarDescripcion(descripcion);
		EditarCargaHoraria(horasCatedra);
	}

	public void Eliminar() =>
		AgregarEvento(new MateriaEliminadaEvent(Id));
}
