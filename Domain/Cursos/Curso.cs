using Domain.Cursos.Exceptions;
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

	#region Validaciones
	/// <summary>
	/// Un curso se elimina sólo cuando no le queda ninguna división. Con divisiones, hay que
	/// eliminarlas primero —una por una, cada una informando qué matrícula destruye— y recién
	/// entonces el curso.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="cantidadDivisiones"/> lo cuenta el servicio con
	/// <c>IDivisionRepository.DivisionesDelCursoAsync</c> y lo pasa ya resuelto, igual que
	/// <c>Division.ValidarCupoDisponible</c> recibe su conteo: es política sobre datos de otro
	/// agregado, no una invariante que el curso pueda sostener solo.
	/// </para>
	/// <para>
	/// <strong>La guarda no es un trámite: es lo que vuelve posible la cascada curricular.</strong>
	/// Al eliminar el curso se van sus currículas y las materias de cada una, y
	/// <c>FK_CALIFICACION_MATERIA</c> está en <c>Restrict</c>: una materia con calificaciones no se
	/// puede borrar. Las calificaciones cuelgan del cursante por <c>FK_CALIFICACION_CURSANTE</c>, que
	/// sí es <c>Cascade</c>, y los cursantes cuelgan de la división. Exigir que no queden divisiones
	/// garantiza que no queden cursantes, y por lo tanto que no queden calificaciones trabando el
	/// borrado de las materias. Al revés no funciona.
	/// </para>
	/// <para>
	/// Contrapartida asumida: para eliminar un curso con años de historia hay que eliminar sus
	/// divisiones, y cada una se lleva las calificaciones de su matrícula. La guarda obliga a
	/// confirmar división por división en lugar de que una sola operación lo borre todo — es un tope
	/// de velocidad deliberado, no una protección del historial.
	/// </para>
	/// </remarks>
	public void ValidarSePuedeEliminar(int cantidadDivisiones)
	{
		if (cantidadDivisiones > 0)
		{
			throw new CursoConDivisionesException();
		}
	}
	#endregion
}