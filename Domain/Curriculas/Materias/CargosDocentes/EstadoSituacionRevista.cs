using Domain.Shared;

namespace Domain.Curriculas.Materias.CargosDocentes;

public class EstadoSituacionRevista : Enumeration
{
	public static readonly EstadoSituacionRevista EMPTY = new EstadoSituacionRevista(0, "EMPTY");
	public static readonly EstadoSituacionRevista Aceptado = new EstadoSituacionRevista(1, "Aceptado");
	//public static readonly EstadoSituacionRevista Cancelado = new EstadoSituacionRevista(2, "Cancelado");
	//public static readonly EstadoSituacionRevista Pendiente = new EstadoSituacionRevista(3, "Pendiente");
	public static readonly EstadoSituacionRevista Finalizado = new EstadoSituacionRevista(2, "Finalizado");


	private EstadoSituacionRevista()
		: base() { }

	private EstadoSituacionRevista(int id, string descripcion)
		: base(id, descripcion) { }
}