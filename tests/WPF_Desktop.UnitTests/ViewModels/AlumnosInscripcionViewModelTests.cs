using Core.ServicioAlumnos;
using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Alumnos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre <c>UI-05</c> (<see cref="InscripcionAlumnoViewModel"/>) y la parte testeable de <c>UI-04</c>
/// (<see cref="RegistrarAlumnoViewModel"/>): la inscripcion llega a <see cref="IServicioCursante.InscribirCursanteAsync"/>,
/// el periodo sale de <see cref="CicloLectivoStore"/>, el alumno de <see cref="LegajoStore"/> y la UI no reimplementa la
/// validacion de cupo (la hace <c>Core</c>: la UI solo muestra la excepcion).
/// <c>RegistrarAlumnoViewModel.GuardarCommand</c> conserva <c>MessageBox.Show</c> (decision #2), asi que su flujo de
/// guardado se cubre por guarda sobre el codigo fuente y no por ejecucion.
/// </summary>
public class AlumnosInscripcionViewModelTests
{
	private const string CicloDelStore = "2031";

	private sealed class Contexto
	{
		public IServicioCurso ServicioCursos { get; } = Substitute.For<IServicioCurso>();
		public IServicioCursante ServicioCursantes { get; } = Substitute.For<IServicioCursante>();
		public IServicioDivision ServicioDivisiones { get; } = Substitute.For<IServicioDivision>();
		public LegajoStore LegajoStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();

		public Guid AlumnoID { get; } = Guid.NewGuid();
		public CursoResponse Primero { get; } = new(Guid.NewGuid(), Grado.Primero, NivelEducativo.Secundaria);
		public CursoResponse Segundo { get; } = new(Guid.NewGuid(), Grado.Segundo, NivelEducativo.Secundaria);
		public DivisionResponse DivisionA { get; } = new(Guid.NewGuid(), "A", null, null, 0);
		public DivisionResponse DivisionB { get; } = new(Guid.NewGuid(), "B", null, null, 0);

		public Contexto()
		{
			CicloLectivoStore.CicloLectivo = CicloDelStore;
			LegajoStore.PersonaID = AlumnoID;

			ServicioCursos.ListarCursosAsync()
						  .Returns(Task.FromResult<IReadOnlyCollection<CursoResponse>>(new List<CursoResponse> { Primero, Segundo }));
			ServicioDivisiones.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							  .Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(new List<DivisionResponse> { DivisionA, DivisionB }));
		}

		public InscripcionAlumnoViewModel CrearInscripcion()
		{
			return new InscripcionAlumnoViewModel(ServicioCursos, ServicioCursantes, ServicioDivisiones, LegajoStore, CicloLectivoStore);
		}

		public RegistrarAlumnoViewModel CrearRegistro()
		{
			return new RegistrarAlumnoViewModel(Substitute.For<IServicioAlumno>(), ServicioCursos, ServicioCursantes, ServicioDivisiones, CicloLectivoStore);
		}
	}

	#region UI-05: InscripcionAlumnoViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_periodo_sale_del_CicloLectivoStore()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearInscripcion();

		viewModel.Periodo.ShouldBe(CicloDelStore);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_periodo_acompania_al_store_sin_estado_propio()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();

		contexto.CicloLectivoStore.CicloLectivo = "2040";

		viewModel.Periodo.ShouldBe("2040");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_cursos_se_cargan_desde_IServicioCurso()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearInscripcion();

		viewModel.Cursos.Select(c => c.CursoID).ShouldBe(new[] { contexto.Primero.CursoID, contexto.Segundo.CursoID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Elegir_un_curso_pide_las_divisiones_de_ese_curso_en_el_ciclo_del_store()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();

		viewModel.Curso = viewModel.Cursos[1];

		contexto.ServicioDivisiones.Received(1).ListarDivisionesAsync(Arg.Is<ListarDivisionesRequest>(r =>
			r.CursoID == contexto.Segundo.CursoID &&
			r.CicloLectivo == CicloDelStore));
		viewModel.Divisiones.Select(d => d.DivisionID).ShouldBe(new[] { contexto.DivisionA.DivisionID, contexto.DivisionB.DivisionID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cambiar_de_curso_descarta_la_division_elegida()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		viewModel.Curso = viewModel.Cursos[1];

		viewModel.Division.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Inscribir_solo_se_habilita_con_curso_y_division()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.InscribirCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Curso = viewModel.Cursos[0];
		viewModel.InscribirCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Division = viewModel.Divisiones[0];
		viewModel.InscribirCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_llega_a_InscribirCursanteAsync_con_curso_division_alumno_del_store_periodo_y_recursante()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[1];
		viewModel.EsRecursante = true;

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.Received(1).InscribirCursanteAsync(
			new RegistrarCursanteRequest(contexto.Primero.CursoID, contexto.DivisionB.DivisionID, contexto.AlumnoID, CicloDelStore, true));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_sin_marcar_recursante_envia_EsRecursante_en_false()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.Received(1).InscribirCursanteAsync(Arg.Is<RegistrarCursanteRequest>(r => r.EsRecursante == false));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_inscripcion_exitosa_avisa_y_limpia_la_seleccion()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];
		viewModel.EsRecursante = true;

		await viewModel.InscribirCommand.ExecuteAsync(null);

		viewModel.HabilitarMessage.ShouldBeTrue();
		viewModel.Message.ShouldContain("inscripto");
		viewModel.Curso.ShouldBeNull();
		viewModel.Division.ShouldBeNull();
		viewModel.EsRecursante.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_Core_rechaza_por_cupo_la_UI_solo_muestra_la_excepcion_y_no_la_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioCursantes.InscribirCursanteAsync(Arg.Any<RegistrarCursanteRequest>())
				.Returns(Task.FromException(new InvalidOperationException("La division no tiene cupo disponible.")));
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await Should.NotThrowAsync(async () =>
		{
			await viewModel.InscribirCommand.ExecuteAsync(null);
		});

		viewModel.HabilitarMessage.ShouldBeTrue();
		viewModel.Message.ShouldBe("La division no tiene cupo disponible.");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task La_UI_no_reimplementa_el_cupo_y_solo_consulta_divisiones_para_el_combo()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		var metodosLlamados = contexto.ServicioDivisiones.ReceivedCalls().Select(c => c.GetMethodInfo().Name).Distinct().ToList();
		metodosLlamados.ShouldBe(new[] { nameof(IServicioDivision.ListarDivisionesAsync) });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_sin_curso_o_sin_division_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.DidNotReceive().InscribirCursanteAsync(Arg.Any<RegistrarCursanteRequest>());
	}
	#endregion

	#region UI-04: RegistrarAlumnoViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_toma_el_periodo_inicial_del_CicloLectivoStore()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearRegistro();

		viewModel.Periodo.ShouldBe(CicloDelStore);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_carga_los_cursos_desde_IServicioCurso()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearRegistro();

		viewModel.Cursos.Select(c => c.CursoID).ShouldBe(new[] { contexto.Primero.CursoID, contexto.Segundo.CursoID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_al_elegir_curso_pide_las_divisiones_del_ciclo_del_store()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();

		viewModel.Curso = viewModel.Cursos[0];

		contexto.ServicioDivisiones.Received(1).ListarDivisionesAsync(Arg.Is<ListarDivisionesRequest>(r =>
			r.CursoID == contexto.Primero.CursoID &&
			r.CicloLectivo == CicloDelStore));
		viewModel.Divisiones.Count.ShouldBe(2);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_Guardar_exige_curso_y_division()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();
		viewModel.GuardarCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Curso = viewModel.Cursos[0];
		viewModel.GuardarCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Division = viewModel.Divisiones[0];
		viewModel.GuardarCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_la_inscripcion_llega_a_IServicioCursante_y_no_reimplementa_el_cupo()
	{
		var fuente = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Alumnos/RegistrarAlumnoViewModel.cs");

		fuente.ShouldContain("await _servicioCursantes.InscribirCursanteAsync(");
		fuente.ShouldNotContain("ContarCursantes");
		fuente.ShouldNotContain("ValidarCupo");
		fuente.ShouldNotContain("//TODO: Refactorizar");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void InscripcionAlumno_la_inscripcion_llega_a_IServicioCursante_y_no_reimplementa_el_cupo()
	{
		var fuente = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Alumnos/InscripcionAlumnoViewModel.cs");

		fuente.ShouldContain("await _servicioCursantes.InscribirCursanteAsync(");
		fuente.ShouldContain("_legajoStore.PersonaID");
		fuente.ShouldNotContain("ContarCursantes");
		fuente.ShouldNotContain("ValidarCupo");
	}
	#endregion
}
