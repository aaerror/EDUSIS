using Shouldly;
// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WPF_Desktop.UnitTests.Dobles;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Guardas de la pasada de UI/UX: lo que se unificó en todas las pantallas y que ningún
/// ViewModel puede afirmar por su cuenta.
/// </summary>
/// <remarks>
/// <para>
/// Las cuatro cosas que esta pasada dejó parejas —de dónde sale el feedback, qué pregunta antes
/// de destruir, cómo se encabeza una pantalla y que la validación se vea— son convenciones de
/// toda la capa, no de un archivo. Sin una guarda, la siguiente pantalla que se escriba vuelve
/// a elegir por su cuenta: así aparecieron los dos caminos de confirmación, los tres idiomas de
/// encabezado y los cinco avisos distintos que había.
/// </para>
/// <para>
/// Se leen del código fuente y no por reflexión porque lo que se audita es la FORMA del XAML y
/// de los ViewModels, que no deja rastro en los tipos compilados.
/// </para>
/// </remarks>
public class GuardasEstaticasUXTests
{
	private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

	/// <summary>
	/// Las pantallas de la aplicación: las que ocupan el área de contenido de MainWindow y
	/// tienen encabezado propio.
	/// </summary>
	/// <remarks>
	/// Quedan afuera a propósito: los sub-formularios que se montan dentro de otra pantalla
	/// (InformacionPersonalView, DomicilioView, ContactoView, LegajoDocenteView,
	/// PuestoDocenteView, LicenciaView, MateriaView, SituacionRevistaView, CursoView,
	/// DivisionView, CursanteView), el contenido del modal (BuscarView) y las dos vistas del
	/// panel de acceso (LoginUsuarioView, RegistrarUsuarioView), que viven en una ventana de
	/// 430px donde una ruta y un hueco de acciones no tienen sentido.
	/// </remarks>
	public static readonly string[] Pantallas =
	{
		"WPF_Desktop/Views/Alumnos/GestionAlumnosView.xaml",
		"WPF_Desktop/Views/Alumnos/InscripcionAlumnoView.xaml",
		"WPF_Desktop/Views/Alumnos/PerfilAlumnoView.xaml",
		"WPF_Desktop/Views/Alumnos/RegistrarAlumnoView.xaml",
		"WPF_Desktop/Views/Cursos/GestionCursosView.xaml",
		"WPF_Desktop/Views/Cursos/RegistrarCursoView.xaml",
		"WPF_Desktop/Views/Cursos/Curriculas/GestionCurriculasView.xaml",
		"WPF_Desktop/Views/Cursos/Curriculas/Materias/Catedras/GestionCatedrasView.xaml",
		"WPF_Desktop/Views/Cursos/Curriculas/Materias/Catedras/GestionHorariosView.xaml",
		"WPF_Desktop/Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml",
		"WPF_Desktop/Views/Cursos/Divisiones/GestionAsistenciasView.xaml",
		"WPF_Desktop/Views/Cursos/Divisiones/GestionCursantesView.xaml",
		"WPF_Desktop/Views/Cursos/Divisiones/GestionDivisionesView.xaml",
		"WPF_Desktop/Views/Docentes/GestionDocentesView.xaml",
		"WPF_Desktop/Views/Docentes/PerfilDocenteView.xaml",
		"WPF_Desktop/Views/Docentes/RegistrarDocenteView.xaml",
		"WPF_Desktop/Views/Docentes/Licencias/GestionLicenciasView.xaml",
		"WPF_Desktop/Views/Docentes/Puestos/GestionPuestosView.xaml",
		"WPF_Desktop/Views/Documentacion/GestionDocumentacionView.xaml",
		"WPF_Desktop/Views/Reportes/GestionReportesView.xaml",
	};

	public static TheoryData<string> CadaPantalla()
	{
		var datos = new TheoryData<string>();

		foreach (var pantalla in Pantallas)
		{
			datos.Add(pantalla);
		}

		return datos;
	}

	#region El feedback sale por IDialogService
	/// <summary>
	/// Ningún ViewModel abre un <c>MessageBox</c>.
	/// </summary>
	/// <remarks>
	/// Había 102 llamadas repartidas en trece ViewModels, en paralelo con los once que ya usaban
	/// <c>IDialogService</c>. Las consecuencias no eran estéticas: un <c>MessageBox</c> no se
	/// puede doblar en una prueba —por eso el flujo de guardado del alta de alumno estaba
	/// cubierto "por guarda sobre el código fuente" y no por ejecución— y bloquea el hilo de la
	/// interfaz hasta que alguien lo despacha a mano.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ningun_ViewModel_abre_un_MessageBox()
	{
		var infractores = ArchivosDeViewModels()
			.Where(archivo => CodigoVivo(archivo).Contains("MessageBox.Show"))
			.Select(NombreCorto)
			.ToList();

		infractores.ShouldBeEmpty("ViewModels que llaman a MessageBox.Show en lugar de IDialogService");
	}

	/// <summary>
	/// Todo ViewModel que llame a un caso de uso destructivo pregunta antes.
	/// </summary>
	/// <remarks>
	/// Se mira la llamada al servicio de Core y no el nombre del comando: lo que destruye datos
	/// es lo que viaja a <c>Core</c>, y un comando puede llamarse de cualquier manera. Reabrir
	/// una planilla y marcar a alguien como recursante no entran por este nombre —no borran
	/// nada— pero también confirman, porque cambian un documento ya firmado o impreso.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_ViewModel_que_destruye_algo_pregunta_antes()
	{
		var destructivo = new Regex(@"\.(Eliminar|Quitar|Revocar|DarDeBaja|Finalizar|Rescindir|RelevarDe)\w*Async\(");

		var infractores = ArchivosDeViewModels()
			.Select(archivo => (Nombre: NombreCorto(archivo), Codigo: CodigoVivo(archivo)))
			.Where(x => destructivo.IsMatch(x.Codigo) && !x.Codigo.Contains("ConfirmarAccion("))
			.Select(x => x.Nombre)
			.ToList();

		infractores.ShouldBeEmpty("ViewModels que llaman a un caso de uso destructivo sin confirmar");
	}

	/// <summary>
	/// Toda confirmación destructiva explica la consecuencia.
	/// </summary>
	/// <remarks>
	/// <see cref="WPF_Desktop.Shared.SolicitudDeDialogo.Destructiva"/> exige la consecuencia por
	/// firma, así que la guarda comprueba lo que la firma no puede: que no se pase vacía ni con
	/// un texto de relleno.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ninguna_confirmacion_destructiva_deja_la_consecuencia_vacia()
	{
		var vacias = new Regex(@"consecuencia:\s*(""""|\$""""|""\s+"")");

		var infractores = ArchivosDeViewModels()
			.Where(archivo => vacias.IsMatch(CodigoVivo(archivo)))
			.Select(NombreCorto)
			.ToList();

		infractores.ShouldBeEmpty("confirmaciones destructivas sin consecuencia");
	}
	#endregion

	#region El encabezado de pantalla es uno solo
	[Theory]
	[Trait("Categoria", "Unidad")]
	[MemberData(nameof(CadaPantalla))]
	public void Cada_pantalla_se_encabeza_con_el_componente_Pagina(string pantalla)
	{
		var xaml = RaizDelRepositorio.Leer(pantalla);

		xaml.ShouldContain("<components:PaginaComponent", Case.Sensitive);
	}

	/// <summary>
	/// El componente Header ya no existe: su reemplazo es Pagina, que además lleva la ruta y el
	/// hueco de la acción principal.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ninguna_vista_vuelve_al_componente_Header()
	{
		var infractores = ArchivosDeVistas()
			.Where(archivo => XamlVivo(archivo).Contains("<components:Header"))
			.Select(NombreCorto)
			.ToList();

		infractores.ShouldBeEmpty("vistas que usan el componente Header, reemplazado por Pagina");
	}

	/// <summary>
	/// El título de una pantalla es el nombre de la pantalla, no el dato que muestra.
	/// </summary>
	/// <remarks>
	/// Cátedras, Horarios y Situación de revista ligaban su título a <c>Materia.Descripcion</c>:
	/// las tres se llamaban igual y ninguna decía qué era. El dato va en la ruta.
	/// </remarks>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[MemberData(nameof(CadaPantalla))]
	public void El_titulo_de_la_pantalla_es_un_texto_y_no_un_enlace_a_datos(string pantalla)
	{
		foreach (var pagina in Cargar(pantalla).Descendants().Where(e => e.Name.LocalName == "PaginaComponent"))
		{
			var titulo = (string?) pagina.Attribute("Titulo");

			titulo.ShouldNotBeNullOrWhiteSpace();
			titulo!.ShouldNotContain("{", Case.Sensitive);
		}
	}
	#endregion

	#region La validación se ve
	/// <summary>
	/// Ninguna vista apaga el adorno de validación.
	/// </summary>
	/// <remarks>
	/// <c>Validation.ErrorTemplate="{x:Null}"</c> apaga la marca de error de TODO el sub-árbol.
	/// Estaba puesto en los dos altas de dos pasos y en el formulario de puestos, o sea
	/// justamente en los formularios largos: el campo con problema no se marcaba y el error
	/// recién aparecía al guardar.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ninguna_vista_apaga_el_adorno_de_validacion()
	{
		var apagado = new Regex(@"Validation\.ErrorTemplate\s*=\s*""\{\s*x:Null\s*\}""");

		var infractores = ArchivosDeVistas()
			.Where(archivo => apagado.IsMatch(XamlVivo(archivo)))
			.Select(NombreCorto)
			.ToList();

		// MainWindow y StartupWindow sí lo apagan, y con razón: el ContentControl que hospeda la
		// pantalla no es un campo, y el adorno rojo de WPF le quedaría alrededor de todo.
		infractores.ShouldBeEmpty("vistas que apagan el adorno de validación de sus campos");
	}
	#endregion

	#region Las listas dicen qué hacer cuando están vacías
	/// <summary>
	/// Toda grilla y toda lista del sistema definen su estado vacío.
	/// </summary>
	/// <remarks>
	/// Los estilos <c>Grilla</c> y <c>Lista.Indice</c> dibujan el texto de <c>Tag</c> cuando no
	/// hay filas. Sin él, una lista vacía y una lista que todavía no cargó se ven igual.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Toda_grilla_y_lista_del_sistema_declara_su_estado_vacio(){
		var sinEstadoVacio = new List<string>();

		foreach (var archivo in ArchivosDeVistas())
		{
			foreach (var lista in XDocument.Load(archivo).Descendants()
											.Where(e => e.Name.LocalName is "DataGrid" or "ListView"))
			{
				var estilo = (string?) lista.Attribute("Style") ?? string.Empty;

				if (!estilo.Contains("Grilla") && !estilo.Contains("Lista.Indice"))
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace((string?) lista.Attribute("Tag")))
				{
					sinEstadoVacio.Add($"{ NombreCorto(archivo) }: { (string?) lista.Attribute(Xaml + "Name") ?? lista.Name.LocalName }");
				}
			}
		}

		sinEstadoVacio.ShouldBeEmpty("listas sin texto de estado vacío en su Tag");
	}
	#endregion

	#region El grado se dice una sola vez
	/// <summary>
	/// El grado se dice como lo declara el dominio, no armando el texto a mano.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>Grado</c> es un enum cuyos miembros se llaman <c>Primero</c>, <c>Segundo</c>…, y los
	/// ViewModels guardan ese NOMBRE como cadena. Diecisiete lugares le concatenaban el símbolo
	/// de grado —<c>$"{ Curso }° año"</c> en los ViewModels, <c>StringFormat={}{0}° Año</c> en
	/// las vistas— y en pantalla salía "Primero° año" y "Primero° PRIMARIA · DIVISIONES".
	/// </para>
	/// <para>
	/// El símbolo pegado a una interpolación o a un marcador de <c>StringFormat</c> es la forma
	/// del defecto: ningún otro dato de la aplicación lleva ese símbolo. Quien lo necesite tiene
	/// <c>Grados.Decir</c>, que lee el texto que el propio enum declara.
	/// </para>
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Nadie_pega_el_simbolo_de_grado_a_un_dato()
	{
		var pegado = new Regex(@"\}\s*[°º]|\{0\}\s*[°º]");

		var infractores = ArchivosDeViewModels()
						  .Concat(ArchivosBajo("WPF_Desktop/Shared", ".cs"))
						  .Where(archivo => pegado.IsMatch(CodigoVivo(archivo)))
						  .Concat(ArchivosDeVistas().Where(archivo => pegado.IsMatch(XamlVivo(archivo))))
						  .Select(NombreCorto)
						  .OrderBy(n => n)
						  .ToList();

		infractores.ShouldBeEmpty("archivos que arman el texto del grado a mano en lugar de pedírselo a Grados.Decir");
	}

	/// <summary>El texto del grado sale del <c>[Description]</c> que declara el dominio.</summary>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Primero", "1° Año")]
	[InlineData("Septimo", "7° Año")]
	[InlineData("NoEsUnGrado", "")]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void El_grado_se_dice_como_lo_declara_el_dominio(string? nombre, string esperado) =>
		global::WPF_Desktop.Shared.Grados.Decir(nombre).ShouldBe(esperado);
	#endregion

	#region Infraestructura
	private static XDocument Cargar(string rutaRelativa) =>
		XDocument.Load(RaizDelRepositorio.Resolver(rutaRelativa));

	private static IEnumerable<string> ArchivosDeViewModels() =>
		ArchivosBajo("WPF_Desktop/ViewModels", ".cs");

	private static IEnumerable<string> ArchivosDeVistas() =>
		ArchivosBajo("WPF_Desktop/Views", ".xaml");

	private static IEnumerable<string> ArchivosBajo(string carpeta, string extension)
	{
		var raiz = RaizDelRepositorio.Resolver(carpeta);
		var separador = Path.DirectorySeparatorChar;

		return Directory.EnumerateFiles(raiz, $"*{ extension }", SearchOption.AllDirectories)
						.Where(f => !f.Contains($"{separador}obj{separador}") && !f.Contains($"{separador}bin{separador}"));
	}

	private static string NombreCorto(string ruta) =>
		Path.GetFileName(ruta);

	/// <summary>
	/// Devuelve el XAML sin sus comentarios.
	/// </summary>
	/// <remarks>
	/// Hace falta porque varias vistas explican EN UN COMENTARIO qué línea se sacó y por qué
	/// —"sin Validation.ErrorTemplate=…"—, y una guarda que busca texto no distingue la
	/// explicación del defecto.
	/// </remarks>
	private static string XamlVivo(string rutaAbsoluta) =>
		Regex.Replace(File.ReadAllText(rutaAbsoluta), @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

	/// <summary>Devuelve el código sin los bloques <c>/* */</c> ni los comentarios de línea.</summary>
	private static string CodigoVivo(string rutaAbsoluta)
	{
		var fuente = File.ReadAllText(rutaAbsoluta);
		fuente = Regex.Replace(fuente, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

		return string.Join("\n", fuente.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
	}
	#endregion
}
