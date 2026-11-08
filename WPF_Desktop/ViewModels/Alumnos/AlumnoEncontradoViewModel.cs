using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.DTOs.Personas.Response;
using System;

namespace WPF_Desktop.ViewModels.Alumnos;

/// <summary>
/// Una fila del índice de alumnos. Es el envoltorio de <see cref="PersonaResponse"/>: los
/// <c>Response</c> de <c>Core</c> no se bindean.
/// </summary>
/// <remarks>
/// La pantalla anterior bindeaba el <c>PersonaResponse</c> directo —<c>PersonaResponse.Documento</c>
/// en el XAML— porque mostraba un solo alumno y no una lista. Con el índice hace falta una fila, y
/// la fila es el lugar donde "Apellido, Nombre" se arma una vez en lugar de con dos <c>Run</c> y una
/// coma suelta en cada vista que lo necesite.
/// </remarks>
internal partial class AlumnoEncontradoViewModel : ObservableObject
{
	#region Response
	private readonly PersonaResponse _personaResponse;
	#endregion

	[ObservableProperty]
	private Guid _personaID = Guid.Empty;

	[ObservableProperty]
	private string _apellido = string.Empty;

	[ObservableProperty]
	private string _nombre = string.Empty;

	[ObservableProperty]
	private string _documento = string.Empty;

	[ObservableProperty]
	private string _sexo = string.Empty;

	[ObservableProperty]
	private string _fechaNacimiento = string.Empty;

	[ObservableProperty]
	private string _nacionalidad = string.Empty;

	/// <summary>Como lo escribe el dominio: apellido primero, que es el orden en que se lista.</summary>
	public string NombreCompleto => $"{ Apellido }, { Nombre }";


	public AlumnoEncontradoViewModel(PersonaResponse personaResponse)
	{
		if (personaResponse is not null)
		{
			_personaResponse = personaResponse;

			PersonaID = _personaResponse.PersonaID;
			Apellido = _personaResponse.Apellido;
			Nombre = _personaResponse.Nombre;
			Documento = _personaResponse.Documento;
			Sexo = _personaResponse.Sexo;
			FechaNacimiento = _personaResponse.FechaNacimiento;
			Nacionalidad = _personaResponse.Nacionalidad;
		}
	}

	/// <summary>El <c>Response</c> original, para quien necesite pasárselo de vuelta a <c>Core</c>.</summary>
	public PersonaResponse Response => _personaResponse;
}
