using Domain.Cursos;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Curso"/>. Estado por defecto válido: <c>Primero</c> de
/// <c>Secundaria</c>, sin divisiones. <see cref="ConDivision"/> agrega divisiones que
/// <see cref="Build"/> crea vía <c>Curso.AgregarDivision()</c> (la descripción la asigna el
/// dominio: A, B, C…).
/// </summary>
public sealed class CursoBuilder
{
	#region ESTADO POR DEFECTO
	private string _grado = "Primero";
	private string _nivelEducativo = "Secundaria";
	private int _cantidadDivisiones = 0;
	#endregion

	#region CONFIGURACIÓN
	public CursoBuilder ConGrado(string grado)
	{
		_grado = grado;
		return this;
	}

	public CursoBuilder ConNivelEducativo(string nivelEducativo)
	{
		_nivelEducativo = nivelEducativo;
		return this;
	}

	public CursoBuilder ConDivision(int cantidad = 1)
	{
		_cantidadDivisiones += cantidad;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Curso Build()
	{
		var curso = new Curso(_grado, _nivelEducativo);

		for (var i = 0; i < _cantidadDivisiones; i++)
		{
			curso.AgregarDivision();
		}

		return curso;
	}
	#endregion
}
