using System.Configuration;
using EDUSIS.TestSupport;
using Infrastructure.Documentos.Configuracion;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Documentos;

/// <summary>
/// La sección <c>&lt;institucion&gt;</c> que ship­ea la aplicación parsea contra
/// <see cref="InstitucionSection"/>.
/// </summary>
/// <remarks>
/// Es una prueba de integración de verdad: lee el <c>App.config</c> REAL de <c>WPF_Desktop</c>,
/// el mismo archivo que la aplicación carga al arrancar. Un error de tipeo en un nombre de
/// atributo, o un <c>type</c> mal escrito en <c>configSections</c>, no se nota al compilar y
/// recién aparece cuando alguien entra a Documentación y se encuentra con una excepción de
/// configuración. Acá se nota en la suite.
/// <para>
/// Se usa <see cref="ConfigurationManager.OpenMappedExeConfiguration"/> y no
/// <c>ConfigurationManager.GetSection</c> porque el archivo de configuración del proceso de
/// prueba es el del host de xUnit, no el de la aplicación.
/// </para>
/// </remarks>
[Trait("Categoria", Categorias.Integracion)]
public sealed class InstitucionSectionTests
{
	private static InstitucionSection SeccionDeLaAplicacion()
	{
		var mapa = new ExeConfigurationFileMap { ExeConfigFilename = RutaDelAppConfig() };
		var configuracion = ConfigurationManager.OpenMappedExeConfiguration(mapa, ConfigurationUserLevel.None);

		return configuracion.GetSection(InstitucionSection.NOMBRE_SECCION) as InstitucionSection
			?? throw new ConfigurationErrorsException($"No se encontró la sección <{ InstitucionSection.NOMBRE_SECCION }>.");
	}

	/// <summary>
	/// Sube desde el binario de prueba hasta la raíz del repositorio —la carpeta que tiene el
	/// <c>.sln</c>— y de ahí baja al <c>App.config</c>. Así la prueba no depende de cuántos
	/// niveles tenga <c>bin/Debug/…</c> ni de correr desde un worktree o del clon principal.
	/// </summary>
	private static string RutaDelAppConfig()
	{
		var directorio = new DirectoryInfo(AppContext.BaseDirectory);

		while (directorio is not null && !directorio.EnumerateFiles("EDUSIS.sln").Any())
		{
			directorio = directorio.Parent;
		}

		directorio.ShouldNotBeNull("No se encontró la raíz del repositorio (EDUSIS.sln) desde el binario de prueba.");

		var ruta = Path.Combine(directorio!.FullName, "WPF_Desktop", "App.config");
		File.Exists(ruta).ShouldBeTrue($"No existe { ruta }.");

		return ruta;
	}

	[Fact]
	public void La_seccion_institucion_del_App_config_parsea()
	{
		var seccion = SeccionDeLaAplicacion();

		seccion.ShouldNotBeNull();
	}

	[Fact]
	public void Los_datos_que_el_encabezado_imprime_estan_completos()
	{
		var seccion = SeccionDeLaAplicacion();

		// Estos cinco son los que todo documento oficial tiene que llevar. Si alguno queda vacío,
		// se emite documentación que no sirve para el trámite.
		seccion.Nombre.ShouldNotBeNullOrWhiteSpace();
		seccion.CUE.ShouldNotBeNullOrWhiteSpace();
		seccion.Direccion.ShouldNotBeNullOrWhiteSpace();
		seccion.Localidad.ShouldNotBeNullOrWhiteSpace();
		seccion.Jurisdiccion.ShouldNotBeNullOrWhiteSpace();
	}

	[Fact]
	public void Los_atributos_se_mapean_a_las_propiedades_que_les_corresponden()
	{
		var seccion = SeccionDeLaAplicacion();

		// Un atributo mal nombrado en el XML no falla: la propiedad queda en su valor por omisión.
		// Comprobar valores concretos es lo único que distingue "mapeó" de "quedó vacío".
		seccion.Nombre.ShouldBe("I.P.E.T. Nº 416");
		seccion.CUE.ShouldBe("1405675");
		seccion.CueAnexo.ShouldBe("140567500");
		seccion.Localidad.ShouldBe("Córdoba");
		seccion.CodigoPostal.ShouldBe("5000");
		seccion.Departamento.ShouldBe("Capital");
		seccion.Jurisdiccion.ShouldBe("Córdoba");
		seccion.Telefono.ShouldBe("(0351) 4349835");
		seccion.Sector.ShouldBe("Estatal");
		seccion.Ambito.ShouldBe("Urbano");
		seccion.Modalidad.ShouldBe("Educación común");
		seccion.NivelesEnsenanza.ShouldBe("Secundaria, Secundaria Técnica (INET)");
		seccion.Direccion.ShouldBe("CALLE PUBLICA CIUDAD DE MIS SUEÑOS RUTA 9 KM 690");
	}
}
