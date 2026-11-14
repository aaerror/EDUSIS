using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursos.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using System;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class CursoViewModel : ObservableObject
{
	#region Response
	private readonly CursoResponse _cursoResponse;
	#endregion

	[ObservableProperty]
	private Guid _cursoID = Guid.Empty;

	[ObservableProperty]
	private string _grado;

	[ObservableProperty]
	private string _nivelEducativo;

	//TODO: Recuperar contadores de divisiones (Divisiones) y alumnos (Alumnos) como feature (decisión #5, ID a registrarse en docs/todos.md)

	public CursoViewModel(CursoResponse cursoResponses)
	{
		if (cursoResponses is not null)
		{
			_cursoResponse = cursoResponses;

			CursoID = _cursoResponse.CursoID;
			Grado = _cursoResponse.Grado.ToString();
			NivelEducativo = _cursoResponse.NivelEducativo.ToString();
		}
	}

	/// <summary>
	/// Grado y nivel como <strong>valores del dominio</strong> y no como el texto que se muestra.
	/// </summary>
	/// <remarks>
	/// Las dos propiedades de arriba son cadenas, y se quedan así: diez vistas, tres stores y el
	/// converter <c>grado</c> las consumen de esa forma desde antes. Estas dos existen para lo que
	/// necesita un valor y no una etiqueta — hoy <c>Domain.Cursos.EdadEscolar</c>, que compara la
	/// edad del alumno contra el grado al que se lo inscribe. Salen del mismo <c>CursoResponse</c>
	/// que esta clase ya guarda, así que <strong>no hay un <c>Enum.Parse</c> sobre el texto de
	/// pantalla</strong>, que es lo único que quedaría sin ellas.
	/// Anulables porque el constructor acepta un response nulo y en ese caso no asigna nada: en
	/// pantalla eso es un hueco y no una excepción, el mismo criterio que <c>Grados.Decir</c>.
	/// </remarks>
	public Grado? ValorGrado => _cursoResponse?.Grado;

	/// <inheritdoc cref="ValorGrado"/>
	public NivelEducativo? ValorNivelEducativo => _cursoResponse?.NivelEducativo;
}