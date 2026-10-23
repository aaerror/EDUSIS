using Core.Shared.Documentos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Doble de <see cref="IGeneradorDocumentos"/> que guarda el último documento recibido de cada
/// tipo en lugar de renderizar un PDF.
/// </summary>
/// <remarks>
/// Es un fake con memoria y no un mock de verificación porque lo que interesa comprobar de los
/// casos de uso es QUÉ datos componen el documento —qué materias quedaron del lado de las
/// aprobadas, qué nota se informó en cada instancia—, y eso se afirma sobre el objeto que
/// recibió el puerto. Nunca toca el sistema de archivos.
/// <para>
/// <see cref="Falla"/> permite que una prueba haga fallar el renderizado para comprobar que el
/// caso de uso propaga la excepción sin transformarla.
/// </para>
/// </remarks>
public sealed class GeneradorDocumentosFake : IGeneradorDocumentos
{
	/// <summary>Excepción a lanzar en lugar de generar. En nulo, el fake genera normalmente.</summary>
	public Exception? Falla { get; set; }

	public int Invocaciones { get; private set; }

	public CertificadoAlumnoRegularDocumento? CertificadoAlumnoRegular { get; private set; }

	public ConstanciaEscolaridadDocumento? ConstanciaEscolaridad { get; private set; }

	public CertificadoAsistenciaDocumento? CertificadoAsistencia { get; private set; }

	public ConstanciaMateriasDocumento? ConstanciaMaterias { get; private set; }

	public BoletinCalificacionesDocumento? Boletin { get; private set; }

	public ReporteTabularDocumento? Reporte { get; private set; }


	public Task<DocumentoGenerado> GenerarCertificadoAlumnoRegularAsync(CertificadoAlumnoRegularDocumento documento)
	{
		CertificadoAlumnoRegular = documento;

		return Entregar("certificado-alumno-regular.pdf");
	}

	public Task<DocumentoGenerado> GenerarConstanciaEscolaridadAsync(ConstanciaEscolaridadDocumento documento)
	{
		ConstanciaEscolaridad = documento;

		return Entregar("constancia-escolaridad.pdf");
	}

	public Task<DocumentoGenerado> GenerarCertificadoAsistenciaAsync(CertificadoAsistenciaDocumento documento)
	{
		CertificadoAsistencia = documento;

		return Entregar("certificado-asistencia.pdf");
	}

	public Task<DocumentoGenerado> GenerarConstanciaMateriasAsync(ConstanciaMateriasDocumento documento)
	{
		ConstanciaMaterias = documento;

		return Entregar("constancia-materias.pdf");
	}

	public Task<DocumentoGenerado> GenerarBoletinCalificacionesAsync(BoletinCalificacionesDocumento documento)
	{
		Boletin = documento;

		return Entregar("boletin.pdf");
	}

	public Task<DocumentoGenerado> GenerarReporteAsync(ReporteTabularDocumento documento)
	{
		Reporte = documento;

		return Entregar("reporte.pdf");
	}

	private Task<DocumentoGenerado> Entregar(string nombre)
	{
		Invocaciones++;

		if (Falla is not null)
		{
			return Task.FromException<DocumentoGenerado>(Falla);
		}

		return Task.FromResult(new DocumentoGenerado(nombre, new byte[] { 37, 80, 68, 70 }));
	}
}
