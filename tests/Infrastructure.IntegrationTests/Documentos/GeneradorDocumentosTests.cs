using System.Text;
using Core.Shared.Documentos;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Fakes;
using Infrastructure.Documentos;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Documentos;

/// <summary>
/// Renderizado real con iText: verifica que cada documento sale como un PDF bien formado y que
/// el texto que se le pidió imprimir está efectivamente en la página.
/// </summary>
/// <remarks>
/// Es la contraparte necesaria de las pruebas de <c>Core</c>, que doblan
/// <see cref="IGeneradorDocumentos"/> y por lo tanto no ejercitan una sola línea de iText. Acá
/// no hay base de datos: el renderizado no la necesita, así que estas pruebas corren siempre y
/// no dependen del fixture de SQL Server.
/// <para>
/// Las aserciones son sobre el TEXTO EXTRAÍDO del PDF y no sobre el tamaño del arreglo de bytes:
/// un PDF de cuatro kilobytes con el cuerpo vacío también pesa cuatro kilobytes.
/// </para>
/// </remarks>
[Trait("Categoria", Categorias.Integracion)]
public sealed class GeneradorDocumentosTests
{
	private const string CICLO = "2026";

	private readonly IDatosInstitucionales _institucion = new DatosInstitucionalesDePrueba();
	private readonly IGeneradorDocumentos _generador;

	public GeneradorDocumentosTests()
	{
		_generador = new GeneradorDocumentos(new PortableDocumentFormat(_institucion), _institucion);
	}

	#region Datos de prueba
	private static IdentificacionAlumno Alumno() =>
		new(NombreCompleto: "Ñáñez Piñón, José María",
			Documento: "30111222",
			Legajo: "AL1234",
			NivelEducativo: "Secundaria",
			Curso: "3° Año",
			Division: "B");

	private static MetadatosEmision Emision(string? finalidad = null) =>
		new("Localidad de Prueba", DateTime.Today, finalidad);
	#endregion

	#region Lectura del PDF
	/// <summary>Texto de todas las páginas del PDF, concatenado.</summary>
	private static string TextoDe(DocumentoGenerado documento)
	{
		using var lector = new PdfReader(new MemoryStream(documento.Contenido));
		using var pdf = new PdfDocument(lector);

		var texto = new StringBuilder();
		for (var pagina = 1; pagina <= pdf.GetNumberOfPages(); pagina++)
		{
			texto.AppendLine(PdfTextExtractor.GetTextFromPage(pdf.GetPage(pagina)));
		}

		return texto.ToString();
	}

	private static void DebeSerUnPdfValido(DocumentoGenerado documento)
	{
		documento.NombreSugerido.ShouldEndWith(".pdf");
		documento.Contenido.ShouldNotBeEmpty();

		// La firma de cabecera: si falta, no es un PDF y cualquier aserción posterior sobre el
		// contenido mentiría.
		Encoding.ASCII.GetString(documento.Contenido, 0, 5).ShouldBe("%PDF-");

		using var lector = new PdfReader(new MemoryStream(documento.Contenido));
		using var pdf = new PdfDocument(lector);
		pdf.GetNumberOfPages().ShouldBeGreaterThan(0);
	}
	#endregion

	#region Certificado de alumno regular
	[Fact]
	public void El_certificado_de_alumno_regular_imprime_al_alumno_y_al_establecimiento()
	{
		var generado = _generador
			.GenerarCertificadoAlumnoRegularAsync(new CertificadoAlumnoRegularDocumento(
				Alumno(), CICLO, DateTime.Today.AddYears(-2), Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		var texto = TextoDe(generado);
		texto.ShouldContain("CERTIFICADO DE ALUMNO REGULAR");
		// Los acentos y la ñ tienen que sobrevivir la codificación de la fuente.
		texto.ShouldContain("Ñáñez Piñón, José María");
		texto.ShouldContain("30111222");
		texto.ShouldContain("AL1234");
		texto.ShouldContain(_institucion.Nombre);
		texto.ShouldContain(_institucion.CUE);
		texto.ShouldContain(_institucion.Direccion);
		texto.ShouldContain(CICLO);
	}

	[Fact]
	public void Todo_documento_reserva_el_espacio_de_firma_y_sello()
	{
		var generado = _generador
			.GenerarCertificadoAlumnoRegularAsync(new CertificadoAlumnoRegularDocumento(
				Alumno(), CICLO, DateTime.Today.AddYears(-2), Emision()))
			.Result;

		var texto = TextoDe(generado);
		texto.ShouldContain("Firma de la autoridad competente");
		texto.ShouldContain("Sello institucional");
	}

	[Fact]
	public void La_finalidad_se_imprime_solo_cuando_el_tramite_la_declara()
	{
		var conFinalidad = _generador
			.GenerarCertificadoAlumnoRegularAsync(new CertificadoAlumnoRegularDocumento(
				Alumno(), CICLO, DateTime.Today.AddYears(-2), Emision("Trámite de beca")))
			.Result;

		var sinFinalidad = _generador
			.GenerarCertificadoAlumnoRegularAsync(new CertificadoAlumnoRegularDocumento(
				Alumno(), CICLO, DateTime.Today.AddYears(-2), Emision()))
			.Result;

		TextoDe(conFinalidad).ShouldContain("Trámite de beca");
		TextoDe(sinFinalidad).ShouldNotContain("a los efectos de");
	}

	[Fact]
	public void La_fecha_de_expedicion_se_escribe_en_espanol()
	{
		var generado = _generador
			.GenerarCertificadoAlumnoRegularAsync(new CertificadoAlumnoRegularDocumento(
				Alumno(), CICLO, new DateTime(2024, 3, 4), Emision()))
			.Result;

		// Sin cultura explícita, "MMMM" sale en el idioma de la máquina que corre la prueba.
		TextoDe(generado).ShouldContain("4 de marzo de 2024");
	}
	#endregion

	#region Resto de la documentación
	[Fact]
	public void La_constancia_de_escolaridad_se_renderiza()
	{
		var generado = _generador
			.GenerarConstanciaEscolaridadAsync(new ConstanciaEscolaridadDocumento(
				Alumno(), CICLO, DateTime.Today.AddYears(-1), Emision()))
			.Result;

		DebeSerUnPdfValido(generado);
		TextoDe(generado).ShouldContain("CONSTANCIA DE ESCOLARIDAD");
	}

	[Fact]
	public void El_certificado_de_asistencia_imprime_el_periodo_que_acredita()
	{
		var generado = _generador
			.GenerarCertificadoAsistenciaAsync(new CertificadoAsistenciaDocumento(
				Alumno(),
				CICLO,
				new RangoPeriodo(new DateTime(2026, 3, 2), new DateTime(2026, 6, 30)),
				Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		var texto = TextoDe(generado);
		texto.ShouldContain("CERTIFICADO DE ASISTENCIA");
		texto.ShouldContain("2 de marzo de 2026");
		texto.ShouldContain("30 de junio de 2026");
	}

	[Fact]
	public void La_constancia_de_materias_imprime_la_tabla_de_espacios_curriculares()
	{
		var generado = _generador
			.GenerarConstanciaMateriasAsync(new ConstanciaMateriasDocumento(
				Alumno(),
				CICLO,
				TipoConstanciaMaterias.Aprobadas,
				new[]
					{
						new MateriaInformada("Matemática", 5, "Aprobada (8)"),
						new MateriaInformada("Lengua y Literatura", 4, "Aprobada (7)")
					},
				Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		var texto = TextoDe(generado);
		texto.ShouldContain("CONSTANCIA DE MATERIAS APROBADAS");
		texto.ShouldContain("Matemática");
		texto.ShouldContain("Lengua y Literatura");
		texto.ShouldContain("Aprobada (8)");
	}

	[Fact]
	public void Una_constancia_de_materias_sin_filas_explica_la_tabla_vacia()
	{
		var generado = _generador
			.GenerarConstanciaMateriasAsync(new ConstanciaMateriasDocumento(
				Alumno(), CICLO, TipoConstanciaMaterias.Pendientes, Array.Empty<MateriaInformada>(), Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		// Una tabla vacía sin leyenda se lee como una falla del sistema.
		TextoDe(generado).ShouldContain("no registra espacios curriculares pendientes");
	}

	[Fact]
	public void El_boletin_imprime_una_fila_por_materia_con_sus_instancias()
	{
		var generado = _generador
			.GenerarBoletinCalificacionesAsync(new BoletinCalificacionesDocumento(
				Alumno(),
				CICLO,
				new[]
					{
						new FilaBoletin("Matemática", "3", "7", "Aprobada (7)"),
						new FilaBoletin("Historia", "—", "—", "Sin calificar")
					},
				Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		var texto = TextoDe(generado);
		texto.ShouldContain("BOLETÍN DE CALIFICACIONES");
		texto.ShouldContain("Recuperatorio");
		texto.ShouldContain("Sin calificar");
	}
	#endregion

	#region Reportes
	[Fact]
	public void Un_reporte_se_renderiza_apaisado_con_sus_columnas_y_filas()
	{
		var generado = _generador
			.GenerarReporteAsync(new ReporteTabularDocumento(
				Titulo: "Reporte de matrículas",
				Alcance: $"Ciclo lectivo { CICLO } · Institución completa",
				Columnas: new[] { "Curso", "Div.", "Legajo", "Alumno" },
				Filas: new[]
					{
						(IReadOnlyCollection<string>) new[] { "3° Año", "B", "AL1234", "Ñáñez Piñón, José María" },
						(IReadOnlyCollection<string>) new[] { "3° Año", "B", "AL1235", "Pérez, Ana" }
					},
				Emision: Emision()))
			.Result;

		DebeSerUnPdfValido(generado);

		var texto = TextoDe(generado);
		texto.ShouldContain("REPORTE DE MATRÍCULAS");
		texto.ShouldContain("Institución completa");
		texto.ShouldContain("Pérez, Ana");
		texto.ShouldContain("Total de filas informadas: 2.");

		using var lector = new PdfReader(new MemoryStream(generado.Contenido));
		using var pdf = new PdfDocument(lector);

		// Apaisado: diez columnas en A4 vertical no entran.
		var pagina = pdf.GetFirstPage().GetPageSize();
		pagina.GetWidth().ShouldBeGreaterThan(pagina.GetHeight());
	}

	[Fact]
	public void Un_reporte_largo_pagina_y_repite_el_encabezado_de_la_tabla()
	{
		var filas = Enumerable.Range(1, 120)
			.Select(numero => (IReadOnlyCollection<string>) new[]
				{
					"3° Año",
					"B",
					$"AL{ numero:0000}",
					$"Apellido{ numero }, Nombre"
				})
			.ToList();

		var generado = _generador
			.GenerarReporteAsync(new ReporteTabularDocumento(
				Titulo: "Reporte de matrículas",
				Alcance: $"Ciclo lectivo { CICLO } · Institución completa",
				Columnas: new[] { "Curso", "Div.", "Legajo", "Alumno" },
				Filas: filas,
				Emision: Emision()))
			.Result;

		using var lector = new PdfReader(new MemoryStream(generado.Contenido));
		using var pdf = new PdfDocument(lector);

		var paginas = pdf.GetNumberOfPages();
		paginas.ShouldBeGreaterThan(1);

		var texto = TextoDe(generado);

		// La última fila tiene que estar: si la tabla no paginara, se perdería sin aviso.
		texto.ShouldContain("Apellido120, Nombre");

		// El pie numera cada página.
		texto.ShouldContain("Página 2");

		// El encabezado de la tabla se repite en cada hoja QUE LLEVA FILAS: una página de datos
		// sin encabezado es una columna de valores sin nombre. Son celdas de encabezado
		// (AddHeaderCell) y no la primera fila justamente para esto.
		//
		// El tope es la cantidad de páginas y no la igualdad: la última hoja puede contener sólo
		// el cierre y el pie de firma, sin tabla, y entonces no corresponde que lo tenga.
		var vecesQueApareceElEncabezado = texto.Split("Legajo").Length - 1;
		vecesQueApareceElEncabezado.ShouldBeGreaterThan(1);
		vecesQueApareceElEncabezado.ShouldBeLessThanOrEqualTo(paginas);
	}
	#endregion
}
