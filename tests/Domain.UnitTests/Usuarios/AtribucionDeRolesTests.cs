using Domain.Shared;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Usuarios;

/// <summary>
/// Fija la atribución rol → permisos contra los tres diagramas de casos de uso de
/// <c>docs/uml/</c>: cada actor puede lo suyo y nada más.
/// </summary>
/// <remarks>
/// <para>
/// No duplica <see cref="RolTests"/>, que cubre la enumeración (descripciones, resolución por
/// texto, igualdad). Lo que se fija acá es la <strong>política</strong>, que es la que se
/// desalineó: el catálogo tenía cuatro roles administrativos genéricos mientras los diagramas
/// hablaban de tres actores concretos, y <c>Secretaria</c> no podía ejecutar ninguno de los seis
/// casos que el diagrama le asigna.
/// </para>
/// <para>
/// Las pruebas negativas valen tanto como las positivas: el modo de falla de un RBAC mal atribuido
/// no es la excepción ruidosa sino el permiso de más, que no rompe nada y no se ve.
/// </para>
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class AtribucionDeRolesTests
{
	#region Encargado de Secretaría — CU-04/05/06, CU-28/29, CU-34
	[Fact]
	public void Secretaria_puede_ejecutar_los_casos_de_su_diagrama()
	{
		Rol.Secretaria.Tiene(Permiso.GestionarPreceptores).ShouldBeTrue();   // CU-04/05/06
		Rol.Secretaria.Tiene(Permiso.GenerarDocumentos).ShouldBeTrue();      // CU-28/29
		Rol.Secretaria.Tiene(Permiso.GenerarReportes).ShouldBeTrue();        // CU-34
	}

	[Fact]
	public void Secretaria_no_gestiona_divisiones_ni_catedras()
	{
		// CU-01/02/03 y CU-17 a CU-27 son de Secretaría Académica, no de Secretaría.
		Rol.Secretaria.Tiene(Permiso.GestionarCursos).ShouldBeFalse();
		Rol.Secretaria.Tiene(Permiso.GestionarCatedras).ShouldBeFalse();
	}

	[Fact]
	public void Secretaria_ya_no_inscribe_cursantes()
	{
		// CU-07 a CU-11 cuelgan del Encargado de Secretaría Académica en el diagrama.
		Rol.Secretaria.Tiene(Permiso.InscribirCursantes).ShouldBeFalse();
	}
	#endregion

	#region Encargado de Secretaría Académica — CU-01/02/03, CU-07 a CU-11, CU-17 a CU-27, CU-31/32/33, CU-37/38
	[Fact]
	public void SecretariaAcademica_puede_ejecutar_los_casos_de_su_diagrama()
	{
		Rol.SecretariaAcademica.Tiene(Permiso.GestionarCursos).ShouldBeTrue();
		Rol.SecretariaAcademica.Tiene(Permiso.InscribirCursantes).ShouldBeTrue();
		Rol.SecretariaAcademica.Tiene(Permiso.GestionarCatedras).ShouldBeTrue();
		Rol.SecretariaAcademica.Tiene(Permiso.GenerarDocumentos).ShouldBeTrue();
		Rol.SecretariaAcademica.Tiene(Permiso.GenerarReportes).ShouldBeTrue();
	}

	[Fact]
	public void SecretariaAcademica_no_designa_preceptores()
	{
		// CU-04/05/06 son del Encargado de Secretaría: por eso GestionarPreceptores salió de
		// GestionarCursos, que concedía los dos grupos juntos.
		Rol.SecretariaAcademica.Tiene(Permiso.GestionarPreceptores).ShouldBeFalse();
	}

	[Fact]
	public void SecretariaAcademica_no_emite_documentacion_ni_reportes_de_preceptoria()
	{
		Rol.SecretariaAcademica.Tiene(Permiso.GenerarDocumentosAsistencia).ShouldBeFalse();
		Rol.SecretariaAcademica.Tiene(Permiso.GenerarReportesDeCursada).ShouldBeFalse();
	}
	#endregion

	#region Encargado de Preceptoría — CU-12 a CU-16, CU-30, CU-35/36
	[Fact]
	public void Preceptoria_puede_ejecutar_los_casos_de_su_diagrama()
	{
		Rol.Preceptoria.Tiene(Permiso.TomarAsistencia).ShouldBeTrue();
		Rol.Preceptoria.Tiene(Permiso.GenerarDocumentosAsistencia).ShouldBeTrue();
		Rol.Preceptoria.Tiene(Permiso.GenerarReportesDeCursada).ShouldBeTrue();
	}

	[Fact]
	public void Preceptoria_no_emite_documentacion_academica_ni_reportes_institucionales()
	{
		// Es lo que se ganó al partir los permisos: con GenerarDocumentos a secas, Preceptoría
		// podía emitir un boletín de calificaciones.
		Rol.Preceptoria.Tiene(Permiso.GenerarDocumentos).ShouldBeFalse();
		Rol.Preceptoria.Tiene(Permiso.GenerarReportes).ShouldBeFalse();
	}

	[Fact]
	public void Preceptoria_no_es_docente()
	{
		// Son dos roles distintos y ninguno es subconjunto del otro: Preceptoría toma asistencia
		// y no carga notas; Docente carga notas y no toma asistencia.
		Rol.Preceptoria.Tiene(Permiso.CargarCalificaciones).ShouldBeFalse();
		Rol.Docente.Tiene(Permiso.TomarAsistencia).ShouldBeFalse();
	}
	#endregion

	#region Roles sin actor en los diagramas
	[Fact]
	public void Admin_conserva_todos_los_permisos()
	{
		foreach (var permiso in Enumeration.GetAll<Permiso>())
		{
			Rol.Admin.Tiene(permiso).ShouldBeTrue($"Admin debería tener «{permiso.Descripcion}»");
		}
	}

	/// <remarks>
	/// Dirección no es actor de ningún diagrama y se conserva para más adelante. Lo que se fija
	/// es que partir los permisos no le haya recortado el alcance: tenía
	/// <c>GestionarCursos</c>, que incluía los preceptores, así que ahora necesita los dos.
	/// </remarks>
	[Fact]
	public void Direccion_mantiene_el_alcance_que_tenia_antes_de_partir_los_permisos()
	{
		Rol.Direccion.Tiene(Permiso.GestionarCursos).ShouldBeTrue();
		Rol.Direccion.Tiene(Permiso.GestionarPreceptores).ShouldBeTrue();
		Rol.Direccion.Tiene(Permiso.GestionarCurriculas).ShouldBeTrue();
		Rol.Direccion.Tiene(Permiso.GestionarCatedras).ShouldBeTrue();
		Rol.Direccion.Tiene(Permiso.InscribirCursantes).ShouldBeTrue();
		Rol.Direccion.Tiene(Permiso.TomarAsistencia).ShouldBeTrue();
	}

	[Fact]
	public void Direccion_sigue_sin_emitir_documentos_ni_reportes()
	{
		Rol.Direccion.Tiene(Permiso.GenerarDocumentos).ShouldBeFalse();
		Rol.Direccion.Tiene(Permiso.GenerarReportes).ShouldBeFalse();
	}

	[Fact]
	public void Docente_conserva_unicamente_la_carga_de_calificaciones()
	{
		Rol.Docente.Permisos.ShouldHaveSingleItem().ShouldBe(Permiso.CargarCalificaciones);
	}
	#endregion

	#region Cobertura del catálogo
	/// <remarks>
	/// Un permiso que ningún rol tiene es una guarda que nadie puede pasar: el caso de uso queda
	/// inalcanzable sin un solo error. Admin los tiene todos, así que esto falla sólo si alguien
	/// agrega un <see cref="Permiso"/> y se olvida de atribuirlo.
	/// </remarks>
	[Fact]
	public void Ningun_permiso_queda_sin_rol_que_lo_conceda()
	{
		var roles = Enumeration.GetAll<Rol>();

		foreach (var permiso in Enumeration.GetAll<Permiso>())
		{
			roles.Any(rol => rol.Tiene(permiso))
				 .ShouldBeTrue($"Ningún rol concede «{permiso.Descripcion}»");
		}
	}

	[Fact]
	public void Un_rol_que_el_codigo_no_conoce_no_habilita_nada()
	{
		// Falla cerrado: Rol.Permisos devuelve vacío para un Id desconocido en vez de lanzar.
		Enumeration.GetAll<Rol>().Select(x => x.Id).ShouldNotContain(99);
	}
	#endregion
}
