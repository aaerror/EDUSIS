using Core.ServicioDocumentos.DTOs.Requests;
using Core.Shared;
using Core.Shared.Documentos;
using Core.Shared.Seguridad;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDocumentos;

internal class ServicioDocumento : IServicio, IServicioDocumento
{
	private readonly ILogger<ServicioDocumento> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IGeneradorDocumentos _generadorDocumentos;
	private readonly IDatosInstitucionales _institucion;
	private readonly ISesionActual _sesion;

	private readonly ContextoAcademico _contexto;


	public ServicioDocumento(ILogger<ServicioDocumento> logger,
							 IUnitOfWork unitOfWork,
							 IGeneradorDocumentos generadorDocumentos,
							 IDatosInstitucionales institucion,
							 ISesionActual sesion)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
		_generadorDocumentos = generadorDocumentos;
		_institucion = institucion;
		_sesion = sesion;

		_contexto = new ContextoAcademico(unitOfWork);
	}

	#region Certificado de alumno regular
	public async Task<DocumentoGenerado> GenerarCertificadoAlumnoRegularAsync(EmitirDocumentoAlumnoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarDocumentos);

			var cursante = await ResolverAsync(request.AlumnoID, request.Periodo);

			var documento = new CertificadoAlumnoRegularDocumento(
				Alumno: Identificar(cursante),
				CicloLectivo: request.Periodo,
				FechaIngreso: cursante.Alumno.Periodo.FechaInicio,
				Emision: Emitir(request.Finalidad));

			var generado = await _generadorDocumentos.GenerarCertificadoAlumnoRegularAsync(documento);

			_logger.LogInformation($"Certificado de alumno regular emitido para el legajo { cursante.Alumno.Legajo }.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Constancia de escolaridad
	public async Task<DocumentoGenerado> GenerarConstanciaEscolaridadAsync(EmitirDocumentoAlumnoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarDocumentos);

			var cursante = await ResolverAsync(request.AlumnoID, request.Periodo);

			var documento = new ConstanciaEscolaridadDocumento(
				Alumno: Identificar(cursante),
				CicloLectivo: request.Periodo,
				FechaIngreso: cursante.Alumno.Periodo.FechaInicio,
				Emision: Emitir(request.Finalidad));

			var generado = await _generadorDocumentos.GenerarConstanciaEscolaridadAsync(documento);

			_logger.LogInformation($"Constancia de escolaridad emitida para el legajo { cursante.Alumno.Legajo }.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Certificado de asistencia
	public async Task<DocumentoGenerado> GenerarCertificadoAsistenciaAsync(EmitirCertificadoAsistenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarDocumentos);

			if (request.Desde.Date > request.Hasta.Date)
			{
				throw new ArgumentException("El inicio del período es posterior a su fin.", nameof(request.Desde));
			}

			if (request.Desde.Date > DateTime.Today)
			{
				throw new ArgumentException("El período del certificado no puede comenzar en el futuro.", nameof(request.Desde));
			}

			var cursante = await ResolverAsync(request.AlumnoID, request.Periodo);

			// El certificado acredita concurrencia, pero no se emite sobre un período en el que no
			// se tomó asistencia: sería una acreditación sin respaldo.
			var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionYRangoAsync(
				cursante.Division.Id, request.Desde, request.Hasta);

			var concurrio = planillas.Any(planilla => planilla.RegistroDe(cursante.Cursante.Id) is not null);
			if (!concurrio)
			{
				throw new InvalidOperationException("No hay planillas de asistencia del alumno en el período indicado: no se puede certificar la concurrencia.");
			}

			var documento = new CertificadoAsistenciaDocumento(
				Alumno: Identificar(cursante),
				CicloLectivo: request.Periodo,
				Periodo: new RangoPeriodo(request.Desde.Date, request.Hasta.Date),
				Emision: Emitir(request.Finalidad));

			var generado = await _generadorDocumentos.GenerarCertificadoAsistenciaAsync(documento);

			_logger.LogInformation($"Certificado de asistencia emitido para el legajo { cursante.Alumno.Legajo }.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Constancias de materias
	public Task<DocumentoGenerado> GenerarConstanciaMateriasAprobadasAsync(EmitirDocumentoAlumnoRequest request) =>
		GenerarConstanciaMateriasAsync(request, TipoConstanciaMaterias.Aprobadas);

	public Task<DocumentoGenerado> GenerarConstanciaMateriasPendientesAsync(EmitirDocumentoAlumnoRequest request) =>
		GenerarConstanciaMateriasAsync(request, TipoConstanciaMaterias.Pendientes);

	/// <summary>
	/// Las dos constancias recorren lo mismo —las materias de la currícula vigente contra las
	/// calificaciones del cursante— y se diferencian sólo en qué mitad del resultado informan.
	/// </summary>
	private async Task<DocumentoGenerado> GenerarConstanciaMateriasAsync(EmitirDocumentoAlumnoRequest request, TipoConstanciaMaterias tipo)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarDocumentos);

			var cursante = await ResolverAsync(request.AlumnoID, request.Periodo);
			var materias = await _contexto.MateriasVigentesDelCursoAsync(cursante.Curso.Id);

			var informadas = materias
				.Select(materia => new
					{
						Materia = materia,
						Aprobada = ContextoAcademico.EstaAprobada(cursante.Cursante, materia.Id)
					})
				.Where(x => x.Aprobada == (tipo == TipoConstanciaMaterias.Aprobadas))
				.OrderBy(x => x.Materia.Descripcion)
				.Select(x => new MateriaInformada(
					Materia: x.Materia.Descripcion,
					HorasCatedra: x.Materia.HorasCatedra,
					Condicion: ContextoAcademico.DescribirCondicion(cursante.Cursante, x.Materia.Id, conNota: true)))
				.ToList();

			var documento = new ConstanciaMateriasDocumento(
				Alumno: Identificar(cursante),
				CicloLectivo: request.Periodo,
				Tipo: tipo,
				Materias: informadas.AsReadOnly(),
				Emision: Emitir(request.Finalidad));

			var generado = await _generadorDocumentos.GenerarConstanciaMateriasAsync(documento);

			_logger.LogInformation($"Constancia de materias { tipo.ToString().ToLower() } emitida para el legajo { cursante.Alumno.Legajo }.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Boletín de calificaciones
	public async Task<DocumentoGenerado> GenerarBoletinCalificacionesAsync(EmitirDocumentoAlumnoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarDocumentos);

			var cursante = await ResolverAsync(request.AlumnoID, request.Periodo);
			var materias = await _contexto.MateriasVigentesDelCursoAsync(cursante.Curso.Id);

			var filas = materias
				.OrderBy(materia => materia.Descripcion)
				.Select(materia => new FilaBoletin(
					Materia: materia.Descripcion,
					Parcial: ContextoAcademico.DescribirNota(cursante.Cursante, materia.Id, Instancia.Parcial),
					Recuperatorio: ContextoAcademico.DescribirNota(cursante.Cursante, materia.Id, Instancia.Recuperatorio),
					Condicion: ContextoAcademico.DescribirCondicion(cursante.Cursante, materia.Id, conNota: true)))
				.ToList();

			var documento = new BoletinCalificacionesDocumento(
				Alumno: Identificar(cursante),
				CicloLectivo: request.Periodo,
				Filas: filas.AsReadOnly(),
				Emision: Emitir(request.Finalidad));

			var generado = await _generadorDocumentos.GenerarBoletinCalificacionesAsync(documento);

			_logger.LogInformation($"Boletín de calificaciones emitido para el legajo { cursante.Alumno.Legajo }.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Resolución
	private async Task<ContextoCursante> ResolverAsync(Guid alumnoID, string periodo) =>
		await _contexto.ResolverCursanteAsync(alumnoID, CicloLectivo.Crear(periodo));

	private static IdentificacionAlumno Identificar(ContextoCursante cursante) =>
		new(NombreCompleto: cursante.Alumno.DatosPersonales.NombreCompleto(),
			Documento: cursante.Alumno.DatosPersonales.Documento,
			Legajo: cursante.Alumno.Legajo,
			NivelEducativo: cursante.Curso.NivelEducativo.ToString(),
			Curso: ContextoAcademico.Describir(cursante.Curso.Grado),
			Division: cursante.Division.Descripcion);

	private MetadatosEmision Emitir(string? finalidad) =>
		new(Localidad: _institucion.Localidad,
			Fecha: DateTime.Today,
			Finalidad: string.IsNullOrWhiteSpace(finalidad) ? null : finalidad.Trim());
	#endregion

}
