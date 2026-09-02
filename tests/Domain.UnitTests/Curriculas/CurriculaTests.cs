using Domain.Curriculas;
using Domain.Curriculas.Materias.CargosDocentes;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Agregado <see cref="Curricula"/>: vigencia del período, alta/edición/baja de materias y
/// asignación de docentes a materias. Las excepciones del módulo son <c>internal</c>: se
/// verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CurriculaTests
{
	#region Alta y vigencia
	[Fact]
	public void Una_curricula_nueva_guarda_el_curso_y_arranca_vigente()
	{
		var cursoID = Guid.NewGuid();

		var curricula = new CurriculaBuilder().ConCurso(cursoID).Build();

		curricula.CursoID.ShouldBe(cursoID);
		curricula.EstaVigente().ShouldBeTrue();
		curricula.TotalEspacios.ShouldBe(0);
	}

	[Fact]
	public void Desafectar_le_pone_fecha_de_fin_al_periodo()
	{
		var curricula = new CurriculaBuilder().Build();

		curricula.Desafectar();

		curricula.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void EstablecerFechaFinalizacion_sobre_una_curricula_ya_expirada_lanza_CurriculaNoVigenteException()
	{
		var curricula = new CurriculaBuilder()
			.ConFechaInicio(new DateTime(2020, 1, 1))
			.ConFechaFin(new DateTime(2020, 12, 31))
			.Build();

		var ex = Should.Throw<Exception>(() => curricula.EstablecerFechaFinalizacion(DateTime.Today));

		ex.GetType().Name.ShouldBe("CurriculaNoVigenteException");
	}
	#endregion

	#region Materias
	[Fact]
	public void AgregarMateria_incorpora_la_materia_y_suma_sus_horas()
	{
		var curricula = new CurriculaBuilder().Build();

		curricula.AgregarMateria("Matemática", 4);
		curricula.AgregarMateria("Química", 3);

		curricula.TotalEspacios.ShouldBe(2);
		curricula.TotalHorasSemanales.ShouldBe(7);
	}

	[Fact]
	public void AgregarMateria_con_un_nombre_ya_usado_lanza_NombreMateriaDuplicadoException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();

		var ex = Should.Throw<Exception>(() => curricula.AgregarMateria("matemática", 2));

		ex.GetType().Name.ShouldBe("NombreMateriaDuplicadoException");
	}

	[Fact]
	public void AgregarMateria_sobre_una_curricula_expirada_lanza_CurriculaNoVigenteException()
	{
		var curricula = new CurriculaBuilder()
			.ConFechaInicio(new DateTime(2020, 1, 1))
			.ConFechaFin(new DateTime(2020, 12, 31))
			.Build();

		var ex = Should.Throw<Exception>(() => curricula.AgregarMateria("Matemática", 4));

		ex.GetType().Name.ShouldBe("CurriculaNoVigenteException");
	}

	[Fact]
	public void ActualizarMateria_cambia_descripcion_y_horas()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();
		var materiaID = curricula.Materias.First().Id;

		curricula.ActualizarMateria(materiaID, "Análisis Matemático", 6);

		var materia = curricula.Materias.First();
		materia.Descripcion.ShouldBe("Análisis Matemático");
		materia.HorasCatedra.ShouldBe(6);
	}

	[Fact]
	public void QuitarMateria_elimina_la_materia_indicada()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).ConMateria("Química", 3).Build();
		var materiaID = curricula.Materias.First().Id;

		curricula.QuitarMateria(materiaID);

		curricula.TotalEspacios.ShouldBe(1);
	}

	[Fact]
	public void QuitarMateria_con_un_id_desconocido_lanza_MateriaNoEncontradaException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();

		var ex = Should.Throw<Exception>(() => curricula.QuitarMateria(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("MateriaNoEncontradaException");
	}

	[Fact]
	public void QuitarMateria_con_Guid_vacio_lanza_MateriaDuplicadaException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();

		var ex = Should.Throw<Exception>(() => curricula.QuitarMateria(Guid.Empty));

		ex.GetType().Name.ShouldBe("MateriaDuplicadaException");
	}
	#endregion

	#region Docentes en materias
	[Fact]
	public void AsignarDocenteEnMateria_registra_la_situacion_de_revista()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();
		var materiaID = curricula.Materias.First().Id;
		var docenteID = Guid.NewGuid();

		curricula.AsignarDocenteEnMateria(materiaID, docenteID, nameof(Cargo.Titular), DateTime.Today, null, false);

		curricula.DocentesAsignadosEnMateria(materiaID).ShouldHaveSingleItem().DocenteID.ShouldBe(docenteID);
	}

	[Fact]
	public void AsignarDocenteEnMateria_al_mismo_docente_dos_veces_lanza_DocenteRegistradoException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();
		var materiaID = curricula.Materias.First().Id;
		var docenteID = Guid.NewGuid();
		curricula.AsignarDocenteEnMateria(materiaID, docenteID, nameof(Cargo.Titular), DateTime.Today, null, false);

		var ex = Should.Throw<Exception>(() =>
			curricula.AsignarDocenteEnMateria(materiaID, docenteID, nameof(Cargo.Interino), DateTime.Today, DateTime.Today.AddMonths(2), false));

		ex.GetType().Name.ShouldBe("DocenteRegistradoException");
	}

	[Fact]
	public void AsignarDocenteEnMateria_con_un_cargo_que_no_existe_lanza_CargoInexistenteException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();
		var materiaID = curricula.Materias.First().Id;

		var ex = Should.Throw<Exception>(() =>
			curricula.AsignarDocenteEnMateria(materiaID, Guid.NewGuid(), "Rector", DateTime.Today, null, false));

		ex.GetType().Name.ShouldBe("CargoInexistenteException");
	}

	[Fact]
	public void AsignarDocenteEnMateria_con_un_cargo_ya_ocupado_lanza_CargoOcupadoException()
	{
		var curricula = new CurriculaBuilder().ConMateria("Matemática", 4).Build();
		var materiaID = curricula.Materias.First().Id;
		curricula.AsignarDocenteEnMateria(materiaID, Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false);

		var ex = Should.Throw<Exception>(() =>
			curricula.AsignarDocenteEnMateria(materiaID, Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false));

		ex.GetType().Name.ShouldBe("CargoOcupadoException");
	}
	#endregion
}
