# Plan — Reparar y completar la capa `Core` tras el refactor de agregados

> **Alcance**: sólo `Core/`, con dos toques aditivos y acotados en `Domain/` e `Infrastructure/` (Fase 1). `WPF_Desktop` queda **explícitamente fuera**: va a dejar de compilar por diseño y su adaptación es la tarea siguiente.
>
> Documentos hermanos: [`plan.md`](plan.md) y [`tasks.md`](tasks.md) (refactor de `Domain`), [`handoff-errores.md`](handoff-errores.md) (inventario de errores heredados), [`../../specs/001-automated-test-suite/hallazgos.md`](../../specs/001-automated-test-suite/hallazgos.md) (defectos documentados).

> **Estado al 2026-09-28 — ejecutado y cumplido.** Este plan se llevó a cabo tal como está escrito, commits `b3ac87b`…`76bc1d8`. Verificación de la sección "Verificación" de este documento: `dotnet build Core/Core.csproj` → **0 errores** ✓; `dotnet build Infrastructure/Infrastructure.csproj` → **0 errores** ✓; `tests/Core.UnitTests` → **191 correctas, 1 omitida** — los `Skip` de H-015/H-016/H-017/H-018 se retiraron y los de H-014 se eliminaron, tal como pedía la Fase 5, con la salvedad de que apareció un `Skip` nuevo por **CORE-13/H-024** (un defecto preexistente que la suite destapó al ejercitarse por primera vez, no una regresión de este bloque de trabajo — ver más abajo). Las seis fases (0 a 6) y las seis decisiones de diseño de este documento se aplicaron tal como estaban escritas: un servicio por raíz de agregado, los cinco servicios nuevos de la Fase 3, las once excepciones de `Core` (las diez nuevas de este plan más `CursoDuplicadoException`, que ya existía), `MateriaEliminadaEventHandler` movido a `Core/ServicioCatedras/Events/` como pedía la Fase 4, y `Logout` eliminado de `Core` como pedía la decisión #6.
>
> **La predicción sobre `WPF_Desktop` (final de "Verificación") quedó a medio verificar.** El plan afirmaba que `dotnet build EDUSIS.sln` seguiría fallando con los 41 sitios de llamada en 6 archivos de `WPF_Desktop` listados ahí. Lo verificado con `dotnet build EDUSIS.sln --no-incremental`: el build falla con **7 errores**, y **ninguno** de esos 7 es uno de los 41 anticipados. Son **1** `MC3066` en `WPF_Desktop/Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml:25` (no encuentra el tipo público `Cargo`) y **6** `CS1061` en `Tests/EDUSIS.EndToEndTests` (consecuencia esperada del adelgazamiento de `ServicioCurso` de la Fase 2). El `MC3066` corre en la etapa de **markup compile** de XAML, que es **anterior** al compilador de C# — aborta el proyecto entero antes de que Roslyn llegue a mirar los ViewModels, así que los 41 sitios de llamada que este documento predijo **siguen sin poder confirmarse**. Es el mismo enmascaramiento de etapa que este documento describe para Roslyn (línea 11), una etapa más arriba. **Caveat operativo para quien retome esto**: un `dotnet build WPF_Desktop/WPF_Desktop.csproj` incremental devuelve `0 Errores` de forma engañosa, porque el paso de XAML queda cacheado como válido de una corrida anterior — cualquier afirmación sobre el estado de build de un proyecto WPF exige `--no-incremental`.
>
> **Violaciones DDD (sección final): 1–4 corregidas como preveía este plan; de la 5–12, se resolvieron la 10 y la 11, y siguen vigentes la 5, 6, 7, 8, 9 y 12.** Sobre la #7: `Docente.Puesto` (singular) sigue existiendo y sin asignar, pero el `NullReferenceException` garantizado que este documento señalaba en `ListarPreceptoresActivosAsync` **sí se resolvió** — ese caso de uso hoy usa `IDocenteRepository.BuscarSegunPosicionAsync` en lugar de leer `Docente.Puesto`. Sobre la #11: ojo con un falso positivo al re-verificar con `rg` — aparecen 5 coincidencias de `GuardarCambiosAsync()` sin `await` en `Core/ServicioDocentes/ServicioDocente.cs` (líneas 413, 439, 456, 475, 574), pero las cinco caen dentro de dos bloques comentados (`/*` 401 → `*/` 491, y `/*` 566 → `*/` 585); no queda ningún `Task` descartado en código que compile.
>
> **Lo que queda abierto**: **UI-09** (el `MC3066` de arriba, bloqueante para terminar de verificar `WPF_Desktop`), **TEST-07** (los 6 `CS1061` de `EDUSIS.EndToEndTests`), **CORE-13**/H-024 (ver abajo), y la migración de EF Core (H-023), que pasó de estar bloqueada por `Core` a estarlo por UI-09 — el *startup project* de las migraciones es `WPF_Desktop`, y necesita compilar. **Actualización 2026-10-03**: UI-09 y TEST-07 se **cerraron** (`cae617f` y `dd89f93`), `EDUSIS.sln` compila con 0 errores y `WPF_Desktop` quedó adaptado a este bloque (ver la corrección de abajo). Lo que sigue abierto de esta lista es CORE-13/H-024 y H-023, y H-023 ya no está bloqueada por la compilación sino por la base de datos, que no está instalada: la migración sigue siendo sólo `InitCreate`. La verificación funcional de `WPF_Desktop` en Windows sigue **pendiente** (ver [`wpf-desktop-tasks.md`](wpf-desktop-tasks.md) §Verificación funcional).
>
> **Corrección a la predicción de los 41 sitios de llamada (2026-10-03, contra lo que el compilador terminó imprimiendo).** La predicción de "Verificación" —41 sitios en 6 archivos: `GestionCurriculasViewModel.cs` 20, `GestionSituacionRevistaViewModel.cs` 10, `GestionDivisionesViewModel.cs` 5, `GestionCursantesViewModel.cs` 2, `GestionDocentesViewModel.cs` 1, `GestionCurriculasView.xaml` 3— **no se cumplió ni en el total ni en el reparto**, y la nota de arriba (que la daba por "a medio verificar") también se queda corta: el enmascaramiento no era de dos etapas sino de **tres**, y la secuencia de errores que `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` imprimió a lo largo de la tanda fue `1` (markup) → `18` (declaración) → `40` (cuerpos de método, pico) → `32` → `25` → `20` → `1` → `0`. El pico fue **40**, no los ~45-60 que preveía `wpf-desktop.md`, ni 41 sitios de `core.md`.
>
> - **Lo que el compilador imprimió, por etapa.** *Markup*: 1 `MC3066` (`GestionSituacionRevistaView.xaml:25`). *Declaración*: 18 errores (1 `CS0234` + 17 `CS0246`) en 10 archivos, todos por tipos que cambiaron de namespace; uno de ellos (`RegistrarAlumnoViewModel.cs:28`) cae fuera de `ViewModels/Cursos/**` y la predicción ni lo mencionaba. *Cuerpos de método*, al cerrar W1: **40** errores distintos, 31 `CS1061`, 6 `CS0246`, 2 `CS0029` y 1 `CS7036`. Esa mezcla ya contradice la predicción, que sólo hablaba de llamadas a miembros mudados.
> - **Seis de esos errores no eran llamadas sino tipos eliminados.** Los 6 `CS0246` de `GestionSituacionRevistaViewModel` (`ListarCargosDocenteSegunMateriaRequest`, `RelevarDocenteDeAulaRequest`, `RescindirCargoDocenteRequest`, `EliminarCargoDocenteRequest`, `EstablecerDocenteDeAulaRequest` y `RegistrarDocenteEnMateriaRequest`) son requests que **este bloque eliminó** al renombrar y mover los casos de uso a `IServicioCatedra`, así que ningún `using` los resuelve: había que reprogramar las seis llamadas. Aparecieron sólo cuando el grafo de declaraciones quedó limpio, porque son referencias dentro de cuerpos de método: un `CS0246` no es necesariamente un error de etapa de declaración.
> - **El reparto por archivo, medido al cerrar W3 (32 errores)**: `GestionSituacionRevistaViewModel` **19** (predicho 10), `GestionDivisionesViewModel` **7** (predicho 5), `GestionCurriculasViewModel` **4** (predicho 20), `GestionCursantesViewModel` **1** (predicho 2), `GestionDocentesViewModel` **1** (predicho 1). Entre el cierre de W1 (40) y el de W3 (32), W3 cerró otros 8 errores, que por construcción están en sus archivos propios (los ViewModels y vistas de ítem: `CursoViewModel`, `CurriculaViewModel`, `MateriaViewModel`, `DivisionViewModel`, `CursanteViewModel`, `CalificacionViewModel`, `SituacionRevistaViewModel`) y que la predicción **no listaba**. Los 40 no están desglosados por archivo en la Bitácora, así que esos 8 son la diferencia 40 − 32 y no un conteo medido archivo por archivo. `GestionCurriculasViewModel` fue la sobreestimación grande (20 previstos, 4 reales), y la Bitácora no registra el motivo del desfase.
> - **Los 3 sitios de `GestionCurriculasView.xaml` no eran errores de compilación.** Un binding roto en WPF **no falla el build**: deja el control vacío. Ningún compilador los imprimió, y la capa tuvo defectos de esa familia que la predicción no podía ver y se encontraron por lectura y por revisión (`GestionSituacionRevistaView.xaml:194` bindeando la propiedad que W3.B eliminó, `GestionDivisionesView.xaml:229` y `:387`, `GestionCatedrasView.xaml:112` con un estilo de `Label` aplicado a un `TextBlock`, que lanza `InvalidOperationException` sólo con al menos una cátedra).
> - **Dos distorsiones de medición que conviene no repetir.** El build completo imprime cada error y cada advertencia **dos veces** (WPF compila un proyecto temporal `*_wpftmp.csproj`), así que todo conteo se deduplica con `sort -u` sobre `file:line: code`; y correr `-t:ResolveReferences;CoreCompile` después de un `--no-incremental` agrega ~45 errores falsos (`CS0103` por `InitializeComponent`, `CS5001` por falta de `Main`) porque no se generaron los `*.g.cs`.
>
> **Lección para predecir el próximo refactor de este tipo**: contar los sitios de llamada por `rg` sobre los miembros mudados sirve para dimensionar el esfuerzo, pero no para predecir el conteo de errores. El compilador revela el trabajo en etapas, y entre etapas **aparecen errores de una clase distinta** (aquí, tipos eliminados y no mudados) que ningún barrido textual de miembros anticipa.
>
> **CORE-13 / H-024** (hallazgo nuevo, no anticipado por este plan): `Core/ServicioAlumnos/ServicioAlumno.cs` (`RegistrarAlumnoAsync`) genera el legajo con `Guid.NewGuid().ToString().GetHashCode().ToString("x")`, que produce un hexadecimal en minúsculas de largo variable (a veces con signo negativo), mientras que `Alumno..ctor` exige el patrón `^([A-Z]{2}\d{4})$`: el alta de alumno revienta siempre con `FormatException`. Lo destapó `Core.UnitTests` al ejercitarse por primera vez, ahora que `Core` compila — no es una regresión de este bloque, es un defecto preexistente que estaba oculto porque la suite nunca llegaba a correr.

## Context

El refactor de límites de agregado de `Domain` (rama `002-refactor-agregados-dominio`, commits `db14e72`…`0f39316`) partió los agregados monolíticos `Curso` y `Curricula` en once raíces independientes. `Infrastructure` ya quedó alineada: once repositorios implementados, `UnitOfWork` completo, configuraciones EF con tabla propia para `Division`, `Catedra`, `Cursante` y `PlanillaAsistencia`, y despacho de eventos en cascada (`MediatrExtension.DispatchDomainEventsAsync` drena en bucle, así que un handler que encole eventos nuevos se despacha en la misma transacción).

`Core` es la única capa sin migrar, y hoy es la que corta la compilación de toda la solución: `dotnet build EDUSIS.sln` falla en `Core`, y por eso `Infrastructure`, `WPF_Desktop` y los seis proyectos de `tests` nunca llegan a compilar su propio código. El compilador sólo imprime 2–4 errores de *etapa de declaración*; detrás hay ~40 llamadas rotas enmascaradas, porque Roslyn no liga cuerpos de método mientras exista un error de declaración.

Además de reparar lo roto, esta tarea cierra un hueco funcional: cinco agregados (`Division`, `Cursante`, `PlanillaAsistencia`, `Materia`, `Catedra`) tienen repositorio y mapeo EF pero **ningún caso de uso los alcanza**.

Resultado buscado: `Core` compilando, con un servicio de aplicación por raíz de agregado, los invariantes que cruzan instancias resueltos en `Core` con consulta a repositorio, y los cinco hallazgos de `Core` corregidos.

---

## Diagnóstico

### A. Errores de etapa de declaración (los que el compilador imprime hoy)

| Archivo | Problema |
|---|---|
| `Core/ServicioCurriculas/Events/MateriaEliminadaEventHandler.cs:3` | `using Domain.Cursos.DomainEvents;` — el evento se mudó a `Domain.Materias.DomainEvents` |
| `Core/ServicioCursos/DTOs/Responses/CursoResponse.cs:1` | `using Domain.Cursos;` pero `NivelEducativo` se mudó a `Domain.Shared` |

### B. Llamadas rotas enmascaradas

Dos familias, y no conviene confundirlas porque se arreglan distinto.

**B.1 — API de dominio eliminada por el refactor.** Hay que reescribir el caso de uso, no renombrar la llamada:

- `ServicioCurso`: `Curso.Divisiones`, `Curso.CantidadAlumnos`, `Curso.AsignarPreceptor`, `Curso.QuitarPreceptor`, `Curso.AgregarDivision`, `Curso.QuitarDivision`, `Curso.AgregarAlumnoEnDivision`, `ICursoRepository.DivisionesDelCurso`, `ICursoRepository.CursoConDivisiones`, `Division.TotalAlumnos`.
- `ServicioCurricula`: `Curricula.Materias` (líneas 80, 163, 203, 322), `Curricula.AgregarMateria`, `.ActualizarMateria`, `.QuitarMateria`, `.AsignarDocenteEnMateria`, `.RelevarDocenteDeMateria`, `.RescindirDocenteDeMateria`, `.EstablecerDocenteEnFuncionesEnMateria`, `.EliminarCargoDocenteDeMateria`, `Materia.Docente`, `Materia.Docentes`, `Materia.CargosOcupados()`, `SituacionRevista.EnFunciones`, `SituacionRevista.MateriaID`.
- `ServicioDocente`: `Docente.Puesto` (propiedad singular, siempre `null`) en `ListarPreceptoresActivosAsync`.

**B.2 — Desalineación de nombre o firma contra los contratos de repositorio.** Arreglo mecánico:

| Llamada en `Core` | Contrato real |
|---|---|
| `Alumnos.Eliminar(id)`, `Cursos.Eliminar(id)` | `IRepository<T>.EliminarAsync(Guid)` |
| `Licencias.Eliminar(id, docenteID)` | `EliminarAsync(Guid)` — la aridad de 2 no existe |
| `Licencias.BuscarPorIDAsync(licenciaID, docenteID)` ×4 | `ILicenciaRepository.BuscarPorIDYDocenteAsync(Guid, Guid)` |
| `Usuarios.BuscarPorEmail(x)` ×2 | `IUsuarioRepository.BuscarPorUsernameAsync(string)` |
| `Usuarios.ExisteUsuarioDelDocente(x)` ×2 | `ExisteUsuarioDelDocenteAsync(Guid)` |
| `Docentes.BuscarDocentePorIDConPuestosAsync(x)` ×6 | No existe. `DocenteRepository.Consulta()` ya hace `Include(Puestos)`, así que `BuscarPorIDAsync` alcanza |
| `Cursos.BuscarAsync(pred)`, `Docentes.BuscarAsync(pred)` ×3, `Usuarios.BuscarAsync(pred)` | `IRepository<T>` no expone búsqueda por predicado, y no debe: la consulta va nombrada en el repositorio |

### C. Agregados sin servicio de aplicación

`Division`, `Cursante` (+`Calificacion`), `PlanillaAsistencia`, `Materia`, `Catedra` (+`SituacionRevista`, `Horario`).

### D. Hallazgos de `Core` pendientes

H-014 `Logout` → `NotImplementedException`. H-015 `ActualizarRol` → `NotImplementedException`. H-016 `SolicitarLicencia` con plazo termina en `LicenciaInactivaException`. H-017 `RegistrarCalificacion` no persiste nada. H-018 `QuitarDocente` es `async void`.

---

## Decisiones tomadas

1. **Un servicio por raíz de agregado.** Alinea servicio ↔ raíz ↔ repositorio, que ya es 1:1:1, y sigue la convención vigente del repo.
2. **Los cinco hallazgos de `Core` entran en el alcance.**
3. **Los invariantes que cruzan instancias se resuelven en `Core`** con consulta a repositorio más excepción propia, siguiendo el precedente de `Core/ServicioCursos/Exceptions/CursoDuplicadoException.cs`: `public class`, `private const string ERROR`, ctor sin parámetros, tabs.
4. **Los handlers de eventos reciben `IUnitOfWork`**, nunca un repositorio concreto (ver violación #1).
5. **El ciclo lectivo va explícito en el request**, nunca inferido del reloj. Es lo que ya exigen los cuatro métodos de `ICursanteRepository`: `Cursante` acumula una fila por cohorte anual y sin ese filtro los conteos mezclarían años.
6. **`Logout` se elimina de `Core`** (H-014). La sesión es `Thread.CurrentPrincipal`, asignado en `WPF_Desktop/ViewModels/Usuarios/LoginUsuarioViewModel.cs:171` con `CustomPrincipal.Create(...)`. `Core.Login` sólo verifica credenciales contra el repositorio y devuelve un `UsuarioResponse`; no abre sesión ni persiste nada. El logout es el inverso exacto de tres pasos de presentación (limpiar el principal, mandar el mensaje, navegar a `StartupWindow`) y ninguno pasa por `Core`. Verificado: **nadie llama a `Logout`** — las únicas coincidencias en la solución son su declaración, su `throw` y la prueba que verifica el `throw`.

---

## Fase 0 — Línea base

Capturar el conjunto real de errores antes de tocar nada:

```bash
dotnet build Core/Core.csproj 2>&1 | rg "error CS" | sort | uniq -c
```

Arreglar **primero** los dos `using` de la sección A. Eso **sube** el conteo al destapar los enmascarados: es lo esperado, no una regresión — el mismo patrón que `tasks.md` documentó para H0 del refactor de `Domain`.

## Fase 1 — Alineación mecánica (sección B.2)

Renombrar las llamadas de la tabla B.2 en `ServicioAlumno`, `ServicioDocente`, `ServicioLicencia`, `ServicioUsuario`, `ServicioAutenticacion`.

Los cinco `BuscarAsync(predicado)` **no** se resuelven filtrando en memoria: la consulta se declara en el repositorio. Toques aditivos:

| Contrato | Método nuevo | Motivo |
|---|---|---|
| `Domain/Cursos/ICursoRepository.cs` (hoy vacío) | `Task<bool> ExisteCursoAsync(Grado grado, NivelEducativo nivelEducativo)` | `ServicioCurso.RegistrarCurso` |
| `Domain/Docentes/IDocenteRepository.cs` | `Task<IReadOnlyCollection<Docente>> BuscarSegunPosicionAsync(Posicion posicion)` | listado de preceptores |

`IDocenteRepository.BuscarActivosAsync()` y `IUsuarioRepository.BuscarPorDocenteAsync(Guid)` ya cubren los otros tres usos.

Cada método nuevo se implementa en `Infrastructure/Repository/` y en su fake de `tests/EDUSIS.TestSupport/Fakes/`.

## Fase 2 — Adelgazar `ServicioCurso` y `ServicioCurricula`

**`Core/ServicioCursos/ServicioCurso.cs`** queda con tres casos de uso: `ListarCursosAsync`, `RegistrarCurso`, `EliminarCurso`. Todo lo de divisiones, preceptores, cursantes y calificaciones se muda. `CursoResponse` pierde los conteos que ya no son del curso:

```csharp
// antes: record CursoResponse(Guid CursoID, Grado Grado, NivelEducativo NivelEducativo, int Divisiones, int Alumnos);
public record CursoResponse(Guid CursoID, Grado Grado, NivelEducativo NivelEducativo);
```

**`Core/ServicioCurriculas/ServicioCurricula.cs`** queda con `ListarCurriculasSegunCursoAsync`, `RegistrarCurriculaAsync`, `DesafectarCurriculaAsync`. `CurriculaResponse` deja de anidar `MateriaResponse` — las materias se piden a `IServicioMateria`. Corregir el `throw ex` de la línea 153 por `throw` (resetea el stack trace; el resto del repo usa `throw`).

Los DTOs de materias, horarios y situación de revista se **mueven** (no se duplican) a los módulos nuevos que los usan.

## Fase 3 — Servicios nuevos

Patrón del repo para cada uno: carpeta `Core/Servicio<X>/`, `IServicio<X>.cs` público, `internal class Servicio<X> : IServicio, IServicio<X>`, ctor con `IUnitOfWork` + `ILogger<T>`, `DTOs/Requests/` y `DTOs/Responses/` con `record` posicionales en español, `try` → cargar agregado → invocar dominio → `GuardarCambiosAsync()` → mapear a `Response`; `catch` → `LogDebug` + `throw`.

| Servicio | Casos de uso | Invariante de `Core`: consulta + excepción nueva |
|---|---|---|
| `Core/ServicioDivisiones/` | listar del curso, agregar (`Division.Siguiente` + `DescripcionesDelCursoAsync`), eliminar (`ValidarSePuedeEliminar` con nivel del curso + conteo de cursantes), asignar/quitar preceptor | `DivisionDuplicadaException` (`ExisteDivisionConDescripcionAsync`), `PreceptorAsignadoException` (`ExistePreceptorAsignadoAsync`) |
| `Core/ServicioCursantes/` | inscribir, listar por división, registrar/quitar calificación, inasistencia a examen, modificar observación | `InscripcionDuplicadaException` (`ExisteInscripcionAsync`); cupo vía `ContarCursantesDeDivisionAsync` + `Division.ValidarCupoDisponible` |
| `Core/ServicioAsistencias/` | abrir planilla (cursantes desde `BuscarInscriptosEnFechaAsync`), marcar presente/ausencia/inasistencia/tardanza, incorporar cursante, cerrar, reabrir, contar faltas | `PlanillaDuplicadaException` (`ExistePlanillaAsync`) |
| `Core/ServicioMaterias/` | listar por currícula, registrar, modificar, eliminar (`Materia.Eliminar()` dispara el evento) | `NombreMateriaDuplicadoException` (las dos sobrecargas de `ExisteNombreMateriaEnCurriculaAsync`), `LimiteEspaciosCurricularesException` (`TotalEspaciosSegunCurriculaAsync`), `LimiteHorasSemanalesException` (`TotalHorasCatedraSegunCurriculaAsync`) |
| `Core/ServicioCatedras/` | crear cátedra, designar docente, poner/relevar de funciones, establecer fin y finalizar designación, agregar/quitar horario, listar por materia/división/docente | `CatedraDuplicadaException` (`ExisteCatedraAsync`), `ColisionHorariaEnDivisionException` (`CatedrasSegunDivisionAsync` + `Horario.SeSuperponeCon`), `DocenteEnDosAulasException` (`CatedrasSegunDocenteAsync` + `SeSuperponeCon`) |

Notas de diseño:

- **`ServicioCatedra.CrearCatedraAsync` es el punto de congelamiento.** Copia `Materia.HorasCatedra` a `Catedra.CargaHoraria`. Es una instantánea legítima (dato congelado con la currícula vigente), no duplicación mutable — pero el servicio debe validar antes que la currícula de la materia esté vigente (`Curricula.EstaVigente()`), que es lo que habilita la copia según `CLAUDE.md`.
- **Las dos validaciones de horario reutilizan `Horario.SeSuperponeCon(otro)`** del dominio. Lo que aporta `Core` es el barrido sobre las otras cátedras, que el agregado no puede ver.
- **`Division` recibe los conteos, no los consulta.** `ValidarSePuedeEliminar(NivelEducativo, int)` y `ValidarCupoDisponible(int)` los toman por parámetro desde el servicio: el agregado nunca llama a un repositorio.
- **Los requests con matrícula llevan ciclo lectivo** (`string` de 4 dígitos, validado por `CicloLectivo.Crear`), p. ej. `ListarDivisionesRequest(Guid CursoID, string CicloLectivo)`.

## Fase 4 — Handlers de eventos

- **`MateriaEliminadaEventHandler`**: reescribir y **mover** a `Core/ServicioCatedras/Events/`. Hoy inyecta `ICurriculaRepository` (con el campo mal nombrado `_materiaRepository`), usa el `Task` sin `await` y llama a un `Eliminar` que no existe. Su propósito real con el diseño nuevo es la cascada entre agregados: al eliminarse una `Materia`, las `Catedra` que la referencian quedan huérfanas → `Catedras.CatedrasSegunMateriaAsync(MateriaID)` + `EliminarRango`. Pasa a recibir `IUnitOfWork`.
- **`LicenciaSolicitadaEventHandler`**: cambiar `ICursoRepository` por `IUnitOfWork` (el repositorio inyectado nunca se usa) y **conservar el `TODO`** — es trabajo en curso y la convención del repo es no borrarlo.
- Eventos sin handler, anotados como trabajo futuro y **no** implementados acá: `CatedraSinDocenteEnFuncionesEvent`, `PlanillaAsistenciaCerradaEvent`, `PlanillaAsistenciaReabiertaEvent`.

## Fase 5 — Hallazgos de `Core`

| # | Corrección |
|---|---|
| H-014 | Eliminar `Logout` de `IServicioAutenticacion` y `ServicioAutenticacion`, junto con `DTOs/Requests/LogoutRequest.cs`. En `hallazgos.md`, pasa de "Pendiente" a **"Eliminado"** con la justificación de la decisión #6. Se borran las dos pruebas de `Logout` (no queda `Skip`) |
| H-015 | Implementar como `Task AsignarRolAsync(ActualizarRolRequest)` → `usuario.AgregarRol(request.Rol)` + `GuardarCambiosAsync()`. El DTO lleva un solo rol y la prueba se llama `ActualizarRol_asigna_el_rol_indicado_al_usuario`, así que la semántica es *asignar*, no reemplazar el conjunto. Agregar el simétrico `QuitarRolAsync` (el dominio ya expone `Usuario.QuitarRol` y hoy no hay forma de llegar) |
| H-016 | `ServicioLicencia.SolicitarLicencia`: usar el ctor de `Licencia` de 5 argumentos (con `fechaFin`) en lugar de crear la licencia `Pendiente` y después llamar a `EstablecerFechaFinalizacion`, que exige licencia activa |
| H-017 | Resuelto por construcción: `RegistrarCalificacion` se reimplementa en `ServicioCursante` contra `Cursante.RegistrarCalificacion` + `GuardarCambiosAsync()` |
| H-018 | `ServicioDocente.QuitarDocente` → `Task QuitarDocenteAsync(...)` en interfaz e implementación. El `async void` se traga las excepciones |

Al cerrar cada uno: quitar el `Skip` de su prueba en `tests/Core.UnitTests/` y actualizar la tabla de estado de `hallazgos.md`.

## Fase 6 — Registro y cierre

- `Core/Shared/CoreDI.cs`: registrar los cinco servicios nuevos como `Scoped`.
- `Core/Core.csproj`: quitar `<Folder Include="ServicioUsuarios\DTOs\Responses\" />` si la carpeta queda vacía.
- `tests/Core.UnitTests/`: `HostDeServicios` ya monta `CoreDI` real contra `UnitOfWorkFake`, y los once fakes de repositorio existen — los servicios nuevos son testeables sin tocar la infraestructura de prueba.
- **Mayúsculas de `tests/`**: para agregar archivos nuevos hay que usar la ruta de disco (`git add Tests/...`). Verificar con `git diff --cached --name-status` antes de commitear.

---

## Verificación

```bash
# Criterio real de esta tarea: los dos proyectos en alcance
dotnet build Core/Core.csproj
dotnet build Infrastructure/Infrastructure.csproj

# Pruebas de unidad (dominio + Core, sin BD, < 60 s)
./tests/run-tests.sh --rapidas
dotnet test tests/Core.UnitTests
```

Criterios de aceptación:

- `dotnet build Core/Core.csproj` e `Infrastructure/Infrastructure.csproj` **sin errores**. Advertencias: la línea base preexistente es ~51 (nulabilidad); no debe crecer por código nuevo.
- `tests/Core.UnitTests` verde, con las pruebas de H-015/H-016/H-017/H-018 **sin `Skip`** y las de H-014 eliminadas.
- `rg -n "using Infrastructure|EntityFrameworkCore|IQueryable|\.Include\(|EF\.Functions" Core/` → cero coincidencias. Hoy ya es cero: es una regresión a evitar, no un arreglo.
- `rg -n "BuscarAsync\(x =>" Core/` → cero coincidencias (ninguna consulta por predicado dentro de un servicio).
- Cada servicio nuevo registrado en `CoreDI` y resoluble desde `HostDeServicios`.

**`dotnet build EDUSIS.sln` va a seguir fallando, y está bien.** Los errores se concentran en `WPF_Desktop`: 41 sitios de llamada en 6 archivos (`GestionCurriculasViewModel.cs` 20, `GestionSituacionRevistaViewModel.cs` 10, `GestionDivisionesViewModel.cs` 5, `GestionCursantesViewModel.cs` 2, `GestionDocentesViewModel.cs` 1, `GestionCurriculasView.xaml` 3). Confirmar que **todos** nombran métodos mudados o firmas cambiadas por este trabajo, y ninguno es un error propio; dejar el listado registrado para la tarea de UI. La ejecución manual de la app en Windows no aplica en esta tarea. **[Corregido 2026-10-03]**: las cifras de este párrafo (41 sitios, 20/10/5/2/1/3) no coinciden con lo que el compilador terminó imprimiendo; ver la corrección en la nota de cabecera.

---

## Violaciones DDD / Clean Architecture detectadas

`CLAUDE.md` exige notificar toda violación detectada, aunque quede fuera de alcance. Las 1–4 se corrigen en este plan; las 5–12 quedan anotadas.

1. **Los handlers de eventos inyectan repositorios concretos que nadie registra en el contenedor.** `MateriaEliminadaEventHandler(ICurriculaRepository)` y `LicenciaSolicitadaEventHandler(ICursoRepository)`. Ni `CoreDI` ni `InfrastructureDI` registran repositorios individuales — sólo `IUnitOfWork` — y los repositorios concretos de `Infrastructure` son `internal`. MediatR fallaría al construir el handler en tiempo de ejecución con un error de resolución de DI. Es un bug latente que no se ve porque ningún handler se ejerció nunca: `MateriaEliminadaEvent` estaba huérfano hasta que el refactor le dio un throw site en `Materia.Eliminar()`. **Se corrige** (Fase 4).
2. **`ServicioCurricula` atravesaba tres agregados por navegación de objetos.** `curricula.Materias.SelectMany(x => x.Docentes)` (línea 322) y `materia.Docente.Periodo.FechaInicio` (líneas 172–190). Ya no compila, pero `CurriculaResponse` sigue anidando `MateriaResponse`, lo que induce a reconstruir la navegación. **Se corrige** aplanando el DTO (Fase 2).
3. **`SituacionRevistaResponse` lleva `MateriaID`**, pero `SituacionRevista` es entidad interna de `Catedra` y no conoce la materia: la conoce la cátedra. **Se corrige** al mover el DTO a `ServicioCatedras` (Fase 3).
4. **`ServicioCurso.BuscarDivisionesAsync` expone `Alumnos: x.TotalAlumnos`**, leyendo una colección que `Division` ya no guarda por decisión de diseño (la inscripción vive en `Cursante.DivisionID`). **Se corrige** con `ContarCursantesDeDivisionAsync` desde el servicio (Fase 3).
5. **`IGeneradorDocumentos` es el testigo de assembly de `Core` en MediatR.** `InfrastructureDI` registra los handlers con `typeof(IGeneradorDocumentos).Assembly`, y ese puerto vive en `Core.Shared.Documentos`, así que funciona. Pero es implícito hasta la opacidad: si el puerto se mueve a `Domain.Shared` o se elimina, **todos** los `INotificationHandler` de `Core` dejan de registrarse sin un solo error de compilación. Un tipo-marcador explícito (`Core.Shared.CoreDI`) sería honesto.
6. **`Curso` quedó anémico.** Tras el refactor sólo tiene `Grado` y `NivelEducativo`, sin un método de comportamiento: es una raíz de agregado que no protege ningún invariante más allá de validar sus dos enums en el constructor. Vale preguntarse si es raíz o catálogo.
7. **`Docente.Puesto` (singular) coexiste con `Docente.Puestos` (colección) y nunca se asigna** — es siempre `null`. `ServicioDocente.ListarPreceptoresActivosAsync` lo lee (`x.Puesto.Posicion`), así que además es un `NullReferenceException` garantizado. Es dominio, fuera de alcance.
8. **`Core/ServicioSecurity/` mezcla idiomas**: carpeta en inglés, interfaz `IServicioSeguridad` en español. Contradice "todo el dominio y la UI están en español".
9. **Dos namespaces para una carpeta física**: `Core/Shared/DTOs/Personas/Response/` declara `Core.Shared.DTOs.Personas.Response` (singular) en dos archivos y `...Responses` (plural) en tres. `ServicioDocente` y `ServicioAlumno` importan ambos.
10. **Métodos públicos inalcanzables**: `ServicioCurso.RegistrarCursanteEnDivision` y `ServicioDocente.ListarPreceptoresActivosAsync` no están declarados en su interfaz, y las clases son `internal` resueltas por DI vía contrato público. Se resuelven por mudanza (Fase 3) y por eliminación (#7).
11. **`GuardarCambiosAsync()` sin `await`** en `ServicioCurso.EliminarCurso:106`, `ServicioCurricula.ModificarMateriaAsync:283` y `.EliminarMateriaAsync:303`. El `Task` se descarta: el log imprime el `Task` en lugar del número de filas y **las excepciones de persistencia se pierden en silencio** dentro de un `try/catch` que parecía cubrirlas. Es el modo de fallo inverso al `async void` de H-018. Se corrige de paso al reescribir esos métodos.
12. **`Thread.CurrentPrincipal` como sesión** (`WPF_Desktop`, preexistente, fuera de alcance): es estado por hilo, y en WPF con `async/await` las continuaciones pueden retomar en otro hilo del pool, así que la identidad no se propaga de forma confiable. `UsuarioViewModel.cs` y `App.xaml.cs` la leen y pueden encontrarla `null`.
