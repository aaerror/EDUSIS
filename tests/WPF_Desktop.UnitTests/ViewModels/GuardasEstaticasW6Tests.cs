using Shouldly;
// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.ViewModels.Docentes;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Guardas estaticas de la ola W6 sobre XAML y codigo fuente, para lo que ningun ViewModel puede afirmar:
/// el <c>DataContext</c> del bloque "Docente en Funciones" (un binding roto no falla el build), las guardas de R6
/// (<c>Materia.SituacionRevista</c>, <c>QuitarDocente(</c>, <c>Models/</c>), <c>D.3</c> (<c>await</c> y <c>Task</c>) y la
/// auditoria de bindings y estilos grilla por grilla de <c>GestionSituacionRevistaView</c> e <c>InscripcionAlumnoView</c>.
/// </summary>
public class GuardasEstaticasW6Tests
{
	private const string GestionSituacionRevistaXaml = "WPF_Desktop/Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml";
	private const string GestionSituacionRevistaCs = "WPF_Desktop/ViewModels/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaViewModel.cs";
	private const string InscripcionAlumnoXaml = "WPF_Desktop/Views/Alumnos/InscripcionAlumnoView.xaml";
	private const string GestionDocentesCs = "WPF_Desktop/ViewModels/Docentes/GestionDocentesViewModel.cs";

	private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

	private static XDocument Cargar(string rutaRelativa)
	{
		return XDocument.Load(RaizDelRepositorio.Resolver(rutaRelativa));
	}

	private static IEnumerable<string> ArchivosFuente(params string[] extensiones)
	{
		var raiz = RaizDelRepositorio.Resolver("WPF_Desktop");
		var separador = Path.DirectorySeparatorChar;

		return Directory.EnumerateFiles(raiz, "*.*", SearchOption.AllDirectories)
						.Where(f => extensiones.Contains(Path.GetExtension(f)))
						.Where(f => !f.Contains($"{separador}obj{separador}") && !f.Contains($"{separador}bin{separador}"));
	}

	/// <summary>Devuelve el codigo sin los bloques <c>/* */</c> ni los comentarios de linea.</summary>
	private static string CodigoVivo(string rutaRelativa)
	{
		var fuente = RaizDelRepositorio.Leer(rutaRelativa);
		fuente = Regex.Replace(fuente, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

		return string.Join("\n", fuente.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
	}

	#region Bloque "Docente en Funciones"
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_bloque_Docente_en_Funciones_tiene_como_DataContext_SituacionRevistaEnFunciones()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);

		var coincidencia = Regex.Match(xaml, @"DOCENTE EN FUNCIONES-->.*?<local:SituacionRevistaView\s+DataContext=""\{\s*Binding\s+Path=(\w+)", RegexOptions.Singleline);

		coincidencia.Success.ShouldBeTrue();
		coincidencia.Groups[1].Value.ShouldBe("SituacionRevistaEnFunciones");
		coincidencia.Groups[1].Value.ShouldNotBe("SituacionRevistaUPDATE");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_bloque_Docente_en_Funciones_se_habilita_con_HabilitarDocenteEnFunciones()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);

		var coincidencia = Regex.Match(xaml, @"DOCENTE EN FUNCIONES-->\s*<Border[^>]*?IsEnabled=""\{\s*Binding\s+Path=(\w+)", RegexOptions.Singleline);

		coincidencia.Success.ShouldBeTrue();
		coincidencia.Groups[1].Value.ShouldBe("HabilitarDocenteEnFunciones");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todas_las_propiedades_que_la_vista_liga_directo_al_ViewModel_existen()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);
		var tipo = typeof(GestionSituacionRevistaViewModel);

		// Solo los bindings "Path=X" sin punto ni ElementName/RelativeSource, que se resuelven contra el ViewModel.
		var propiedades = Regex.Matches(xaml, @"\{\s*Binding\s+Path=(\w+)\s*[,}]")
							   .Select(m => m.Groups[1].Value)
							   .Distinct()
							   .ToList();

		// Propiedades que pertenecen a los items de las grillas y a las vistas anidadas, no al ViewModel contenedor.
		var delItem = new HashSet<string> { "Cargo", "Estado", "Docente", "EnFunciones", "FechaAlta", "NombreCompleto", "EstaActivo", "IsEnabled", "Foreground", "SelectedItem", "SelectedDate", "Descripcion" };
		var faltantes = propiedades.Where(p => !delItem.Contains(p) && tipo.GetProperty(p) is null).ToList();

		faltantes.ShouldBeEmpty("propiedades bindeadas que GestionSituacionRevistaViewModel no expone");
	}
	#endregion

	#region Grillas: ItemsSource antes de juzgar las columnas
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void DataGrid_Docentes_bindea_columnas_que_existen_en_SituacionRevistaViewModel()
	{
		var documento = Cargar(GestionSituacionRevistaXaml);

		var grilla = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Docentes");
		((string) grilla.Attribute("ItemsSource")!).ShouldContain("Path=DocentesEnMateria");

		var columnas = ColumnasBindeadas(grilla);
		columnas.ShouldBe(new[] { "Cargo", "Estado", "Docente", "EnFunciones" });
		DebenExistirEn(typeof(SituacionRevistaViewModel), columnas);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void DataGrid_LegajosDocentes_bindea_columnas_que_existen_en_LegajoDocenteViewModel()
	{
		var documento = Cargar(GestionSituacionRevistaXaml);

		var grilla = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_LegajosDocentes");
		((string) grilla.Attribute("ItemsSource")!).ShouldContain("Path=Docentes");

		var columnas = ColumnasBindeadas(grilla);
		columnas.ShouldBe(new[] { "FechaAlta", "NombreCompleto", "EstaActivo" });
		DebenExistirEn(typeof(LegajoDocenteViewModel), columnas);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_combo_de_reemplazo_liga_ReemplazaA_por_el_ID_de_la_situacion_elegida()
	{
		var documento = Cargar(GestionSituacionRevistaXaml);

		var combo = documento.Descendants().Single(e => e.Name.LocalName == "ComboBox" && ((string?) e.Attribute("ItemsSource") ?? string.Empty).Contains("SituacionesReemplazables"));

		((string) combo.Attribute("SelectedValuePath")!).ShouldBe("SituacionRevistaID");
		((string) combo.Attribute("SelectedValue")!).ShouldContain("SituacionRevistaINSERT.ReemplazaA");
		typeof(SituacionRevistaViewModel).GetProperty("SituacionRevistaID").ShouldNotBeNull();
		typeof(SituacionRevistaViewModel).GetProperty("ReemplazaA").ShouldNotBeNull();
	}

	private static void DebenExistirEn(Type tipo, IEnumerable<string> propiedades)
	{
		var faltantes = propiedades.Distinct().Where(p => tipo.GetProperty(p) is null).ToList();

		faltantes.ShouldBeEmpty($"propiedades bindeadas que {tipo.Name} no expone");
	}

	private static List<string> ColumnasBindeadas(XElement grilla)
	{
		var columnas = new List<string>();

		foreach (var columna in grilla.Descendants().Where(e => e.Name.LocalName.StartsWith("DataGrid") && e.Name.LocalName.EndsWith("Column")))
		{
			var enlace = (string?) columna.Attribute("Binding");

			if (enlace is null)
			{
				continue;
			}

			var coincidencia = Regex.Match(enlace, @"Path=(\w+)");

			if (coincidencia.Success)
			{
				columnas.Add(coincidencia.Groups[1].Value);
			}
		}

		return columnas;
	}
	#endregion

	#region Estilos: el TargetType coincide con el tipo del elemento
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionSituacionRevistaXaml)]
	[InlineData(InscripcionAlumnoXaml)]
	public void Cada_Style_estatico_aplicado_tiene_el_TargetType_del_elemento(string vista)
	{
		var tiposPorEstilo = LeerEstilosDelTema();
		var incompatibles = new List<string>();
		var verificados = 0;

		foreach (var elemento in Cargar(vista).Descendants())
		{
			var atributo = (string?) elemento.Attribute("Style");

			if (atributo is null)
			{
				continue;
			}

			var coincidencia = Regex.Match(atributo, @"StaticResource\s+(?:ResourceKey=)?(\w+)");

			if (!coincidencia.Success || !tiposPorEstilo.Contains(coincidencia.Groups[1].Value))
			{
				continue;
			}

			verificados++;
			var clave = coincidencia.Groups[1].Value;

			if (!tiposPorEstilo[clave].Contains(elemento.Name.LocalName))
			{
				incompatibles.Add($"<{elemento.Name.LocalName} Style={clave}> (TargetType: {string.Join("/", tiposPorEstilo[clave])})");
			}
		}

		verificados.ShouldBeGreaterThan(0);
		incompatibles.ShouldBeEmpty();
	}

	private static ILookup<string, string> LeerEstilosDelTema()
	{
		var tema = RaizDelRepositorio.Resolver("WPF_Desktop/Theme");
		var pares = new List<(string Clave, string Tipo)>();

		foreach (var archivo in Directory.EnumerateFiles(tema, "*.xaml"))
		{
			foreach (var estilo in XDocument.Load(archivo).Descendants().Where(e => e.Name.LocalName == "Style"))
			{
				var clave = (string?) estilo.Attribute(Xaml + "Key");
				var tipo = (string?) estilo.Attribute("TargetType");

				if (clave is null || tipo is null)
				{
					continue;
				}

				var coincidencia = Regex.Match(tipo, @"x:Type\s+(?:TypeName=)?\s*(?:\w+:)?(\w+)");

				if (coincidencia.Success)
				{
					pares.Add((clave, coincidencia.Groups[1].Value));
				}
			}
		}

		return pares.ToLookup(p => p.Clave, p => p.Tipo);
	}
	#endregion

	#region Guardas de R6 y de las tareas de W6
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ningun_archivo_de_WPF_Desktop_bindea_Materia_SituacionRevista()
	{
		var infractores = ArchivosFuente(".cs", ".xaml")
			.Where(f => File.ReadAllText(f).Contains("Materia.SituacionRevista"))
			.ToList();

		infractores.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ningun_archivo_de_WPF_Desktop_llama_al_QuitarDocente_sincronico()
	{
		var infractores = ArchivosFuente(".cs")
			.Where(f => Regex.IsMatch(File.ReadAllText(f), @"\bQuitarDocente\("))
			.ToList();

		infractores.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void D3_QuitarDocenteAsync_se_espera_con_await_y_el_metodo_contenedor_devuelve_Task()
	{
		var fuente = RaizDelRepositorio.Leer(GestionDocentesCs);

		fuente.ShouldContain("await _servicioDocentes.QuitarDocenteAsync(");
		fuente.ShouldContain("private async Task ExecuteEliminarCommandAsync()");
		fuente.ShouldNotContain("private void ExecuteEliminarCommand");
		fuente.ShouldNotMatch(@"(?<!await )_servicioDocentes\.QuitarDocenteAsync\(");

		typeof(GestionDocentesViewModel).GetProperty("EliminarCommand")!.PropertyType.Name.ShouldBe("IAsyncRelayCommand");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_carpeta_Models_de_WPF_Desktop_ya_no_existe()
	{
		Directory.Exists(RaizDelRepositorio.Resolver("WPF_Desktop/Models")).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionSituacionRevista_no_usa_MessageBox_ni_async_void_ni_IServicioCurricula_en_codigo_vivo()
	{
		var codigo = CodigoVivo(GestionSituacionRevistaCs);

		// "MessageBox.Show" y no "MessageBox": ShouldNotContain ignora mayusculas y chocaria con la variable local messageBoxText.
		codigo.ShouldNotContain("MessageBox.Show");
		codigo.ShouldNotContain("async void");
		codigo.ShouldNotContain("IServicioCurricula");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionSituacionRevista_llama_a_ListarDocentesActivosAsync_una_sola_vez_en_codigo_vivo_y_fuera_de_todo_bucle()
	{
		var codigo = CodigoVivo(GestionSituacionRevistaCs);

		Regex.Matches(codigo, @"ListarDocentesActivosAsync\(").Count.ShouldBe(1);

		// Ninguna llamada dentro de un foreach/for/while: se mira el texto entre el bucle y su llave de cierre mas proxima.
		foreach (Match bucle in Regex.Matches(codigo, @"\b(foreach|for|while)\s*\([^)]*\)\s*\{[^{}]*\}"))
		{
			bucle.Value.ShouldNotContain("ListarDocentesActivosAsync");
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionSituacionRevista_los_seis_casos_de_uso_pegan_en_IServicioCatedra()
	{
		var codigo = CodigoVivo(GestionSituacionRevistaCs);

		foreach (var metodo in new[]
		{
			"ListarSituacionesRevistaAsync", "DesignarDocenteAsync", "PonerEnFuncionesAsync",
			"RelevarDeFuncionesAsync", "EstablecerFinDeDesignacionAsync", "FinalizarDesignacionAsync"
		})
		{
			codigo.ShouldContain($"_servicioCatedra.{metodo}(");
		}
	}
	#endregion
}
