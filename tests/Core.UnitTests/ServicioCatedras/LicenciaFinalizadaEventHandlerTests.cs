using Core.ServicioCatedras.Events;
using Domain.Catedras.SituacionesRevista;
using Domain.Licencias.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCatedras;

/// <summary>
/// <see cref="LicenciaFinalizadaEventHandler"/> contra el fake: en qué cátedras repone al docente.
/// La regla de cada cátedra la fija <c>CatedraTests</c> (<c>ReponerTrasLicencia</c>); acá, que el
/// handler la aplique a todas las del docente y a ninguna ajena.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class LicenciaFinalizadaEventHandlerTests
{
	private readonly UnitOfWorkFake _unidadDeTrabajo = new();
	private readonly LicenciaFinalizadaEventHandler _handler;
	private readonly Guid _docente = Guid.NewGuid();


	public LicenciaFinalizadaEventHandlerTests()
	{
		_handler = new LicenciaFinalizadaEventHandler(_unidadDeTrabajo);
	}

	private Task Finalizar() =>
		_handler.Handle(new LicenciaFinalizadaEvent(Guid.NewGuid(), _docente), CancellationToken.None);

	[Fact]
	public async Task Repone_al_docente_en_la_catedra_vacia_y_en_la_que_cubre_su_suplente()
	{
		var vacia = new CatedraBuilder().Build();
		var enVacia = vacia.Designar(_docente, Cargo.Titular, DateTime.Today.AddDays(-30), null);
		_unidadDeTrabajo.CatedrasFake.Sembrar(vacia);

		var cubierta = new CatedraBuilder().Build();
		var enCubierta = cubierta.Designar(_docente, Cargo.Titular, DateTime.Today.AddDays(-30), null);
		var suplente = cubierta.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(20), enCubierta);
		cubierta.PonerEnFunciones(suplente);
		_unidadDeTrabajo.CatedrasFake.Sembrar(cubierta);

		await Finalizar();

		vacia.SituacionEnFuncionesID.ShouldBe(enVacia);
		cubierta.SituacionEnFuncionesID.ShouldBe(enCubierta);
	}

	[Fact]
	public async Task No_toca_las_catedras_donde_el_docente_no_esta_designado()
	{
		var ajena = new CatedraBuilder().Build();
		var otro = ajena.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddDays(-30), null);
		ajena.PonerEnFunciones(otro);
		_unidadDeTrabajo.CatedrasFake.Sembrar(ajena);

		await Finalizar();

		ajena.SituacionEnFuncionesID.ShouldBe(otro);
	}

	/// <remarks>
	/// Ida y vuelta completa: la aprobación lo releva y la finalización lo devuelve al mismo lugar.
	/// </remarks>
	[Fact]
	public async Task Deshace_el_relevo_de_LicenciaActivadaEventHandler()
	{
		var catedra = new CatedraBuilder().Build();
		var situacion = catedra.Designar(_docente, Cargo.Titular, DateTime.Today.AddDays(-30), null);
		catedra.PonerEnFunciones(situacion);
		_unidadDeTrabajo.CatedrasFake.Sembrar(catedra);

		await new LicenciaActivadaEventHandler(_unidadDeTrabajo)
			.Handle(new LicenciaActivadaEvent(Guid.NewGuid(), _docente), CancellationToken.None);
		catedra.SituacionEnFuncionesID.ShouldBeNull();

		await Finalizar();

		catedra.SituacionEnFuncionesID.ShouldBe(situacion);
	}
}
