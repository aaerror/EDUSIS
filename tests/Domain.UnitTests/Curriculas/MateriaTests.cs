using Domain.Curriculas.DomainEvents;
using Domain.Curriculas.Materias;
using Domain.Curriculas.Materias.CargosDocentes;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Entidad <see cref="Materia"/>: alta (con el evento <see cref="MateriaRegistradaEvent"/>
/// encolado), edición, ciclo de vida de los cargos docentes y registro de calificaciones. Las
/// excepciones del módulo son <c>internal</c>: se verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class MateriaTests
{
	#region Alta
	[Fact]
	public void Una_materia_nueva_guarda_descripcion_y_horas_y_encola_MateriaRegistradaEvent()
	{
		var curriculaID = Guid.NewGuid();

		var materia = new Materia(curriculaID, "Matemática", 4);

		materia.CurriculaID.ShouldBe(curriculaID);
		materia.Descripcion.ShouldBe("Matemática");
		materia.HorasCatedra.ShouldBe(4);
		var evento = materia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<MateriaRegistradaEvent>();
		evento.MateriaID.ShouldBe(materia.Id);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Una_materia_sin_descripcion_lanza_ArgumentNullException(string descripcion)
	{
		Should.Throw<ArgumentNullException>(() => new Materia(Guid.NewGuid(), descripcion, 4));
	}

	[Fact]
	public void Una_materia_con_menos_de_una_hora_catedra_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => new Materia(Guid.NewGuid(), "Matemática", 0));
	}

	[Fact]
	public void ModificarMateria_actualiza_descripcion_y_carga_horaria()
	{
		var materia = new MateriaBuilder().Build();

		materia.ModificarMateria("Análisis Matemático", 6);

		materia.Descripcion.ShouldBe("Análisis Matemático");
		materia.HorasCatedra.ShouldBe(6);
	}

	[Fact]
	public void ModificarMateria_con_carga_horaria_no_positiva_lanza_ArgumentException()
	{
		var materia = new MateriaBuilder().Build();

		Should.Throw<ArgumentException>(() => materia.ModificarMateria("Matemática", 0));
	}
	#endregion

	#region Cargos docentes
	[Fact]
	public void RegistrarCargoDocente_agrega_la_situacion_de_revista()
	{
		var materia = new MateriaBuilder().Build();

		materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false);

		materia.Docentes.ShouldHaveSingleItem().Cargo.ShouldBe(Cargo.Titular);
	}

	[Fact]
	public void RegistrarCargoDocente_con_un_cargo_inexistente_lanza_CargoInexistenteException()
	{
		var materia = new MateriaBuilder().Build();

		var ex = Should.Throw<Exception>(() =>
			materia.RegistrarCargoDocente(Guid.NewGuid(), "Rector", DateTime.Today, null, false));

		ex.GetType().Name.ShouldBe("CargoInexistenteException");
	}

	[Fact]
	public void RegistrarCargoDocente_con_un_cargo_ya_ocupado_lanza_CargoOcupadoException()
	{
		var materia = new MateriaBuilder().Build();
		materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false);

		var ex = Should.Throw<Exception>(() =>
			materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false));

		ex.GetType().Name.ShouldBe("CargoOcupadoException");
	}

	[Fact]
	public void AsignarDocenteDeAula_pone_al_docente_en_funciones()
	{
		var materia = new MateriaBuilder().Build();
		materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false);
		var situacionID = materia.Docentes.First().Id;

		materia.AsignarDocenteDeAula(situacionID);

		materia.Docentes.First().EnFunciones.ShouldBeTrue();
	}

	[Fact]
	public void AsignarDocenteDeAula_sobre_un_cargo_inexistente_lanza_CargoNoEncontradoException()
	{
		var materia = new MateriaBuilder().Build();

		var ex = Should.Throw<Exception>(() => materia.AsignarDocenteDeAula(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoNoEncontradoException");
	}

	[Fact]
	public void AsignarDocenteDeAula_a_un_docente_que_ya_esta_en_funciones_lanza_DocenteEnFuncionesException()
	{
		var materia = new MateriaBuilder().Build();
		materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, true);
		var situacionID = materia.Docentes.First().Id;

		var ex = Should.Throw<Exception>(() => materia.AsignarDocenteDeAula(situacionID));

		ex.GetType().Name.ShouldBe("DocenteEnFuncionesException");
	}

	[Fact]
	public void RescindirCargoDocente_sobre_un_cargo_inexistente_lanza_CargoNoEncontradoException()
	{
		var materia = new MateriaBuilder().Build();

		var ex = Should.Throw<Exception>(() => materia.RescindirCargoDocente(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoNoEncontradoException");
	}

	[Fact]
	public void EliminarCargoDocente_quita_la_situacion_de_revista()
	{
		var materia = new MateriaBuilder().Build();
		materia.RegistrarCargoDocente(Guid.NewGuid(), nameof(Cargo.Titular), DateTime.Today, null, false);
		var situacionID = materia.Docentes.First().Id;

		materia.EliminarCargoDocente(situacionID);

		materia.Docentes.ShouldBeEmpty();
	}

	[Fact]
	public void EliminarCargoDocente_sobre_un_cargo_inexistente_lanza_CargoNoEncontradoException()
	{
		var materia = new MateriaBuilder().Build();

		var ex = Should.Throw<Exception>(() => materia.EliminarCargoDocente(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoNoEncontradoException");
	}
	#endregion

	#region Calificaciones
	[Fact]
	public void RegistrarCalificacion_con_asistencia_guarda_la_nota()
	{
		var materia = new MateriaBuilder().Build();
		var cursante = Guid.NewGuid();

		materia.RegistrarCalificacion(cursante, DateTime.Today, Instancia.Parcial, true, 8, null);

		var calificacion = materia.Calificaciones.ShouldHaveSingleItem();
		calificacion.Nota.ShouldBe(8d);
		calificacion.Asistencia.ShouldBeTrue();
	}

	[Fact]
	public void RegistrarCalificacion_dos_veces_en_la_misma_fecha_lanza_ArgumentException()
	{
		var materia = new MateriaBuilder().Build();
		var cursante = Guid.NewGuid();
		materia.RegistrarCalificacion(cursante, DateTime.Today, Instancia.Parcial, true, 8, null);

		Should.Throw<ArgumentException>(() =>
			materia.RegistrarCalificacion(cursante, DateTime.Today, Instancia.Parcial, true, 6, null));
	}

	[Fact]
	public void QuitarCalificacion_inexistente_lanza_ArgumentException()
	{
		var materia = new MateriaBuilder().Build();

		Should.Throw<ArgumentException>(() =>
			materia.QuitarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Parcial));
	}
	#endregion
}
