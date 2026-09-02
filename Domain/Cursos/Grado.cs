using System.ComponentModel;

namespace Domain.Cursos;

public enum Grado
{
	[Description("1° Año")]
	Primero = 1,
	[Description("2° Año")]
	Segundo,
	[Description("3° Año")]
	Tercero,
	[Description("4° Año")]
	Cuarto,
	[Description("5° Año")]
	Quinto,
	[Description("6° Año")]
	Sexto,
	[Description("7° Año")]
	Septimo,
}