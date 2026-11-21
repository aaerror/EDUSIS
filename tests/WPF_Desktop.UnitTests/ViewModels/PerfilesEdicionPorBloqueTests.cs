using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos;
using Core.ServicioDocentes;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Alumnos;
using WPF_Desktop.ViewModels.Docentes;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Los perfiles de docente y de alumno abren un bloque de edición por vez.
/// </summary>
/// <remarks>
/// Cancelar recarga el perfil entero desde Core, así que con dos bloques abiertos cancelar uno
/// descartaba lo tipeado en el otro sin avisar. Las dos pantallas son la misma sobre otra
/// entidad: cada prueba corre contra las dos.
/// </remarks>
[Trait("Categoria", "Unidad")]
public class PerfilesEdicionPorBloqueTests
{
	private static readonly string[] BLOQUES = { "Sexo", "Contacto", "Domicilio" };

	private static LegajoStore Legajo() => new() { PersonaID = Guid.NewGuid(), Documento = "30111222" };

	private static (IRelayCommand Editar, IAsyncRelayCommand Cancelar) Perfil(string pantalla)
	{
		if (pantalla == "Docente")
		{
			var docente = new PerfilDocenteViewModel(Substitute.For<IServicioDocente>(), new DialogServiceFalso(),
													 new NavegacionFalsa<GestionDocentesViewModel>(), Legajo());
			return (docente.EditarCommand, docente.CancelarCommand);
		}

		var alumno = new PerfilAlumnoViewModel(Substitute.For<IServicioAlumno>(), new DialogServiceFalso(),
											   new NavegacionFalsa<GestionAlumnosViewModel>(), Legajo());
		return (alumno.EditarCommand, alumno.CancelarCommand);
	}

	public static TheoryData<string, string> PantallasYBloques()
	{
		var datos = new TheoryData<string, string>();
		foreach (var pantalla in new[] { "Docente", "Alumno" })
		{
			foreach (var bloque in BLOQUES)
			{
				datos.Add(pantalla, bloque);
			}
		}
		return datos;
	}

	[Theory]
	[MemberData(nameof(PantallasYBloques))]
	public void Con_un_bloque_abierto_no_se_puede_abrir_ningun_otro(string pantalla, string abierto)
	{
		var (editar, _) = Perfil(pantalla);

		editar.Execute(abierto);

		foreach (var bloque in BLOQUES)
		{
			editar.CanExecute(bloque).ShouldBeFalse($"{ pantalla }: con { abierto } abierto, { bloque } no debería poder abrirse");
		}
	}

	[Theory]
	[MemberData(nameof(PantallasYBloques))]
	public async Task Cancelar_el_bloque_abierto_vuelve_a_habilitar_los_tres(string pantalla, string abierto)
	{
		var (editar, cancelar) = Perfil(pantalla);
		editar.Execute(abierto);

		await cancelar.ExecuteAsync(abierto);

		foreach (var bloque in BLOQUES)
		{
			editar.CanExecute(bloque).ShouldBeTrue($"{ pantalla }: tras cancelar { abierto }, { bloque } debería poder abrirse");
		}
	}

	[Theory]
	[InlineData("Docente")]
	[InlineData("Alumno")]
	public void Un_bloque_inexistente_no_se_ofrece(string pantalla)
	{
		var (editar, _) = Perfil(pantalla);

		editar.CanExecute("Usuario").ShouldBeFalse();
	}
}
