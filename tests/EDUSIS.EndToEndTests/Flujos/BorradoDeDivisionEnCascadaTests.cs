using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Domain.Catedras.Horarios;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// <see cref="IServicioDivision.EliminarDivisionAsync"/> contra SQL Server real: la cascada de
/// cuatro pasos que vacía cátedras, planillas e inscripciones antes de borrar la división.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque el único artefacto que ejercitaba esa cascada era un fake sin claves
/// ajenas.</strong> El caso de uso borra en la capa de aplicación y en un orden que no es
/// decorativo —cátedras, planillas, inscripciones, división—, dentro de una transacción explícita
/// con rollback. Nada de eso lo puede acreditar <c>UnitOfWorkFake</c>: en LINQ to Objects quitar de
/// una lista siempre "funciona", no hay <c>FK_CURSANTE_DIVISION</c> que rechace un <c>DELETE</c>
/// prematuro ni <c>Cascade</c> que arrastre un hijo, así que las pruebas de <c>Core</c> pasan
/// igual con el orden correcto y con cualquier otro. Es la ceguera de CORE-23 e INFRA-19 sobre una
/// operación de cuatro pasos.
/// </para>
/// <para>
/// Reemplaza lo que pedía <c>TEST-14</c>, cuya premisa venció el 2026-11-06: la fila reclamaba una
/// prueba que afirmara que el <c>DELETE</c> de una división con inscripciones <em>falla</em> en la
/// base, para justificar la guarda <c>DivisionConInscripcionesException</c>. Esa guarda se borró
/// ese día junto con la excepción, cuando la política se dio vuelta y eliminar una división pasó a
/// arrastrar la matrícula entera (ver <c>ServicioDivisionTests</c>, región
/// <c>EliminarDivisionAsync</c>). Lo que quedó sin verificar no es el rechazo sino la cascada que
/// lo reemplazó.
/// </para>
/// </remarks>
public sealed class BorradoDeDivisionEnCascadaTests : BaseE2E
{
	public BorradoDeDivisionEnCascadaTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// Cuenta las filas de una tabla por SQL directo y no por el modelo de EF.
	/// </summary>
	/// <remarks>
	/// Es deliberado: lo que se afirma es el estado de la base, no lo que el <c>ChangeTracker</c>
	/// cree. Además <c>horario</c> es una colección <c>OwnsMany</c> y <c>calificacion</c> cuelga de
	/// <c>cursante</c>, así que por el modelo habría que consultarlas a través de su dueño —
	/// justamente el que ya no existe cuando la cascada terminó.
	/// </remarks>
	private async Task<int> ContarFilasAsync(string tabla)
	{
		await using var contexto = Fixture.CrearContexto();
		return await contexto.Database
			.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM { tabla }")
			.SingleAsync();
	}

	[RequiereSqlServerFact]
	public async Task EliminarDivisionAsync_arrastra_catedras_planillas_inscripciones_y_calificaciones()
	{
		var curso = await SembrarCursoAsync();
		var division = await SembrarDivisionAsync(curso.Id);
		var otraDivision = await SembrarDivisionAsync(curso.Id, "B");
		var preceptor = await SembrarDocenteAsync();

		Guid materiaID;
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
			contexto.Add(materia);
			await contexto.SaveChangesAsync();
			materiaID = materia.Id;
		}

		// Una cátedra con horarios en la división que se borra, y otra en la división hermana que
		// tiene que sobrevivir: el paso 1 filtra por división, y sin este contraste una cascada que
		// barriera la tabla entera también pasaría.
		await using (var contexto = Fixture.CrearContexto())
		{
			var catedra = new CatedraBuilder().ConMateria(materiaID).ConDivision(division.Id).Build();
			catedra.AgregarHorario(new HorarioBuilder().ConDia(Dia.Lunes).Build());
			catedra.AgregarHorario(new HorarioBuilder().ConDia(Dia.Miercoles).Build());

			var catedraAjena = new CatedraBuilder().ConMateria(materiaID).ConDivision(otraDivision.Id).Build();
			catedraAjena.AgregarHorario(new HorarioBuilder().ConDia(Dia.Viernes).Build());

			contexto.AddRange(catedra, catedraAjena);
			await contexto.SaveChangesAsync();
		}

		// Dos inscripciones de ciclos distintos, una de ellas ya cerrada: el paso 3 usa
		// BuscarInscripcionesDeDivisionAsync, que no filtra por ciclo ni por baja. Una inscripción
		// vieja que quedara afuera haría fallar el DELETE de la división contra
		// FK_CURSANTE_DIVISION, que es Restrict.
		Guid cursanteVigenteID;
		await using (var contexto = Fixture.CrearContexto())
		{
			var cerrada = new CursanteBuilder()
				.ConAlumno((await SembrarAlumnoAsync()).Id)
				.ConDivision(division.Id)
				.ConCicloLectivo(CicloLectivo.Crear("2024"))
				.ConFechaInicio(new DateTime(2024, 3, 1))
				.DadoDeBajaEl(new DateTime(2024, 12, 1))
				.Build();
			cerrada.RegistrarCalificacion(materiaID, new DateTime(2024, 6, 1), Periodo.PrimerCuatrimestre, false, 7, null);

			var vigente = new CursanteBuilder()
				.ConAlumno((await SembrarAlumnoAsync()).Id)
				.ConDivision(division.Id)
				.ConCicloLectivo(CicloLectivo.Crear("2025"))
				.ConFechaInicio(new DateTime(2025, 3, 1))
				.Build();
			vigente.RegistrarCalificacion(materiaID, new DateTime(2025, 6, 1), Periodo.PrimerCuatrimestre, false, 9, null);
			vigente.RegistrarCalificacion(materiaID, new DateTime(2025, 9, 1), Periodo.SegundoCuatrimestre, false, 8, null);

			var ajeno = new CursanteBuilder()
				.ConAlumno((await SembrarAlumnoAsync()).Id)
				.ConDivision(otraDivision.Id)
				.ConCicloLectivo(CicloLectivo.Crear("2025"))
				.Build();

			contexto.AddRange(cerrada, vigente, ajeno);
			await contexto.SaveChangesAsync();
			cursanteVigenteID = vigente.Id;
		}

		// Una planilla con sus registros. Sus hijos caen por
		// FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA, que es Cascade, pero los registros apuntan
		// además al cursante con FK_REGISTRO_ASISTENCIA_CURSANTE en Restrict: si el paso 2 no
		// corriera antes del 3, el borrado de las inscripciones fallaría.
		await using (var contexto = Fixture.CrearContexto())
		{
			var planilla = new PlanillaAsistenciaBuilder()
				.ConDivision(division.Id)
				.ConFecha(DateTime.Today.AddDays(-1))
				.ConPreceptor(preceptor.Id)
				.ConCursantes(cursanteVigenteID)
				.Build();
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		// Línea base: todo sembrado y contado antes de tocar nada.
		(await ContarFilasAsync("division")).ShouldBe(2);
		(await ContarFilasAsync("catedra")).ShouldBe(2);
		(await ContarFilasAsync("horario")).ShouldBe(3);
		(await ContarFilasAsync("cursante")).ShouldBe(3);
		(await ContarFilasAsync("calificacion")).ShouldBe(3);
		(await ContarFilasAsync("planilla_asistencia")).ShouldBe(1);
		(await ContarFilasAsync("registro_asistencia")).ShouldBe(1);

		await EnUnScope(sp => sp.GetRequiredService<IServicioDivision>()
			.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id)));

		// La división y los cuatro niveles que colgaban de ella.
		(await ContarFilasAsync("planilla_asistencia")).ShouldBe(0);
		(await ContarFilasAsync("registro_asistencia")).ShouldBe(0);
		(await ContarFilasAsync("calificacion")).ShouldBe(0);

		// Sobrevive exactamente lo de la división hermana: una división, una cátedra con su
		// horario y una inscripción.
		(await ContarFilasAsync("division")).ShouldBe(1);
		(await ContarFilasAsync("catedra")).ShouldBe(1);
		(await ContarFilasAsync("horario")).ShouldBe(1);
		(await ContarFilasAsync("cursante")).ShouldBe(1);

		// La materia y la currícula no son hijas de la división y no se tocan. Importa porque el
		// comentario del paso 3 explica que vaciar `calificacion` es lo que después vuelve
		// borrables las materias del curso: acá se borró la calificación, no la materia.
		(await ContarFilasAsync("materia")).ShouldBe(1);

		// Los alumnos tampoco: la inscripción es del cursante, no de la persona.
		(await ContarFilasAsync("alumno")).ShouldBe(3);

		var divisionesDelCurso = await EnUnScope(sp => sp.GetRequiredService<IServicioDivision>()
			.ListarDivisionesAsync(new ListarDivisionesRequest(curso.Id, "2025")));
		divisionesDelCurso.ShouldHaveSingleItem().Descripcion.ShouldBe("B");
	}
}
