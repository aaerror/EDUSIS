using Shouldly;
// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Guardas estaticas de la ola W5 sobre el XAML y el codigo fuente, para lo que ningun ViewModel puede afirmar:
/// <c>UI-01</c> en su raiz (que items ofrece el combo y que propiedad liga), <c>UI-08</c> (un solo sitio que construye
/// <c>RegistrarMateriaRequest</c>) y que cada <c>Style</c> aplicado a un elemento tenga el <c>TargetType</c> de ese elemento
/// (un estilo de <c>Label</c> sobre un <c>TextBlock</c> lanza <c>InvalidOperationException</c> al instanciar la plantilla,
/// y solo cuando la lista tiene un item).
/// </summary>
public class GuardasEstaticasW5Tests
{
	private const string GestionCurriculas = "WPF_Desktop/Views/Cursos/Curriculas/GestionCurriculasView.xaml";
	private const string MateriaView = "WPF_Desktop/Views/Cursos/Curriculas/Materias/MateriaView.xaml";
	private const string GestionCursantes = "WPF_Desktop/Views/Cursos/Divisiones/GestionCursantesView.xaml";

	private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

	private static XDocument Cargar(string rutaRelativa)
	{
		return XDocument.Load(RaizDelRepositorio.Resolver(rutaRelativa));
	}

	private static IEnumerable<XElement> Combos(XDocument documento)
	{
		return documento.Descendants().Where(e => e.Name.LocalName == "ComboBox");
	}

	#region UI-01: el combo expone el valor de dominio, no su indice
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas, "CargaHoraria")]
	[InlineData(MateriaView, "HorasCatedra")]
	public void El_combo_de_horas_liga_SelectedItem_al_valor_y_no_SelectedIndex(string vista, string propiedad)
	{
		var combo = Combos(Cargar(vista)).Single(c => ((string?) c.Attribute("SelectedItem") ?? string.Empty).Contains(propiedad));

		combo.Attribute("SelectedIndex").ShouldBeNull();
		combo.Attribute("ItemsSource").ShouldNotBeNull();
		combo.Elements().ShouldBeEmpty(); // sin ComboBoxItem con IsSelected="True" que empuje un valor al cargar
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas)]
	[InlineData(MateriaView)]
	public void El_primer_valor_del_combo_es_1_y_los_items_son_1_a_5_sin_corrimiento(string vista)
	{
		var documento = Cargar(vista);
		var arreglo = documento.Descendants().Single(e => e.Name.LocalName == "Array" && (string?) e.Attribute(Xaml + "Key") == "horasCatedra");

		var valores = arreglo.Elements().Where(e => e.Name.LocalName == "Int32").Select(e => int.Parse(e.Value.Trim())).ToList();

		valores.ShouldBe(new[] { 1, 2, 3, 4, 5 });
		valores.First().ShouldBe(1);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas)]
	[InlineData(MateriaView)]
	public void El_combo_de_horas_apunta_al_arreglo_de_enteros_declarado_en_la_misma_vista(string vista)
	{
		var documento = Cargar(vista);
		var combo = Combos(documento).Single(c => ((string?) c.Attribute("SelectedItem") ?? string.Empty).Contains("Binding"));

		var origen = (string?) combo.Attribute("ItemsSource");

		origen.ShouldNotBeNull();
		Regex.IsMatch(origen!, @"StaticResource\s+(ResourceKey=)?horasCatedra").ShouldBeTrue();
		var arreglo = documento.Descendants().Single(e => e.Name.LocalName == "Array" && (string?) e.Attribute(Xaml + "Key") == "horasCatedra");
		((string?) arreglo.Attribute("Type")).ShouldNotBeNull();
		((string) arreglo.Attribute("Type")!).ShouldContain("Int32");
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas)]
	[InlineData(MateriaView)]
	[InlineData(GestionCursantes)]
	public void Ninguna_vista_de_W5_liga_SelectedIndex(string vista)
	{
		var enlaces = Cargar(vista).Descendants().Where(e => e.Attribute("SelectedIndex") is not null).ToList();

		enlaces.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ningun_SelectedIndex_de_la_capa_esta_ligado_a_un_dato_de_dominio_salvo_los_conocidos()
	{
		// Excepciones conocidas y preexistentes: "Tab" es de un TabControl (el indice ES la pestana) y "Sexo" funciona
		// porque Sexo.Femenino = 0 y Sexo.Masculino = 1 coinciden con el orden de Enum.GetNames (frágil, fuera de W5).
		var permitidos = new[] { "Tab", "Sexo" };
		var raiz = RaizDelRepositorio.Resolver("WPF_Desktop");
		var sospechosos = new List<string>();

		foreach (var archivo in Directory.EnumerateFiles(raiz, "*.xaml", SearchOption.AllDirectories)
										 .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
												  && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
		{
			foreach (Match coincidencia in Regex.Matches(File.ReadAllText(archivo), @"SelectedIndex=""\{\s*Binding\s+(?:Path=)?(\w+)"))
			{
				if (!permitidos.Contains(coincidencia.Groups[1].Value))
				{
					sospechosos.Add($"{Path.GetFileName(archivo)}: {coincidencia.Value}");
				}
			}
		}

		sospechosos.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_codigo_de_GestionCurriculasViewModel_no_suma_ni_resta_a_la_carga_horaria()
	{
		var codigo = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Cursos/Curriculas/GestionCurriculasViewModel.cs");

		Regex.IsMatch(codigo, @"(CargaHoraria|HorasCatedra)\s*[+\-]\s*\d").ShouldBeFalse();
		Regex.IsMatch(codigo, @"[+\-]\s*\d\s*\)?\s*horas", RegexOptions.IgnoreCase).ShouldBeFalse();
	}
	#endregion

	#region UI-08: un solo camino de alta de materia
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_solo_sitio_de_toda_la_capa_construye_RegistrarMateriaRequest()
	{
		var raiz = RaizDelRepositorio.Resolver("WPF_Desktop");
		var sitios = new List<string>();

		foreach (var archivo in Directory.EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories)
										 .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
												  && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
		{
			var lineas = File.ReadAllLines(archivo);

			for (var i = 0; i < lineas.Length; i++)
			{
				var linea = lineas[i].TrimStart();

				if (!linea.StartsWith("//") && linea.Contains("new RegistrarMateriaRequest("))
				{
					sitios.Add($"{Path.GetFileName(archivo)}:{i + 1}");
				}
			}
		}

		sitios.Count.ShouldBe(1);
		sitios[0].ShouldStartWith("GestionCurriculasViewModel.cs");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_dos_ViewModels_reescritos_no_usan_MessageBox_ni_async_void_ni_ViewModelCommand()
	{
		var rutas = new[]
		{
			"WPF_Desktop/ViewModels/Cursos/Curriculas/GestionCurriculasViewModel.cs",
			"WPF_Desktop/ViewModels/Cursos/Divisiones/GestionCursantesViewModel.cs",
		};

		foreach (var ruta in rutas)
		{
			var codigo = RaizDelRepositorio.Leer(ruta);

			// "MessageBox.Show" y no "MessageBox": ShouldNotContain ignora mayusculas y chocaria con la variable local messageBoxText.
			codigo.ShouldNotContain("MessageBox.Show");
			codigo.ShouldNotContain("async void");
			codigo.ShouldNotContain("ViewModelCommand");
			codigo.ShouldNotContain("IServicioDocente");
			codigo.ShouldNotContain("Guid.Empty");
		}
	}
	#endregion

	#region Estilos: el TargetType coincide con el tipo del elemento
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas)]
	[InlineData(MateriaView)]
	[InlineData(GestionCursantes)]
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

	#region Grillas: ItemsSource de cada una (el metodo que R4 valido)
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionCurriculas_cada_grilla_bindea_columnas_que_existen_en_su_tipo_de_fila()
	{
		var documento = Cargar(GestionCurriculas);

		var curriculas = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Curriculas");
		((string) curriculas.Attribute("ItemsSource")!).ShouldContain("Curriculas");
		var columnasCurricula = ColumnasBindeadas(curriculas);
		columnasCurricula.ShouldBe(new[] { "FechaInicio", "FechaInicio", "FechaFin", "EstaActiva" }); // sin "Materias": la elimino W3.B
		DebenExistirEn(typeof(CurriculaViewModel), columnasCurricula);

		var materias = documento.Descendants().Single(e => e.Name.LocalName == "ListView" && (string?) e.Attribute(Xaml + "Name") == "ListView_Materias");
		((string) materias.Attribute("ItemsSource")!).ShouldContain("Path=Materias");
		var columnasMateria = ColumnasBindeadas(materias);
		columnasMateria.ShouldBe(new[] { "Descripcion" });
		DebenExistirEn(typeof(MateriaViewModel), columnasMateria);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ItemsSource_de_las_grillas_de_GestionCurriculas_y_GestionCursantes_son_propiedades_del_ViewModel()
	{
		DebenExistirEn(typeof(GestionCurriculasViewModel), new[] { "Curriculas", "Materias", "Curricula", "Materia", "CargarCurriculasCommandAsync", "CargarMateriasCommandAsync", "NavigationCommand" });
		DebenExistirEn(typeof(GestionCursantesViewModel), new[] { "Cursantes", "Cursante", "Calificaciones", "CalificacionSeleccionada", "Materias", "Materia", "Calificacion", "ObservacionEdicion", "MostrarCalificacionView", "CicloLectivo", "CargarCommandAsync", "BuscarCommandAsync", "NuevaCalificacionCommand", "ModificarObservacionCommandAsync", "QuitarCalificacionCommandAsync", "GuardarCalificacionCommandAsync", "RegistrarInasistenciaCommandAsync", "CancelarCalificacionCommand" });
		DebenExistirEn(typeof(CalificacionViewModel), new[] { "Instancia", "Fecha", "Nota", "Observacion" });
	}

	private static void DebenExistirEn(Type tipo, IEnumerable<string> propiedades)
	{
		var faltantes = propiedades.Distinct().Where(p => tipo.GetProperty(p) is null).ToList();

		faltantes.ShouldBeEmpty($"propiedades bindeadas que {tipo.Name} no expone");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionCursantes_cada_grilla_bindea_columnas_que_existen_en_su_tipo_de_fila()
	{
		var documento = Cargar(GestionCursantes);

		var cursantes = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Cursantes");
		((string) cursantes.Attribute("ItemsSource")!).ShouldContain("Cursantes");
		var columnasCursante = ColumnasBindeadas(cursantes);
		columnasCursante.ShouldBe(new[] { "NombreCompleto", "Documento", "Edad" });
		DebenExistirEn(typeof(CursanteViewModel), columnasCursante);

		var calificaciones = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Calificaciones");
		((string) calificaciones.Attribute("ItemsSource")!).ShouldContain("Calificaciones");
		var columnasCalificacion = ColumnasBindeadas(calificaciones);
		columnasCalificacion.ShouldBe(new[] { "Materia", "Fecha", "Instancia", "Rindio", "Nota", "Aprobado", "Observacion" });
		DebenExistirEn(typeof(CalificacionViewModel), columnasCalificacion);
	}

	private static List<string> ColumnasBindeadas(XElement grilla)
	{
		var columnas = new List<string>();

		foreach (var columna in grilla.Descendants().Where(e => e.Name.LocalName.StartsWith("DataGrid") && e.Name.LocalName.EndsWith("Column") || e.Name.LocalName == "GridViewColumn"))
		{
			var enlace = (string?) columna.Attribute("Binding") ?? (string?) columna.Attribute("DisplayMemberBinding");

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
}
