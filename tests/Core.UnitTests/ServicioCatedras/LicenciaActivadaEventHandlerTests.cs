using Core.ServicioCatedras.Events;
using Domain.Catedras.DomainEvents;
using Domain.Catedras.SituacionesRevista;
using Domain.Licencias.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCatedras;

/// <summary>
/// <see cref="LicenciaActivadaEventHandler"/> contra el fake: qué cátedras releva. Que el relevo
/// llegue a la base en el mismo guardado de la aprobación lo cubre
/// <c>EDUSIS.EndToEndTests.Flujos.LicenciaDeDocenteTests</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class LicenciaActivadaEventHandlerTests
{
	private readonly UnitOfWorkFake _unidadDeTrabajo = new();
	private readonly LicenciaActivadaEventHandler _handler;
	private readonly Guid _docente = Guid.NewGuid();


	public LicenciaActivadaEventHandlerTests()
	{
		_handler = new LicenciaActivadaEventHandler(_unidadDeTrabajo);
	}

	private Domain.Catedras.Catedra SembrarCatedraConDocenteEnFunciones(Guid docente)
	{
		var catedra = new CatedraBuilder().Build();
		var situacion = catedra.Designar(docente, Cargo.Titular, DateTime.Today.AddDays(-10), null);
		catedra.PonerEnFunciones(situacion);
		catedra.LiberarEventos();
		_unidadDeTrabajo.CatedrasFake.Sembrar(catedra);
		return catedra;
	}

	private Task Activar() =>
		_handler.Handle(new LicenciaActivadaEvent(Guid.NewGuid(), _docente), CancellationToken.None);

	[Fact]
	public async Task Releva_al_docente_de_cada_catedra_donde_esta_en_funciones_y_conserva_la_designacion()
	{
		var una = SembrarCatedraConDocenteEnFunciones(_docente);
		var otra = SembrarCatedraConDocenteEnFunciones(_docente);

		await Activar();

		una.SituacionEnFuncionesID.ShouldBeNull();
		otra.SituacionEnFuncionesID.ShouldBeNull();
		una.SituacionesRevista.ShouldHaveSingleItem().EsCargoVigente().ShouldBeTrue();
		una.Eventos.ShouldHaveSingleItem().ShouldBeOfType<CatedraSinDocenteEnFuncionesEvent>();
	}

	/// <remarks>
	/// El repositorio trae toda cátedra donde el docente tiene alguna designación; relevar sin
	/// filtrar sacaría de funciones al titular de una cátedra donde el docente es sólo suplente.
	/// </remarks>
	[Fact]
	public async Task No_toca_una_catedra_donde_el_docente_esta_designado_pero_en_funciones_esta_otro()
	{
		var titular = Guid.NewGuid();
		var catedra = SembrarCatedraConDocenteEnFunciones(titular);
		catedra.Designar(_docente, Cargo.Suplente, DateTime.Today.AddDays(-5), DateTime.Today.AddMonths(1), catedra.SituacionEnFuncionesID);
		catedra.LiberarEventos();

		await Activar();

		catedra.DocenteEnFunciones().ShouldBe(titular);
		catedra.Eventos.ShouldBeEmpty();
	}

	[Fact]
	public async Task Sin_catedras_en_funciones_no_hace_nada_ni_falla()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(_docente, Cargo.Titular, DateTime.Today.AddDays(-10), null);
		_unidadDeTrabajo.CatedrasFake.Sembrar(catedra);

		await Should.NotThrowAsync(Activar);

		catedra.SituacionEnFuncionesID.ShouldBeNull();
	}

	[Fact]
	public async Task No_guarda_por_su_cuenta()
	{
		SembrarCatedraConDocenteEnFunciones(_docente);

		await Activar();

		_unidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}
}
