using Bogus;
using Core.ServicioSecurity;
using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Catedras;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using Domain.Curriculas;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Docentes;
using Domain.Docentes.Puestos;
using Domain.Materias;
using Domain.Personas;
using Domain.Personas.Domicilios;
using Domain.Shared;
using Domain.Usuarios;
using EDUSIS.TestSupport.Builders;
using Infrastructure;
using Microsoft.Extensions.Logging;

namespace EDUSIS.Seed;

/// <summary>
/// Puebla la base con la escuela demo completa (plan/rbac-y-seed.md, Fase 7).
/// </summary>
/// <remarks>
/// Tres capas que conviene no mezclar, y que el plan separó a propósito:
/// <list type="bullet">
///   <item><b>Referencia</b> (<c>rol</c>, <c>ciclo_lectivo</c>): ya la siembra <c>HasData</c> en la migración. Acá sólo se <em>adjunta</em>.</item>
///   <item><b>Construcción</b>: los builders de <c>EDUSIS.TestSupport.Domain</c>, que usan los constructores públicos del dominio y por lo tanto validan invariantes y encolan eventos igual que producción.</item>
///   <item><b>Volumen</b>: Bogus con locale <c>es</c> y semilla fija, sólo para nombres, domicilios y fechas.</item>
/// </list>
/// Las identidades únicas (DNI, CUIL, legajos) <strong>no</strong> salen de Bogus sino de
/// contadores: el dominio y la base exigen unicidad, y un generador aleatorio la rompe por
/// colisión tarde o temprano, de forma intermitente y por lo tanto difícil de diagnosticar.
/// </remarks>
internal sealed class Sembradora
{
	#region Parámetros del dataset
	/// <summary>Alumnos por división. <c>Division.MAX_ALUMNOS</c> es 35, así que 25 deja margen para probar inscripciones a mano.</summary>
	private const int ALUMNOS_POR_DIVISION = 25;

	/// <summary>Días hábiles hacia atrás con planilla de asistencia tomada.</summary>
	private const int DIAS_DE_ASISTENCIA = 5;

	/// <summary>Una de cada tantas cátedras recibe además un suplente que reemplaza al titular.</summary>
	private const int CATEDRAS_POR_SUPLENTE = 8;

	/// <summary>Materias con calificaciones cargadas, por cursante.</summary>
	private const int MATERIAS_CALIFICADAS = 3;

	/// <summary>Contraseña de todos los usuarios sembrados. Es un dataset de demo, no un entorno real.</summary>
	internal const string PASSWORD_DEMO = "Edusis2026";

	/// <summary>Hora cátedra, en minutos. <c>Horario</c> exige múltiplo de 5 y no menos de 30.</summary>
	private const int MINUTOS_HORA_CATEDRA = 40;
	#endregion

	private readonly EdusisDBContext _contexto;
	private readonly IUnitOfWork _unidadDeTrabajo;
	private readonly IServicioSeguridad _seguridad;
	private readonly ILogger<Sembradora> _logger;
	private readonly Faker _faker = new("es");

	#region Secuencias de identidad
	private int _siguienteDocumento = 25_000_000;
	private int _siguienteLegajoDocente = 100_200;
	private int _siguienteLegajoAlumno = 1;
	#endregion

	#region Ocupación horaria
	// Una división no puede tener dos cátedras en el mismo slot, y un docente no puede estar en
	// dos aulas a la vez. Ninguna de las dos reglas vive en un agregado (son de las que el plan
	// resuelve "con consulta a repositorio más restricción en BD"), así que el seeder tiene que
	// respetarlas por su cuenta o produciría un dataset que la app rechazaría al editarlo.
	private readonly HashSet<(Guid Division, Dia Dia, TimeOnly Hora)> _ocupacionDeDivision = new();
	private readonly HashSet<(Guid Docente, Dia Dia, TimeOnly Hora)> _ocupacionDeDocente = new();
	#endregion


	/// <remarks>
	/// Recibe el <see cref="EdusisDBContext"/> además del <see cref="IUnitOfWork"/> porque
	/// adjuntar los roles al <c>ChangeTracker</c> (§C del plan) es una operación de EF que la
	/// unidad de trabajo no expone, y con razón: <c>IUnitOfWork</c> vive en <c>Domain</c> y no
	/// conoce EF. Ambos se resuelven del mismo scope, así que son el mismo contexto.
	/// </remarks>
	public Sembradora(
		EdusisDBContext contexto,
		IUnitOfWork unidadDeTrabajo,
		IServicioSeguridad seguridad,
		ILogger<Sembradora> logger)
	{
		_contexto = contexto;
		_unidadDeTrabajo = unidadDeTrabajo;
		_seguridad = seguridad;
		_logger = logger;
	}

	public async Task<ResumenDeSiembra> SembrarAsync()
	{
		AdjuntarDatosDeReferencia();

		var docentes = await SembrarDocentesAsync();
		var alumnos = await SembrarAlumnosAsync();
		var cursos = await SembrarCursosAsync();
		var curriculas = await SembrarCurriculasAsync(cursos);
		var materias = await SembrarMateriasAsync(cursos, curriculas);
		var divisiones = await SembrarDivisionesAsync(cursos, docentes.Preceptores);
		var catedras = await SembrarCatedrasAsync(cursos, divisiones, materias, docentes.Profesores);
		var cursantes = await SembrarCursantesAsync(divisiones, alumnos, materias, cursos);
		var planillas = await SembrarPlanillasAsync(divisiones, cursantes, docentes.Preceptores);
		var usuarios = await SembrarUsuariosAsync(docentes);

		return new ResumenDeSiembra(
			Docentes: docentes.Todos.Count,
			Alumnos: alumnos.Count,
			Cursos: cursos.Count,
			Curriculas: curriculas.Count,
			Materias: materias.Values.Sum(lista => lista.Count),
			Divisiones: divisiones.Values.Sum(lista => lista.Count),
			Catedras: catedras.Count,
			HorariosAsignados: catedras.Sum(catedra => catedra.HorasAsignadas),
			HorariosSinAsignar: catedras.Sum(catedra => catedra.HorasSinAsignar),
			Cursantes: cursantes.Count,
			Calificaciones: cursantes.Sum(cursante => cursante.Calificaciones.Count),
			Planillas: planillas.Count,
			Usuarios: usuarios.Count);
	}

	#region Capa de referencia
	/// <summary>
	/// Adjunta los cuatro roles al <c>ChangeTracker</c> como <c>Unchanged</c> antes de que
	/// cualquier <see cref="Usuario"/> los referencie.
	/// </summary>
	/// <remarks>
	/// Es el patrón que evita la trampa del §C del plan. <c>Rol.Admin</c> y sus hermanos son
	/// <c>static readonly</c>: si un <see cref="Usuario"/> los agrega y el contexto no sabe que
	/// ya existen en la base, EF los descubre como <c>Added</c> e intenta
	/// <c>INSERT INTO rol</c>, que choca con la PK que sembró <c>HasData</c>. Adjuntarlos primero
	/// hace que EF inserte únicamente las filas de <c>usuario_rol</c>.
	/// </remarks>
	private void AdjuntarDatosDeReferencia()
	{
		_contexto.AttachRange(Rol.Admin, Rol.Direccion, Rol.Docente, Rol.Secretaria);
		_logger.LogInformation("Roles de referencia adjuntos al contexto (los cuatro, como Unchanged).");
	}
	#endregion

	#region Docentes
	private async Task<PlantelDocente> SembrarDocentesAsync()
	{
		// Un plantel con la forma de una escuela: una conducción chica, un preceptor por
		// división y profesores de sobra para cubrir las cátedras sin saturar horarios.
		var directivos = ConstruirDocentes(cantidad: 2, Posicion.Directivo);
		var secretarias = ConstruirDocentes(cantidad: 2, Posicion.Secretaria);
		var preceptores = ConstruirDocentes(
			cantidad: EscuelaDemo.Cursos.Length * EscuelaDemo.Divisiones.Length,
			Posicion.Preceptor);
		var profesores = ConstruirDocentes(cantidad: 35, Posicion.Profesor);

		var todos = directivos.Concat(secretarias).Concat(preceptores).Concat(profesores).ToList();

		await _unidadDeTrabajo.Docentes.AgregarRangoAsync(todos);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Docentes sembrados: {Cantidad}.", todos.Count);

		return new PlantelDocente(todos, directivos, secretarias, preceptores, profesores);
	}

	private List<Docente> ConstruirDocentes(int cantidad, Posicion posicion)
	{
		var docentes = new List<Docente>(cantidad);

		for (var i = 0; i < cantidad; i++)
		{
			var sexo = _faker.PickRandom<Sexo>();
			var fechaAlta = DateTime.Today.AddYears(-_faker.Random.Int(1, 15));

			var docente = new DocenteBuilder()
				.ConDatosPersonales(DatosPersonalesDe(sexo, edad: EdadDocenteEnActividad(sexo)))
				.ConDomicilio(DomicilioAlAzar())
				.ConEmail(_faker.Internet.Email())
				.ConTelefono(TelefonoAlAzar())
				.ConLegajo(SiguienteLegajoDocente())
				.ConCuil(CuilPara(_siguienteDocumento - 1))
				.ConFechaAlta(fechaAlta)
				// Estado "Activo" y NO el "Pendiente" del builder por omisión:
				// DocenteRepository.BuscarSegunPosicionAsync filtra por
				// `Estado == EstadoPuesto.Activo`, así que un puesto pendiente no aparece en
				// ListarPreceptoresActivosAsync y la pantalla de divisiones quedaría sin
				// preceptores para elegir.
				//
				// La fecha de inicio es HOY y no la de alta porque AsignarCargoDocente exige
				// que no sea anterior al alta del docente, y Puesto.EstaActivo() mira que el
				// período esté vigente.
				.ConPuesto(posicion.ToString(), nameof(EstadoPuesto.Activo), DateTime.Today)
				.Build();

			docentes.Add(docente);
		}

		return docentes;
	}
	#endregion

	#region Alumnos
	private async Task<List<Alumno>> SembrarAlumnosAsync()
	{
		var cantidad = EscuelaDemo.Cursos.Length * EscuelaDemo.Divisiones.Length * ALUMNOS_POR_DIVISION;
		var alumnos = new List<Alumno>(cantidad);

		for (var i = 0; i < cantidad; i++)
		{
			var sexo = _faker.PickRandom<Sexo>();

			alumnos.Add(new AlumnoBuilder()
				.ConDatosPersonales(DatosPersonalesDe(sexo, edad: _faker.Random.Int(12, 19)))
				.ConDomicilio(DomicilioAlAzar())
				.ConEmail(_faker.Internet.Email())
				.ConTelefono(TelefonoAlAzar())
				.ConLegajo(SiguienteLegajoAlumno())
				.Build());
		}

		await _unidadDeTrabajo.Alumnos.AgregarRangoAsync(alumnos);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Alumnos sembrados: {Cantidad}.", alumnos.Count);

		return alumnos;
	}
	#endregion

	#region Cursos, currículas y materias
	private async Task<List<Curso>> SembrarCursosAsync()
	{
		var cursos = EscuelaDemo.Cursos
			.Select(grado => new CursoBuilder()
				.ConGrado(grado.ToString())
				.ConNivelEducativo(EscuelaDemo.Nivel.ToString())
				.Build())
			.ToList();

		await _unidadDeTrabajo.Cursos.AgregarRangoAsync(cursos);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Cursos sembrados: {Cantidad}.", cursos.Count);

		return cursos;
	}

	/// <summary>
	/// Una currícula <strong>vigente</strong> por curso: arrancó en marzo de este ciclo y no
	/// tiene fecha de fin. Es el punto de congelamiento que habilita a las cátedras a copiar la
	/// carga horaria de las materias.
	/// </summary>
	private async Task<List<Curricula>> SembrarCurriculasAsync(List<Curso> cursos)
	{
		var inicioDelCiclo = new DateTime(DateTime.Today.Year, 3, 1);

		var curriculas = cursos
			.Select(curso => new CurriculaBuilder()
				.ConCurso(curso.Id)
				.ConFechaInicio(inicioDelCiclo)
				.ConFechaFin(null)
				.Build())
			.ToList();

		await _unidadDeTrabajo.Curriculas.AgregarRangoAsync(curriculas);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Currículas vigentes sembradas: {Cantidad}.", curriculas.Count);

		return curriculas;
	}

	private async Task<Dictionary<Guid, List<Materia>>> SembrarMateriasAsync(
		List<Curso> cursos,
		List<Curricula> curriculas)
	{
		var porCurricula = new Dictionary<Guid, List<Materia>>();
		var todas = new List<Materia>();

		for (var i = 0; i < cursos.Count; i++)
		{
			var curricula = curriculas[i];
			var plan = EscuelaDemo.PlanDeEstudios(EscuelaDemo.Cursos[i]);

			var materias = plan
				.Select(espacio => new MateriaBuilder()
					.ConCurricula(curricula.Id)
					.ConDescripcion(espacio.Descripcion)
					.ConHorasCatedra(espacio.HorasCatedra)
					.Build())
				.ToList();

			porCurricula[curricula.Id] = materias;
			todas.AddRange(materias);
		}

		await _unidadDeTrabajo.Materias.AgregarRangoAsync(todas);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Materias sembradas: {Cantidad}.", todas.Count);

		return porCurricula;
	}
	#endregion

	#region Divisiones
	private async Task<Dictionary<Guid, List<Division>>> SembrarDivisionesAsync(
		List<Curso> cursos,
		List<Docente> preceptores)
	{
		var porCurso = new Dictionary<Guid, List<Division>>();
		var todas = new List<Division>();
		var siguientePreceptor = 0;

		foreach (var curso in cursos)
		{
			var divisiones = new List<Division>();

			foreach (var letra in EscuelaDemo.Divisiones)
			{
				var division = new DivisionBuilder()
					.ConCurso(curso.Id)
					.ConDescripcion(letra)
					.Build();

				// Un preceptor por división: así ninguna queda con el cargo vacante y se puede
				// abrir planilla de asistencia en todas.
				division.AsignarPreceptor(preceptores[siguientePreceptor % preceptores.Count].Id);
				siguientePreceptor++;

				divisiones.Add(division);
			}

			porCurso[curso.Id] = divisiones;
			todas.AddRange(divisiones);
		}

		await _unidadDeTrabajo.Divisiones.AgregarRangoAsync(todas);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Divisiones sembradas: {Cantidad}, todas con preceptor.", todas.Count);

		return porCurso;
	}
	#endregion

	#region Cátedras, designaciones y horarios
	private async Task<List<Catedra>> SembrarCatedrasAsync(
		List<Curso> cursos,
		Dictionary<Guid, List<Division>> divisionesPorCurso,
		Dictionary<Guid, List<Materia>> materiasPorCurricula,
		List<Docente> profesores)
	{
		var catedras = new List<Catedra>();
		var siguienteProfesor = 0;
		var conSuplente = 0;

		// Qué designación hay que poner en funciones en cada cátedra, para aplicarlo DESPUÉS del
		// primer guardado. Ver el comentario de PonerEnFuncionesDeTodasAsync: el mapeo no admite
		// insertar la cátedra y su situación "en funciones" en la misma unidad de trabajo.
		var enFuncionesPorCatedra = new Dictionary<Guid, Guid>();

		for (var i = 0; i < cursos.Count; i++)
		{
			var materias = materiasPorCurricula.Values.ElementAt(i);

			foreach (var division in divisionesPorCurso[cursos[i].Id])
			{
				foreach (var materia in materias)
				{
					// La cátedra guarda una INSTANTÁNEA de la carga horaria de la materia. Es
					// legítimo porque el dato está congelado con la currícula vigente.
					var catedra = new CatedraBuilder()
						.ConMateria(materia.Id)
						.ConDivision(division.Id)
						.ConCargaHoraria(materia.HorasCatedra)
						.Build();

					var titular = profesores[siguienteProfesor % profesores.Count];
					siguienteProfesor++;

					var designacionTitular = catedra.Designar(
						titular.Id,
						Cargo.Titular,
						DateTime.Today.AddMonths(-6),
						fechaFin: null);

					var designacionEnFunciones = designacionTitular;
					var docenteEnFunciones = titular;

					// Algunas cátedras tienen además un suplente, que es quien queda EN
					// FUNCIONES: el titular sigue designado pero no dictando. "Uno en funciones"
					// es un campo único en Catedra, no un booleano por designación.
					if (catedras.Count % CATEDRAS_POR_SUPLENTE == CATEDRAS_POR_SUPLENTE - 1)
					{
						var suplente = profesores[siguienteProfesor % profesores.Count];
						siguienteProfesor++;

						var designacionSuplente = catedra.Designar(
							suplente.Id,
							Cargo.Suplente,
							DateTime.Today.AddDays(-15),
							fechaFin: DateTime.Today.AddDays(45),
							reemplazaA: designacionTitular);

						designacionEnFunciones = designacionSuplente;
						docenteEnFunciones = suplente;
						conSuplente++;
					}

					AsignarHorarios(catedra, division.Id, docenteEnFunciones.Id);

					enFuncionesPorCatedra[catedra.Id] = designacionEnFunciones;
					catedras.Add(catedra);
				}
			}
		}

		await _unidadDeTrabajo.Catedras.AgregarRangoAsync(catedras);
		await _unidadDeTrabajo.GuardarCambiosAsync();

		await PonerEnFuncionesDeTodasAsync(catedras, enFuncionesPorCatedra);

		_logger.LogInformation(
			"Cátedras sembradas: {Cantidad} ({ConSuplente} con suplente en funciones).",
			catedras.Count,
			conSuplente);

		return catedras;
	}

	/// <summary>
	/// Segundo guardado: recién ahora se marca quién está en funciones en cada cátedra.
	/// </summary>
	/// <remarks>
	/// No es una preferencia de estilo, es una restricción del mapeo.
	/// <c>FK_CATEDRA_SITUACION_REVISTA</c> es una FK compuesta de
	/// <c>(catedra_id, situacion_en_funciones_id)</c> contra la clave alterna de
	/// <c>SituacionRevista</c> (<c>CatedrasConfiguration.cs:64-67</c>), y
	/// <c>FK_SITUACION_REVISTA_CATEDRA</c> va en el sentido contrario. Las dos juntas forman un
	/// <strong>ciclo de claves foráneas</strong>: si la cátedra y su situación "en funciones" se
	/// insertan en el mismo <c>SaveChanges</c>, EF no puede ordenar los comandos y falla con
	/// <c>"circular dependency was detected in the data to be saved"</c>.
	/// <para>
	/// Producción no lo sufre porque <c>ServicioCatedra</c> parte el trabajo en tres casos de uso
	/// con tres <c>GuardarCambiosAsync()</c> (<c>CrearCatedraAsync</c> →
	/// <c>DesignarDocenteAsync</c> → <c>PonerEnFuncionesAsync</c>). El seeder hace lo mismo: deja
	/// la cátedra y la designación guardadas y en una segunda pasada marca las funciones.
	/// Registrado en docs/todos.md.
	/// </para>
	/// </remarks>
	private async Task PonerEnFuncionesDeTodasAsync(
		List<Catedra> catedras,
		Dictionary<Guid, Guid> enFuncionesPorCatedra)
	{
		foreach (var catedra in catedras)
		{
			catedra.PonerEnFunciones(enFuncionesPorCatedra[catedra.Id]);
		}

		_unidadDeTrabajo.Catedras.ModificarRango(catedras);
		await _unidadDeTrabajo.GuardarCambiosAsync();
	}

	/// <summary>
	/// Llena la cátedra con tantos horarios como su carga horaria, tomando el primer slot que
	/// esté libre para la división y para el docente en funciones. Si no queda ninguno, deja la
	/// cátedra con horas sin asignar en lugar de fallar: el dataset sigue siendo válido y el
	/// resumen lo reporta.
	/// </summary>
	private void AsignarHorarios(Catedra catedra, Guid divisionID, Guid docenteID)
	{
		foreach (var (dia, hora) in SlotsDelTurnoManana())
		{
			if (catedra.TieneHorasCompletas())
			{
				return;
			}

			var slotDeDivision = (divisionID, dia, hora);
			var slotDeDocente = (docenteID, dia, hora);

			if (_ocupacionDeDivision.Contains(slotDeDivision) || _ocupacionDeDocente.Contains(slotDeDocente))
			{
				continue;
			}

			catedra.AgregarHorario(new HorarioBuilder()
				.ConTurno(Turno.Mañana)
				.ConDia(dia)
				.ConHoraInicio(hora)
				.ConDuracionHoraCatedra(MINUTOS_HORA_CATEDRA)
				.Build());

			_ocupacionDeDivision.Add(slotDeDivision);
			_ocupacionDeDocente.Add(slotDeDocente);
		}
	}

	/// <summary>
	/// Los slots del turno mañana: cinco días hábiles por nueve horas cátedra de 40 minutos
	/// desde las 07:20, que es lo que entra en los límites 07:00–13:00 que fija
	/// <c>Horario</c> para ese turno.
	/// </summary>
	private static IEnumerable<(Dia Dia, TimeOnly Hora)> SlotsDelTurnoManana()
	{
		var dias = new[] { Dia.Lunes, Dia.Martes, Dia.Miercoles, Dia.Jueves, Dia.Viernes };

		foreach (var dia in dias)
		{
			var hora = new TimeOnly(7, 20);

			while (hora <= new TimeOnly(13, 0).AddMinutes(-MINUTOS_HORA_CATEDRA))
			{
				yield return (dia, hora);
				hora = hora.AddMinutes(MINUTOS_HORA_CATEDRA);
			}
		}
	}
	#endregion

	#region Cursantes y calificaciones
	private async Task<List<Cursante>> SembrarCursantesAsync(
		Dictionary<Guid, List<Division>> divisionesPorCurso,
		List<Alumno> alumnos,
		Dictionary<Guid, List<Materia>> materiasPorCurricula,
		List<Curso> cursos)
	{
		var periodo = DateTime.Today.Year.ToString();
		var inicioDelCiclo = new DateTime(DateTime.Today.Year, 3, 1);
		var cursantes = new List<Cursante>();
		var siguienteAlumno = 0;

		for (var i = 0; i < cursos.Count; i++)
		{
			var materias = materiasPorCurricula.Values.ElementAt(i);

			foreach (var division in divisionesPorCurso[cursos[i].Id])
			{
				for (var k = 0; k < ALUMNOS_POR_DIVISION && siguienteAlumno < alumnos.Count; k++)
				{
					var cursante = new CursanteBuilder()
						.ConDivision(division.Id)
						.ConAlumno(alumnos[siguienteAlumno].Id)
						// Una instancia NUEVA de CicloLectivo por cursante, y no una compartida.
						// CursantesConfiguration lo mapea con OwnsOne, o sea como owned entity, y
						// EF exige un dueño por instancia: si varios cursantes comparten el mismo
						// objeto, EF lo asocia a uno solo y los demás van a la base con
						// ciclo_lectivo en NULL, que la columna rechaza por NOT NULL.
						.ConCicloLectivo(CicloLectivo.Crear(periodo))
						.ConFechaInicio(inicioDelCiclo)
						// Uno de cada diez recursa: hace visible el caso en las pantallas.
						.ComoRecursante(siguienteAlumno % 10 == 0)
						.Build();

					siguienteAlumno++;

					CargarCalificaciones(cursante, materias);
					cursantes.Add(cursante);
				}
			}
		}

		await _unidadDeTrabajo.Cursantes.AgregarRangoAsync(cursantes);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation(
			"Cursantes inscriptos: {Cantidad}, con {Calificaciones} calificaciones.",
			cursantes.Count,
			cursantes.Sum(cursante => cursante.Calificaciones.Count));

		return cursantes;
	}

	/// <summary>
	/// Carga dos instancias por materia en las primeras <see cref="MATERIAS_CALIFICADAS"/>
	/// materias del plan: un parcial aprobado o no, y un recuperatorio sólo cuando el parcial
	/// quedó debajo de 6. Es la secuencia que la pantalla de calificaciones espera poder mostrar.
	/// </summary>
	private void CargarCalificaciones(Cursante cursante, List<Materia> materias)
	{
		var fechaDelParcial = new DateTime(DateTime.Today.Year, 6, 15);

		foreach (var materia in materias.Take(MATERIAS_CALIFICADAS))
		{
			var notaDelParcial = _faker.Random.Int(2, 10);

			cursante.RegistrarCalificacion(
				materia.Id,
				fechaDelParcial,
				Instancia.Parcial,
				notaDelParcial,
				observacion: null);

			if (notaDelParcial < 6)
			{
				cursante.RegistrarCalificacion(
					materia.Id,
					fechaDelParcial.AddMonths(1),
					Instancia.Recuperatorio,
					_faker.Random.Int(4, 10),
					observacion: "Recuperatorio del primer parcial.");
			}
		}
	}
	#endregion

	#region Planillas de asistencia
	private async Task<List<PlanillaAsistencia>> SembrarPlanillasAsync(
		Dictionary<Guid, List<Division>> divisionesPorCurso,
		List<Cursante> cursantes,
		List<Docente> preceptores)
	{
		var planillas = new List<PlanillaAsistencia>();
		var cursantesPorDivision = cursantes
			.GroupBy(cursante => cursante.DivisionID)
			.ToDictionary(grupo => grupo.Key, grupo => grupo.Select(cursante => cursante.Id).ToList());

		// Ordenados de ayer hacia atrás, así que el último es el día más viejo.
		var fechas = UltimosDiasHabiles(DIAS_DE_ASISTENCIA);
		var fechaMasVieja = fechas[^1];

		foreach (var division in divisionesPorCurso.Values.SelectMany(lista => lista))
		{
			if (!cursantesPorDivision.TryGetValue(division.Id, out var idsDeCursantes) || idsDeCursantes.Count == 0)
			{
				continue;
			}

			foreach (var fecha in fechas)
			{
				// Nace completa, con todos en Presente y un registro por cursante.
				var planilla = PlanillaAsistencia.Abrir(
					division.Id,
					fecha,
					division.Preceptor!.Value,
					idsDeCursantes);

				MarcarAlgunasInasistencias(planilla, idsDeCursantes);

				// La planilla del día más viejo queda cerrada: así hay de los dos estados para
				// probar que una planilla cerrada no se modifica y que se puede reabrir.
				if (fecha == fechaMasVieja)
				{
					planilla.Cerrar();
				}

				planillas.Add(planilla);
			}
		}

		await _unidadDeTrabajo.PlanillasAsistencia.AgregarRangoAsync(planillas);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Planillas de asistencia sembradas: {Cantidad}.", planillas.Count);

		return planillas;
	}

	private void MarcarAlgunasInasistencias(PlanillaAsistencia planilla, List<Guid> idsDeCursantes)
	{
		foreach (var cursanteID in idsDeCursantes)
		{
			switch (_faker.Random.Int(1, 20))
			{
				case 1:
					planilla.MarcarInasistencia(cursanteID, "Sin aviso.");
					break;
				case 2:
					planilla.MarcarAusencia(cursanteID, "Con certificado médico.");
					break;
				case 3:
					planilla.MarcarTardanza(cursanteID, TimeSpan.FromMinutes(_faker.Random.Int(5, 25)));
					break;
				default:
					// Queda Presente, que es como nació el registro.
					break;
			}
		}
	}

	/// <summary>
	/// Los últimos <paramref name="cantidad"/> días hábiles terminando ayer. Nunca hoy ni
	/// adelante: una planilla con fecha futura es inválida en el dominio.
	/// </summary>
	private static List<DateTime> UltimosDiasHabiles(int cantidad)
	{
		var dias = new List<DateTime>(cantidad);
		var fecha = DateTime.Today.AddDays(-1);

		while (dias.Count < cantidad)
		{
			if (fecha.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
			{
				dias.Add(fecha);
			}

			fecha = fecha.AddDays(-1);
		}

		return dias;
	}
	#endregion

	#region Usuarios
	/// <summary>
	/// Un usuario por rol, con la misma contraseña (<see cref="PASSWORD_DEMO"/>) y hasheada con
	/// el <c>IServicioSeguridad</c> real, que es el mismo que después valida el login.
	/// </summary>
	private async Task<List<Usuario>> SembrarUsuariosAsync(PlantelDocente plantel)
	{
		// Cada usuario cuelga de un docente real: Usuario exige un DocenteID no vacío, y así el
		// perfil que muestra la app tiene datos personales de verdad detrás.
		//
		// Los nombres tienen forma de CORREO y no de usuario corto ("admin") porque
		// LoginUsuarioViewModel valida ese campo con una RegularExpression de email y deja el
		// botón deshabilitado si no matchea, mientras que el dominio y
		// ServicioAutenticacion.Login hablan de "username" y aceptan cualquier string no vacío.
		//
		// Y el dominio del correo es "edu.ar" y no algo más descriptivo porque la columna es
		// varchar(18) (UsuariosConfiguration.cs:47). Ese límite es la prueba de que el campo se
		// pensó como nombre de usuario corto y no como dirección de correo: no hay email
		// institucional real que entre en 18 caracteres. Son dos vocabularios en conflicto y acá
		// se cede al de la interfaz, que es quien bloquea el botón, para no cambiar una validación
		// que está fuera del alcance de esta fase. Registrado en docs/todos.md.
		var cuentas = new (string Username, Rol Rol, Docente Docente)[]
		{
			("admin@edu.ar", Rol.Admin, plantel.Directivos[0]),
			("direccion@edu.ar", Rol.Direccion, plantel.Directivos[1]),
			("secretaria@edu.ar", Rol.Secretaria, plantel.Secretarias[0]),
			("docente@edu.ar", Rol.Docente, plantel.Profesores[0])
		};

		var usuarios = new List<Usuario>(cuentas.Length);

		foreach (var cuenta in cuentas)
		{
			var credenciales = _seguridad.HashPassword(PASSWORD_DEMO);

			// El constructor que toma List<Rol> y no AgregarRol(string): evita resolver el rol
			// por descripción —que depende de la colación de una columna varchar no unicode— y
			// usa las instancias ya adjuntas al contexto en AdjuntarDatosDeReferencia().
			usuarios.Add(new Usuario(
				cuenta.Docente.Id,
				cuenta.Username,
				credenciales["salt"],
				credenciales["hash"],
				new List<Rol> { cuenta.Rol }));
		}

		await _unidadDeTrabajo.Usuarios.AgregarRangoAsync(usuarios);
		await _unidadDeTrabajo.GuardarCambiosAsync();
		_logger.LogInformation("Usuarios sembrados: {Cantidad}, uno por rol.", usuarios.Count);

		await VerificarQueLosUsuariosPuedenIngresarAsync(cuentas.Select(cuenta => cuenta.Username));

		return usuarios;
	}

	/// <summary>
	/// Relee cada usuario de la base y comprueba que su clave valide y que su rol haya quedado
	/// asociado.
	/// </summary>
	/// <remarks>
	/// Un seeder que informa "4 usuarios sembrados" y deja cuentas con las que no se puede entrar
	/// es peor que uno que falla: el problema aparece recién al intentar usar la app, lejos de su
	/// causa. Esta comprobación recorre el mismo camino que el login —buscar por username, validar
	/// con <c>IServicioSeguridad</c>, leer los roles con <c>Include</c>— y por eso también
	/// verifica de paso que el <c>AttachRange</c> de los roles hizo lo que debía: si hubiera
	/// fallado, acá se leería un usuario sin roles.
	/// </remarks>
	private async Task VerificarQueLosUsuariosPuedenIngresarAsync(IEnumerable<string> usernames)
	{
		foreach (var username in usernames)
		{
			var usuario = await _unidadDeTrabajo.Usuarios.BuscarPorUsernameAsync(username)
				?? throw new InvalidOperationException(
					$"El usuario '{username}' se sembró pero no se puede releer de la base.");

			using var clave = new System.Net.NetworkCredential(string.Empty, PASSWORD_DEMO).SecurePassword;

			if (!_seguridad.ValidatePassword(clave, usuario.PasswordSalt, usuario.PasswordHash))
			{
				throw new InvalidOperationException(
					$"La clave sembrada para '{username}' no valida: nadie podría iniciar sesión con esa cuenta.");
			}

			if (usuario.Roles.Count == 0)
			{
				throw new InvalidOperationException(
					$"El usuario '{username}' quedó sin roles, así que entraría sin ningún permiso.");
			}
		}

		_logger.LogInformation("Verificado: las cuatro cuentas validan su clave y tienen su rol asociado.");
	}
	#endregion

	#region Generadores de volumen e identidad
	private DatosPersonalesBuilder DatosPersonalesDe(Sexo sexo, int edad)
	{
		var genero = sexo == Sexo.Femenino ? Bogus.DataSets.Name.Gender.Female : Bogus.DataSets.Name.Gender.Male;

		return new DatosPersonalesBuilder()
			.ConApellidoYNombre(_faker.Name.LastName(), _faker.Name.FirstName(genero))
			.ConDocumento(SiguienteDocumento())
			.ConSexo(sexo)
			.ConEdad(edad)
			.ConNacionalidad("Argentina");
	}

	/// <summary>
	/// Edad de un docente en actividad. <c>Docente</c> rechaza menores de 18 y la edad
	/// jubilatoria, que es <strong>asimétrica por sexo</strong> (<c>Docente.cs:43</c>): más de 64
	/// para los varones, más de 59 para las mujeres. Un rango único para ambos rompe la
	/// invariante la mitad de las veces.
	/// </summary>
	private int EdadDocenteEnActividad(Sexo sexo) =>
		_faker.Random.Int(28, sexo == Sexo.Masculino ? 64 : 59);

	private DomicilioBuilder DomicilioAlAzar() =>
		new DomicilioBuilder()
			.ConCalle(_faker.Address.StreetName())
			.ConAltura(_faker.Random.Int(100, 4999).ToString())
			.ConVivienda(_faker.PickRandom<Vivienda>())
			.ConLocalidad(_faker.Address.City())
			.ConProvincia("Buenos Aires")
			.ConPais("Argentina");

	private string TelefonoAlAzar() =>
		$"11{_faker.Random.Int(10_000_000, 99_999_999)}";

	/// <summary>DNI de 8 dígitos, secuencial: el dominio exige <c>^(\d{7,8})$</c> y la base, unicidad.</summary>
	private string SiguienteDocumento() =>
		(_siguienteDocumento++).ToString();

	/// <summary>Legajo docente de 6 dígitos exactos (<c>^(\d{6})$</c>).</summary>
	private string SiguienteLegajoDocente() =>
		(_siguienteLegajoDocente++).ToString("D6");

	/// <summary>Legajo de alumno con el formato dos letras + cuatro dígitos (<c>^([A-Z]{2}\d{4})$</c>).</summary>
	private string SiguienteLegajoAlumno() =>
		$"AL{_siguienteLegajoAlumno++:D4}";

	/// <summary>
	/// CUIL con prefijo de los habilitados por <c>Docente</c>
	/// (<c>20|23|24|27|30|33|34</c>) más el documento y un dígito verificador de relleno.
	/// </summary>
	private static string CuilPara(int documento) =>
		$"20{documento:D8}9";
	#endregion

	#region Tipos de apoyo
	private sealed record PlantelDocente(
		List<Docente> Todos,
		List<Docente> Directivos,
		List<Docente> Secretarias,
		List<Docente> Preceptores,
		List<Docente> Profesores);
	#endregion
}

/// <summary>Conteos de lo que quedó en la base, para el informe final de la corrida.</summary>
internal sealed record ResumenDeSiembra(
	int Docentes,
	int Alumnos,
	int Cursos,
	int Curriculas,
	int Materias,
	int Divisiones,
	int Catedras,
	int HorariosAsignados,
	int HorariosSinAsignar,
	int Cursantes,
	int Calificaciones,
	int Planillas,
	int Usuarios);
