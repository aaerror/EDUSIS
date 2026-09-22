using Domain.Cursos;
using Domain.Shared;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Curso"/>. Estado por defecto válido: <c>Primero</c> de
/// <c>Secundaria</c>. Las divisiones ya no son responsabilidad de <c>Curso</c> (agregado propio
/// <c>Domain.Divisiones.Division</c>): este builder no las crea; usar el builder de división
/// correspondiente y asociarlo por <c>CursoID</c>.
/// </summary>
public sealed class CursoBuilder
{
	#region ESTADO POR DEFECTO
	private string _grado = "Primero";
	private string _nivelEducativo = "Secundaria";
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
	#endregion

	#region CONSTRUCCIÓN
	public Curso Build() =>
		new(_grado, _nivelEducativo);
	#endregion
}
