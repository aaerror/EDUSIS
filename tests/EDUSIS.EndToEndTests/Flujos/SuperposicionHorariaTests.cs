using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.Exceptions;
using Domain.Catedras;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// <see cref="IServicioCatedra.AgregarHorarioAsync"/> contra SQL Server real: los dos invariantes
/// que cruzan cátedras —la colisión horaria dentro de una división y el mismo docente en dos aulas
/// a la vez— cuando las cátedras con las que hay que comparar vienen de la base y no de un fake.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque los dos invariantes se resuelven en memoria sobre agregados que trae el
/// repositorio, y de eso un fake no puede decir nada.</strong> Ningún agregado puede ver estas dos
/// reglas: el barrido lo hace <c>Core</c> con <c>CatedrasSegunDivisionAsync</c> y
/// <c>CatedrasSegunDocenteAsync</c>, y después recorre <c>otra.Horarios</c> y consulta
/// <c>catedra.DocenteEnFunciones()</c> con LINQ to Objects. En <c>Core.UnitTests</c> eso pasa
/// siempre, porque <c>CatedraRepositorioFake</c> devuelve agregados construidos a mano cuyas
/// colecciones están completas por definición. Contra EF, en cambio, cada colección está cargada o
/// vacía según lo que la consulta haya pedido, y una colección vacía no hace fallar nada: hace que
/// el invariante **no encuentre colisiones**. Es la ceguera de INFRA-19 y CORE-23 aplicada a una
/// regla de autorización de horarios.
/// </para>
/// <para>
/// <strong>De qué cuelga el segundo invariante: <c>.Include(x => x.SituacionesRevista)</c> en
/// <c>CatedraRepository.Consulta()</c>.</strong> <c>Catedra.DocenteEnFunciones()</c>
/// (<c>Catedra.cs:192</c>) resuelve <c>SituacionEnFuncionesID</c> buscando en <c>_situaciones</c>,
/// o sea en memoria: sin ese <c>Include</c> la colección llega vacía, <c>DocenteEnFunciones()</c>
/// devuelve <c>null</c> aunque la columna tenga valor, y el bloque entero de
/// <see cref="DocenteEnDosAulasException"/> se saltea.
/// </para>
/// <para>
/// Ese <c>Include</c> <strong>ya estaba fijado</strong> por
/// <c>CatedraRepositorioTests.BuscarPorIDAsync_carga_horarios_y_situaciones_revista</c>, y conviene
/// no sobrevender lo que estas pruebas agregan: borrarlo ya daba rojo antes de que existieran. Lo
/// que no existía es una prueba de que los dos invariantes <em>se disparen</em> —que la excepción
/// salga del caso de uso con las cátedras vecinas leídas de la base—, que es lo que <c>TEST-04</c>
/// pedía. Las que había viven en <c>Core.UnitTests</c> contra <c>CatedraRepositorioFake</c>, y ahí
/// las colecciones están completas porque el fake las construye así, no porque una consulta las
/// haya traído.
/// </para>
/// <para>
/// <c>Horarios</c> no tiene el mismo riesgo y conviene no confundirlos: es una colección
/// <c>OwnsMany</c> (<c>CatedrasConfiguration.cs:76</c>), y EF carga los owned types junto con su
/// dueño sin que nadie los pida. El <c>Include</c> de <c>Horarios</c> en <c>Consulta()</c> es
/// redundante, y borrarlo no rompería ninguna de estas pruebas.
/// </para>
/// <para>
/// Cubre dos de los cuatro invariantes que <c>TEST-04</c> declaraba sin cobertura. Los otros dos
/// —unicidad de inscripción por <c>(Alumno, CicloLectivo)</c> y preceptor único— ya estaban
/// cubiertos contra SQL Server en <c>CursanteRepositorioTests</c> y <c>DivisionRepositorioTests</c>,
/// igual que el nombre de materia único por currícula: la premisa de la fila ("las consultas que
/// los resuelven todavía no las implementa <c>Core</c>/<c>Infrastructure</c>") había vencido.
/// </para>
/// </remarks>
public sealed class SuperposicionHorariaTests : BaseE2E
{
	public SuperposicionHorariaTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	#region ESCENARIO

	/// <summary>
	/// Lo que devuelve: la cátedra que ya ocupa el lunes de 08:00 a 08:40 con un docente en
	/// funciones, una vecina de la <em>misma</em> división y sin docente, y una tercera en
	/// <em>otra</em> división con el <em>mismo</em> docente en funciones.
	/// </summary>
	/// <remarks>
	/// El reparto no es decorativo: separa los dos invariantes para que cada prueba falle por una
	/// sola razón. La vecina comparte división y no docente, así que sólo puede chocar por la
	/// división; la ajena comparte docente y no división, así que la guarda de división la deja
	/// pasar y lo único que puede detenerla es <see cref="DocenteEnDosAulasException"/>. Si una
	/// sola cátedra compartiera las dos cosas, la primera guarda taparía a la segunda y la prueba
	/// del docente pasaría sin ejercitar <c>DocenteEnFunciones()</c>.
	/// </remarks>
	private async Task<(Guid Ocupada, Guid Vecina, Guid Ajena, Guid DocenteID)> SembrarEscenarioAsync()
	{
		var curso = await SembrarCursoAsync();
		var divisionA = await SembrarDivisionAsync(curso.Id, "A");
		var divisionB = await SembrarDivisionAsync(curso.Id, "B");
		var docente = await SembrarDocenteAsync();

		Guid materiaID;
		Guid otraMateriaID;
		await using (var contexto = Fixture.CrearContexto())
		{
			var curricula = new CurriculaBuilder()
				.ConCurso(curso.Id)
				.ConFechaInicio(DateTime.Today.AddDays(-30))
				.Build();
			contexto.Add(curricula);
			await contexto.SaveChangesAsync();

			var materia = new MateriaBuilder()
				.ConCurricula(curricula.Id)
				.ConDescripcion("Matemática")
				.ConHorasCatedra(4)
				.Build();

			var otraMateria = new MateriaBuilder()
				.ConCurricula(curricula.Id)
				.ConDescripcion("Historia")
				.ConHorasCatedra(4)
				.Build();

			contexto.AddRange(materia, otraMateria);
			await contexto.SaveChangesAsync();

			materiaID = materia.Id;
			otraMateriaID = otraMateria.Id;
		}

		Guid ocupadaID;
		Guid vecinaID;
		Guid ajenaID;
		Guid situacionDeLaAjenaID;
		await using (var contexto = Fixture.CrearContexto())
		{
			// `ocupada` alcanza con que el docente esté DESIGNADO, sin ponerlo en funciones:
			// `CatedrasSegunDocenteAsync` filtra por `SituacionesRevista.Any(s => s.DocenteID == …)`,
			// que no mira `SituacionEnFuncionesID`. Lo que tiene que estar en funciones es la cátedra
			// que recibe el horario, porque es sobre ella que `Core` llama `DocenteEnFunciones()`.
			var ocupada = new CatedraBuilder().ConMateria(materiaID).ConDivision(divisionA.Id).Build();
			ocupada.AgregarHorario(new HorarioBuilder().Build());
			ocupada.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddDays(-10), null);

			// Misma división que `ocupada`, otra materia (el índice único es (materia, división)) y
			// sin docente: su única colisión posible es la de la división.
			var vecina = new CatedraBuilder().ConMateria(otraMateriaID).ConDivision(divisionA.Id).Build();

			// Otra división, mismo docente: su única colisión posible es la del docente.
			var ajena = new CatedraBuilder().ConMateria(materiaID).ConDivision(divisionB.Id).Build();
			situacionDeLaAjenaID = ajena.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddDays(-10), null);

			contexto.AddRange(ocupada, vecina, ajena);
			await contexto.SaveChangesAsync();

			ocupadaID = ocupada.Id;
			vecinaID = vecina.Id;
			ajenaID = ajena.Id;
		}

		// Poner en funciones va en un SaveChanges APARTE, y no es una preferencia de estilo:
		// `FK_CATEDRA_SITUACION_REVISTA` ata `catedra.situacion_en_funciones_id` a la clave alterna
		// de `situacion_revista`, que a su vez tiene FK a `catedra`. Insertar las dos filas en la
		// misma unidad de trabajo con la columna ya escrita hace que EF corte con "circular
		// dependency ... Catedra [Added] <- SituacionRevista [Added] <- Catedra [Added]" antes de
		// tocar la base. La app real tampoco lo hace junto: designar y poner en funciones son dos
		// casos de uso, y esta siembra reproduce ese orden.
		await using (var contexto = Fixture.CrearContexto())
		{
			var ajena = await contexto.Catedras
				.Include(x => x.SituacionesRevista)
				.SingleAsync(x => x.Id == ajenaID);

			ajena.PonerEnFunciones(situacionDeLaAjenaID);
			await contexto.SaveChangesAsync();
		}

		return (ocupadaID, vecinaID, ajenaID, docente.Id);
	}

	/// <summary>
	/// Un horario del lunes que arranca a las 08:20 y dura 40 minutos: se superpone con el 08:00 a
	/// 08:40 del escenario por los últimos 20 minutos. <c>Horario.SeSuperponeCon</c> compara
	/// día e intervalo abierto, así que 08:40 en punto <em>no</em> colisionaría.
	/// </summary>
	private static AgregarHorarioRequest HorarioSolapado(Guid catedraID) =>
		new(catedraID, Turno.Mañana.ToString(), Dia.Lunes.ToString(), new TimeOnly(8, 20), 40);

	#endregion

	#region COLISIÓN EN LA DIVISIÓN

	[RequiereSqlServerFact]
	public async Task AgregarHorarioAsync_rechaza_el_horario_que_se_superpone_con_otra_catedra_de_la_division()
	{
		var escenario = await SembrarEscenarioAsync();

		await EnUnScope(async proveedor =>
		{
			var servicio = proveedor.GetRequiredService<IServicioCatedra>();

			await Should.ThrowAsync<ColisionHorariaEnDivisionException>(
				() => servicio.AgregarHorarioAsync(HorarioSolapado(escenario.Vecina)));
		});

		// El rechazo tiene que haber dejado la cátedra sin horarios: si la excepción saltara después
		// de `AgregarHorario`, el horario quedaría en el agregado y la prueba de arriba pasaría igual.
		await using var contexto = Fixture.CrearContexto();
		var vecina = await BuscarCatedraAsync(contexto, escenario.Vecina);
		vecina.Horarios.ShouldBeEmpty();
	}

	#endregion

	#region EL MISMO DOCENTE EN DOS AULAS

	[RequiereSqlServerFact]
	public async Task AgregarHorarioAsync_rechaza_el_horario_que_pone_al_docente_en_dos_aulas_a_la_vez()
	{
		var escenario = await SembrarEscenarioAsync();

		await EnUnScope(async proveedor =>
		{
			var servicio = proveedor.GetRequiredService<IServicioCatedra>();

			await Should.ThrowAsync<DocenteEnDosAulasException>(
				() => servicio.AgregarHorarioAsync(HorarioSolapado(escenario.Ajena)));
		});

		await using var contexto = Fixture.CrearContexto();
		var ajena = await BuscarCatedraAsync(contexto, escenario.Ajena);
		ajena.Horarios.ShouldBeEmpty();
	}

	/// <summary>
	/// El control positivo del invariante anterior: el mismo docente, la misma división ajena y el
	/// mismo día, pero en una franja que no se superpone.
	/// </summary>
	/// <remarks>
	/// Sin esta prueba, la de arriba pasaría igual si <c>AgregarHorarioAsync</c> rechazara
	/// <em>cualquier</em> horario de una cátedra con docente en funciones —o si fallara por
	/// cualquier otro motivo que también sea una excepción de ese tipo—. Lo que acredita es que la
	/// regla discrimina por superposición y no por presencia de docente.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task AgregarHorarioAsync_acepta_el_horario_del_mismo_docente_cuando_no_se_superpone()
	{
		var escenario = await SembrarEscenarioAsync();

		await EnUnScope(async proveedor =>
		{
			var servicio = proveedor.GetRequiredService<IServicioCatedra>();

			await servicio.AgregarHorarioAsync(
				new AgregarHorarioRequest(escenario.Ajena, Turno.Mañana.ToString(), Dia.Lunes.ToString(), new TimeOnly(8, 40), 40));
		});

		await using var contexto = Fixture.CrearContexto();
		var ajena = await BuscarCatedraAsync(contexto, escenario.Ajena);
		ajena.Horarios.Count.ShouldBe(1);
		ajena.Horarios.Single().HoraInicio.ShouldBe(new TimeOnly(8, 40));
	}

	#endregion

	#region AYUDA

	/// <summary>
	/// Trae la cátedra con sus horarios para afirmar sobre el estado persistido. Se pide el
	/// <c>Include</c> de <c>SituacionesRevista</c> a propósito aunque estas aserciones no lo usen:
	/// así la consulta de la prueba no depende de lo que haga <c>CatedraRepository.Consulta()</c>,
	/// que es justamente lo que estas pruebas tienen que poder ver romperse.
	/// </summary>
	private static async Task<Catedra> BuscarCatedraAsync(Infrastructure.EdusisDBContext contexto, Guid catedraID)
	{
		var catedra = await contexto.Catedras
			.Include(x => x.SituacionesRevista)
			.FirstOrDefaultAsync(x => x.Id == catedraID);

		catedra.ShouldNotBeNull();
		return catedra;
	}

	#endregion
}
