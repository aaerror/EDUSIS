using Shouldly;
// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Alumnos;
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
	private const string PerfilAlumno = "WPF_Desktop/Views/Alumnos/PerfilAlumnoView.xaml";

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

	/// <summary>
	/// Ningún combo de horas cátedra sale de una lista fija declarada en el XAML.
	/// </summary>
	/// <remarks>
	/// Reemplaza a las dos guardas que exigían el <c>x:Array</c> de 1 a 5, porque ese arreglo era la
	/// causa de dos defectos distintos. En el ALTA ofrecía 5 horas con la currícula casi llena y el
	/// alta moría en <c>LimiteHorasSemanalesException</c>. En la EDICIÓN era peor: con el
	/// <c>SelectedItem</c> en <c>TwoWay</c>, una materia cuya carga no estuviera en la lista no
	/// encontraba su ítem y WPF le escribía <c>null</c> de vuelta, así que la materia quedaba
	/// inválida sola al abrir el editor y el botón de guardar —cuya visibilidad cuelga del
	/// <c>IsEnabled</c>— desaparecía sin explicación.
	///
	/// Que los ítems sigan siendo valores de dominio y no índices lo cubre
	/// <c>El_combo_de_horas_liga_SelectedItem_al_valor_y_no_SelectedIndex</c>.
	/// </remarks>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(GestionCurriculas)]
	[InlineData(MateriaView)]
	public void Ningun_combo_de_horas_sale_de_una_lista_fija(string vista)
	{
		var documento = Cargar(vista);

		documento.Descendants()
				 .Any(e => e.Name.LocalName == "Array" && (string?) e.Attribute(Xaml + "Key") == "horasCatedra")
				 .ShouldBeFalse($"{ vista } volvió a declarar la lista fija de horas cátedra");

		var combo = Combos(documento).Single(c => ((string?) c.Attribute("SelectedItem") ?? string.Empty).Contains("Binding"));

		((string?) combo.Attribute("ItemsSource")).ShouldNotBeNull();
		((string) combo.Attribute("ItemsSource")!).ShouldContain("OpcionesCargaHoraria");
	}

	/// <summary>
	/// El combo de la edición sale de la MATERIA, que es la única que puede garantizar que su propio
	/// valor esté entre las opciones.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_combo_de_la_edicion_ofrece_las_opciones_de_la_materia()
	{
		var combo = Combos(Cargar(MateriaView)).Single(c => ((string?) c.Attribute("SelectedItem") ?? string.Empty).Contains("HorasCatedra"));

		((string) combo.Attribute("ItemsSource")!).ShouldContain("OpcionesCargaHoraria");

		DebenExistirEn(typeof(MateriaViewModel), new[] { "OpcionesCargaHoraria", "HorasCatedra" });
	}

	/// <summary>
	/// El combo del alta sale del ViewModel y no de una lista fija.
	/// </summary>
	/// <remarks>
	/// Es la guarda del defecto que reemplazó al arreglo: con 38 de las 40 horas de la currícula
	/// consumidas, un combo fijo de 1 a 5 seguía ofreciendo 5 y el alta moría en
	/// <c>LimiteHorasSemanalesException</c>. Ofrecer una carga que no entra es ofrecer un error.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_combo_del_alta_ofrece_solo_las_horas_que_entran()
	{
		var documento = Cargar(GestionCurriculas);
		var combo = Combos(documento).Single(c => ((string?) c.Attribute("SelectedItem") ?? string.Empty).Contains("CargaHoraria"));

		((string) combo.Attribute("ItemsSource")!).ShouldContain("OpcionesCargaHoraria");

		documento.Descendants()
				 .Any(e => e.Name.LocalName == "Array" && (string?) e.Attribute(Xaml + "Key") == "horasCatedra")
				 .ShouldBeFalse("el alta ya no usa la lista fija de horas");

		DebenExistirEn(typeof(GestionCurriculasViewModel), new[] { "OpcionesCargaHoraria", "AdmiteOtraMateria", "EspaciosOcupados", "EspaciosMaximos", "HorasOcupadas", "HorasMaximas" });
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
		// "Vigente" reemplaza a "EstaActiva": la vigencia la decide el dominio y viaja en el response,
		// en vez de deducirse de que la fecha de fin sea nula. La columna además dejó de ser un
		// DataGridCheckBoxColumn de sólo lectura y pasó a Celda.Booleana, cuyo enlace entra por Tag.
		columnasCurricula.ShouldBe(new[] { "FechaInicio", "FechaInicio", "FechaFin", "Vigente" });
		DebenExistirEn(typeof(CurriculaViewModel), columnasCurricula);

		var materias = documento.Descendants().Single(e => e.Name.LocalName == "ListView" && (string?) e.Attribute(Xaml + "Name") == "ListView_Materias");
		((string) materias.Attribute("ItemsSource")!).ShouldContain("Path=Materias");
		var columnasMateria = ColumnasBindeadas(materias);
		// La carga horaria es la mitad del dato: es lo que suma contra el tope de 40 horas de la
		// currícula, y sin la columna no había forma de saber a qué materia bajarle horas.
		columnasMateria.ShouldBe(new[] { "Descripcion", "HorasCatedra" });
		DebenExistirEn(typeof(MateriaViewModel), columnasMateria);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ItemsSource_de_las_grillas_de_GestionCurriculas_y_GestionCursantes_son_propiedades_del_ViewModel()
	{
		DebenExistirEn(typeof(GestionCurriculasViewModel), new[] { "Curriculas", "Materias", "Curricula", "Materia", "CargarCurriculasCommandAsync", "CargarMateriasCommandAsync", "NavigationCommand" });
		DebenExistirEn(typeof(GestionCursantesViewModel), new[] { "Cursantes", "Cursante", "Calificaciones", "CalificacionSeleccionada", "Materias", "Materia", "Calificacion", "ObservacionEdicion", "MostrarCalificacionView", "CicloLectivo", "CargarCommandAsync", "BuscarCommandAsync", "NuevaCalificacionCommand", "ModificarObservacionCommandAsync", "QuitarCalificacionCommandAsync", "GuardarCalificacionCommandAsync", "RegistrarInasistenciaCommandAsync", "CancelarCalificacionCommand",
			// UI-CUR-1: la sección de inscripción y el historial de divisiones.
			"EsRecursante", "FechaBaja", "FechaCambio", "DivisionDestino", "DivisionesDestino", "Historial", "ModificarRecursanteCommandAsync", "RegistrarBajaCommandAsync", "CambiarDivisionCommandAsync", "CargarHistorialCommandAsync" });
		// "Periodo" y "EsRecuperatorio" son los dos campos que EDITA el formulario; "Instancia" es la
		// lectura de ambos que muestra la grilla. Los tres tienen que existir: el formulario bindea
		// los primeros en TwoWay y un error de nombre ahí no rompe el build, deja el campo muerto.
		DebenExistirEn(typeof(CalificacionViewModel), new[] { "Periodo", "EsRecuperatorio", "Instancia", "Fecha", "Nota", "Observacion" });
	}

	private static void DebenExistirEn(Type tipo, IEnumerable<string> propiedades)
	{
		var faltantes = propiedades.Distinct().Where(p => tipo.GetProperty(p) is null).ToList();

		faltantes.ShouldBeEmpty($"propiedades bindeadas que {tipo.Name} no expone");
	}

	/// <summary>
	/// La tabla de materias del perfil bindea propiedades que <c>MateriaCursadaViewModel</c> expone.
	/// </summary>
	/// <remarks>
	/// Sus columnas son <c>GridViewColumn</c> con <c>CellTemplate</c>, así que no declaran
	/// <c>Binding</c> y <see cref="ColumnasBindeadas"/> no las ve: hay que buscar los <c>Path</c> en
	/// el texto. Es exactamente la clase de error que no rompe el build y deja la celda vacía, y acá
	/// nacieron cinco bindings de golpe al reemplazar la columna única "PROMEDIO" por una por
	/// período. La guarda falla en los dos sentidos: si el XAML deja de bindear una, y si el
	/// ViewModel deja de exponerla.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void PerfilAlumno_la_tabla_de_materias_bindea_propiedades_que_el_ViewModel_expone()
	{
		var propiedades = new[]
		{
			"Materia", "PrimerCuatrimestre", "SegundoCuatrimestre", "Final", "Recuperacion",
			"NotaDeCierre", "Instancias", "Estado"
		};

		var xaml = File.ReadAllText(RaizDelRepositorio.Resolver(PerfilAlumno));

		var sinBindear = propiedades.Where(p => !Regex.IsMatch(xaml, $@"Path={ p }\b")).ToList();
		sinBindear.ShouldBeEmpty("propiedades que la tabla de materias del perfil dejó de bindear");

		DebenExistirEn(typeof(MateriaCursadaViewModel), propiedades);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionCursantes_cada_grilla_bindea_columnas_que_existen_en_su_tipo_de_fila()
	{
		var documento = Cargar(GestionCursantes);

		var cursantes = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Cursantes");
		((string) cursantes.Attribute("ItemsSource")!).ShouldContain("Cursantes");
		var columnasCursante = ColumnasBindeadas(cursantes);

		// "Edad" se fue de la grilla al pasar la pantalla a índice + detalle: el índice es una
		// columna angosta al costado y lo único que se busca ahí es el nombre. La edad y el
		// documento se leen enteros en la ficha del alumno elegido (CursanteView), que es
		// donde el detalle los muestra. El documento se queda también acá porque es el
		// desempate cuando dos alumnos se llaman igual.
		columnasCursante.ShouldBe(new[] { "NombreCompleto", "Documento" });
		DebenExistirEn(typeof(CursanteViewModel), columnasCursante);
		DebenExistirEn(typeof(CursanteViewModel), new[] { "Edad", "EsRecursante" });

		var calificaciones = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Calificaciones");
		((string) calificaciones.Attribute("ItemsSource")!).ShouldContain("Calificaciones");
		var columnasCalificacion = ColumnasBindeadas(calificaciones);
		columnasCalificacion.ShouldBe(new[] { "Materia", "Fecha", "Instancia", "Rindio", "Nota", "Aprobado", "Observacion" });
		DebenExistirEn(typeof(CalificacionViewModel), columnasCalificacion);

		// UI-CUR-1. "Estado" no es columna bindeada sino el texto de la insignia, así que se
		// verifica aparte: la columna es un DataGridTemplateColumn y no declara Binding.
		var historial = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_Historial");
		((string) historial.Attribute("ItemsSource")!).ShouldContain("Historial");
		var columnasHistorial = ColumnasBindeadas(historial);
		columnasHistorial.ShouldBe(new[] { "CicloLectivo", "Division", "Grado", "FechaInicio", "FechaFin", "EsRecursante" });
		DebenExistirEn(typeof(InscripcionHistoricaViewModel), columnasHistorial);
		DebenExistirEn(typeof(InscripcionHistoricaViewModel), new[] { "Estado", "EstaActiva" });
	}

	private static List<string> ColumnasBindeadas(XElement grilla)
	{
		var columnas = new List<string>();

		foreach (var columna in grilla.Descendants().Where(e => e.Name.LocalName.StartsWith("DataGrid") && e.Name.LocalName.EndsWith("Column") || e.Name.LocalName == "GridViewColumn"))
		{
			// Una columna de plantilla no declara Binding. Las booleanas —RINDIÓ, APROBADO,
			// RECURSANTE— dibujan un glifo cuyo estado entra por el Tag, así que el enlace
			// está ahí: sin leerlo, cambiar "Rindio" por un nombre que CalificacionViewModel
			// no expone dejaba de detectarlo nadie, y la columna se dibujaría siempre en "no".
			var enlace = (string?) columna.Attribute("Binding")
					  ?? (string?) columna.Attribute("DisplayMemberBinding")
					  ?? columna.Descendants().Select(e => (string?) e.Attribute("Tag")).FirstOrDefault(tag => tag is not null);

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
