using Core.Shared.Documentos;
using Domain.Cursantes.Calificaciones;
using Infrastructure.Documentos.Content;
using Infrastructure.Documentos.Plantilla;

namespace Infrastructure.Documentos;

/// <summary>
/// Compone cada documento con los bloques de <see cref="PlantillaDocumento"/> y lo renderiza a
/// PDF.
/// </summary>
/// <remarks>
/// Acá vive la redacción de los documentos y nada más: qué datos los componen lo resolvieron los
/// casos de uso, y cómo se dibuja una tabla o un pie de firma lo resuelven los bloques de
/// contenido.
/// </remarks>
internal sealed class GeneradorDocumentos : IGeneradorDocumentos
{
	private readonly IPortableDocumentFormat _pdf;
	private readonly PlantillaDocumento _plantilla;


	public GeneradorDocumentos(IPortableDocumentFormat pdf, IDatosInstitucionales institucion)
	{
		_pdf = pdf;
		_plantilla = new PlantillaDocumento(institucion);
	}

	#region Certificado de alumno regular
	public Task<DocumentoGenerado> GenerarCertificadoAlumnoRegularAsync(CertificadoAlumnoRegularDocumento documento)
	{
		const string TITULO = "Certificado de alumno regular";

		var cuerpo = Parrafo.Crear(
			$"Se certifica que { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			$"es alumno/a regular de este establecimiento, en el que se encuentra escolarizado/a desde el " +
			$"{ PlantillaDocumento.EnLetras(documento.FechaIngreso) }, y cursa durante el ciclo lectivo " +
			$"{ documento.CicloLectivo } el { Trayectoria(documento.Alumno) }.")
			.ConSangria();

		var pdf = Armar(TITULO, $"Ciclo lectivo { documento.CicloLectivo }", documento.Alumno, documento.Emision, cuerpo);

		return Task.FromResult(Entregar(TITULO, documento.Alumno.Legajo, documento.CicloLectivo, pdf));
	}
	#endregion

	#region Constancia de escolaridad
	public Task<DocumentoGenerado> GenerarConstanciaEscolaridadAsync(ConstanciaEscolaridadDocumento documento)
	{
		const string TITULO = "Constancia de escolaridad";

		var cuerpo = Parrafo.Crear(
			$"Se hace constar que { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			$"legajo { documento.Alumno.Legajo }, se encuentra escolarizado/a en este establecimiento desde el " +
			$"{ PlantillaDocumento.EnLetras(documento.FechaIngreso) }, cursando el { Trayectoria(documento.Alumno) } " +
			$"correspondiente al ciclo lectivo { documento.CicloLectivo }.")
			.ConSangria();

		var pdf = Armar(TITULO, $"Ciclo lectivo { documento.CicloLectivo }", documento.Alumno, documento.Emision, cuerpo);

		return Task.FromResult(Entregar(TITULO, documento.Alumno.Legajo, documento.CicloLectivo, pdf));
	}
	#endregion

	#region Certificado de asistencia
	public Task<DocumentoGenerado> GenerarCertificadoAsistenciaAsync(CertificadoAsistenciaDocumento documento)
	{
		const string TITULO = "Certificado de asistencia";

		var cuerpo = Parrafo.Crear(
			$"Se certifica que { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			$"concurrió a este establecimiento durante el período comprendido entre el " +
			$"{ PlantillaDocumento.EnLetras(documento.Periodo.Desde) } y el " +
			$"{ PlantillaDocumento.EnLetras(documento.Periodo.Hasta) }, cursando el { Trayectoria(documento.Alumno) } " +
			$"correspondiente al ciclo lectivo { documento.CicloLectivo }.")
			.ConSangria();

		var subtitulo = $"Período del { PlantillaDocumento.Corta(documento.Periodo.Desde) } al { PlantillaDocumento.Corta(documento.Periodo.Hasta) }";
		var pdf = Armar(TITULO, subtitulo, documento.Alumno, documento.Emision, cuerpo);

		return Task.FromResult(Entregar(TITULO, documento.Alumno.Legajo, documento.CicloLectivo, pdf));
	}
	#endregion

	#region Constancias de materias
	public Task<DocumentoGenerado> GenerarConstanciaMateriasAsync(ConstanciaMateriasDocumento documento)
	{
		var aprobadas = documento.Tipo == TipoConstanciaMaterias.Aprobadas;
		var titulo = aprobadas ? "Constancia de materias aprobadas" : "Constancia de materias pendientes";

		var introduccion = aprobadas
			? $"Se hace constar que { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			  $"registra como aprobados, al { PlantillaDocumento.EnLetras(documento.Emision.Fecha) }, los espacios " +
			  $"curriculares que se detallan, correspondientes al { Trayectoria(documento.Alumno) } del ciclo lectivo " +
			  $"{ documento.CicloLectivo }:"
			: $"Se hace constar que { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			  $"registra como pendientes de aprobación, al { PlantillaDocumento.EnLetras(documento.Emision.Fecha) }, " +
			  $"los espacios curriculares que se detallan, correspondientes al { Trayectoria(documento.Alumno) } del " +
			  $"ciclo lectivo { documento.CicloLectivo }:";

		var sinDatos = aprobadas
			? "El estudiante no registra espacios curriculares aprobados en el ciclo lectivo."
			: "El estudiante no registra espacios curriculares pendientes de aprobación.";

		var tabla = Tabla
			.Crear(columnas: new[] { "Espacio curricular", "Hs. cátedra", "Condición" },
				   filas: documento.Materias
					   .Select(materia => (IReadOnlyCollection<string>) new[]
						   {
							   materia.Materia,
							   materia.HorasCatedra.ToString(),
							   materia.Condicion
						   })
					   .ToList())
			.ConMensajeSinDatos(sinDatos);

		var pdf = Armar(titulo,
						$"Ciclo lectivo { documento.CicloLectivo }",
						documento.Alumno,
						documento.Emision,
						Parrafo.Crear(introduccion).ConSangria(),
						tabla,
						NotaDeAprobacion());

		return Task.FromResult(Entregar(titulo, documento.Alumno.Legajo, documento.CicloLectivo, pdf));
	}
	#endregion

	#region Boletín de calificaciones
	public Task<DocumentoGenerado> GenerarBoletinCalificacionesAsync(BoletinCalificacionesDocumento documento)
	{
		const string TITULO = "Boletín de calificaciones";

		var introduccion = Parrafo.Crear(
			$"Calificaciones registradas de { documento.Alumno.NombreCompleto }, D.N.I. { documento.Alumno.Documento }, " +
			$"en el { Trayectoria(documento.Alumno) } correspondiente al ciclo lectivo { documento.CicloLectivo }. " +
			$"Cuando una instancia tiene más de una calificación, se informa la última por fecha.")
			.ConSangria();

		var tabla = Tabla
			.Crear(columnas: new[] { "Espacio curricular", "Parcial", "Recuperatorio", "Condición" },
				   filas: documento.Filas
					   .Select(fila => (IReadOnlyCollection<string>) new[]
						   {
							   fila.Materia,
							   fila.Parcial,
							   fila.Recuperatorio,
							   fila.Condicion
						   })
					   .ToList())
			.ConMensajeSinDatos("El curso no tiene una currícula vigente con espacios curriculares.");

		var pdf = Armar(TITULO,
						$"Ciclo lectivo { documento.CicloLectivo }",
						documento.Alumno,
						documento.Emision,
						introduccion,
						tabla,
						NotaDeAprobacion());

		return Task.FromResult(Entregar(TITULO, documento.Alumno.Legajo, documento.CicloLectivo, pdf));
	}
	#endregion

	#region Reportes
	public Task<DocumentoGenerado> GenerarReporteAsync(ReporteTabularDocumento documento)
	{
		// Apaisado: los reportes llegan a diez columnas y en A4 vertical no entra un apellido por
		// celda sin partirlo en tres líneas.
		var pdfDocumento = Documento.Crear(documento.Titulo, documento.Alcance, Orientacion.Apaisada)
			.Agregar(_plantilla.BloqueInstitucional())
			.Agregar(_plantilla.Titulo(documento.Titulo, documento.Alcance))
			.Agregar(Tabla.Crear(documento.Columnas, documento.Filas))
			.Agregar(Parrafo.Crear($"Total de filas informadas: { documento.Filas.Count }.")
				.ALaIzquierda()
				.EnItalica()
				.ConTamano(9f)
				.ConEspacio(0f, 4f))
			.Agregar(_plantilla.Cierre(documento.Emision));

		var pdf = _pdf.Generar(pdfDocumento);

		return Task.FromResult(new DocumentoGenerado(NombreArchivo($"{ documento.Titulo } - { documento.Alcance }"), pdf));
	}
	#endregion

	#region Armado
	/// <summary>
	/// Secuencia común de un documento de alumno: establecimiento, título, identificación del
	/// estudiante, cuerpo y cierre con firma.
	/// </summary>
	private byte[] Armar(
		string titulo,
		string? subtitulo,
		IdentificacionAlumno alumno,
		MetadatosEmision emision,
		params IContenido[] cuerpo)
	{
		var documento = Documento.Crear(titulo, subtitulo)
			.Agregar(_plantilla.BloqueInstitucional())
			.Agregar(_plantilla.Titulo(titulo, subtitulo))
			.Agregar(_plantilla.BloqueAlumno(alumno))
			.Agregar(cuerpo)
			.Agregar(_plantilla.Cierre(emision));

		return _pdf.Generar(documento);
	}

	/// <summary>
	/// Declara el umbral de aprobación que se aplicó. El número sale del dominio
	/// (<see cref="Calificacion.NOTA_APROBACION"/>): es la misma regla que decidió la condición
	/// de cada materia, no una copia escrita en el texto del documento.
	/// </summary>
	private static IContenido NotaDeAprobacion() =>
		Parrafo.Crear($"Se considera aprobado el espacio curricular con una calificación igual o mayor a {Calificacion.NOTA_APROBACION:0}.")
			.ALaIzquierda()
			.EnItalica()
			.ConTamano(9f)
			.ConEspacio(0f, 4f);

	private static string Trayectoria(IdentificacionAlumno alumno) =>
		string.IsNullOrWhiteSpace(alumno.Division)
			? $"{ alumno.Curso } del nivel { alumno.NivelEducativo }"
			: $"{ alumno.Curso } división { alumno.Division } del nivel { alumno.NivelEducativo }";

	private static DocumentoGenerado Entregar(string titulo, string legajo, string cicloLectivo, byte[] pdf) =>
		new(NombreArchivo($"{ titulo } - { legajo } - { cicloLectivo }"), pdf);

	/// <summary>
	/// Nombre sugerido para el cuadro de guardado, sin los caracteres que Windows no admite en
	/// un nombre de archivo.
	/// </summary>
	private static string NombreArchivo(string descripcion)
	{
		var limpio = new string(descripcion
			.Select(caracter => Path.GetInvalidFileNameChars().Contains(caracter) ? '-' : caracter)
			.ToArray());

		return $"{ limpio.Trim() }.pdf";
	}
	#endregion
}
