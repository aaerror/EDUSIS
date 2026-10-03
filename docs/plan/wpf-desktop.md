# Plan — Adaptar la capa `WPF_Desktop` al refactor de agregados

> **Alcance**: sólo `WPF_Desktop/`. `Domain`, `Core`, `Infrastructure` y `tests/` quedan **explícitamente fuera**: compilan limpio y no se tocan. Cambios **funcionales**, no de UI/UX.
>
> Documentos hermanos: [`core.md`](core.md) (bloque `Core`, cerrado), [`handoff-errores.md`](handoff-errores.md) §4, [`plan.md`](plan.md) y [`tasks.md`](tasks.md) (refactor de `Domain`), [`../todos.md`](../todos.md) §WPF_Desktop (IDs `UI-01`…`UI-09`).

> **Estado al 2026-10-03 — la adaptación se ejecutó, compila y sus pruebas pasan; la tarea NO está cerrada porque la verificación funcional en Windows sigue PENDIENTE.** 42 commits sobre `f4f57fb` (`dd89f93`…`e8d5043`), ejecutados con la disección [`wpf-desktop-tasks.md`](wpf-desktop-tasks.md), cuya §Bitácora es la fuente de verdad. **Criterio alcanzado: builds + pruebas + guardas.** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → 0 errores; `dotnet build EDUSIS.sln --no-incremental` → 0 errores; `dotnet test tests/WPF_Desktop.UnitTests` → 320 pruebas, 320 pasan, 0 omitidas, 0 con error; `./tests/run-tests.sh --rapidas` sin regresiones (`Domain` 303/308, `Core` 191/192, omitidas preexistentes). Secuencia de errores: `1` → `18` → `40` → `32` → `25` → `20` → `1` → `0` (el pico fue 40, no los "~45-60" que preveía este plan). Advertencias de `WPF_Desktop`: 328 (línea base real de W1; la "7" de la Fase 0 era parcial) → 282; `CS4014` de 7 a 1. Guardas de §Verificación: `using Infrastructure` → sólo `App.xaml.cs:2`; `clr-namespace:Domain.Curriculas.Materias` → 0; `BuildServiceProvider` → 0; `async void` en `ViewModels/` → 6 (cinco handlers de comando y uno, `PerfilAlumnoViewModel.CargarPerfil`, que es un caso de uso: UI-22).
>
> **Lo que NO está hecho, sin ambigüedad.** Este plan dice que la §Verificación funcional en Windows es "lo único que cierra la tarea de verdad", y que "si la base no se puede instalar en esta tanda [...] la verificación funcional queda anotada como pendiente — sin declarar la tarea cerrada". La base de datos **no está instalada** (`H-023`: la migración sigue siendo sólo `InitCreate` y no refleja el mapeo de `ff2de55`), así que los cinco flujos de los pasos 1 a 5 y el paso 0 (arranque de la app, que es el que verifica el composition root de la Fase 9.0) están **sin ejecutar**. Las pruebas no lo reemplazan: no cubren los `Gestion*` que conservan `MessageBox.Show`, ni que `App` use el `IHost` correcto (construir `App` exige un `Application` de WPF). No debe leerse que la feature esté terminada.
>
> **Qué se ejecutó tal cual.** Las seis decisiones de "Decisiones tomadas" (`Domain` se conserva referenciado y sólo se repuntan namespaces; no se crean pantallas de `Asistencias`; la deuda `UI-01`…`UI-08` entra completa; el flujo de autenticación no se toca; `SituacionRevista` se alcanza por la pantalla intermedia `GestionCatedras`; `CicloLectivo` vive en un `CicloLectivoStore`). Las Fases 1 a 9 en el orden previsto: repunte de `xmlns` y `using` (W1), `CicloLectivoStore`/`CatedraStore`/`CatedraViewModel`/`IDialogService` (W2), los siete item ViewModels contra los DTO nuevos (W3), `GestionDivisiones` y `GestionCatedras` (W4), la dependencia de `GestionCurriculas` partida en `IServicioCurricula` + `IServicioMateria` y `GestionCursantes` reescrito (W5), `GestionSituacionRevista` contra `IServicioCatedra`, `UI-04`/`UI-05`/`UI-06`, `Models/` y `D.3` (W6), y el composition root (W7). Cerrados con evidencia en `docs/todos.md`: `UI-01`…`UI-13`, `UI-15` y `UI-16`.
>
> **Qué cambió de ruta respecto de este documento.** (1) **Entró `tests/`**, que el plan excluía: se creó `tests/WPF_Desktop.UnitTests` (fuera de `EDUSIS.sln`, TEST-08), `TEST-07` se cerró como ola propia (`W0.T`, `dd89f93`) y las olas de revisión `R1`…`R7` escribieron pruebas; por eso las correcciones de defectos que esas revisiones encontraron (`b57834c`, `0b1398b`, `dd12037`) se commitearon aparte. (2) **`IDialogService` entró en alcance** para los cinco `Gestion*` reescritos —este plan lo dejaba fuera por tocar UI/UX, violación 12—, porque es lo que vuelve testeables los remapeos; los demás `Gestion*` conservan `MessageBox.Show` (UI-14 sigue vigente). (3) Los contadores que los DTO dejaron de traer **se eliminaron de la vista** en lugar de recomponerse (UI-17). (4) **`UI-01` estaba mal diagnosticado**: el offset era el síntoma y la causa era que el combo ligaba `SelectedIndex` a un dato de dominio; ningún valor de offset arreglaba a la vez la visualización y la edición. (5) **Seis `CS0246` no se podían resolver con un `using`**, porque `Core` había **eliminado** esos requests al renombrar los casos de uso: había que reprogramar las seis llamadas. (6) **El criterio de `W2.C` era inalcanzable en su ola** (el proyecto de pruebas referencia a `WPF_Desktop`, que no compilaba hasta W6), lo que obligó a que `R2`…`R5` escribieran pruebas sin ejecutarlas. (7) **Cuatro criterios medían una propiedad global desde un alcance que no la controlaba** (`W2.C`, `W3.D`, `W5.B` y la cuarta guarda de §Verificación), y el reparto de errores por archivo que predecían `core.md` y este plan no coincidió con el compilador (ver la corrección en [`core.md`](core.md)). (8) `9.2` / `UI-13` se cerró **por verificación y no por cambio**: ninguno de los seis ViewModels recibe `INavigationService` por constructor. (9) `BuscarNavigationService` se eliminó en lugar de corregirle el tipo, y se quitó el `PackageReference` a `MediatR`. En total, 13 correcciones al plan, listadas en [`wpf-desktop-tasks.md`](wpf-desktop-tasks.md) §"Correcciones al plan".
>
> **Qué quedó abierto.** La verificación funcional y `H-023` (arriba). Deuda registrada en `docs/todos.md` al cerrar, UI-17…UI-37 y los items 23…27 de violaciones: los contadores eliminados (UI-17); el `DbContext` único de vida de aplicación, que el arreglo del contenedor hizo visible y cuyo arreglo (un `IServiceScope` por operación) este plan excluye (UI-18); `PlanillaAsistencia`, horarios y roles sin pantalla (UI-19); `CalificacionView` sin consumidores (UI-20); el `CS4014` que queda, un comando mal tipado (UI-21); `CargarPerfil` como caso de uso `async void` (UI-22); el riesgo de `ValidateScopes` en `Development` (UI-23); `OnExit` sin `Dispose` (UI-24); `CanExecute` obsoletos (UI-25, UI-32, UI-33); `EsVigente` y la vigencia de la currícula reimplementadas en la UI (UI-26, UI-28); el combo de horas 1-5 contra una `Materia` que acepta cualquier valor positivo (UI-27); `BuscarViewModel` sin camino de navegación (UI-29); `CLAUDE.md` desactualizado (UI-30); la cuarta guarda mal especificada (UI-31); el centinela `LegajoStore.PersonaID` (UI-34); y las dos transacciones de designar-y-poner-en-funciones y de alta-e-inscripción de alumno (violaciones 24 y 25).

## Context

La rama `002-refactor-agregados-dominio` partió los agregados monolíticos `Curso` y `Curricula` en once raíces independientes, y después alineó `Infrastructure` (once repositorios, mapeo EF con tabla propia para `Division`, `Catedra`, `Cursante` y `PlanillaAsistencia`) y `Core` (catorce servicios, cinco nuevos: `ServicioDivisiones`, `ServicioCursantes`, `ServicioAsistencias`, `ServicioMaterias`, `ServicioCatedras`). `git diff --name-status master...HEAD -- WPF_Desktop` devuelve **cero archivos**: la capa de presentación es la única que no se tocó, y es la que hoy corta el build de la solución.

Hoy `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` falla con **un solo error**:

```
Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml(25,13):
error MC3066: la referencia de tipo no puede encontrar un tipo público con el nombre 'Cargo'.
```

Es `UI-09`, y es engañoso por una razón de etapas: `MarkupCompilePass1` (XAML → BAML) corre **antes** de `CoreCompile`, así que aborta el proyecto antes de que Roslyn mire un solo ViewModel. Forzando `dotnet msbuild WPF_Desktop/WPF_Desktop.csproj -t:ResolveReferences;CoreCompile` aparecen **18 errores de C#** reales, todos `CS0234`/`CS0246` de *etapa de declaración*. Y detrás de esos hay una tercera ola: los `CS1061` por miembros de DTO que el refactor eliminó, que Roslyn no reporta mientras haya un tipo sin resolver.

**Consecuencia operativa, igual que en `tasks.md` H0 y en `core.md` Fase 0: el conteo de errores va a subir dos veces antes de bajar a cero. Es lo esperado, no una regresión.** Secuencia prevista: `1` (markup) → `~18` (declaración) → `~45`-`60` (cuerpos de método) → `0`.

Resultado buscado: `dotnet build EDUSIS.sln` limpio, cada ViewModel existente programado contra el servicio de `Core` que hoy es dueño de su caso de uso, la deuda `UI-01`…`UI-08` cerrada, el composition root arreglado (Fase 9), y la app ejecutable en Windows — lo que además desbloquea `H-023`, la migración de EF Core, cuyo *startup project* es `WPF_Desktop`.

> **Nota metodológica, leer antes de empezar.** `WPF_Desktop` tiene mucho código comentado preexistente, y un barrido con `rg`/`grep` cuenta los sitios comentados como si fueran vivos. Al relevar este plan, un barrido inicial reportó seis llamadas rotas en los módulos de Docentes, Usuarios y Alumnos (`EsCuilInvalido`, `EsLegajoInvalido`, `EsCUILValido`, `EsDNIValido`, `EsLegajoValido`, `ActualizarPuestoDocenteAsync`, `ModificarEventualidadPuestoDocenteRequest`, `RecuperarAcceso`, `IServicioCurso.BuscarDivisiones`, `InscribirAlumnoEnDivision`) — y **todas menos una** caen dentro de bloques `/* */`: `RegistrarDocenteViewModel` 131→164, `GestionPuestosViewModel` 159→189, `LoginUsuarioViewModel` 184→211, `RegistrarAlumnoViewModel` 100→157. No son errores y no se tocan (`CLAUDE.md`, FR-013). Es el mismo falso positivo que `core.md` nota en su encabezado y que `tasks.md` nota en T2.5/T4.3. **Verificar los delimitadores con `grep -n '/\*\|\*/' <archivo>` antes de dar por roto cualquier sitio que no haya impreso el compilador.**

## Decisiones tomadas

1. **`WPF_Desktop` conserva el `ProjectReference` a `Domain`**, y el acoplamiento se evalúa archivo por archivo (Fase 1). Criterio: se repunta el namespace cuando el tipo es un **enum de catálogo** que la UI bindea para elegir un valor (`Sexo`, `Vivienda`, `Grado`, `NivelEducativo`, `Articulo`, `Estado`, `Posicion`, `EstadoPuesto`, `Cargo`, `Instancia`); se aísla sólo donde el tipo acarree comportamiento o estado de agregado. Hoy no hay ningún caso del segundo tipo, así que la Fase 1 queda puramente mecánica. La dependencia se anota en "Violaciones".
2. **Se adaptan los ViewModels existentes; no se crean pantallas para `Asistencias`.** `PlanillaAsistencia` queda sin UI, anotado.
3. **La deuda `UI-01`…`UI-08` entra completa**, incluido descomentar y reescribir el flujo duplicado de `UI-08` y completar la inscripción de `UI-05`.
4. **El flujo de autenticación no se toca.** `App.xaml.cs` sigue arrancando en `MainWindow` con el login comentado, y la sesión sigue en `Thread.CurrentPrincipal`. Sólo se verifica que `LoginUsuarioViewModel`/`UsuarioViewModel` compilen contra `IServicioAutenticacion` sin `Logout`.
5. **`SituacionRevista` se alcanza por un paso intermedio de cátedra**: `GestionCurriculas` (materias) → **`GestionCatedras` (nueva)** → `GestionSituacionRevista` (reescrita). Es lo único que respeta el agregado: `SituacionRevista` es entidad interna de `Catedra` (materia + división) y `IServicioCatedra` exige `CatedraID` en nueve de sus trece métodos.
6. **`CicloLectivo` vive en un `CicloLectivoStore` singleton** en `WPF_Desktop/Store/`, inicializado en `DateTime.Today.Year`, inyectado a los ViewModels que lo necesiten. Nunca se infiere del reloj dentro de un ViewModel.

## Diagnóstico — lo que está roto, verificado

### A. Etapa de markup compile (1 error, bloqueante)

| ID | Archivo | Causa |
|---|---|---|
| `UI-09` | `Views/.../SituacionRevista/GestionSituacionRevistaView.xaml:9,25` | `xmlns:domainCargosDocentes="clr-namespace:Domain.Curriculas.Materias.CargosDocentes;assembly=Domain"`. `Cargo` se mudó a `Domain.Catedras.SituacionesRevista`. |

### B. Etapa de declaración (18 errores, confirmados por el compilador)

Casi todos son el mismo defecto: **el DTO existe con el mismo nombre, en otro namespace**. Tabla de repunte completa:

| Tipo | Namespace viejo | Namespace nuevo |
|---|---|---|
| `MateriaResponse`, `RegistrarMateriaRequest`, `ModificarMateriaRequest`, `EliminarMateriaRequest`, `ListarMateriasSegunCurriculaRequest`, `MateriaRequest`, `NombreDuplicadoRequest` | `Core.ServicioCurriculas.DTOs.*` | `Core.ServicioMaterias.DTOs.*` |
| `HorarioResponse`, `SituacionRevistaResponse`, `ListarHorariosRequest` | `Core.ServicioCurriculas.DTOs.*` | `Core.ServicioCatedras.DTOs.*` |
| `DivisionResponse`, `EliminarDivisionRequest`, `EliminarPreceptorRequest`, `RegistrarPreceptorRequest` | `Core.ServicioCursos.DTOs.*` | `Core.ServicioDivisiones.DTOs.*` |
| `CursanteResponse`, `CalificacionResponse`, `RegistrarCursanteRequest`, `BuscarListadoRequest`, `CrearCalificationRequest` | `Core.ServicioCursos.DTOs.*` | `Core.ServicioCursantes.DTOs.*` |

Más 2 referencias rotas a `Domain` en C# y 5 en XAML (sección C).

### C. Referencias a `Domain` desde la UI (7 rotas de 28)

| Archivo | Referencia | Corrección |
|---|---|---|
| `Shared/Converters/CargoConverter.cs:1` | `using Domain.Curriculas.Materias.CargosDocentes` | `Domain.Catedras.SituacionesRevista` |
| `Shared/Converters/NivelEducativoConverter.cs` | `using Domain.Cursos` — el namespace existe, el tipo ya no | `Domain.Shared` |
| `Views/Alumnos/RegistrarAlumnoView.xaml:6,17` | `clr-namespace:Domain.Cursos` → `NivelEducativo` | `Domain.Shared` |
| `Views/Cursos/RegistrarCursoView.xaml:9,28` | `clr-namespace:Domain.Cursos` → `NivelEducativo` | agregar un xmlns a `Domain.Shared` para `NivelEducativo` y **conservar** el de `Domain.Cursos`, porque `Grado` (línea 22) sigue ahí |
| `Views/.../SituacionRevista/GestionSituacionRevistaView.xaml:9,25` | `Cargo` | `Domain.Catedras.SituacionesRevista` |
| `Views/.../SituacionRevista/SituacionRevistaView.xaml:9,23` | `Cargo` | `Domain.Catedras.SituacionesRevista` |
| `Views/Cursos/Divisiones/CalificacionView.xaml:4,15` | `clr-namespace:Domain.Curriculas.Materias` → `Instancia` | `Domain.Cursantes.Calificaciones` |

Sanas, no tocar: `ArticuloConverter`, `EstadoPuestoConverter`, `GradoConverter`, `LicenciaEstadoConverter`, `PosicionConverter`, `SexoConverter`, `ViviendaConverter`, y los xmlns de `Domain.Personas`, `Domain.Personas.Domicilios`, `Domain.Licencias`, `Domain.Docentes.Puestos`.

### D. Etapa de cuerpos de método (enmascarados hoy; inferidos leyendo los DTO nuevos)

Dos familias, y conviene no confundirlas porque se arreglan distinto.

**D.1 — Miembros de DTO renombrados o eliminados.** Arreglo mecánico de mapeo:

| ViewModel | Lee hoy | DTO real |
|---|---|---|
| `Cursos/CursoViewModel.cs:38,39` | `CursoResponse.Divisiones`, `.Alumnos` | `CursoResponse(CursoID, Grado, NivelEducativo)` — los conteos se fueron a `DivisionResponse.Cursantes` |
| `Cursos/Curriculas/CurriculaViewModel.cs:40` | `CurriculaResponse.Materias.Count` | `CurriculaResponse(CursoID, CurriculaID, FechaInicio, FechaFin)` — aplanado a propósito |
| `Cursos/Curriculas/Materias/MateriaViewModel.cs:53,54` | `MateriaResponse.CargosOcupados`, `.SituacionRevistaResponse` | `MateriaResponse(CursoID, CurriculaID, MateriaID, Descripcion, HorasCatedra)` |
| `Cursos/Divisiones/DivisionViewModel.cs:37,38,39` | `.DocenteID`, `.Docente`, `.Alumnos` | `DivisionResponse(DivisionID, Descripcion, PreceptorID, Preceptor, Cursantes)` |
| `Cursos/Divisiones/CursanteViewModel.cs:30` | `.Edad` asignado a un `string` | `CursanteResponse.Edad` es `int`; falta mapear `CursanteID` y `EsRecursante` |
| `Cursos/Divisiones/CalificacionViewModel.cs:49,51` | `.Asistencia`, `.Instancia` como `int` | `CalificacionResponse(CalificacionID, MateriaID, Materia, Fecha, Instancia:string, Rindio, Nota, Aprobado, Observacion)` |
| `.../SituacionRevista/SituacionRevistaViewModel.cs:50,52,55,56` | `.MateriaID`, `.Docente`, `.FechaAlta`, `.FechaBaja` | `SituacionRevistaResponse(SituacionRevistaID, CatedraID, DocenteID, Estado, Cargo, FechaInicio, FechaFin, ReemplazaA, EnFunciones)` — **no trae el nombre del docente** |

**D.2 — Casos de uso que cambiaron de dueño.** Hay que reprogramar el ViewModel, no renombrar la llamada:

| Llamada hoy | Servicio dueño hoy |
|---|---|
| `IServicioCurricula.ListarMateriasSegunCurriculaAsync`, `.RegistrarMateria`, `.ModificarMateriaAsync`, `.EliminarMateriaAsync` | `IServicioMateria.ListarMateriasSegunCurriculaAsync`, `.RegistrarMateriaAsync`, `.ModificarMateriaAsync`, `.EliminarMateriaAsync` |
| `IServicioCurricula.ListarCargosDocenteSegunMateriaAsync`, `.RegistrarDocenteEnMateriaAsync`, `.EstablecerDocenteDeAulaAsync`, `.RelevarDocenteDeFuncionesEnMateriaAsync`, `.RescindirCargoDocenteDeMateriaAsync`, `.EliminarCargoDocenteAsync` | `IServicioCatedra.ListarSituacionesRevistaAsync`, `.DesignarDocenteAsync`, `.PonerEnFuncionesAsync`, `.RelevarDeFuncionesAsync`, `.EstablecerFinDeDesignacionAsync`, `.FinalizarDesignacionAsync` |
| `IServicioCurso.BuscarDivisionesAsync`, `.AgregarDivisionAlCurso`, `.QuitarDivisiosDelCurso`, `.RegistrarPreceptorEnDivision`, `.EliminarPreceptorDeDivision` | `IServicioDivision.ListarDivisionesAsync`, `.AgregarDivisionAsync`, `.EliminarDivisionAsync`, `.AsignarPreceptorAsync`, `.QuitarPreceptorAsync` |
| `IServicioCurso.BuscarListado` (comentada) | `IServicioCursante.ListarCursantesAsync` |
| `IServicioCurso` para calificaciones | `IServicioCursante.RegistrarCalificacionAsync`, `.ListarCalificacionesAsync`, `.QuitarCalificacionAsync`, `.ModificarObservacionCalificacionAsync`, `.RegistrarInasistenciaAExamenAsync` |

`IServicioCurso` queda con tres casos de uso (`ListarCursosAsync`, `RegistrarCurso`, `EliminarCurso`) e `IServicioCurricula` con tres (`ListarCurriculasSegunCursoAsync`, `RegistrarCurriculaAsync`, `DesafectarCurriculaAsync`).

**D.3 — Un error fuera del clúster de cursos.** El único sitio roto en los módulos de Docentes/Usuarios/Alumnos que **no** está comentado (ver la nota metodológica): `ViewModels/Docentes/GestionDocentesViewModel.cs:178` llama `_servicioDocentes.QuitarDocente(...)`, que `Core` renombró a `QuitarDocenteAsync` al corregir `H-018` (era `async void`). Además la llamada **no tiene `await`**, así que hoy descarta el `Task` — hay que corregir las dos cosas juntas, porque la primera sin la segunda deja el defecto que `H-018` quiso cerrar.

### E. Defectos estructurales que ningún compilador reporta

Verificados leyendo `App.xaml`, `App.xaml.cs` y `Store/DivisionStore.cs`. No bloquean el build, bloquean que la app **funcione**, que es el objetivo de esta tarea.

| # | Ubicación | Qué pasa |
|---|---|---|
| E.1 | `App.xaml:5` + `App.xaml.cs:65` | `App.xaml` declara `Startup="Application_Startup"` **y** la clase sobrescribe `OnStartup`. Los dos se ejecutan en cada arranque. |
| E.2 | `App.xaml.cs:62` | `ConfigureServices` llama a `services.BuildServiceProvider()` **dentro** del `ConfigureServices` del `IHost`, así que quedan **dos contenedores raíz** sobre la misma `IServiceCollection`: `AppHost.Services` y `_serviceProvider`. Los `*NavigationStore` están registrados `Singleton`, de modo que hay **dos** instancias de cada uno. |
| E.3 | `App.xaml.cs:111-114` | `Application_Startup` resuelve `MainWindow` de `AppHost.Services` (contenedor A) y el `INavigationService` de `_serviceProvider` (contenedor B). La navegación inicial escribe el ViewModel activo en el `MainWindowNavigationStore` de B, y la ventana observa el de A. |
| E.4 | `App.xaml.cs:67` | `OnStartup` hace `await AppHost.StopAsync()` **al arrancar**, sobre un host al que nadie le hizo `StartAsync()`. Y `OnStartup`/`OnExit` son `async void`: una excepción ahí termina el proceso sin pasar por `Application_DispatcherUnhandledException`. |
| E.5 | `Store/DivisionStore.cs:14` | La propiedad se llama `Curso` pero está tipada **`CursoStore`**, no `CursoViewModel`: es un store que guarda otro store. Nadie puede asignarle un curso; la propiedad es inservible. |

---

## Fase 0 — Línea base

Capturar el conjunto real de errores antes de tocar nada, y dejarlo anotado:

```bash
dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental 2>&1 | grep -E ': (error|warning) '
dotnet msbuild WPF_Desktop/WPF_Desktop.csproj -t:ResolveReferences\;CoreCompile -v:q -nologo 2>&1 | grep ': error'
```

**Siempre `--no-incremental`.** Un build incremental de un proyecto WPF devuelve `0 Errores` de forma engañosa porque el paso de XAML queda cacheado como válido de una corrida anterior (caveat ya documentado en `core.md` línea 9). El segundo comando es el que destapa la etapa de C# saltando el markup compile: sirve en la Fase 0 y en la Fase 1, y deja de ser necesario en cuanto `UI-09` esté resuelto.

## Fase 1 — Repunte mecánico (secciones B y C)

Objetivo: que el proyecto llegue a la etapa de cuerpos de método. Bajo en riesgo, pero **al terminar esta fase el conteo de errores sube**.

- **1.1** — `UI-09` primero, solo. Corregir el xmlns de `GestionSituacionRevistaView.xaml` y `SituacionRevistaView.xaml` a `Domain.Catedras.SituacionesRevista`. Criterio: desaparece el `MC3066` y el build empieza a imprimir errores `CS`.
- **1.2** — Los otros tres XAML de la sección C (`RegistrarAlumnoView`, `RegistrarCursoView`, `CalificacionView`). Ojo con `RegistrarCursoView`: necesita **dos** xmlns distintos, porque `Grado` sigue en `Domain.Cursos` y `NivelEducativo` se fue a `Domain.Shared`.
- **1.3** — Los dos converters (`CargoConverter`, `NivelEducativoConverter`). De paso, quitar el `using Domain.Personas` redundante de `ViviendaConverter.cs`.
- **1.4** — Los `using` de DTOs de la tabla de la sección B, en los ~14 archivos de `ViewModels/Cursos/**`. Un archivo puede necesitar **varios** namespaces nuevos: `GestionCursantesViewModel` necesita `Core.ServicioCursantes.DTOs.Requests`, `.Responses` y `Core.ServicioMaterias.DTOs.*`.

**Criterio Fase 1**: cero `CS0234`/`CS0246`/`MC3066`. Los errores restantes son todos `CS1061`/`CS7036`/`CS0117` de cuerpos de método — la ola D.

> **Orden de las fases.** Las fases 1 a 8 llevan el build a verde; la 9 es la que hace que la app *funcione*. No se pueden invertir: los defectos estructurales de la sección E sólo se pueden observar con la app corriendo, y la app no corre hasta que compila. Pero tampoco se puede declarar la tarea cerrada con el build verde y la 9 pendiente — ver el paso 0 de la verificación funcional.

## Fase 2 — Piezas nuevas de infraestructura de UI

Antes de tocar ViewModels, dejar lista la plomería que varios van a necesitar. Todo aditivo.

- **2.1** — `WPF_Desktop/Store/CicloLectivoStore.cs`. Mismo patrón que `CursoStore`: campo privado, propiedad pública, `event Action ...Changed`. Inicializa en `DateTime.Today.Year.ToString()`. Registrar como `AddSingleton` en `WPF_DesktopDI.AddStores`.
- **2.2** — `WPF_Desktop/Store/CatedraStore.cs`, idéntico patrón, guarda la cátedra seleccionada.
- **2.3** — `WPF_Desktop/ViewModels/Cursos/Curriculas/Materias/Catedras/CatedraViewModel.cs` sobre `CatedraResponse(CatedraID, MateriaID, DivisionID, CargaHoraria, HorasAsignadas, HorasSinAsignar, DocenteEnFuncionesID)`, más una propiedad `Division` (string) que el ViewModel contenedor completa desde `DivisionResponse.Descripcion`.

> **Nota de diseño**: los stores existentes guardan un **ViewModel** (`MateriaStore.Materia` es un `MateriaViewModel`), no un identificador, contra lo que dice `CLAUDE.md` ("pasan el ID de la entidad seleccionada entre ViewModels"). `CatedraStore` y `CicloLectivoStore` se escriben siguiendo la convención **documentada** (guardan el valor/`Guid`), no la del vecino. La inconsistencia preexistente queda anotada en "Violaciones" #4.

## Fase 3 — Item ViewModels contra los DTO nuevos (sección D.1)

Cada uno es un cambio de mapeo acotado. Son hojas del árbol: hacerlos antes de los `Gestion*` evita arrastrar errores.

- **3.1** — `Cursos/CursoViewModel.cs`: eliminar `Divisiones` y `Alumnos`. **Requiere tocar los XAML que los bindean** (`Views/Cursos/CursoView.xaml`, `Views/Cursos/GestionCursosView.xaml`): el conteo de divisiones ya no es del curso. Si la pantalla lo necesita, sale de `IServicioDivision.ListarDivisionesAsync(...).Count` desde `GestionCursosViewModel`.
- **3.2** — `Cursos/Curriculas/CurriculaViewModel.cs`: eliminar `Materias`. El conteo, si la vista lo muestra, sale de `IServicioMateria.ListarMateriasSegunCurriculaAsync(...).Count`.
- **3.3** — `Cursos/Curriculas/Materias/MateriaViewModel.cs`: eliminar `CargosOcupados` y `SituacionRevista`. Es la corrección de las violaciones DDD #2 y #3 de `core.md` llegando a la UI: la materia ya no conoce sus docentes. Actualizar `Views/.../Materias/MateriaView.xaml` y `GestionCurriculasView.xaml`.
- **3.4** — `Cursos/Divisiones/DivisionViewModel.cs`: `DocenteID`→`PreceptorID`, `Docente`→`Preceptor`, `Alumnos`→`Cursantes`. Actualizar los bindings de `DivisionView.xaml` y `GestionDivisionesView.xaml`, y el `CanExecute` de `GestionDivisionesViewModel:185`, que lee `Division.DocenteID`.
- **3.5** — `Cursos/Divisiones/CursanteViewModel.cs`: `Edad` pasa a `int` (o se mapea con `.ToString()`), agregar `CursanteID` y `EsRecursante`. `CursanteID` es necesario: todos los requests de asistencia y de calificación lo usan como clave.
- **3.6** — `Cursos/Divisiones/CalificacionViewModel.cs`: `Asistencia`→`Rindio`, `Instancia` pasa de `int` a `string`, `Fecha` deja de ser nullable, agregar `CalificacionID`, `Aprobado` y `Observacion`. La colección interna pasa a `ObservableCollection<MateriaViewModel>` — hoy guarda `MateriaResponse` crudo, un DTO de `Core` bindeado directo en la vista. Migrar el `INotifyDataErrorInfo` manual a `ObservableValidator`, como el resto del repo.
- **3.7** — `.../SituacionRevista/SituacionRevistaViewModel.cs`: `MateriaID`→`CatedraID`, `FechaAlta`/`FechaBaja`→`FechaInicio`/`FechaFin`, agregar `ReemplazaA`. **`Docente` (el nombre) deja de venir en el DTO**: se conserva como propiedad del ViewModel y la completa el contenedor (6.4).
- **3.8** — `Cursos/Curriculas/Materias/HorarioViewModel.cs`: sólo el `using`. La forma de `HorarioResponse(Turno, Dia, HoraInicio, HoraFin)` no cambió. **No tiene View ni `DataTemplate`**: queda anotado, no se crea (decisión #2).

## Fase 4 — Divisiones

- **4.1** — `Cursos/Divisiones/GestionDivisionesViewModel.cs`: cambiar `IServicioCurso` por `IServicioDivision` (el `IServicioDocente` se conserva para la búsqueda de preceptores). Remapear las cinco llamadas de la tabla D.2. `ListarDivisionesRequest(CursoID, CicloLectivo)` y `EliminarDivisionRequest(CursoID, DivisionID, CicloLectivo)` llevan ciclo lectivo → inyectar `CicloLectivoStore`.
- **4.2** — Corregir el bug de navegación: el constructor recibe `DivisionStore divisionStore` y **lo descarta** (no hay campo que lo guarde), y la línea 345 `//_divisionStore.Division = Division;` está comentada. Navegar a cursantes hoy pierde la división elegida. Guardar el store en un campo y asignar antes de `Navigate()`. Al hacerlo, resolver `E.5`: eliminar la propiedad `DivisionStore.Curso` (tipada `CursoStore`, inservible) o tiparla como corresponde. Quien necesite el curso ya tiene `CursoStore` inyectado.
- **4.3** — Corregir los tres `CargarDivisionesAsync()` sin `await` (líneas 211, 236, 383): el `Task` se descarta y las excepciones de recarga se pierden en silencio dentro de un `try/catch` que parecía cubrirlas. Es el mismo modo de fallo que la violación #11 de `core.md`.
- **4.4** — Actualizar el registro de `GestionDivisionesViewModel` en `WPF_DesktopDI` y los bindings de `GestionDivisionesView.xaml` afectados por 3.4.

## Fase 5 — Materias: partir `GestionCurriculasViewModel`

La pantalla mezcla dos agregados (`Curricula` y `Materia`) y hoy los pide a un solo servicio. No hace falta partir la **pantalla**, pero sí la **dependencia**.

- **5.1** — `Cursos/Curriculas/GestionCurriculasViewModel.cs`: agregar `IServicioMateria` al constructor y mover a él `ListarMateriasSegunCurriculaAsync`, `RegistrarMateriaAsync`, `ModificarMateriaAsync`, `EliminarMateriaAsync`. `IServicioCurricula` queda sólo para `ListarCurriculasSegunCursoAsync` y `RegistrarCurriculaAsync`. Quitar `IServicioDocente`, que se inyecta y no se usa en código vivo. Renombrar el parámetro `servicioMateria` de la línea 108, que en realidad recibe la currícula.
- **5.2** — **`UI-01`**: unificar el offset de carga horaria. El XAML liga `CargaHoraria` al `SelectedIndex` (0-based) del combo; el diálogo de confirmación (línea 240) muestra `CargaHoraria + 1` y el request (línea 252) envía `CargaHoraria` sin el `+1`. Corregir para que **ambos** usen el mismo valor. Elegir el índice 0 hoy intenta registrar `HorasCatedra = 0`, que `Materia` rechaza con `ArgumentException`. Y el flujo "Update" (líneas 283-288) debe usar el valor elegido en el combo en lugar de reenviar `Materia.HorasCatedra` sin cambios: hoy editar la carga horaria no tiene ningún efecto.
- **5.3** — **`UI-08`**: eliminar la duplicación. Descomentar el bloque de las líneas 379-426 (que ya tiene el offset bien) y fusionarlo con el flujo activo de `ExecuteGuardarCommandAsync` caso `"Materia"`, reescribiendo sus referencias muertas: `_servicioMateria` → el campo nuevo de 5.1, `_registrarMateriaRequest` → variable local, `LoadMaterias()` → `CargarMaterias()`, `NivelEducativoDescripcion` → `NivelEducativo`. Queda **un** flujo de alta de materia, no dos.
- **5.4** — **`UI-03`**: resolver los dos `//TODO: Verificar id de curricula` (líneas 248, 354). El de alta de materia usa `Curricula.CursoID`/`Curricula.CurriculaID` de la currícula seleccionada, que es correcto → el TODO es obsoleto y se elimina. El de alta de currícula usa `_cursoStore.Curso.CursoID` y no necesita `CurriculaID` → también se elimina.
- **5.5** — Convertir a `Task` los cuatro `async void` del archivo (`CargarCurriculas`, `CargarMaterias`, `ExecuteRegistrarCommand`, `ExecuteCancelarCommand`). Los dos primeros se invocan desde XAML con `behaviors:CallMethodAction` sobre `Loaded`, que exige un método sin retorno observable: envolverlos en un `IAsyncRelayCommand` disparado con `behaviors:InvokeCommandAction`, patrón que `GestionDivisionesView` ya usa con `CargarDivisionesCommandAsync`.
- **5.6** — Actualizar el registro en `WPF_DesktopDI` y los bindings de `GestionCurriculasView.xaml` (tres sitios predichos por `core.md`, más los de 3.3).

## Fase 6 — Cátedras y situación de revista

El corte más grande, y el único que agrega pantalla. Razón: `SituacionRevista` dejó de colgar de `Materia` y ahora es entidad interna de `Catedra` = **materia + división**.

- **6.1** — Pantalla nueva `GestionCatedras`: `ViewModels/Cursos/Curriculas/Materias/Catedras/GestionCatedrasViewModel.cs` + `Views/Cursos/Curriculas/Materias/Catedras/GestionCatedrasView.xaml(.cs)`. Recibe `IServicioCatedra`, `IServicioDivision`, `MateriaStore`, `CatedraStore`, `CicloLectivoStore` y dos `INavigationService` (volver a materias, ir a situación de revista). Lista las cátedras de la materia con `ListarCatedrasSegunMateriaAsync(MateriaID)`, cruza con `ListarDivisionesAsync(CursoID, CicloLectivo)` para mostrar la letra de división, y ofrece `CrearCatedraAsync(MateriaID, DivisionID)` para las divisiones que todavía no tienen cátedra de esa materia.
- **6.2** — Plomería de la pantalla nueva: `Navigation/NavigationServices/Cursos/GestionCatedrasNavigationService.cs` (copiar `GestionSituacionRevistaNavigationService`), `DataTemplate` en `Shared/DataTemplate.xaml`, y los registros en `WPF_DesktopDI` (`AddTransient<GestionCatedrasViewModel>` con factory lambda + `CreateGestionCatedrasNavigationService`).
- **6.3** — `.../SituacionRevista/GestionSituacionRevistaViewModel.cs`: reescritura. `IServicioCurricula` sale, entra `IServicioCatedra`; `MateriaStore` se complementa con `CatedraStore`. Remapeo de los seis casos de uso:

  | Antes | Ahora |
  |---|---|
  | `ListarCargosDocenteSegunMateriaAsync(CursoID, CurriculaID, MateriaID)` | `ListarSituacionesRevistaAsync(CatedraID)` |
  | `RegistrarDocenteEnMateriaAsync(...)` | `DesignarDocenteAsync(CatedraID, DocenteID, Cargo, FechaInicio, FechaFin, ReemplazaA)` |
  | `EstablecerDocenteDeAulaAsync(...)` | `PonerEnFuncionesAsync(CatedraID, SituacionRevistaID)` |
  | `RelevarDocenteDeFuncionesEnMateriaAsync(...)` | `RelevarDeFuncionesAsync(CatedraID)` |
  | `RescindirCargoDocenteDeMateriaAsync(...)` | `EstablecerFinDeDesignacionAsync(CatedraID, SituacionRevistaID, FechaFin)` |
  | `EliminarCargoDocenteAsync(...)` | `FinalizarDesignacionAsync(CatedraID, SituacionRevistaID)` |

  **Dos cambios semánticos a propagar a los textos de los diálogos**: (a) "eliminar definitivamente el cargo docente" ya no existe — el dominio *finaliza* la designación, no la borra; (b) `DesignarDocenteRequest` acepta `ReemplazaA`, que es la cadena de suplencias, así que el combo de cargo, al elegir `Suplente`, debe pedir a quién reemplaza entre las situaciones de revista vigentes de la misma cátedra. Sin eso, designar un suplente falla con `SuplenciaSinReemplazoException`.
- **6.4** — Resolver el nombre del docente. `SituacionRevistaResponse` trae `DocenteID` pero no el nombre, y la pantalla lo muestra en seis diálogos. Composición en el ViewModel: **una sola** llamada a `IServicioDocente.ListarDocentesActivosAsync()` y cruce en memoria por `DocenteID`, no una llamada por fila. La alternativa limpia es enriquecer `SituacionRevistaResponse` en `Core`, fuera del alcance de esta tarea; anotado en "Violaciones" #3.
- **6.5** — El `Query` de búsqueda de docentes y `SeleccionarCommand` se conservan tal cual: usan `IServicioDocente.BuscarDocenteSegunNombreCompletoAsync`, que no cambió. Convertir los cuatro `async void` del archivo a `IAsyncRelayCommand`.
- **6.6** — `GestionCurriculasViewModel.ExecuteNavigationCommand` caso `"SituacionRevista"` pasa a navegar a `GestionCatedras`. Renombrar el caso a `"Catedras"` y actualizar el `CommandParameter` en `GestionCurriculasView.xaml`.
- **6.7** — Actualizar `Views/.../SituacionRevista/GestionSituacionRevistaView.xaml` y `SituacionRevistaView.xaml` por los renombres de 3.7 (`FechaAlta`→`FechaInicio`, etc.).

## Fase 7 — Cursantes y calificaciones

- **7.1** — `Cursos/Divisiones/GestionCursantesViewModel.cs`: es el ViewModel más desactualizado del proyecto — usa `ViewModelCommand` (el command base legado de `Shared/Commands/`) e `INotifyDataErrorInfo` manual en lugar de CommunityToolkit. Reescribirlo como `ObservableValidator` con `IAsyncRelayCommand`, recibiendo `IServicioCursante` + `IServicioMateria` en lugar de `IServicioCurso` + `IServicioCurricula`.
- **7.2** — **`UI-02`**, las dos mitades:
  - El constructor llama `ListarMateriasSegunCurriculaAsync` con `CurriculaID: Guid.Empty`, **sin `await`**, y descarta el resultado — `_materias` queda siempre vacía. Mover la carga a un `CargarCommandAsync` disparado en `Loaded`, y obtener la currícula vigente del curso con `IServicioCurricula.ListarCurriculasSegunCursoAsync` + `FechaFin is null` (el criterio que `CurriculaViewModel.EstaActiva` ya usa).
  - `BuscarCursantes` tiene la llamada real comentada y devuelve `new List<CursanteResponse>()` hardcodeado. Reemplazar por `IServicioCursante.ListarCursantesAsync(new BuscarListadoRequest(CursoID, DivisionID, Periodo))`.
- **7.3** — El `Periodo` local con regex `^(20)\d{2}$` se reemplaza por `CicloLectivoStore`. La validación de formato ya la hace `CicloLectivo.Crear` en el dominio.
- **7.4** — Conectar las calificaciones, que hoy no llegan a `Core`: `CalificacionViewModel` recibe `IServicioCursante` en lugar de `IServicioCurso`, y `ExecuteABMCommand` caso `"Calificacion"` pasa a invocar `RegistrarCalificacionAsync(CrearCalificationRequest(AlumnoID, Periodo, MateriaID, Fecha, Instancia, Nota, Observacion))`. Agregar los comandos de `QuitarCalificacionAsync`, `ModificarObservacionCalificacionAsync` y `RegistrarInasistenciaAExamenAsync`, que el dominio y `Core` ya soportan y la UI no alcanza. **El dominio no permite corregir una nota** — se quita y se vuelve a cargar; la pantalla debe reflejar eso y no ofrecer "editar nota", sólo "editar observación".
- **7.5** — Actualizar los registros en `WPF_DesktopDI` y los bindings de `GestionCursantesView.xaml`, `CalificacionView.xaml` y `CursanteView.xaml`.

## Fase 8 — Deuda `UI` restante

- **8.1** — **`UI-04`**: `Alumnos/RegistrarAlumnoViewModel.cs`. Resolver el `//TODO: Refactorizar` (línea 21), la `#region TODO:` (línea 99) y el `//TODO: Refactorizar inscripción de alumno` (línea 323). El alta en sí usa `IServicioAlumno.RegistrarAlumnoAsync`, que no cambió; lo que hay que separar es la inscripción, que ahora es un caso de uso de otro agregado (`IServicioCursante.InscribirCursanteAsync`).
- **8.2** — **`UI-05`**: `Alumnos/InscripcionAlumnoViewModel.cs`. Completar el flujo con `InscribirCursanteAsync(RegistrarCursanteRequest(CursoID, DivisionID, AlumnoID, Periodo, EsRecursante))`. El `TODO` de la línea 37 pide "alumno/división/período de origen": el período sale de `CicloLectivoStore`, la división de un combo alimentado por `IServicioDivision.ListarDivisionesAsync`, y el alumno de `LegajoStore`. El cupo lo valida `Core` (`Division.ValidarCupoDisponible` + `ContarCursantesDeDivisionAsync`), así que la UI sólo tiene que mostrar la excepción.
- **8.3** — **`UI-06`**: `Shared/DomicilioViewModel.cs:51`, `//TODO:Verificar string`. Revisar la validación señalada y cerrarla, o documentar por qué queda.
- **8.4** — `Models/`: **las tres clases están huérfanas**, nadie importa `WPF_Desktop.Models`. `PersonaDTO.cs` declara una clase llamada `AlumnoDTO` (nombre de archivo y clase no coinciden) que sólo usa `DomicilioDTO`, y `Usuario.cs` no tiene consumidores. Duplican `PersonaConDetallesResponse`, `DomicilioResponse` y `UsuarioResponse` de `Core`. Eliminar la carpeta.
- **8.5** — **`D.3`**: `Docentes/GestionDocentesViewModel.cs:178` → `await _servicioDocentes.QuitarDocenteAsync(...)`. Es el único error real fuera del clúster de cursos. El método contenedor tiene que pasar a `Task` para poder esperarlo (hoy el `Task` se descarta y las excepciones de la baja se pierden, que es exactamente el defecto `H-018`).

## Fase 9 — Composition root, DI, navegación, `DataTemplate`

Esta fase tiene el defecto más grave de la capa y a la vez el que ningún compilador señala. **Sin 9.0 la verificación funcional no es concluyente**: la app puede compilar, arrancar y mostrar una ventana vacía o no reaccionar a la navegación, y el síntoma se confundiría con un error en los ViewModels recién reescritos.

- **9.0** — `App.xaml.cs` + `App.xaml`, los defectos `E.1`…`E.4`. **Un solo contenedor y un solo punto de arranque**:
  - Elegir entre `OnStartup` y `Application_Startup`, no los dos. Lo idiomático con `IHost` es `OnStartup`: quitar `Startup="Application_Startup"` de `App.xaml:5` y mover el cuerpo de `Application_Startup` ahí.
  - Eliminar `services.BuildServiceProvider()` y el campo `_serviceProvider`. Todo se resuelve de `AppHost.Services`. Es la causa de `E.2` y `E.3`: hoy la navegación inicial escribe en el `MainWindowNavigationStore` del contenedor que `MainWindow` no observa.
  - Reemplazar el `await AppHost.StopAsync()` de `OnStartup` por `await AppHost.StartAsync()`, y conservar el `StopAsync()` de `OnExit`. Envolver los cuerpos de ambos en `try/catch` mientras sigan siendo `async void`, o pasar a `OnStartup` sincrónico con `AppHost.Start()`.
  - Los servicios de `Core` están registrados `Scoped` en `CoreDI` y hoy se resuelven del proveedor raíz, así que cada `GetRequiredService` devuelve una instancia que vive lo que vive la aplicación — incluido el `DbContext` detrás de `IUnitOfWork`. Con un único contenedor esto se vuelve visible y hay que decidirlo: un `IServiceScope` por operación de ViewModel es lo correcto, pero **es un cambio de diseño que excede esta tarea**. Anotar en `docs/todos.md` como ID nuevo y verificar al menos que no haya dos `DbContext` concurrentes sobre el mismo agregado.
- **9.1** — `Shared/WPF_DesktopDI.cs`: agregar los `using` de los servicios nuevos (`Core.ServicioDivisiones`, `Core.ServicioMaterias`, `Core.ServicioCatedras`, `Core.ServicioCursantes`) y actualizar las factory lambdas de los cinco ViewModels reprogramados, más los registros nuevos de las fases 2 y 6.2.
- **9.2** — Corregir el riesgo latente de DI: `INavigationService` está registrado **una sola vez** (línea 175), como `MainNavigationService<MainViewModel>`. Los ViewModels registrados **sin** factory lambda (`BuscarViewModel`, `GestionPuestosViewModel`, `GestionLicenciasViewModel`, `RegistrarUsuarioViewModel`, `RegistrarAlumnoViewModel`, `InscripcionAlumnoViewModel`) reciben por inyección ese servicio, no el que su pantalla necesita. Darles factory lambda explícita como el resto, o verificar uno por uno que no dependan de `INavigationService`.
- **9.3** — `Shared/DataTemplate.xaml`: agregar el template de `GestionCatedrasViewModel` (y de `CatedraViewModel` si la vista lo usa como item). Los `DataTemplate` existentes cubren todas las Views salvo tres huecos preexistentes: `CurriculaViewModel` (no existe `CurriculaView`), `HorarioViewModel` (template comentado, `HorarioView` no existe) y `UsuarioViewModel` (`UsuarioView` existe pero se instancia directo en `MainWindow.xaml:128`). Los dos primeros quedan anotados; el tercero funciona y no se toca. Ningún `DataTemplate` apunta hoy a un tipo inexistente — verificar que siga así después de las fases 3 y 6.
- **9.4** — `Navigation/NavigationServices/Docentes/GestionLicenciasNavigationService.cs` **no declara `namespace`**: la clase queda en el namespace global, aunque `WPF_DesktopDI` importa `...NavigationServices.Docentes`. Compila por accidente (el global es visible desde todas partes). Agregar el namespace que le corresponde.
- **9.5** — Limpieza acotada, todo preexistente y verificado sin consumidores:
  - `CreateBuscarModalNavigationService` (`WPF_DesktopDI.cs:183`) nunca se llama.
  - `Navigation/NavigationServices/Modal/BuscarNavigationService.cs` no se usa, y además está tipado contra `MainWindowNavigationStore` en lugar del store modal — si alguien lo activara, abriría el modal en la ventana principal.
  - `Navigation/NavigationService.cs` y `Navigation/NavigationWithParameterService.cs` no participan del contenedor.
  - `IDialogService`/`DialogService` están registrados y ningún ViewModel los consume. **No eliminar**: es la pieza que corresponde usar para sacar los `MessageBox.Show` de los ViewModels (ver "Violaciones" #12). Anotar, no borrar.
  - `WPF_Desktop.csproj` tiene un `PackageReference` a `MediatR` que la capa no debería necesitar (los handlers viven en `Core`) — verificar con `rg -n "MediatR" WPF_Desktop/` antes de quitarlo.
- **9.6** — Verificar que `LoginUsuarioViewModel` y `Usuarios/UsuarioViewModel.cs` compilen contra `IServicioAutenticacion` sin `Logout` (decisión #4) y sigan coherentes con `UsuarioResponse(UsuarioID, DocenteID, Usuario, NombreCompleto, Puesto, Roles)`. `UsuarioViewModel` se construye con `new` dentro de `MainViewModel.cs:72` y no está en el contenedor: dejarlo así (decisión #4), anotado.
- **9.7** — Actualizar la documentación. Las violaciones y los IDs nuevos **ya están registrados** (`docs/todos.md`, `UI-10`…`UI-16` y violaciones 11-22, anotados 2026-10-02 junto con este plan), así que acá sólo queda cambiar **estados**:
  - `docs/todos.md` §WPF_Desktop: `UI-01`…`UI-05`, `UI-08`, `UI-09`, `UI-12`, `UI-13`, `UI-15`, `UI-16` pasan a `[RESUELTO]` con su commit; `UI-10`/`UI-11` también si se completa 9.0. Quedan vigentes por decisión: `UI-14` (diálogos, es UI/UX) y lo que el plan deja afuera.
  - `docs/todos.md` §Violaciones: marcar las que el plan cierra (items 15, 16, 20) y actualizar las mitigadas-no-resueltas (13, 14).
  - `docs/plan/core.md`: corregir la predicción de los 41 sitios de llamada contra lo que el compilador terminó imprimiendo.
  - `docs/plan/handoff-errores.md` §4 (que decía "indeterminado, no cero"), `docs/revision-arquitectura.md` y `docs/roadmap.md` Fase 2.1.
  - Agregar en este documento la nota de cabecera de estado, como la tienen `core.md`, `tasks.md` y `handoff-errores.md`: qué se ejecutó tal cual, qué cambió de ruta y qué quedó abierto.

---

## Verificación

```bash
# Criterio real de esta tarea
dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental   # 0 errores
dotnet build EDUSIS.sln --no-incremental                        # 0 errores (ver caveat de TEST-07)

# No debe haber regresiones en las otras capas
./tests/run-tests.sh --rapidas

# Guardas de arquitectura
rg -n "using Infrastructure|EntityFrameworkCore|IQueryable|\.Include\(" WPF_Desktop/ --glob '!obj' --glob '!bin'
rg -n "clr-namespace:Domain\.(Curriculas\.Materias|Cursos\.)" WPF_Desktop/
rg -n "async void" WPF_Desktop/ViewModels/
rg -n "BuildServiceProvider" WPF_Desktop/
```

- La primera guarda debe dar como única coincidencia `App.xaml.cs:2`, que es el composition root y el único lugar legítimo con `using Infrastructure`.
- La segunda debe dar cero: ningún XAML debe seguir apuntando a los namespaces que el refactor vació.
- La tercera no tiene por qué llegar a cero en esta tanda, pero **no debe crecer**: cada `async void` que quede tiene que ser un handler de evento de WPF, no un caso de uso.
- La cuarta debe dar **cero** después de 9.0: un único contenedor, construido por el `IHost`.
- **Línea base de advertencias**: medirla en la Fase 0 y no dejarla crecer por código nuevo. `WPF_Desktop` nunca llegó a compilar C# desde el refactor, así que su conteo real de `CS8618`/`CS8602` (nulabilidad, con `<Nullable>enable</Nullable>` en el `.csproj`) es **desconocido** hasta la Fase 1 — puede ser alto, y no sería un defecto de este trabajo.

**Verificación funcional en Windows** — lo único que cierra la tarea de verdad: `dotnet run --project WPF_Desktop/WPF_Desktop.csproj` y recorrer los flujos tocados. Requiere la base de datos, que **todavía no está instalada** (`H-023`: la migración sigue siendo sólo `InitCreate` y no refleja el mapeo de `ff2de55`). Secuencia:

0. **La app arranca, muestra `MainWindow` y la navegación inicial pinta la pantalla de inicio.** Es la verificación de 9.0: si la ventana sale vacía o los botones del menú no cambian la vista, el problema es el composition root, no los ViewModels reescritos. Comprobar este paso **antes** de cualquier otro, y antes de culpar a las fases 3-8.
1. Con `WPF_Desktop` compilando, regenerar la migración: `dotnet ef migrations add <Nombre> --project Infrastructure --startup-project WPF_Desktop` y `dotnet ef database update`. La cadena está hardcodeada en `Infrastructure/InfrastructureDI.cs` (`localhost`/`EdusisDB`, Integrated Security).
2. Curso → Divisiones: agregar, asignar y quitar preceptor, eliminar.
3. Curso → Diseño curricular: crear currícula, alta/edición/baja de materia. Verifica `UI-01`: la carga horaria que se muestra en el diálogo es la que se persiste, y editarla tiene efecto.
4. Materia → Cátedras: crear cátedra para una división, designar titular, poner en funciones, relevar, designar suplente con `ReemplazaA`.
5. División → Cursantes: inscribir alumno, listar (verifica `UI-02`), cargar y quitar calificación.

Si la base no se puede instalar en esta tanda, el criterio de aceptación se limita a los builds y las guardas de `rg`, y la verificación funcional queda anotada como pendiente — **sin declarar la tarea cerrada**.

---

## Violaciones DDD / Clean Architecture detectadas

`CLAUDE.md` exige notificarlas aunque queden fuera de alcance. Va marcado lo que este plan corrige.

> **Las 13 están portadas al registro vivo**: [`../todos.md`](../todos.md) §"Violaciones DDD detectadas", items 11 a 22 (más el 10, que ya estaba y se reconfirmó), y los defectos concretos y localizables también como filas `UI-10`…`UI-16` de la tabla `## WPF_Desktop`. Esta sección queda como el detalle de por qué cada una importa **para esta tarea**; el estado autoritativo de cada violación vive en `todos.md`, no acá. El motivo de portarlas antes de implementar y no después: los documentos de `docs/plan/` se marcan "documento histórico" al ejecutarse, y de las seis violaciones que `core.md` dejó vigentes, dos nunca llegaron al registro.

1. **`WPF_Desktop` referencia `Domain.csproj` directamente** y bindea 10 enums de dominio en 19 XAML vía `ObjectDataProvider`. Es una dependencia hacia adentro, así que no invierte el flujo de Clean Architecture, pero ata la presentación a tipos que el dominio puede renombrar — es exactamente lo que produjo `UI-09` y las otras seis referencias rotas. **No se corrige** (decisión #1); el arreglo honesto es que `Core` exponga los catálogos y que la UI no vea `Domain`.
2. **`CursoResponse` expone `Domain.Cursos.Grado` y `Domain.Shared.NivelEducativo` sin mapear a `string`**, a diferencia de los otros ~20 DTOs de `Core`, que ya los traen como texto. Es la única razón por la que `CursoViewModel` necesita el enum. Arreglo de una línea en `Core`, fuera de alcance. Igual `RegistroAsistenciaResponse` y `ContarFaltasRequest`, que exponen `TipoAsistencia`.
3. **`SituacionRevistaResponse` no trae el nombre del docente**, así que la UI tiene que recomponerlo con una segunda llamada a `IServicioDocente`: la presentación termina haciendo el *join* que corresponde al modelo de lectura de `Core`. Mitigado en 6.4, no resuelto.
4. **Los stores guardan ViewModels, no identificadores.** `MateriaStore.Materia` es un `MateriaViewModel`, `CursoStore.Curso` un `CursoViewModel`. `CLAUDE.md` dice que los stores "pasan el ID de la entidad seleccionada entre ViewModels". Acopla pantallas entre sí por su tipo de presentación y hace que el estado compartido arrastre validación y comandos. **Se mitiga** escribiendo los dos stores nuevos por valor/ID (Fase 2); los tres existentes no se migran.
5. **`CalificacionViewModel` expone `ObservableCollection<MateriaResponse>`**: un DTO de `Core` bindeado directo en la vista, sin ViewModel intermedio. **Se corrige** (3.6).
6. **`INavigationService` registrado una sola vez** con una implementación concreta, mientras seis ViewModels lo reciben por inyección automática. Cualquiera de ellos que navegue, navega a `MainViewModel`. **Se corrige** (9.2).
7. **Dos parámetros `INavigationService` distinguidos sólo por su orden** en `GestionCurriculasViewModel` y `GestionDivisionesViewModel`: el contenedor los resuelve bien únicamente porque la factory lambda los pasa en ese orden. Invertirlos compila y rompe la navegación en silencio. Un `INavigationService<TDestino>` tipado, o un enum de destino, lo haría imposible. Anotado, no corregido (`GestionCatedrasViewModel` hereda el problema, porque también necesita dos).
8. **`Thread.CurrentPrincipal` como sesión** (violación #12 de `core.md`): es estado por hilo, y con `async/await` las continuaciones pueden retomar en otro hilo del pool. `UsuarioViewModel` y `App.xaml.cs` la leen y pueden encontrarla `null`. **No se corrige** (decisión #4).
9. **`Shared/Messenger.cs` / `IMessengerTEST.cs`** son una implementación propia de mensajería en desuso, coexistiendo con `WeakReferenceMessenger` de CommunityToolkit, que es la que se registra. Código muerto preexistente; por convención del repo no se borra.
10. **`Core/Shared/DTOs/Personas/Response/` declara dos namespaces** para una misma carpeta: `...Personas.Response` (singular, en `PersonaResponse` y `PersonaConDetallesResponse`) y `...Personas.Responses` (plural, en los otros tres). Los ViewModels de alumnos y docentes tienen que importar ambos. Es la violación #9 de `core.md`, sigue vigente, y es de `Core` — fuera de alcance.
11. **Dos contenedores de DI en el composition root** (`E.2`/`E.3`) y dos puntos de arranque (`E.1`). Es la violación más costosa de la capa: rompe la premisa de que un `Singleton` es único, que es justamente de lo que depende todo el mecanismo de navegación del proyecto (`*NavigationStore` + `event Action`). **Se corrige** (9.0).
12. **Los ViewModels llaman a `MessageBox.Show` directamente** — unas 40 veces entre los cinco `Gestion*` del clúster de cursos. Acopla la lógica de presentación a `System.Windows` y hace los ViewModels no testeables sin un hilo de UI. `IDialogService` existe y está registrado **para exactamente esto**, y nadie lo usa. No se corrige en esta tanda (sería una reescritura transversal de los diálogos, y toca UI/UX), pero cada ViewModel que se reescriba conviene dejarlo preparado: el mensaje armado en una variable y la llamada al `MessageBox` en un solo lugar del método.
13. **`PlanillaAsistencia` queda sin ninguna pantalla.** `ServicioAsistencias` existe completo y registrado en `CoreDI`, con diez casos de uso, y ninguno es alcanzable. Igual `IServicioCatedra.AgregarHorarioAsync`/`QuitarHorarioAsync`/`ListarHorariosSegunCatedraAsync` (hay `HorarioViewModel` pero no `HorarioView` ni `DataTemplate`) e `IServicioUsuario.AsignarRolAsync`/`QuitarRolAsync`. **Fuera de alcance** por la decisión #2; es la brecha que queda entre lo modelado y lo operativo.

## Fuera de alcance (no hacer)

- Cualquier edición en `Domain/`, `Core/`, `Infrastructure/` o `tests/`. Si una fase parece necesitarla, se detiene y se consulta.
- `TEST-07`: los 6 `CS1061` de `tests/EDUSIS.EndToEndTests` por el adelgazamiento de `ServicioCurso`. Son de la suite, no de la UI — pero **`dotnet build EDUSIS.sln` no va a quedar limpio hasta que se arreglen**, así que el criterio de "solución verde" depende de una tarea hermana.
- Cambios de UI/UX: estilos, layout, `Theme/EDUSIS_*.xaml`, y los textos que no cambien por semántica de dominio.
- Reactivar el login o reemplazar `Thread.CurrentPrincipal`.
- Pantallas nuevas más allá de `GestionCatedras`, que es el mínimo que el agregado `Catedra` obliga.
- Limpiar el código comentado y los `TODO` preexistentes que este plan no nombra (`CLAUDE.md`, FR-013).
