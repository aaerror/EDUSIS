using System.ComponentModel;

namespace Domain.Docentes.Puestos;

public enum EstadoPuesto
{
	[Description("Activo")]
	Activo = 1,
	[Description("En suspenso")]
	EnSupenso,
	[Description("Inactivo")]
	Inactivo,
	[Description("Pendiente")]
	Pendiente
}