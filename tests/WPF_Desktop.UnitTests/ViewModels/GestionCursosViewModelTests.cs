using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre el ciclo de habilitación del comando de borrado de <see cref="GestionCursosViewModel"/> y el camino
/// del diálogo de impacto, que hasta el 2026-11-14 no tenía ninguna prueba.
/// </summary>
/// <remarks>
/// La pantalla se recorrió a mano como parte de TEST-16 y el botón <c>Eliminar</c> estaba gris con un curso
/// seleccionado, con divisiones y sin ellas. La causa era que las dos propiedades notificaban con
/// <c>[NotifyPropertyChangedFor]</c> en vez de <c>[NotifyCanExecuteChangedFor]</c>: eso levanta
/// <c>PropertyChanged</c> para la propiedad del comando, el binding relee la propiedad, obtiene la misma
/// instancia de <c>ICommand</c>, la DP no cambia y <c>ButtonBase</c> no vuelve a evaluar <c>CanExecute</c>.
/// Los comandos del toolkit tampoco se cuelgan de <c>CommandManager.RequerySuggested</c>, así que
/// <c>CanExecuteChanged</c> no se levantaba nunca y el botón quedaba gris para siempre: eliminar un curso era
/// inalcanzable y el bloque del diálogo de impacto era código muerto.
///
/// De ahí la forma de estas pruebas. <strong>No alcanza con afirmar que <c>CanExecute</c> da <c>true</c> con un
/// curso puesto</strong> —eso ya pasaba con el atributo equivocado, porque el defecto no estaba en el predicado
/// sino en que nadie lo volvía a consultar—: hay que afirmar que <strong>asignar la propiedad levanta
/// <c>CanExecuteChanged</c></strong>, que es la señal que la vista escucha. Es la única aserción que distingue
/// el código arreglado del roto, y la que ninguna de las 1604 pruebas anteriores hacía.
/// </remarks>
public class GestionCursosViewModelTests
{
	private sealed class Contexto
	{
		public IServicioCurso ServicioCursos { get; } = Substitute.For<IServicioCurso>();
		public NavegacionFalsa NavegarARegistrarCurso { get; } = new();
		public NavegacionFalsa NavegarADivisiones { get; } = new();
		public NavegacionFalsa NavegarARegistrarMateria { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public GestionCursosViewModel ViewModel { get; }

		public Contexto()
		{
			DevolverImpacto();

			ViewModel = new GestionCursosViewModel(ServicioCursos,
												   Dialogos,
												   NavegarARegistrarCurso,
												   NavegarADivisiones,
												   NavegarARegistrarMateria,
												   CursoStore);
		}

		/// <summary>
		/// Impacto por defecto: curso sin divisiones. Hace falta en el constructor y no sólo en las pruebas
		/// que lo miran, porque el ViewModel consulta el impacto <strong>antes</strong> de abrir el diálogo:
		/// sin stub, NSubstitute devuelve <c>null</c> y el armado del texto revienta.
		/// </summary>
		public void DevolverImpacto(int divisiones = 0)
		{
			ServicioCursos.ConsultarImpactoEliminacionCursoAsync(Arg.Any<EliminarCursoRequest>())
						  .Returns(Task.FromResult(CrearImpacto(divisiones)));
		}
	}

	/// <remarks>
	/// <c>TieneDivisiones</c> no es parámetro: el record lo deriva de <c>Divisiones &gt; 0</c>.
	/// </remarks>
	private static ImpactoEliminacionCursoResponse CrearImpacto(int divisiones)
	{
		return new ImpactoEliminacionCursoResponse(Curriculas: 0, Materias: 0, Catedras: 0, Divisiones: divisiones);
	}

	private static CursoViewModel CrearCurso()
	{
		return new CursoViewModel(new CursoResponse(Guid.NewGuid(), Grado.Primero, NivelEducativo.Secundaria));
	}

	/// <summary>
	/// El predicado de habilitación es privado, así que se lo consulta por donde lo consulta la vista:
	/// <c>ICommand.CanExecute</c> con el mismo <c>CommandParameter</c> que pasa el XAML.
	/// </summary>
	private static bool PuedeEliminar(GestionCursosViewModel viewModel)
	{
		return viewModel.EliminarCommandAsync.CanExecute("Curso");
	}

	#region Habilitación del comando de borrado
	/// <remarks>
	/// <strong>Esta es la prueba que fija el defecto de TEST-16.</strong> La aserción que importa es la de
	/// <c>avisos</c>: con <c>[NotifyPropertyChangedFor]</c> el contador quedaba en 0 y el botón nunca se
	/// enteraba de que ya había un curso elegido. Las dos aserciones de <c>CanExecute</c> pasaban igual
	/// antes y después del arreglo, así que por sí solas no habrían detectado nada.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Elegir_un_curso_avisa_que_el_borrado_cambio_de_estado()
	{
		var contexto = new Contexto();
		var avisos = 0;
		contexto.ViewModel.EliminarCommandAsync.CanExecuteChanged += (_, _) => avisos++;

		PuedeEliminar(contexto.ViewModel).ShouldBeFalse();

		contexto.ViewModel.Curso = CrearCurso();

		avisos.ShouldBeGreaterThan(0, "sin CanExecuteChanged la vista no vuelve a consultar CanExecute y el botón queda gris para siempre");
		PuedeEliminar(contexto.ViewModel).ShouldBeTrue();
	}

	/// <summary>
	/// La contracara: deseleccionar vuelve a apagar el botón, y también avisa.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Soltar_el_curso_vuelve_a_apagar_el_borrado_y_tambien_avisa()
	{
		var contexto = new Contexto();
		contexto.ViewModel.Curso = CrearCurso();

		var avisos = 0;
		contexto.ViewModel.EliminarCommandAsync.CanExecuteChanged += (_, _) => avisos++;

		contexto.ViewModel.Curso = null!;

		avisos.ShouldBeGreaterThan(0);
		PuedeEliminar(contexto.ViewModel).ShouldBeFalse();
	}
	#endregion

	#region Diálogo de impacto
	/// <summary>
	/// Con divisiones el borrado está condenado a fallar, así que la pantalla advierte y no pregunta: no
	/// tiene que llegar a <c>EliminarCurso</c> ni abrir una confirmación destructiva.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_curso_con_divisiones_advierte_y_no_pregunta_ni_elimina()
	{
		var contexto = new Contexto();
		contexto.DevolverImpacto(divisiones: 3);
		contexto.ViewModel.Curso = CrearCurso();

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Curso");

		contexto.Dialogos.Advertencias.ShouldNotBeEmpty();
		contexto.Dialogos.Confirmaciones.ShouldBeEmpty();
		await contexto.ServicioCursos.DidNotReceive().EliminarCurso(Arg.Any<EliminarCursoRequest>());
	}

	/// <summary>
	/// Sin divisiones sí pregunta, y recién con el sí del usuario elimina. Es el camino que el atributo
	/// equivocado dejaba inalcanzable.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_curso_sin_divisiones_pregunta_y_elimina_con_la_confirmacion()
	{
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = true;
		contexto.ViewModel.Curso = CrearCurso();

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Curso");

		contexto.Dialogos.Confirmaciones.ShouldNotBeEmpty();
		await contexto.ServicioCursos.Received(1).EliminarCurso(Arg.Any<EliminarCursoRequest>());
	}

	/// <summary>
	/// Y si el usuario dice que no, no se elimina nada.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Decir_que_no_en_la_confirmacion_no_elimina()
	{
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.ViewModel.Curso = CrearCurso();

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Curso");

		await contexto.ServicioCursos.DidNotReceive().EliminarCurso(Arg.Any<EliminarCursoRequest>());
	}
	#endregion
}
