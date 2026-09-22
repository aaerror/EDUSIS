using Domain.Shared;

namespace Domain.Cursos;

public sealed class Curso : Entity
{
	// Educación Primaria - Educación Secundaria
	public NivelEducativo NivelEducativo { get; }
	public Grado Grado { get; private set; }


	#region CONSTRUCTOR
	private Curso()
		: base() { }

	private Curso(Guid unCurso)
		: base(unCurso) { }

	private Curso(Guid unCurso, string unGrado, string unNivelEducativo)
		: this(unCurso)
	{
		/*if (!Regex.IsMatch(grado.Trim(), @"^(\d){1}$", RegexOptions.None))
		{
			throw new ArgumentException("El año/grado debe ser un número.", nameof(grado));
		}*/

		/*if (int.Parse(grado) > 7)
		{
			throw new ArgumentException("El curso máximo en educación es septimo ya sea en educación primaria o secundaria.", nameof(grado));
		}*/


		var result = Enum.TryParse<Grado>(unGrado, out Grado grado);
		if (!result)
		{
			throw new ArgumentException("El grado que se especifico no existe.", nameof(unGrado));
		}

		result = Enum.TryParse<NivelEducativo>(unNivelEducativo, out NivelEducativo nivelEducativo);
		if (!result)
		{
			throw new ArgumentException("El grado que se especifico no existe.", nameof(unGrado));
		}

		Grado = grado;
		NivelEducativo = nivelEducativo;
	}

	public Curso(string grado, string nivelEducativo)
		: this(Guid.NewGuid(), grado, nivelEducativo) { }
	#endregion
}