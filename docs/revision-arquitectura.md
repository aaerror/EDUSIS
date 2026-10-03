# Revisión de arquitectura — EDUSIS

> Fecha: 2026-09-04 · Actualizado: 2026-09-23 (límites de `Division`, `Cursante`, `Calificacion` y asistencias resueltos en `002-fix-dominio`) · Reverificado 2026-09-25 contra `ff2de55` (mapeo EF alineado con los agregados nuevos) y `8073161` (se quita `Docente._licencias`) · Reverificado 2026-09-26 contra `b377fa5`…`0f39316` (contratos de repositorio, implementaciones, `UnitOfWork` completo, MediatR escaneando `Core`, despacho de eventos en cascada, fakes y tests de integración) · Reverificado 2026-09-28 contra `b3ac87b`…`76bc1d8` (cierre del bloque `Core`: catorce servicios, cinco nuevos por agregado, `Core` compila con 0 errores). · Reverificado 2026-10-03 contra `e8d5043` (adaptación de `WPF_Desktop` al refactor, 42 commits sobre `f4f57fb`: la solución compila con 0 errores y la suite de la capa de presentación, 320 pruebas, pasa; **la verificación funcional en Windows sigue pendiente**, sin base de datos instalada).
> Alcance: lectura estructural del modelo de dominio, el kernel compartido, la capa `Core`, los repositorios, el despacho de eventos y los mapeos EF, evaluada contra el problema de negocio (gestión escolar: docentes, alumnos, cursos, divisiones, currículas, licencias). Material de trabajo para planificar mejoras; no describe el estado deseado como si ya existiera.

## Cómo está organizado este documento

1. **Veredicto corto** y **Lo que está bien**.
2. **Parte 1 — Problemas transversales**: errores a nivel DDD general que aparecen en varios lugares.
3. **Parte 2 — Detalle por agregado**: una ficha por agregado raíz, para ir trabajando de a uno. Cada ficha tiene: rol de negocio, estado e invariantes actuales, problemas, relaciones, y pendientes (hallazgos de la suite + trabajo de diseño).
4. **Qué falta** (transversal) y **Prioridad sugerida**.
5. **Contexto — hallazgos de la suite**.

---

## Veredicto corto

La arquitectura de capas está bien hecha y **respetada**, no sólo declarada. El modelo de dominio es **rico**, no anémico. Donde el DDD flaquea es en los **límites de agregado** y en piezas **a medio cablear** (eventos de dominio, el agregado `Cursante`, calificaciones) que hoy son costo sin beneficio. Nada de esto es estructural-grave: es un modelo joven con varias decisiones de diseño sin cerrar.

## Lo que está bien

- **Inversión de dependencias real.** `Domain` no referencia a nadie salvo MediatR; `Infrastructure → Core → Domain`; `WPF_Desktop` es el único proyecto que toca `Infrastructure`, y sólo desde el composition root. El puerto `Core.Shared.Documentos.IGeneradorDocumentos` mantiene iText fuera de `Core`.
- **Agregados con comportamiento.** Invariantes en constructor y métodos, setters `private`, colecciones privadas expuestas como `IReadOnlyCollection<T>`, constructor privado sin parámetros para EF, factories que validan. Los servicios de `Core` orquestan y no re-validan.
- **Value objects reales.** `ValueObject` con igualdad estructural; `DatosPersonales`, `Domicilio`, `RangoFechas`, `Horario` son inmutables y devuelven instancias nuevas al "mutar". `RangoFechas` concentra bien la lógica de intervalos de fecha.
- **Eventos de dominio con despacho correcto.** `Entity` mantiene la cola; `UnitOfWork.GuardarCambiosAsync` recorre el `ChangeTracker` y publica **antes** de `SaveChangesAsync` (`Infrastructure/Extensions/MediatrExtension.cs`).
- **Lenguaje ubicuo en español, consistente** con conceptos reales de la escuela.

---

# Parte 1 — Problemas transversales (nivel DDD general)

### T-1. Límites de agregado sin decidir

Es el problema de fondo. Se manifiesta en varios agregados (ver Parte 2), pero el patrón es común:

- ~~**`Cursante` no es un agregado huérfano: es una entidad hija mal ubicada.** Discusión resuelta (ver ficha `Cursante` en Parte 2): pertenece dentro de `Division` (y por transitividad, del agregado `Curso`), igual que `Puesto` en `Docente`. No necesita `ICursanteRepository` ni entrada en `IUnitOfWork` — necesita que `Division._cursantes` deje de ser `List<Guid>` y pase a ser `List<Cursante>`.~~ **Resuelto en `002-fix-dominio`, en sentido contrario**: `Division` se separó de `Curso` como raíz propia y `Cursante` quedó como **agregado raíz** (`ICursanteRepository`, entrada en `IUnitOfWork`). `Division` ya no guarda cursantes: la inscripción vive en `Cursante.DivisionID` y los conteos (cupo, baja) se resuelven por repositorio. Ver ficha `Cursante`.
- ~~**`Licencia` tiene doble pertenencia**: es agregado raíz (`ILicenciaRepository`, está en `IUnitOfWork`) y a la vez colección hija de `Docente` (`Docente._licencias`). EF tiene tres configuraciones distintas para `Licencia`.~~ **Resuelto en `8073161`**: se eliminaron `_licencias`/`Licencias` y `BuscarDocentePorIDConLicenciasAsync` de `Docente`/`IDocenteRepository`. `Licencia` queda como único agregado raíz, con una sola configuración EF activa (`LicenciasConfiguration`); el `OwnsMany` de `DocentesConfiguration.cs` (que referenciaba una propiedad `Dias` inexistente) ya estaba comentado y sigue sin invocarse.
- ~~**`SituacionRevista` es hija de `Materia`** pero tiene entidad y ciclo de vida propios (titular/suplente/en funciones) suficientes para ser agregado.~~ **Resuelto en `002-fix-dominio`, pero no como decía esta línea**: `SituacionRevista` *no* pasó a ser agregado. El análisis mostró que sus invariantes más fuertes son **de conjunto** (uno solo en funciones, un solo ocupante del puesto) y que ningún agregado las veía. Sigue siendo entidad interna, pero de una raíz nueva — `Catedra` (materia + división) — que sí las protege. Ver ficha de `Curricula` y la tabla de límites en `CLAUDE.md`.
- ~~**Calificaciones modeladas dos veces**: activo en `Materia._calificaciones`, comentado en `Division`/`Cursante`.~~ **Resuelto**: `Calificacion` es entidad hija de `Cursante` (`Domain/Cursantes/Calificaciones/`), con `Id` propio y `MateriaID` como referencia cruzada.
- ~~**Cursante vs Alumno**: `Division._cursantes` es `List<Guid>`; los llamadores pasan ids de `Alumno`, pero el agregado `Cursante` tiene `AlumnoID` (o sea, cursante ≠ alumno). El concepto de "alumno inscripto en una división en un ciclo lectivo" no está resuelto.~~ **Resuelto**: `Cursante` *es* la inscripción (alumno + división + ciclo lectivo); `Division._cursantes` se eliminó.

### T-2. Repositorio genérico con `Expression`

~~`Domain/Shared/IRepository.cs` expone `BuscarAsync(Expression<Func<TEntity, bool>> predicate)`. Deja que `Core` empuje queries arbitrarias al contrato del dominio.~~ **Resuelto en `b377fa5` (2026-09-26)**: `IRepository<T>` ya no expone `BuscarAsync(Expression<…>)` ni `BuscarPorIDAsync(params object[])` — el contrato quedó en `AgregarAsync`, `AgregarRangoAsync`, `BuscarPorIDAsync(Guid)`, `BuscarTodosAsync`, `Modificar`, `ModificarRango`, `EliminarAsync(Guid)`, `EliminarRango`. Cada consulta puntual pasó a la interfaz de su propio agregado: `IDocenteRepository.BuscarActivosAsync()`/`ExisteDocenteConLegajoAsync(...)` reemplazan los `Docentes.BuscarAsync(x => x.Puesto.Posicion.Equals(...) && x.Puesto.EstaActivo())` que motivaban este punto (misma clase de bug que H-020, ya corregido también, ver ficha `Alumno`). `BuscarPorIDAsync(params object[] ids)` desapareció junto con el resto: el caso concreto que lo motivaba (`Licencia` por `(LicenciaID, DocenteID)`) ya se había resuelto antes, en `ff2de55`, cuando la PK de `Licencia` pasó a ser sólo `Id`.

### T-3. Los eventos de dominio no hacen nada end-to-end

- `LicenciaSolicitadaEventHandler.Handle`: `// TODO`, devuelve `Task.CompletedTask`.
- `MateriaEliminadaEventHandler`: llama a `BuscarPorIDAsync` **sin `await`** y elimina sin `SaveChanges`.
- H-022: ninguno de los dos está registrado (`InfrastructureDI` sólo escanea el assembly de `Infrastructure`).
- `AlumnoInscriptoDomainEvent`, `DocenteDesafectadoDomainEvent`, `LicenciaActivadaEvent`, `LicenciaCanceladaEvent`, `LicenciaFinalizadaEvent`, `MateriaRegistradaEvent` **no tienen handler**.
- `Domain/Shared/IEventHandler<T>` está definido y **no se usa** (MediatR aporta `INotificationHandler`).
- ~~`MateriaEliminadaEvent` vive en `Domain/Cursos/DomainEvents/` mientras que `MateriaRegistradaEvent` vive en `Domain/Curriculas/DomainEvents/`: la materia es hija de `Curricula`, no de `Curso` — el evento está en el módulo equivocado.~~ **Resuelto**: ambos viven en `Domain/Materias/DomainEvents/`.
- `PlanillaAsistenciaCerradaEvent` y `PlanillaAsistenciaReabiertaEvent` (nuevos) tampoco tienen handler todavía.

### T-4. Patrón "método `bool` que lanza"

Repartido por todo el dominio: ~~`Curricula.ExisteMateria` lanza `MateriaDuplicadaException`, `Division.ExisteCursante` lanza `NullReferenceException`, `Curso.ExisteDivision` lanza `ArgumentNullException`~~ (los tres métodos desaparecieron al separar los agregados en `002-fix-dominio`; conviene revisar si el patrón sobrevive en otros). Es exactamente la forma del bug H-008. Conviene un helper de guardas, o separar "existe" (predicado puro) de "exigir que exista" (lanza).

### T-5. Excepciones de infraestructura como reglas de negocio

`NullReferenceException` / `InvalidOperationException` / `ArgumentException` lanzadas a mano donde debería haber excepciones de dominio: ~~constructor de `Cursante`~~ (resuelto: `SinDatosDivisionException` / `SinDatosAlumnoException`), `Division` (descripción vacía o que no es una letra), `Curricula`, `Puesto.EstablecerComoPuestoFijo`, `Puesto.Rescindir`, y `Calificacion` (fecha futura, nota nula y observación extensa siguen con `ArgumentException` / `ArgumentNullException`).

### T-6. Sin abstracción de reloj

`DateTime.Today` directo en todo el dominio. Es la causa de que los tests de H-003 / H-005 necesitaran rodeos "el mismo día" y de que varias invariantes (`Docente` inactivo, `Puesto` vigente, `Licencia` activa) sean difíciles de ejercitar. Introducir `IDateTimeProvider` (o `TimeProvider`) destraba toda esa clase de problemas.

### T-7. Un solo bounded context

Un `Domain`, un `EdusisDBContext`, un `IUnitOfWork` con todos los repos. "Personas / Usuarios / Autenticación" es en la práctica otro contexto que el académico. Olor menor, no urgente.

### T-8. Otros smells transversales

- `Entity.Eventos` devuelve `null` cuando no hay eventos (debería ser colección vacía) y hace `ToList().AsReadOnly()` en cada acceso. `Entity.Equals` con `Id == Guid.Empty`: dos entidades transitorias comparan iguales.
- Migraciones desincronizadas de las configuraciones (FR-019 / H-023).
- Connection string y ruta de PDFs hardcodeadas en `Infrastructure/InfrastructureDI.cs`.
- Nombres mezclados ES/EN: carpeta `Core/ServicioSecurity/` con clase `ServicioSeguridad`; `CrearCalificationRequest`; ~~archivo `MateriaNoEncontrada.cs` sin sufijo `Exception`~~ (eliminado).
- Sin control de concurrencia (`rowversion`) para una app de escritorio multiusuario.

---

# Parte 2 — Detalle por agregado

> Convención de cada ficha:
> **Rol** · **Estado / invariantes hoy** · **Problemas** · **Relaciones** · **Pendientes**.

## Personas — `Persona` (base abstracta) + value objects

**Rol.** Base común de `Alumno` y `Docente`: identidad personal, domicilio y datos de contacto. No es agregado raíz por sí misma.

**Estado / invariantes hoy.**
- `DatosPersonales` (VO): apellido, nombre, documento (regex 7–8 dígitos), sexo, fecha de nacimiento (no futura), nacionalidad. Inmutable, con `CambiarNombreCompleto` / `CambiarSexo` que devuelven instancia nueva.
- `Domicilio` (VO) con `Direccion`, `Ubicacion`, `Vivienda`.
- `Email` y `Telefono` como `string`, validados por regex en el constructor (`FormatoEmailInvalidoException` / `FormatoTelefonoInvalidoException`).
- ~~`Contacto` (VO): tipo + descripción. Igualdad estructural ya corregida (H-002).~~ **Descartado** (commit `f7ad2f8`): el contacto se resuelve 1:1 con `Email` y `Telefono` en `Persona`.
- `DatosPersonales.EsMayorDeEdad()` (edad ≥ 18). `DatosNacimiento` se eliminó: la fecha de nacimiento vive en `DatosPersonales`.

**Problemas.**
- ~~**`Contacto` existe pero `Persona` no lo usa**~~ y ~~**código muerto duplicado** de `EsEmailValido` / `EsTelefonoValido`~~: resueltos al descartar `Contacto`; la validación vive sólo en `Persona`. H-001 queda cerrado por eliminación.
- ~~**`DatosPersonales.Edad()` inexacta**~~ **Resuelto 2026-09-23**: resta años calendario y descuenta uno si el cumpleaños de este año todavía no llegó; un nacido el 29/02 cumple el 28/02 en años no bisiestos. Antes dividía días por 365 y, por los bisiestos, adelantaba la edad unos 4 días a los 18 (afectaba las reglas de edad de `Docente`).
- **`DatosPersonales.NombreCompleto()`** es un método no traducible a SQL; usarlo en un `Where` de repositorio rompe (H-020).
- `IPersonaRepository` existe como interfaz base de `IAlumnoRepository` / `IDocenteRepository` pero no está en `IUnitOfWork` (esto está bien; se anota para que no confunda).

**Relaciones.** Base de `Alumno` y `Docente` (herencia TPT en EF).

**Pendientes.**
- ~~Decidir si `Persona` administra una colección de `Contacto`~~ (resuelto: `Email`/`Telefono` planos).
- ~~Unificar la validación de email/teléfono en un solo lugar~~ (resuelto).
- ~~Corregir `Edad()` para que contemple bisiestos~~ (resuelto).

---

## `Alumno` (agregado raíz)

**Rol.** Persona con legajo escolar y un período de permanencia en la institución (alta / baja).

**Estado / invariantes hoy.**
- `Legajo`: dos letras mayúsculas y cuatro dígitos (p. ej. `AL0001`), validado en el constructor con `FormatException`. El mensaje de error dice "legajo del **docente**" (copiado de `Docente`): hay que corregirlo.
- `Periodo` (`RangoFechas`): inicia en el alta; `Desinscribir()` lo cierra con la fecha de hoy.
- `EstaActivo()` = período iniciado y sin fecha de fin (corregido en H-003).
- Guard de `Desinscribir()`: rechaza la baja de un alumno ya inactivo con `AlumnoInactivoException` (corregido en H-003).

**Problemas.**
- **No valida edad**: `AlumnoMayorDeEdadException` está definida y nunca se lanza (H-004). No hay regla de negocio escrita (¿edad tope?, ¿bloquea el alta o sólo marca?).
- ~~**Relación con la inscripción difusa**~~: resuelto — un alumno cursa a través de `Cursante` (una inscripción por división y ciclo lectivo).
- ~~`Legajo` sin invariante de formato.~~ Resuelto; queda el mensaje de error equivocado y el uso de `FormatException` en vez de una excepción de dominio (T-5).

**Relaciones.**
- Hereda `Persona`.
- Referenciado por id desde `Cursante.AlumnoID`.
- Repo: `IAlumnoRepository` (`BuscarPorNombreCompletoAsync` roto — H-020).

**Eventos.** `AlumnoInscriptoDomainEvent` (alta), `AlumnoDesinscriptoDomainEvent` (baja). **Sin handlers.**

**Pendientes.**
- H-004: definir la regla de mayoría de edad o borrar la excepción.
- ~~Definir formato de `Legajo`~~ (resuelto: `^[A-Z]{2}\d{4}$`); corregir su mensaje de error.
- Arreglar `IAlumnoRepository.BuscarPorNombreCompletoAsync` (H-020) comparando contra columnas mapeadas.

---

## `Docente` (agregado raíz) + `Puesto` (entidad hija)

**Rol.** Persona con legajo y CUIL, un período institucional y cargos docentes (`Puesto`). Ya no administra licencias (ver `Licencia`).

**Estado / invariantes hoy.**
- Alta: edad ≥ 18 (`DocenteMenorDeEdadException`), no en edad jubilatoria por sexo (`DocenteEnEdadJubilatoriaException`), fecha de alta no futura, legajo de 6 dígitos, CUIL con prefijo válido.
- `Activo` (bool): se fija en el alta y pasa a `false` en `Desafectar()` (corregido en H-005).
- `Desafectar()` rechaza una segunda baja con `DocenteInactivoException` (H-005).
- Cargos: `AsignarCargoDocente`, `ModificarCargoDocente`, `RescindirCargoDocente`, `EliminarCargoDocente`, `CambiarEventualidadCargoDocente`; todos chequean `!Activo`.

**Problemas.**
- ~~**Doble pertenencia de `Licencia`**: `Docente._licencias` + agregado raíz propio (ver `Licencia`). EF con tres configs para el mismo tipo.~~ **Resuelto en `8073161`** (ver Parte 1, T-1).
- **`Docente.Puesto` (singular) vs `_puestos` (colección)**: dos formas de exponer el mismo concepto; `Puesto` singular está `Ignore`d en la config de EF.
- `DocenteConfirmadoEnCargoDocenteException` (H-006) y `DocenteSinCargoDocenteException` (H-007) definidas y nunca lanzadas; hoy `Puesto.EstablecerComoPuestoFijo` usa `ArgumentException`.
- `Puesto`: `EstablecerComoPuestoFijo` / `Rescindir` lanzan `ArgumentException`; `Rescindir()` sin fecha **no hace nada** si el puesto no está activo (falla silenciosa). `EstadoPuesto` (Pendiente / Activo / Inactivo) es una máquina de estados implícita sin transiciones explícitas.
- Los chequeos `!Activo` de los métodos de cargo no son ejercitables sin abstracción de reloj (T-6).

**Relaciones.**
- Hereda `Persona`.
- Contiene `Puesto` (composición).
- Referenciado por id desde `Licencia.DocenteID`, `Usuario.DocenteID`, `SituacionRevista.DocenteID`, `Division.Preceptor`.
- Repo: `IDocenteRepository`.

**Eventos.** `DocenteDesafectadoDomainEvent`. **Sin handler.**

**Pendientes.**
- ~~Decidir: `Licencia` ¿hija de `Docente` o agregado independiente?~~ (resuelto en `8073161`: agregado independiente).
- Unificar `Puesto` / `_puestos`.
- H-006, H-007: cablear las excepciones específicas o borrarlas.
- Modelar `EstadoPuesto` como transiciones explícitas; que `Rescindir()` no falle en silencio.

---

## `Licencia` (agregado raíz)

**Rol.** Solicitud de licencia de un docente por artículo, con un ciclo de estados y un período.

**Estado / invariantes hoy.**
- `DocenteID`, `Articulo`, `Estado` (Pendiente → Activa → Finalizada / Cancelada), `Periodo` (`RangoFechas`), `Observacion` (≤ 250).
- Transiciones: `AprobarLicencia`, `CancelarLicencia`, `FinalizarLicencia`, `ExtenderLicencia`, `EstablecerFechaFinalizacion`, `EstablecerLicenciaIndefinida`, `ModificarPeriodo`.
- `EsIndefinida()` ya alineado con `Periodo.EsIndeterminado()` (corregido en H-013).

**Problemas.**
- ~~**Doble pertenencia** (ver `Docente`). La config `OwnsMany` comentada en `DocentesConfiguration` referencia una propiedad `Dias` inexistente → estaba comentada por algo.~~ **Resuelto en `8073161`** (ver Parte 1, T-1). El `OwnsMany` comentado sigue en el archivo como registro histórico, pero ya no representa una doble pertenencia real.
- ~~**Clave compuesta `(LicenciaID, DocenteID)`**: `DocenteID` es una referencia, no debería ser parte de la PK.~~ **Resuelto en `ff2de55`**: la PK pasó a ser sólo `x.Id`, con `DocenteID` como FK simple (`HasOne<Docente>().WithMany()`, antes comentada).
- ~~`EstablecerFechaFinalizacion` exige a la vez fecha futura y licencia activa: combinación rara para un alta con plazo — de hecho `ServicioLicencia.SolicitarLicencia` con `Dias > 0` rompía por esto (H-016, capa `Core`).~~ **[RESUELTO]** en el bloque `Core` (`b3ac87b`…`76bc1d8`, 2026-09-28): `ServicioLicencia.SolicitarLicencia` ya no pasa por `EstablecerFechaFinalizacion` cuando `Dias > 0` — construye la `Licencia` directamente con la fecha de fin calculada (`Core/ServicioLicencias/ServicioLicencia.cs:158-167`), usando el constructor de `Licencia` que ya recibe `fechaFin`.
- ~~`Observacion` mapeada `varchar(10)` (H-019).~~ **Resuelto en `ff2de55`**: ahora `.HasMaxLength(250).IsUnicode(false)`, sin `HasColumnType` explícito pisando el `MaxLength`.

**Relaciones.**
- Referencia a `Docente` por id (ya no es colección de `Docente`, ver `8073161`).
- Repo: `ILicenciaRepository`.
- El handler de su evento de alta (`LicenciaSolicitadaEvent`) debía impactar en el curso del docente — es la precondición de US4-3 / T056 (H-022).

**Eventos.** `LicenciaSolicitadaEvent` (handler vacío), `LicenciaActivadaEvent`, `LicenciaCanceladaEvent`, `LicenciaFinalizadaEvent` (sin handler).

**Pendientes.**
- ~~Decidir el límite de agregado y dejar **una** config de EF.~~ Resuelto (`8073161` + `ff2de55`).
- ~~Identidad simple; `DocenteID` como FK, no PK.~~ Resuelto (`ff2de55`).
- ~~H-016 (Core, sigue abierto)~~ **[RESUELTO]** en `b3ac87b`…`76bc1d8` (2026-09-28). ~~H-019 (persistencia)~~ resuelto (`ff2de55`).
- Cablear el handler de `LicenciaSolicitadaEvent` (H-022) o quitar el evento.

---

## `Curso` (agregado raíz) + `Division` (entidad hija) — **separados en `002-fix-dominio`**

> **Estado posterior a esta revisión.** Lo que sigue describe el modelo **anterior**. `Division` es hoy **raíz propia** (`Domain/Divisiones/`, `IDivisionRepository`) que referencia al curso por `CursoID`; `Curso` quedó con nivel y grado. `Division` ya no guarda cursantes (la inscripción es `Cursante.DivisionID`), valida el cupo con `ValidarCupoDisponible(int)` (usa `MAX_ALUMNOS`) y recuperó `ValidarSePuedeEliminar(NivelEducativo, int)`: secundaria no se elimina, primaria con 15 o más cursantes tampoco (usa `MINIMO_MATRICULA_PRIMARIA`). Ambos reciben el conteo del servicio (política, no invariante). `NivelEducativo` pasó a `Domain.Shared`.
>
> **Pendientes de esta ficha ya cerrados**: qué guarda `Division._cursantes` (nada: se eliminó); `MIN/MAX_ALUMNOS` (ambos usados); `MateriaEliminadaEvent` movido a `Materias`; `ExisteCursante` / `ExisteDivision` eliminados.
>
> **Sigue abierto**: la descripción de `Division` se valida con `ArgumentException` (T-5); `Division.Siguiente` sin tope tras la `Z`.
> ~~H-023 y `DivisionesConfiguration` comentado (Infrastructure todavía mapea divisiones dentro de `Curso`).~~ **Resuelto en `ff2de55`**: `DivisionesConfiguration.cs` tiene ahora una clase activa (`Division` como tabla propia, `Descripcion` con `.HasMaxLength(1).IsUnicode(false)` en vez de `char(1)`, FK sin navegación a `Curso` y a `Docente`); el mapeo viejo de divisiones dentro de `CursosConfiguration.cs` quedó envuelto en `#if false ... #endif` (no se compila). Ver INFRA-05/INFRA-11 en `todos.md`.

**Rol.** Año/grado de un nivel educativo, con sus divisiones (A, B, C…), cada una con preceptor y alumnos.

**Estado / invariantes hoy.**
- `Curso`: `NivelEducativo`, `Grado` (enums validados), `_divisiones`.
- `Division`: `Descripcion` (una letra), `Preceptor` (`Guid?`), `_cursantes` (`List<Guid>`).
- `QuitarAlumno` corregido (H-008).
- `Division` asigna/quita preceptor con excepciones de dominio.

**Problemas.**
- **`Division._cursantes: List<Guid>`**: referencia por id a algo indefinido (¿`Alumno`? ¿`Cursante`?). No hay ciclo lectivo, ni recursante, ni fecha de inscripción — todo eso está en `Cursante`, que no se usa.
- **`Division` declara `MIN_ALUMNOS = 15` / `MAX_ALUMNOS = 35` y no las usa nunca**: invariante muerta.
- **`AgregarDivision`** nombra por letra con `char.ConvertFromUtf32(char.Parse(x.Trim()) + 1)` — frágil; el `.Trim()` está ahí porque `Descripcion` vuelve de la base con relleno `char(1)` (H-023).
- **Calificaciones**: `Curso.AgregarCalificacion` está comentado; el flujo real pasa por `Curricula` → `Materia`. `Curso` no participa.
- `ExisteCursante` lanza `NullReferenceException` (T-5); `ExisteDivision` lanza `ArgumentNullException` (T-4).
- ~~`DivisionesConfiguration.cs` está **enteramente comentado**; el mapeo activo de divisiones vive en `CursosConfiguration`.~~ Resuelto en `ff2de55` (ver nota de cabecera de esta ficha).
- `MateriaEliminadaEvent` en `Domain/Cursos/DomainEvents/` — el módulo equivocado (T-3).

**Relaciones.**
- Contiene `Division` (composición).
- Referencia a `Curricula` sólo indirecta: es `Curricula` la que tiene `CursoID`.
- `Division.Preceptor` → id de `Docente`.
- Repo: `ICursoRepository`.

**Pendientes.**
- ~~Resolver qué guarda `Division._cursantes`~~ (resuelto: se eliminó).
- Usar o borrar `MIN/MAX_ALUMNOS`.
- ~~H-023 (persistencia): `Descripcion` sin relleno; regenerar migración; destapar o borrar `DivisionesConfiguration`.~~ El mapeo (`Descripcion` sin relleno, `DivisionesConfiguration` activa) se resolvió en `ff2de55`; falta todavía regenerar la migración de EF (no verificable sin SQL Server/Windows).
- ~~Mover `MateriaEliminadaEvent` a `Curriculas` (o eliminarlo).~~ Resuelto: vive en `Domain/Materias/DomainEvents/` (ver T-3).

---

## `Curricula` + `Materia` + `Catedra` — **rediseñado en `002-fix-dominio`**

> **Estado posterior a esta revisión.** Lo que sigue en esta ficha describe el modelo **anterior** y se conserva como registro del punto de partida. El rediseño reemplazó la cadena `Curricula → Materia → SituacionRevista` por **tres raíces independientes**:
>
> | Agregado | Contiene | Protege |
> |---|---|---|
> | `Curricula` | — | vigencia del diseño; punto de congelamiento para las instantáneas de terceros |
> | `Materia` | — | nombre y carga horaria del espacio curricular |
> | `Catedra` (materia + división) | `SituacionRevista`, `Horario` | un solo docente en funciones; horarios dentro de la carga horaria; cadena de suplencias |
>
> Ninguna referencia a entidades internas: las tres se referencian por identidad de raíz. Las reglas de conjunto que quedaron fuera (unicidad de nombre, topes, colisiones de horario) son **políticas**, no invariantes, y se resuelven con consulta a repositorio más restricción en BD.
>
> **Pendientes de esta ficha ya cerrados**: `SituacionRevista` extraída de `Materia`; `MateriaEliminadaEvent` movido a su módulo y emitido desde `Materia.Eliminar()`; H-010 (`DocenteSinCargoException`) ahora se lanza desde `Catedra.RelevarDeFunciones()` — la anotación de `hallazgos.md` quedó desactualizada; H-012 (`MateriaSinHorariosAsignadoException`) cerrado por eliminación, la regla es hoy `Catedra.TieneHorasCompletas()`. **Reverificado 2026-09-25**: el mapeo EF de las tres raíces también quedó resuelto en `ff2de55` — `CurriculasConfiguration.cs` y `MateriasConfiguration.cs` sólo mapean lo que sus agregados reducidos exponen hoy (los bloques del diseño viejo quedaron comentados), y `CatedrasConfiguration.cs`/`SituacionRevistaConfiguration.cs` (nuevos) mapean `Catedra` con `SituacionRevista`/`Horario` como entidades internas, exactamente como describe la tabla de límites de agregado de `CLAUDE.md` (FK compuesta para `SituacionEnFuncionesID`, FK sombra + cascada para las entidades internas).
>
> **Sigue abierto**: ~~`Calificacion` sin agregado dueño~~ (resuelto: entidad hija de `Cursante`); `Horario.HoraFin` y `DuracionHoraCatedra()` con los defectos que esta ficha describe; H-011 (`LimiteCantidadHorasCatedrasException`) sin cablear, a la espera de la política de tope de horas en la capa de aplicación. ~~**Nuevo (2026-09-25)**: aunque el mapeo EF ya se resolvió, `Infrastructure/Repository/CurriculaRepository.cs` no se actualizó...~~ **Resuelto en `3cf17b7` (2026-09-26)**: `CurriculaRepository.cs` se reescribió contra el `Curricula` reducido (`BuscarCurriculaAsync`/`CurriculasSegunCursoAsync` filtran por `CursoID`/`Id` sin `Include`); compila. Ver INFRA-06 en `todos.md`.

**Rol.** Plan de estudios de un curso durante un período: conjunto de materias, cada una con su carga horaria, sus cargos docentes y las calificaciones de los cursantes.

**Estado / invariantes hoy.**
- `Curricula`: `CursoID` (**referencia por id — bien**), `Periodo`, `_materias`. Modificar materias exige período vigente (`CurriculaNoVigenteException`); nombre de materia único.
- `Materia`: `Descripcion`, `HorasCatedra` (≥ 1), `_docentes` (`List<SituacionRevista>`), `_calificaciones` (`List<Calificacion>`) — **a mover a `Cursante`, ver diseño resuelto en la ficha de `Cursante`**, `Docente` / `DocenteID` **calculados**.
- `SituacionRevista`: `DocenteID`, `Cargo` (titular / suplente…), `Periodo`, `EnFunciones`, `EstadoSituacionRevista`. Lógica de "un solo docente en funciones", relevo de suplentes, etc. — pero esa lógica vive en `Materia`, no en `SituacionRevista`.
- `Calificacion`: por cursante, fecha, instancia (parcial…), nota/asistencia.

**Problemas.**
- **`Materia` hace demasiado**: su propio estado + cargos docentes + calificaciones + horarios (comentados). 3–4 responsabilidades.
- **`SituacionRevista` debería ser agregado propio**: tiene ciclo de vida y reglas suficientes; hoy es nieta de `Curricula` y su config EF (`ConfigureTableSituacionRevista` en `MateriasConfiguration`) está **comentada**, lo que rompe los `Include` de `CurriculaRepository` (H-021).
- **`Materia.Docente` / `DocenteID` calculados**: corren un LINQ `Where` en cada acceso; expuestos como si fueran propiedades mapeadas.
- `Curricula.ExisteMateria` lanza `MateriaDuplicadaException` cuando el `Guid` es `Empty` — excepción con nombre equivocado para esa condición (T-4).
- **Horarios**: región completa comentada en `Materia`. `MateriaSinHorariosAsignadoException` (H-012) y `LimiteCantidadHorasCatedrasException` (H-011) sólo se referencian ahí dentro. `EditarCargaHoraria` tiene un `// TODO` (divisor de 5/10).
- `DocenteSinCargoException` (H-010) definida y nunca lanzada.
- Turno `Noche` de `Horario` corregido (H-009), pero `Horario.HoraFin = HoraInicio + 40` ignora la `duracionHoraCatedra` recibida.

**Relaciones.**
- Referencia a `Curso` por `CursoID`.
- `SituacionRevista.DocenteID` → id de `Docente`.
- `Calificacion.CursanteID` → id de `Cursante` (que no existe como flujo).
- Repo: `ICurriculaRepository` (`Include` profundo roto — H-021).

**Eventos.** `MateriaRegistradaEvent` (definido, **sin handler**). `MateriaEliminadaEvent` está en el módulo `Cursos` por error.

**Pendientes.**
- Extraer `SituacionRevista` como agregado propio (repo, `IUnitOfWork`, config EF completa) — destraba H-021 y achica `Materia`.
- **Resuelto**: `Calificacion` pasa a `Cursante` (ver ficha `Cursante`, diseño resuelto); `Materia._calificaciones` se elimina, `Calificacion.MateriaID` queda como referencia cruzada.
- Horarios: reactivar la región (feature) o borrarla; de eso dependen H-011 y H-012.
- H-010: cablear `DocenteSinCargoException` o borrarla.
- `Horario`: respetar `duracionHoraCatedra` en `HoraFin`.

---

## `Cursante` (agregado raíz) + `Calificacion` (entidad hija) y `PlanillaAsistencia` — **implementado en `002-fix-dominio`**

> **Estado posterior (2026-09-23).** El diseño de 2026-09-13 que sigue (Cursante como hija de `Division` dentro de `Curso`) **se descartó** al separar `Division` de `Curso`. Lo implementado en `Domain`:
>
> | Agregado | Contiene | Protege |
> |---|---|---|
> | `Cursante` (alumno + división + ciclo lectivo) | `Calificacion` (entidad, con `Id`) | inscripción y notas del ciclo; nota 1–10, aprobación con 6 o más; la nota no se corrige (se quita y se vuelve a cargar), sólo la observación |
> | `PlanillaAsistencia` (división + fecha) | `RegistroAsistencia` (entidad) | la lista diaria nace completa (todos en `Presente`), un registro por cursante; tardanza con minutos; cerrar / reabrir con eventos |
>
> - `Cursante` tiene `ICursanteRepository` y entrada en `IUnitOfWork`; no guarda asistencias. `Calificacion` referencia la materia por `MateriaID`; su bool `Asistencia` se renombró a `Rindio`.
> - La asistencia es diaria y por división: el preceptor toma lista de toda la planilla. `PlanillaAsistencia` no guarda `CicloLectivo` (lo determina la fecha; cada `CursanteID` pertenece a un único ciclo). `Falta` pasó a `TipoAsistencia { Presente, Ausencia, Inasistencia, Tardanza }`.
> - Fuera de los agregados: una planilla por (división, fecha) y el conteo de faltas por cursante (`IPlanillaAsistenciaRepository`).
> - Resuelto de la lista de problemas: el `NullReferenceException` del constructor (T-5).
>
> **Sigue abierto (reverificado 2026-09-26)**: **el mapeo EF ya se resolvió en `ff2de55`** — `CursantesConfiguration.cs` mapea `Cursante` con FK sin navegación a `Alumno`/`Division`, `CicloLectivo` vía `OwnsOne` + catálogo `SharedTypeEntity`, y `Calificaciones` como entidad interna con FK sombra y cascada; `CalificacionesConfiguration.cs`, `PlanillasAsistenciaConfiguration.cs` y `RegistrosAsistenciaConfiguration.cs` (todos nuevos) completan `PlanillaAsistencia`/`RegistroAsistencia`. Los hallazgos de mapeo que este párrafo señalaba (`CursantesConfiguration` 1:1 con `Alumno`, `HasForeignKey` duplicado, `CicloLectivo` como tabla con clave subrogada) ya no aplican. **Los repositorios concretos ya están** (`CursanteRepository`, `PlanillaAsistenciaRepository`, commit `527b1ba`, 2026-09-26) y `UnitOfWork` implementa `Cursantes`/`PlanillasAsistencia` (INFRA-01 resuelto). Falta todavía: regenerar la migración de EF (no verificable sin SQL Server/Windows), `Core` y `WPF_Desktop`. **Gap nuevo detectado en el dominio (2026-09-26)**: `Cursante.FechaFin` tiene `private set` pero ningún método público la asigna — no hay forma de cerrar una inscripción (fijar "inscripción activa" = `FechaFin == null`) desde fuera del constructor; ver DOM-24 en `todos.md`. El plan de implementación por capas de más abajo corresponde al diseño descartado (2026-09-13) y se conserva como registro; su sección `Infrastructure` quedó resuelta por `ff2de55` + `527b1ba` salvo el punto de la migración.
>
> *Nota original (2026-09-13):* Actualizado tras discusión de diseño. Reemplaza el framing anterior de "agregado raíz huérfano": no le faltaba infraestructura, le faltaba límite de agregado correcto.

**Rol.** La inscripción de un `Alumno` en una `Division` para un `CicloLectivo`: quién cursa, desde cuándo, si repite el año, su asistencia diaria y sus calificaciones. Es el nexo entre `Alumno` y `Division` que hoy `Division._cursantes: List<Guid>` no puede representar (le falta todo salvo el id del alumno).

**Estado / invariantes hoy (sin cambios de código aún, ver Pendientes).**
- `AlumnoID`, `CicloLectivo` (VO), `FechaInicio`, `FechaFin`, `EsRecursante`, `_asistencias` (`List<Asistencia>`).
- `RegistrarAsistencia(fecha, falta, minutos, observacion)`: una asistencia por día (`AsistenciaRegistradaException`); `Asistencia` tiene factories `Ausencia` / `Inasistencia` / `Tardanza`. **Confirmado como regla de negocio**: la asistencia es diaria y por división (no existe un circuito de "asistencia por materia").
- Contadores derivados: `Ausencias`, `Inasistencias`, `Tardanzas`.

**Decisiones de diseño (resueltas en conversación con el negocio).**
1. **Límite de agregado**: `Cursante` es entidad hija de `Division` (⊂ agregado `Curso`), no agregado raíz. Mismo patrón que `Puesto` en `Docente`: sin repositorio propio, sin entrada en `IUnitOfWork`; se accede a través de `ICursoRepository`.
   - **Corroboración en el propio código**: el `#region Calificaciones` comentado en `Domain/Cursos/Division.cs` (líneas 116-144) hace `_cursantes.Where(x => x.AlumnoID.Equals(unAlumno) && x.CicloLectivo.Equals(cicloLectivo))` y `cursante.AgregarCalificacion(...)` — trata a `_cursantes` como `List<Cursante>`, no como `List<Guid>`. Es decir, este diseño **ya existió** y se degradó a `List<Guid>` en algún momento; no es una decisión nueva, es una restauración con un borrador de spec ya escrito.
2. **Sin invariante de unicidad global** por `(Alumno, CicloLectivo)`: un alumno puede tener más de una inscripción vigente a la vez (cursada regular del año en curso + una materia adeudada de un año anterior). Aclarado que "adeudar materia" se resuelve con examen final, **sin cursada y sin asistencia** — por lo tanto no es un caso que `Cursante` deba modelar; queda fuera de alcance (ver nota abajo).
3. **`Calificacion` se mueve de `Materia` a `Cursante`**: la nota es "la que sacó este alumno en este cursado", no un dato suelto de la materia. `Calificacion.MateriaID` queda como referencia por id a `Curricula` (mismo patrón que `Licencia.DocenteID`). Contrapartida aceptada: registrar una calificación pasa a requerir cargar y guardar el agregado `Curso` completo (`Curso → Division → Cursante → Calificacion`) vía `ICursoRepository` — aceptable porque la app es mono-puesto y no tiene control de concurrencia optimista (ver NFR de `tercera-etapa-pp3.md` §2.2).
4. **Fuera de alcance explícito**: materias adeudadas / exámenes finales / mesas de examen. No aparecen en `tercera-etapa-pp3.md` §1.3 (dentro de alcance) ni en `alcance-funcional.md` ("Registrar calificación de un cursante" es el único caso de uso de calificación listado). Si en el futuro se necesita modelar el examen final de una previa, es una entidad distinta (no pasa por `Division` ni por `Asistencia`) — no se resuelve acá.

**Problemas (no resueltos por la discusión de diseño, siguen abiertos).**
- ~~El constructor lanza `NullReferenceException` a mano (T-5).~~ Esta línea quedó desactualizada dentro de esta misma ficha: el constructor de `Cursante.cs` ya lanza `SinDatosDivisionException` / `SinDatosAlumnoException` (ver la nota "Resuelto" más arriba en esta misma ficha y T-5 en la Parte 1).
- ~~**Hallazgos de mapeo EF** (encontrados leyendo `CursantesConfiguration.cs` tal como estaba antes de `ff2de55`): `Alumno`↔`Cursante` mapeada 1:1 (contradice que un alumno tenga varios `Cursante` a lo largo de su historia); `ConfigureTableAsistencias` con un `HasForeignKey` duplicado que debía ser `HasConstraintName`; `CicloLectivo` (VO) mapeado como entidad con tabla propia y clave subrogada.~~ **Resueltos en `ff2de55`** (reverificado 2026-09-25, leyendo el archivo actual): `CursantesConfiguration.cs` mapea `Alumno` con `HasOne<Alumno>().WithMany()` (ya no 1:1); `ConfigureTableAsistencias` quedó comentada (las asistencias son de `PlanillaAsistencia`, no de `Cursante`); `CicloLectivo` se mapea con `OwnsOne` más una FK contra un catálogo `SharedTypeEntity` (`CicloLectivoConfiguration.cs`, tabla `ciclo_lectivo`, períodos 2020-2035), sin clase de dominio propia — resuelve exactamente lo que pedía este punto.

**Relaciones.**
- `AlumnoID` → id de `Alumno` (cross-aggregate, correcto).
- Contenida en `Division._cursantes` (pasa de `List<Guid>` a `List<Cursante>`).
- `Calificacion.MateriaID` → id de `Curricula`/`Materia` (cross-aggregate).

**Plan de implementación (diseño ya resuelto arriba; esto es lo ejecutable, capa por capa).**

`Domain`
- [ ] `Domain/Cursos/Division.cs`: `_cursantes` pasa de `List<Guid>` a `List<Cursante>`. Adaptar `ExisteCursante`, `BuscarCursante`, `AgregarCursante`, `QuitarCursante` y la propiedad pública `Cursantes` para operar sobre `Cursante` (comparando por su `Id`, no por el `Guid` de alumno). Recuperar el `#region Calificaciones` comentado (líneas 116-144) como base — ya tiene la forma correcta, sólo hay que descomentar y ajustar a la firma actual.
- [ ] `Domain/Cursos/Curso.cs`: `AgregarAlumnoEnDivision` deja de recibir un `Guid` suelto — construye el `Cursante` (con `CicloLectivo`, `FechaInicio`, `EsRecursante`) antes de pasárselo a `Division.AgregarCursante`. Revisar en el mismo archivo `CursanteRegistrado` y el método de `ICursoRepository.CambiarAlumnoDeCurso` (pase de año), que hoy asume alumnos sueltos.
- [ ] `Domain/Curriculas/Materias/Materia.cs`: eliminar `_calificaciones` (`List<Calificacion>`) y cualquier método activo que la use.
- [ ] `Domain/Cursantes/Cursante.cs`: agregar `_calificaciones` (`List<Calificacion>`) + `AgregarCalificacion` / `QuitarCalificacion`, recuperando la lógica del bloque comentado de `Division.cs`.
- [ ] Evaluar mover `Calificacion.cs` de `Domain/Curriculas/Materias/` a `Domain/Cursantes/` (pasa a ser una entidad hija de `Cursante`; `MateriaID` queda como su única referencia a `Curriculas`) — decisión de organización de carpetas, no bloqueante.
- [ ] Reemplazar el `NullReferenceException` del constructor de `Cursante` por una excepción de dominio propia (T-5, arrastrado).

`Infrastructure` — **reverificado 2026-09-25: resuelto en `ff2de55`, salvo la migración.**
- [x] `CursantesConfiguration.cs`: `Alumno`↔`Cursante` de `HasOne().WithOne()` a `HasOne().WithMany()` (un alumno tiene muchos `Cursante` a lo largo de su historia).
- [~] Mapear `Division` → `Cursante` como colección propia de la división (FK `division_id`), reemplazando la relación implícita actual. — No aplica tal cual: con el diseño vigente `Cursante` es agregado raíz (no colección de `Division`); la FK activa es `Cursante.DivisionID` sin navegación (`CursantesConfiguration.cs`).
- [x] `CicloLectivoConfiguration.cs`: pasar de tabla propia con `ciclo_lectivo_id` a `OwnsOne` (es un VO, no una entidad). — Se resolvió como catálogo `SharedTypeEntity` + `OwnsOne` en `Cursante`, en vez de una entidad con FK propia; mismo objetivo, forma distinta a la planteada acá.
- [x] Corregir el `HasForeignKey` duplicado en `ConfigureTableAsistencias` (el segundo debe ser `HasConstraintName`). — El método quedó comentado (las asistencias pasaron a `PlanillaAsistencia`), así que el bug ya no se ejercita.
- [x] `MateriasConfiguration.cs`: quitar el mapeo de `_calificaciones` si sigue activo ahí. — Confirmado: `ConfigureTableCalificaciones` quedó comentada.
- [ ] Regenerar migración (`dotnet ef migrations add ... --project Infrastructure --startup-project WPF_Desktop`, sólo desde Windows) reflejando los puntos anteriores. — Sigue pendiente; no verificable sin Windows/SQL Server desde esta revisión. **Actualización 2026-10-03**: sigue pendiente, pero cambió de causa por tercera vez. `WPF_Desktop` —el *startup project*— ya compila (`EDUSIS.sln` en 0 errores), así que lo que bloquea ahora es que la base de datos **no está instalada**; sin ella tampoco se puede recorrer la app (H-023).

`Core` — **reverificado 2026-09-28 contra `b3ac87b`…`76bc1d8` (cierre del bloque `Core`): los tres puntos de esta lista se ejecutaron, pero no como `ServicioCurso` — el diseño resuelto arriba (punto 3) los movió a `ServicioCursante`, agregado nuevo de este bloque.**
- [x] ~~`ServicioCurso.RegistrarCursanteEnDivision`: construir un `Cursante` real a partir del request (`CicloLectivo`, `EsRecursante`) en vez de agregar sólo el `AlumnoID`; revisar si `RegistrarCursanteRequest` ya trae esos campos o hay que ampliarlo.~~ **[RESUELTO]**, en `ServicioCursante`, no en `ServicioCurso`: `ServicioCursante.InscribirCursanteAsync(RegistrarCursanteRequest)` (`Core/ServicioCursantes/ServicioCursante.cs:25`) construye el `Cursante` con `CursoID`, `DivisionID`, `AlumnoID`, `Periodo` y `EsRecursante` (`Core/ServicioCursantes/DTOs/Requests/RegistrarCursanteRequest.cs`).
- [x] ~~Completar `ServicioCurso.RegistrarCalificacion` (H-017): cargar el `Cursante` correspondiente dentro del agregado `Curso`, invocar `AgregarCalificacion`, `GuardarCambiosAsync()`.~~ **[RESUELTO]** — H-017 cerrado: `ServicioCursante.RegistrarCalificacionAsync(CrearCalificationRequest)` (`Core/ServicioCursantes/ServicioCursante.cs:147`) carga el `Cursante` por su propio repositorio (ya no a través de `Curso`, consistente con `Cursante` como agregado raíz), invoca `cursante.RegistrarCalificacion(...)` y `GuardarCambiosAsync()`. También existen `QuitarCalificacionAsync` y `ModificarObservacionCalificacionAsync`.
- [x] ~~Descomentar/completar `IServicioCurso.BuscarListado` (hoy comentado en la interfaz) para que deje de depender de la lista vacía hardcodeada en el ViewModel.~~ **Parcialmente resuelto**: el caso de uso existe — `ServicioCursante.ListarCursantesAsync(BuscarListadoRequest)` (`Core/ServicioCursantes/ServicioCursante.cs:68`) —, pero no en `ServicioCurso` como preveía este ítem, sino en `ServicioCursante`. La segunda mitad del ítem **sigue sin cerrarse y es un problema de `WPF_Desktop`, no de `Core`**: `GestionCursantesViewModel.BuscarCursantes` (`WPF_Desktop/ViewModels/Cursos/Divisiones/GestionCursantesViewModel.cs:75-80`) sigue con `var listado = new List<CursanteResponse>()` hardcodeado en vez de llamar a `ListarCursantesAsync`, ver ítem de `WPF_Desktop` abajo.

`WPF_Desktop`
- [x] `GestionCursantesViewModel.BuscarCursantes`: sacar el `var listado = new List<CursanteResponse>()` hardcodeado y llamar al caso de uso real (`IServicioCursante.ListarCursantesAsync`, ya implementado en `Core` desde el 2026-09-28, ver arriba). **[RESUELTO 2026-10-02]** en `0059ebe` (W5.C, UI-02): llama a `IServicioCursante.ListarCursantesAsync(BuscarListadoRequest)` y la currícula vigente se obtiene con `FechaFin is null`; el ViewModel se reescribió como `ObservableValidator` (`GestionCursantesViewModel.cs:171,205`). Cubierto por pruebas (`66ef3f9`), **sin recorrer en la app**.
- [x] Revisar `CursanteViewModel` / `CalificacionViewModel` contra los campos reales de `Cursante` / `Calificacion` una vez migrados. **[RESUELTO 2026-10-02]** en `4522005` (W3.C): `CursanteViewModel` trae `CursanteID` y `EsRecursante` (la clave de todos los requests de calificación), y `CalificacionViewModel` migró a `ObservableValidator` con `Rindio`, `Instancia` como `string`, `Fecha` no nulable, `CalificacionID`, `Aprobado` y `Observacion`; verificado campo por campo (`7fc4bf5`). Las calificaciones llegan a `Core` (alta, inasistencia a examen, quitar y editar observación; sin "editar nota", porque el dominio no la permite). `CalificacionView.xaml` quedó sin consumidores (UI-20).

**Explícitamente fuera de este plan** (ver decisión de diseño arriba): exámenes finales / materias adeudadas; horarios de `Materia` (H-011/H-012, feature aparte).

---

## `Usuario` (agregado raíz)

**Rol.** Credencial de acceso al sistema para un docente, con roles.

**Estado / invariantes hoy.**
- `DocenteID` (no vacío), `Username` (no vacío; regex comentado), `PasswordSalt`, `PasswordHash`, `_roles` (`List<Rol>`).
- `AgregarRol` / `QuitarRol` con `Rol` como `Enumeration`; `CambiarPassword`.
- Hashing en `Core/ServicioSecurity/ServicioSeguridad.cs`.

**Problemas.**
- ~~`ServicioUsuario.ActualizarRol` no implementado (H-015, capa `Core`).~~ **[RESUELTO]** en el bloque `Core` (`b3ac87b`…`76bc1d8`, 2026-09-28): `IServicioUsuario` ya no declara `ActualizarRol` — lo reemplazan `AsignarRolAsync(ActualizarRolRequest)` y `QuitarRolAsync(ActualizarRolRequest)`, ambos implementados en `Core/ServicioUsuarios/ServicioUsuario.cs:195,217`.
- ~~`ServicioAutenticacion.Logout` no implementado (H-014, capa `Core`).~~ **[ELIMINADO]** (no corregido, descartado): `IServicioAutenticacion` no declara ningún método `Logout` — el hallazgo se cerró quitando el método en vez de implementarlo, consistente con `specs/001-automated-test-suite/hallazgos.md`, que ya marca H-014 como *Eliminado*.
- Contexto de identidad mezclado con el académico (T-7); `Usuario` acoplado a `Docente` por id sin un puerto claro.
- Regex de `Username` comentado — sin invariante de formato.
- Nombres ES/EN mezclados en la carpeta del servicio de seguridad.

**Relaciones.**
- `DocenteID` → id de `Docente`.
- Repo: `IUsuarioRepository` (está en `IUnitOfWork`).

**Pendientes.**
- ~~H-014, H-015 (Core).~~ Resueltos en el bloque `Core` (`b3ac87b`…`76bc1d8`, 2026-09-28) — ver Problemas arriba.
- Definir formato de `Username` o documentarlo.
- Evaluar separar identidad/acceso en su propio contexto.

---

# Qué falta (transversal)

1. **Abstracción de reloj** (`IDateTimeProvider` / `TimeProvider`) — T-6.
2. ~~**Cerrar el flujo de inscripción (`Cursante`)** — dominio e Infrastructure implementados (agregado raíz, repositorio y mapeo EF, ver ficha `Cursante`); falta la migración de EF, el caso de uso en `Core` y la API de dominio para cerrar la inscripción (DOM-24).~~ **Actualización 2026-09-28**: el caso de uso de alta ya existe — `ServicioCursante.InscribirCursanteAsync` (bloque `Core`, `b3ac87b`…`76bc1d8`, ver ficha `Cursante`). Sigue faltando la migración de EF (H-023) y, sobre todo, sigue sin existir una API de dominio para **cerrar** la inscripción (`Cursante.FechaFin` sin setter público, DOM-24) — eso también bloquea un eventual caso de uso de baja en `Core`.
3. ~~**Implementar el registro de calificaciones** (H-017) — dominio implementado (`Cursante.RegistrarCalificacion`); falta el caso de uso, que ya no pasa por `ServicioCurso` sino por el agregado `Cursante`.~~ **[RESUELTO]** en el bloque `Core` (`b3ac87b`…`76bc1d8`, 2026-09-28): el caso de uso es `ServicioCursante.RegistrarCalificacionAsync`, ver ficha `Cursante`.
3b. ~~**Asistencia diaria por planilla** — dominio e Infrastructure implementados (`PlanillaAsistencia`, repositorio y mapeo EF); falta la migración, los casos de uso (abrir / marcar / cerrar / reabrir) y la pantalla.~~ **Actualización 2026-09-28**: los casos de uso ya existen en `ServicioAsistencias` (`AbrirPlanillaAsync`, `MarcarPresenteAsync`/`MarcarAusenciaAsync`/`MarcarInasistenciaAsync`/`MarcarTardanzaAsync`, `IncorporarCursanteAsync`, `CerrarPlanillaAsync`, `ReabrirPlanillaAsync`, `ConsultarPlanillaAsync`, `ContarFaltasAsync` — `Core/ServicioAsistencias/IServicioAsistencia.cs`). Falta la migración de EF (H-023) y la pantalla en `WPF_Desktop` (no relevada en este documento, es UI). **Actualización 2026-10-03**: la pantalla **sigue sin existir** y `ServicioAsistencias` no tiene ni un solo caso de uso alcanzable desde la UI (decisión #2 de `plan/wpf-desktop.md`, UI-19).
4. **Handlers de eventos reales, o borrar los eventos especulativos**; borrar `IEventHandler<T>`.
5. **Control de concurrencia** (`rowversion`).
6. **Documento de límites de agregado**: raíz por raíz, invariantes y qué referencia por id. Este mismo documento (Parte 2) es un primer borrador; conviene volverlo la fuente de verdad una vez decididas las dudas.
7. Helper de guardas para reemplazar el patrón "método `bool` que lanza" (T-4).

# Prioridad sugerida

| # | Trabajo | Por qué primero |
|---|---------|-----------------|
| 1 | `IDateTimeProvider` + terminar los hallazgos de dominio pendientes sobre esa base | Destraba la clase entera de bugs "el mismo día" y su testeo |
| ~~2~~ | ~~Sacar `BuscarAsync(Expression)` de `IRepository`; mover cada consulta a su interfaz por agregado~~ | Resuelto en `b377fa5` (2026-09-26) — ver T-2 |
| ~~3~~ | ~~Extraer `SituacionRevista` como agregado propio; achicar `Materia`~~ | Resuelto con `Catedra` (ver ficha `Curricula`) |
| ~~4~~ | ~~Resolver `Cursante` (huérfano) y calificaciones — juntos~~ | Resuelto en `Domain`/`Infrastructure`/`Core` (ver ficha `Cursante`); lo último que quedaba, `GestionCursantesViewModel` con listado hardcodeado, se resolvió el 2026-10-02 en `0059ebe` (sin recorrer en la app) |
| ~~5~~ | ~~Decidir `Licencia`: hija de `Docente` o agregado — y dejar una sola config EF~~ | Resuelto en `8073161` + `ff2de55`: agregado raíz único, una sola config EF |
| 6 | Eventos: cablear handlers reales o quitarlos; borrar `IEventHandler<T>` | Hoy es complejidad sin retorno |

---

# Contexto — estado de los hallazgos de la suite

La rama `002-fix-dominio` ya corrigió los defectos de dominio inequívocos: **H-002** (`Contacto.GetEqualityCommponents`), **H-003** (`Alumno.Desinscribir` / `EstaActivo`), **H-005** (`Docente.Desafectar` / `Activo`), **H-008** (`Curso.QuitarAlumno`), **H-009** (turno `Noche` de `Horario`) y **H-013** (`Licencia.EsIndefinida`).

Después, la misma rama cerró **H-001** por eliminación (se descartó el VO `Contacto`) y **H-010** / **H-012** con el rediseño de `Catedra` (ver ficha `Curricula`; el `[Fact(Skip = "H-010…")]` de `ExcepcionesSinCablearTests` quedó desactualizado y debería reemplazarse por un test contra `Catedra.RelevarDeFunciones()`), además de los límites de `Division`, `Cursante`, `Calificacion` y `PlanillaAsistencia` (ver sus fichas). Regresión menor introducida en `Licencia.ModificarObservaciones`: `ExcesoCaracteresException` se lanza sin el mensaje (se arma `msg` y no se usa).

**Reverificado 2026-09-25** contra dos commits posteriores: `8073161` quita `Docente._licencias` (cierra la doble pertenencia de `Licencia`, ver `Docente`/`Licencia`) y `ff2de55` reescribe todo `Infrastructure/EntityConfigurations/` para alinear el mapeo EF con `Catedra`, `Division`, `Cursante` y `PlanillaAsistencia` — de paso también resuelve **H-019** (`Licencia.Observacion` en `varchar(10)`) y la clave compuesta de `Licencia`. Lo que ese commit **no** tocó, entonces: `Infrastructure/Repository/CurriculaRepository.cs` seguía consultando `Curricula.Materias` (ya eliminado), `Infrastructure/Repository/CursoRepository.cs` seguía usando `Curso.Divisiones` (ya eliminado) y `Infrastructure/UnitOfWork.cs` seguía sin implementar los repositorios de `Divisiones`, `Cursantes`, `Materias`, `Catedras` ni `PlanillasAsistencia`.

**Reverificado 2026-09-26** contra `b377fa5`/`3cf17b7`/`527b1ba`/`d7f6b44`/`58705d8`/`0f39316`: los tres puntos que quedaban abiertos arriba están **resueltos** — `IRepository<T>` sin `Expression` ni `params object[]` (T-2), `CurriculaRepository.cs`/`CursoRepository.cs` reescritos contra los agregados reducidos, `UnitOfWork` implementa los once repositorios de `IUnitOfWork`, y `InfrastructureDI` escanea también el assembly de `Core` (H-022) con despacho de eventos en cascada. Esto resuelve H-020, H-021 y H-022 del todo (ver `todos.md`). Lo que sigue sin resolver: **H-023** (falta regenerar la migración de EF — `InitCreate` sigue siendo la única y ya no refleja ni el mapeo de `ff2de55` ni los repositorios de esta ronda) y **`Core`**, que sigue sin compilar a propósito (los mismos 4 errores de `CORE-01`, confirmado con `dotnet build Core/Core.csproj` el 2026-09-26) — es la tarea siguiente, con tabla de handoff en `docs/plan/handoff-errores.md`. Nuevo hallazgo de dominio: `Cursante.FechaFin` no tiene setter público (DOM-24 en `todos.md`).

**Reverificado 2026-09-28** contra `b3ac87b`/`9b06370`/`70d70bf`/`b18a2a9`/`189e401`/`f4ac8c2`/`33c28f7`/`76bc1d8` (cierre del bloque `Core`, ver `docs/plan/core.md`): `Core` **compila limpio** (`dotnet build Core/Core.csproj` → 0 errores), cerrando la tarea que el párrafo anterior dejaba pendiente. `Core/` pasó de 9 a **14 carpetas de servicio**: los cinco nuevos (`ServicioDivisiones`, `ServicioCursantes`, `ServicioAsistencias`, `ServicioMaterias`, `ServicioCatedras`) cubren, uno por agregado, a `Division`, `Cursante`, `PlanillaAsistencia`, `Materia` y `Catedra` — los cinco agregados que desde `ff2de55`/`527b1ba` ya tenían mapeo EF y repositorio pero ningún caso de uso. `ServicioCurso` y `ServicioCurricula` quedaron adelgazados a tres casos de uso cada uno. De los hallazgos de `Core`: **H-014 se eliminó** (`ServicioAutenticacion.Logout` ya no existe como método, no se implementó), **H-015/H-016/H-017/H-018 se corrigieron** (`ServicioUsuario.AsignarRolAsync`/`QuitarRolAsync` reemplazan al `ActualizarRol` que faltaba; `ServicioLicencia.SolicitarLicencia` ya no pasa por la combinación inválida de `EstablecerFechaFinalizacion`; `ServicioCursante.RegistrarCalificacionAsync` cierra el registro de calificaciones; `ServicioDocente.QuitarDocente`, que era `async void`, pasó a `Task QuitarDocenteAsync(DocenteIDRequest)`). Hallazgo nuevo: **H-024** — `ServicioAlumno.RegistrarAlumnoAsync` genera el legajo con `Guid.NewGuid().ToString().GetHashCode().ToString("x")` (hexadecimal de largo variable, a veces negativo) contra el regex `^([A-Z]{2}\d{4})$` de `Alumno`, así que el alta de alumno revienta siempre con `FormatException` — defecto preexistente, recién visible porque `Core.UnitTests` nunca había llegado a ejecutarse con `Core` roto; documentado con `[Fact(Skip=...)]` en `Tests/Core.UnitTests/ServicioAlumnos/ServicioAlumnoTests.cs:121`. Lo que sigue sin resolver: **H-023** (la migración de EF sigue siendo sólo `InitCreate`) — antes bloqueada porque `Core` no compilaba y el *startup project* de la migración es `WPF_Desktop`, ahora desbloqueada de `Core` pero bloqueada por **UI-09** (`WPF_Desktop` tampoco compila, ver abajo); **DOM-24** sigue sin cambios (`Cursante.FechaFin` sin setter público). Dos hallazgos nuevos fuera del dominio: **UI-09** (`WPF_Desktop/Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml:25`, `MC3066`, no encuentra el tipo público `Cargo` — único error de `WPF_Desktop`, en la etapa de *markup compile*, que enmascara los ~41 sitios de llamada rotos que predecía `docs/plan/core.md`) y **TEST-07** (`Tests/EDUSIS.EndToEndTests`: 6 `CS1061` en 2 archivos por símbolos que el adelgazamiento de `ServicioCurso` retiró — esperado, no defecto nuevo).

Quedan pendientes por requerir una decisión de diseño o una feature aparte: ~~**H-001**~~, **H-004, H-006, H-007**, ~~**H-010**~~, **H-011**, ~~**H-012**~~ (detalle en `specs/001-automated-test-suite/hallazgos.md`, no versionado). En la Parte 2, cada uno está anotado en la ficha del agregado que le corresponde. ~~Los hallazgos de `Core` (H-014…H-018) y de persistencia (H-019…H-023) están fuera de la rama `002-fix-dominio` y se referencian en las fichas de `Licencia`, `Curso`, `Curricula` y `Usuario`.~~ **Actualización 2026-09-28**: H-014…H-018 ya se cerraron (ver el párrafo de reverificación de arriba); sigue abierto H-011 (feature de horarios), H-023 (migración, ahora bloqueada por UI-09) y el nuevo H-024 (`Core`, ver ficha `Alumno`/§3.4 de `plan-implementacion.md`). **Actualización 2026-10-03**: H-023 sigue abierto y ya no por la compilación de `WPF_Desktop` (compila) sino por la base de datos, que no está instalada.

**Reverificado 2026-10-03** contra `e8d5043` (adaptación de `WPF_Desktop`, ver [`plan/wpf-desktop.md`](plan/wpf-desktop.md) y [`plan/wpf-desktop-tasks.md`](plan/wpf-desktop-tasks.md)): `dotnet build EDUSIS.sln --no-incremental` → **0 errores** en los diez proyectos, `dotnet test tests/WPF_Desktop.UnitTests` → 320 pruebas, 320 pasan, y `./tests/run-tests.sh --rapidas` sin regresiones. Efectos sobre este documento: (1) **UI-09 y TEST-07 cerrados**; (2) la última brecha de las fichas `Cursante` y `Calificacion` (`GestionCursantesViewModel`) cerrada, ver los dos ítems de `WPF_Desktop` marcados arriba; (3) los servicios `IServicioDivision`, `IServicioCursante`, `IServicioMateria` e `IServicioCatedra` **son alcanzables desde una pantalla**, por primera vez desde el refactor de agregados; (4) el diseño de los límites de agregado de la ficha `Catedra` se respetó en la UI: `SituacionRevista` ya no cuelga de `Materia` sino de la cátedra, con una pantalla intermedia nueva (`GestionCatedras`), y `Materia` dejó de exponer `CargosOcupados` y `SituacionRevista`. **Lo que este documento NO puede afirmar**: que la aplicación funcione. Sigue sin ejecutarse la verificación funcional en Windows, que requiere la base de datos (H-023). Quedan abiertas violaciones nuevas que tocan estas fichas: **designar un docente y ponerlo en funciones son dos transacciones** (`DesignarDocenteAsync` no recibe `EnFunciones`), **el alta de alumno y su inscripción no tienen transacción conjunta**, y `SituacionRevistaResponse` no expone `EsVigente` (la UI reimplementó la regla y la copió mal); ver los items 24, 25 y 27 de `docs/todos.md` §Violaciones.
