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
	private const string GestionDivisionesXaml = "WPF_Desktop/Views/Cursos/Divisiones/GestionDivisionesView.xaml";
	private const string BuscarXaml = "WPF_Desktop/Views/Shared/Modal/BuscarView.xaml";
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
	/// <remarks>
	/// LA GUARDA SIGUE AL ARTEFACTO A SU LUGAR NUEVO, igual que la del buscador de docentes cuando se
	/// mudó al modal. El bloque dejó de ser una tarjeta propia con un <c>SituacionRevistaView</c>
	/// deshabilitado adentro —que mostraba exactamente lo mismo que el detalle de la grilla, porque la
	/// carga deja preseleccionada justo la fila en funciones— y pasó a ser la ficha del encabezado de
	/// la tarjeta de designaciones, escrita con los estilos de texto del tema.
	///
	/// Lo que la guarda audita no cambió: que el bloque muestre <c>SituacionRevistaEnFunciones</c> y no
	/// <c>SituacionRevistaUPDATE</c>, que es el defecto que la trajo al mundo.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_bloque_Docente_en_Funciones_tiene_como_DataContext_SituacionRevistaEnFunciones()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);

		var coincidencia = Regex.Match(xaml, @"DOCENTE EN FUNCIONES-->.*?DataContext=""\{\s*Binding\s+Path=(\w+)", RegexOptions.Singleline);

		coincidencia.Success.ShouldBeTrue();
		coincidencia.Groups[1].Value.ShouldBe("SituacionRevistaEnFunciones");
		coincidencia.Groups[1].Value.ShouldNotBe("SituacionRevistaUPDATE");
	}

	/// <remarks>
	/// Cuelga de la <c>Visibility</c> y ya no del <c>IsEnabled</c> de un <c>Border</c>: la ficha se
	/// dibuja SIEMPRE, y la bandera elige cuál de sus dos caras: quién está en aula, o que no hay
	/// nadie. Antes la bandera apagaba la tarjeta entera, de modo que una cátedra sin docente frente
	/// al curso —el estado que hay que resolver— no mostraba absolutamente nada.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_bloque_Docente_en_Funciones_se_habilita_con_HabilitarDocenteEnFunciones()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);

		var coincidencia = Regex.Match(xaml, @"DOCENTE EN FUNCIONES-->.*?Visibility=""\{\s*Binding\s+Path=(\w+)", RegexOptions.Singleline);

		coincidencia.Success.ShouldBeTrue();
		coincidencia.Groups[1].Value.ShouldBe("HabilitarDocenteEnFunciones");
	}

	/// <summary>
	/// La ficha "en aula" dice también que NO hay nadie en aula.
	/// </summary>
	/// <remarks>
	/// Es el motivo del rediseño del bloque y no se deduce del enlace: las dos caras cuelgan de la
	/// misma bandera, una con <c>visibilidad</c> y la otra con <c>visibilidadInversa</c>.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_bloque_Docente_en_Funciones_tiene_tambien_su_cara_vacia()
	{
		var xaml = RaizDelRepositorio.Leer(GestionSituacionRevistaXaml);

		var bloque = Regex.Match(xaml, @"DOCENTE EN FUNCIONES-->(.*?)#endregion", RegexOptions.Singleline);

		bloque.Success.ShouldBeTrue();
		bloque.Groups[1].Value.ShouldContain("HabilitarDocenteEnFunciones, Mode=OneWay, Converter={ StaticResource ResourceKey=visibilidad }");
		bloque.Groups[1].Value.ShouldContain("HabilitarDocenteEnFunciones, Mode=OneWay, Converter={ StaticResource ResourceKey=visibilidadInversa }");
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
		// "Vigente", "ReemplazaADocente" y "ExigeFechaFin" son de SituacionRevistaViewModel: las usan la
		// insignia de estado, la columna de la cadena de suplencias y el aviso de fecha de fin obligatoria.
		// "FechaInicio" y "FechaFin" entran con la ficha de "en aula" y con el detalle de la fila elegida,
		// que pasaron de un formulario deshabilitado a texto y a Dato: son las dos únicas cosas que el
		// detalle agregaba a la grilla.
		var delItem = new HashSet<string> { "Cargo", "Estado", "Docente", "EnFunciones", "FechaAlta", "NombreCompleto", "EstaActivo", "IsEnabled", "Foreground", "SelectedItem", "SelectedDate", "Descripcion",
											"Vigente", "ReemplazaADocente", "ExigeFechaFin", "FechaInicio", "FechaFin" };
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
		// "Vigente" entra con la insignia de estado —vigente contra finalizada se distinguía sólo
		// leyendo la palabra— y "ReemplazaADocente" con la cadena de suplencias, que se escribía en
		// el alta y no se mostraba en ninguna parte. "EnFunciones" pasó de casilla de sólo lectura a
		// Celda.Booleana, cuyo enlace entra por Tag.
		columnas.ShouldBe(new[] { "Cargo", "Vigente", "Estado", "Docente", "ReemplazaADocente", "EnFunciones" });
		DebenExistirEn(typeof(SituacionRevistaViewModel), columnas);
	}

	/// <summary>
	/// La grilla de resultados de la búsqueda de docentes se mudó: estaba escrita dos veces, dentro de
	/// <c>GestionSituacionRevistaView</c> y de <c>GestionDivisionesView</c>, y ahora vive una sola vez en
	/// <c>BuscarView</c>, el contenido del modal. La guarda sigue al mismo artefacto a su lugar nuevo:
	/// lo que audita —que las columnas bindeadas existan en <see cref="LegajoDocenteViewModel"/>— no cambió.
	///
	/// EL ORDEN ES PARTE DE LA GUARDA. La grilla abría con "DESDE" —la fecha de alta del legajo—, que
	/// es trazabilidad administrativa y no interviene en la decisión de elegir un docente: el dato
	/// que se busca quedaba segundo. Ahora abre con el nombre y sigue con el legajo, que es como la
	/// escuela nombra a su gente.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void DataGrid_LegajosDocentes_bindea_columnas_que_existen_en_LegajoDocenteViewModel()
	{
		var documento = Cargar(BuscarXaml);

		var grilla = documento.Descendants().Single(e => e.Name.LocalName == "DataGrid" && (string?) e.Attribute(Xaml + "Name") == "DataGrid_LegajosDocentes");
		((string) grilla.Attribute("ItemsSource")!).ShouldContain("Path=Resultados");

		var columnas = ColumnasBindeadas(grilla);
		// "EstaActivo" aparece ahora que la guarda también lee las columnas de plantilla: la columna
		// del glifo booleano existía desde antes, lo que faltaba era que el helper la viera.
		columnas.ShouldBe(new[] { "NombreCompleto", "Legajo", "DocumentoNacionalIdentidad", "EstaActivo" });
		DebenExistirEn(typeof(LegajoDocenteViewModel), columnas);
	}

	/// <summary>
	/// "EN FUNCIONES" era una <c>DataGridCheckBoxColumn</c> de sólo lectura: una casilla que no se
	/// puede tildar miente sobre lo que permite. Pasa al glifo de <c>Celda.Booleana</c>, que es como
	/// se dibujan los booleanos de las grillas en toda la aplicación, y por eso ya no aparece entre
	/// las columnas con atributo <c>Binding</c>: su valor viaja por <c>Tag</c>.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_estado_en_funciones_se_dibuja_con_el_glifo_booleano_y_no_con_una_casilla()
	{
		var documento = Cargar(BuscarXaml);

		documento.Descendants()
				 .Any(e => e.Name.LocalName == "DataGridCheckBoxColumn")
				 .ShouldBeFalse("BuscarView volvió a dibujar un booleano con una casilla de sólo lectura");

		var glifo = documento.Descendants()
							 .Single(e => e.Name.LocalName == "IconBlock"
									   && ((string?) e.Attribute("Style") ?? string.Empty).Contains("Celda.Booleana"));

		((string) glifo.Attribute("Tag")!).ShouldContain("Path=EstaActivo");
		DebenExistirEn(typeof(LegajoDocenteViewModel), new List<string> { "EstaActivo" });
	}

	/// <summary>
	/// Las dos pantallas que antes traían su propia copia del buscador no pueden volver a tenerla: el
	/// bloque duplicado era campo + lupa + grilla de resultados, y ahora lo provee el modal.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ninguna_pantalla_vuelve_a_traer_su_propia_grilla_de_busqueda_de_docentes()
	{
		foreach (var ruta in new[] { GestionSituacionRevistaXaml, GestionDivisionesXaml })
		{
			var documento = Cargar(ruta);

			var grillas = documento.Descendants()
								   .Where(e => e.Name.LocalName == "DataGrid")
								   .Select(e => (string?) e.Attribute(Xaml + "Name"))
								   .ToList();

			grillas.ShouldNotContain("DataGrid_LegajosDocentes", $"{ ruta } volvió a escribir el buscador que vive en el modal");
		}
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

	/// <summary>
	/// Ninguna vista declara una fila <c>*</c> adentro de un <c>GroupBox</c>.
	/// </summary>
	/// <remarks>
	/// LA PLANTILLA DEL ESTILO "Seccion" ENVUELVE SU CONTENIDO EN UN <c>StackPanel</c>, y un
	/// <c>StackPanel</c> mide a sus hijos con alto INFINITO. Adentro de él una fila <c>*</c> no acota
	/// nada: degenera al alto total del contenido, la lista crece con todas sus filas —sin barra de
	/// desplazamiento, porque nunca le falta lugar— y lo que venga debajo queda empujado fuera del
	/// contenedor y recortado.
	///
	/// Pasó con la lista de materias del diseño curricular: su <c>MaxHeight</c> tapaba el problema,
	/// y al sacarlo desaparecieron de la pantalla el editor de la materia y toda la barra de
	/// acciones, incluido "Editar materia". Una sección que necesita una fila <c>*</c> dibuja su
	/// regla y su rótulo a mano, que son dos elementos, en vez de usar el <c>GroupBox</c>.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ninguna_vista_pone_una_fila_estrella_adentro_de_un_GroupBox()
	{
		var vistas = Directory.EnumerateFiles(RaizDelRepositorio.Resolver("WPF_Desktop"), "*.xaml", SearchOption.AllDirectories)
							  .Where(ruta => !ruta.Contains($"{ Path.DirectorySeparatorChar }obj{ Path.DirectorySeparatorChar }")
										  && !ruta.Contains($"{ Path.DirectorySeparatorChar }bin{ Path.DirectorySeparatorChar }")
										  && !ruta.Contains($"{ Path.DirectorySeparatorChar }Theme{ Path.DirectorySeparatorChar }"));

		var infractoras = new List<string>();

		foreach (var ruta in vistas)
		{
			var documento = XDocument.Parse(File.ReadAllText(ruta));

			var hayFilaEstrella = documento.Descendants()
										   .Where(e => e.Name.LocalName == "GroupBox")
										   .SelectMany(grupo => grupo.Descendants())
										   .Any(e => e.Name.LocalName == "RowDefinition"
												  && ((string?) e.Attribute("Height"))?.Trim() == "*");

			if (hayFilaEstrella)
			{
				infractoras.Add(Path.GetFileName(ruta));
			}
		}

		infractoras.ShouldBeEmpty("una fila * adentro de un GroupBox no acota nada: el StackPanel de su plantilla mide con alto infinito");
	}

	/// <summary>
	/// Las propiedades que cada columna de la grilla enlaza, en orden de documento.
	/// </summary>
	/// <remarks>
	/// LEÍA SÓLO EL ATRIBUTO <c>Binding</c>, y eso dejaba ciega a la guarda frente a las columnas de
	/// plantilla: una <c>DataGridTemplateColumn</c> no declara <c>Binding</c> —sus enlaces viven
	/// adentro, en el <c>Tag</c> de un glifo o en la <c>Visibility</c> de una insignia—, así que
	/// renombrar una propiedad que una de ellas usa no lo detectaba nadie y la celda se dibujaba
	/// vacía o siempre en "no". Ahora recoge todo <c>Path=</c> que aparezca en cualquier atributo de
	/// la columna, que es lo que hace falta para que la guarda cubra lo que dice cubrir.
	/// </remarks>
	private static List<string> ColumnasBindeadas(XElement grilla)
	{
		var columnas = new List<string>();

		foreach (var columna in grilla.Descendants().Where(e => e.Name.LocalName.StartsWith("DataGrid") && e.Name.LocalName.EndsWith("Column")))
		{
			var enlaceDirecto = (string?) columna.Attribute("Binding");

			if (enlaceDirecto is not null)
			{
				var coincidencia = Regex.Match(enlaceDirecto, @"Path=(\w+)");

				if (coincidencia.Success)
				{
					columnas.Add(coincidencia.Groups[1].Value);
				}

				continue;
			}

			// Columna de plantilla: los enlaces están en los atributos de sus descendientes.
			var deLaPlantilla = columna.Descendants()
									   .SelectMany(elemento => elemento.Attributes())
									   .Select(atributo => Regex.Match(atributo.Value, @"Path=(\w+)"))
									   .Where(coincidencia => coincidencia.Success)
									   .Select(coincidencia => coincidencia.Groups[1].Value)
									   .Distinct();

			columnas.AddRange(deLaPlantilla);
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
	public void D3_las_dos_bajas_del_docente_se_esperan_con_await_y_sus_metodos_devuelven_Task()
	{
		// `QuitarDocenteAsync` se partió en dos el 2026-11-06 (CORE-24): decía "quitar" y
		// desafectaba, con el borrado real comentado al lado. Esta guarda cubría ese único método;
		// ahora cubre los dos, y además fija que sigan siendo dos. Volver a juntarlos la rompe.
		var fuente = RaizDelRepositorio.Leer(GestionDocentesCs);

		fuente.ShouldContain("await _servicioDocentes.DesafectarDocenteAsync(");
		fuente.ShouldContain("await _servicioDocentes.EliminarDocenteAsync(");
		fuente.ShouldContain("private async Task ExecuteDesafectarCommandAsync()");
		fuente.ShouldContain("private async Task ExecuteEliminarCommandAsync()");
		fuente.ShouldNotContain("private void ExecuteEliminarCommand");
		fuente.ShouldNotContain("private void ExecuteDesafectarCommand");
		fuente.ShouldNotMatch(@"(?<!await )_servicioDocentes\.DesafectarDocenteAsync\(");
		fuente.ShouldNotMatch(@"(?<!await )_servicioDocentes\.EliminarDocenteAsync\(");

		// Y que el impacto se consulte antes de preguntar: sin await acá el diálogo se armaría con
		// un Task en vez de con las cifras.
		fuente.ShouldContain("await _servicioDocentes.ConsultarImpactoEliminacionDocenteAsync(");

		typeof(GestionDocentesViewModel).GetProperty("EliminarCommand")!.PropertyType.Name.ShouldBe("IAsyncRelayCommand");
		typeof(GestionDocentesViewModel).GetProperty("DesafectarCommand")!.PropertyType.Name.ShouldBe("IAsyncRelayCommand");
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
			"RelevarDeFuncionesAsync", "PasarAInterinoAsync", "FinalizarDesignacionAsync"
		})
		{
			codigo.ShouldContain($"_servicioCatedra.{metodo}(");
		}
	}
	#endregion
}
