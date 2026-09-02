using Domain.Cursos.Exceptions;
using Domain.Shared;

namespace Domain.Cursos;

public sealed class Curso : Entity
{
	private List<Division> _divisiones = new();

	// Educación Primaria - Educación Secundaria
	public NivelEducativo NivelEducativo { get; }
	public Grado Grado { get; private set; }
	public int CantidadDivisiones => _divisiones.Count;
	public int CantidadAlumnos => _divisiones.Sum(x => x.TotalAlumnos);

	public IReadOnlyCollection<Division> Divisiones => _divisiones.ToList();


	#region CONSTRUCTOR
	private Curso()
		: base() { }

	private Curso(Guid unCurso)
		: base(unCurso) { }

	protected Curso(Guid unCurso, string unGrado, string unNivelEducativo)
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

	/*
	 * #region Calificacion
	public void AgregarCalificacion(Guid unaDivision, Guid unCursante, Guid unaMateria, bool asistencia, DateTime fecha, Instancia instancia, double? nota)
	{
		var division = BuscarDivision(unaDivision);
		if (division is null)
		{
			throw new ArgumentException("No se encontró la división en este curso.", nameof(unaDivision));
		}

		*//*if (!ExisteMateria(unaMateria))
		{
			throw new ArgumentException("No se encontró la materia en este curso.", nameof(unaDivision));
		}*//*

		division.AgregarCalificacion(unCursante, unaMateria, asistencia, fecha, instancia, nota);
	}
	#endregion
	*/

	#region Preceptor
	public void AsignarPreceptor(Guid unaDivision, Guid unPreceptor)
	{
		var division = BuscarDivision(unaDivision);
		division.AsignarPreceptor(unPreceptor);
	}

	public void QuitarPreceptor(Guid unaDivision)
	{
		var division = BuscarDivision(unaDivision);
		division.QuitarPreceptor();
	}
	#endregion

	#region Division
	private bool ExisteDivision(Guid unaDivision)
	{
		if (Guid.Empty.Equals(unaDivision))
		{
			throw new ArgumentNullException("Se deben especificar los datos de la división.");
		}

		return _divisiones.Any(x => x.Id.Equals(unaDivision));
	}

	private Division? BuscarDivision(Guid unaDivision)
	{
		if (!ExisteDivision(unaDivision))
		{
			throw new DivisionNoEncontradaException();
		}

		return _divisiones.Find(x => x.Id.Equals(unaDivision));
	}

	public void AgregarDivision()
	{
		string siguiente = string.Empty;
		var division = _divisiones.Select(x => x.Descripcion)
								  .OrderDescending()
								  .FirstOrDefault();
		if (division is null)
		{
			siguiente = "A";
		}
		else
		{
			siguiente = char.ConvertFromUtf32(char.Parse(division.Trim()) + 1);
		}

		_divisiones.Add(new Division(siguiente));
	}

	public void QuitarDivision(Guid aEliminar)
	{
		var division = BuscarDivision(aEliminar);
		_divisiones.Remove(division);
	}
	#endregion

	#region Cursantes
	public bool CursanteRegistrado(Guid unCursante) =>
		_divisiones.Any(x => x.ExisteCursante(unCursante));

	public void AgregarAlumnoEnDivision(Guid unaDivision, Guid unCursante)
	{
		if (CursanteRegistrado(unCursante))
		{
			throw new CursanteRegistradoException();
		}

		var division = BuscarDivision(unaDivision);
		if (division is null)
		{
			throw new DivisionNoEncontradaException();
			//ArgumentException("La división del curso a la que desea agregar este alumno no existe.", nameof(unaDivision));
		}

		division.AgregarCursante(unCursante);
	}

	public void QuitarAlumno(Guid unaDivision, Guid unCursante)
	{
		var division = BuscarDivision(unaDivision);
		if (ExisteDivision(unaDivision))
		{
			throw new DivisionNoEncontradaException();
		}

		division.QuitarCursante(unCursante);
	}
	#endregion
}
