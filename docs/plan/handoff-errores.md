# Handoff — errores de compilación tras el refactor de límites de agregado (`002-fix-dominio`)

> **Estado al 2026-09-23 — documento histórico.** Este plan se ejecutó y el dominio ya está commiteado en `002-fix-dominio` (commits `db14e72`…`fbde3d0`). Algunas decisiones cambiaron después de escribirlo: `SituacionRevista` no es agregado propio sino entidad interna de `Catedra`; `Cursante` es agregado raíz (no hija de `Division`) y es dueño de `Calificacion` (entidad hija, no VO); la asistencia diaria vive en el agregado nuevo `PlanillaAsistencia`; `Division` no guarda cursantes; `NivelEducativo` está en `Domain.Shared`. Se conserva como registro; el estado vigente está en `CLAUDE.md`, `docs/plan-implementacion.md` y `docs/revision-arquitectura.md`.
>
> **Estado al 2026-09-25**: la sección 2 (`Infrastructure`, "enmascarados") quedó resuelta por el commit `ff2de55` (`refactor(infraestructura): alinear las configuraciones de EF Core con los agregados`) — `EdusisDBContext` y las `EntityConfiguration` ya no referencian `Curso.Divisiones`, `Curricula.Materias` ni `Materia.Docentes`; hay tabla propia para `Division`, `Catedra`, `Cursante` y `PlanillaAsistencia`. Sigue sin resolverse `Infrastructure/Repository/CurriculaRepository.cs` (línea 39 de este documento): todavía hace `.Include(x => x.Materias)`, ver `docs/plan-implementacion.md` §4.2. La sección 1 (`Core`) sigue vigente sin cambios.
>
> **Estado al 2026-09-26**: el "orden de ataque sugerido" (§7) se ejecutó completo — commits `b377fa5` (contratos de `Domain`), `3cf17b7` (repositorios existentes), `527b1ba` (repositorios nuevos + `UnitOfWork` con los once repos), `d7f6b44` (MediatR escanea `Core` + despacho de eventos en cascada), `58705d8` (fakes) y `0f39316` (tests de integración). El resto de este documento (secciones 1 a 6) describe el punto de partida y se conserva como registro histórico; el estado vigente de `Infrastructure` está resuelto (`dotnet build` limpio sobre el arnés `InfraCheck.csproj` de `C:\Users\P0lybius\.claude\plans\vamos-a-diagramar-un-drifting-mitten.md`). La sección 1 (`Core`, 4 errores confirmados por `dotnet build Core/Core.csproj`, no 20: el conteo bajó porque `CursoResponse.cs`/`NivelEducativo` y `MateriaEliminadaEventHandler.cs` concentran hoy los únicos errores de "etapa de declaración" — el resto de las llamadas rotas listadas abajo siguen ahí pero enmascaradas) sigue sin resolver: es la tarea siguiente, con tabla de handoff en el plan citado arriba (§"Handoff: lo que la tarea de `Core` deberá adaptar").
>
> **Estado al 2026-09-28 (post bloque `Core`, commits `b3ac87b`…`76bc1d8`)**: la sección 1 (`Core`, "20, confirmados por el compilador") quedó **cerrada**: el inventario completo se resolvió y `dotnet build Core/Core.csproj` da 0 errores, detallado en `docs/plan/core.md`. El punto 4 de la sección 7 (`ServicioCurso`/`ServicioCurricula` en `Core` — "Pendiente: es la tarea siguiente") también quedó **cerrado**, por la misma vía. La sección 4 (`WPF_Desktop` — "indeterminado, no cero") **acertó y sigue indeterminada**: hoy el proyecto falla con un solo error propio (`MC3066`, ID **UI-09**), pero por correr en la etapa de markup compile de XAML aborta antes de que el compilador de C# llegue a los ViewModels, así que los efectos reales de la reparación de `Core` sobre `WPF_Desktop` siguen sin poder confirmarse — la cautela de esa sección estuvo bien puesta. Las secciones 5 y 6 no cambian.
>
> **Estado al 2026-10-03 (post adaptación de `WPF_Desktop`, commits `dd89f93`…`e8d5043`)**: la sección 4 (`WPF_Desktop` — "indeterminado, no cero") **acertó**, y ahora tiene número: el conteo de errores subió dos veces antes de bajar y la secuencia medida fue `1` (markup) → `18` (declaración) → `40` (cuerpos de método) → `32` → `25` → `20` → `1` → **`0`**. `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` y `dotnet build EDUSIS.sln --no-incremental` dan **0 errores**; la sección 3 (`tests/`) también quedó cerrada con `TEST-07` (`dd89f93`). Ver la sección 4 por el detalle. **Lo que NO cierra este documento**: la verificación funcional de la app en Windows sigue **pendiente** (requiere la base de datos, que no está instalada: H-023, la migración sigue siendo sólo `InitCreate`), así que "compila" no equivale a "funciona". Seguimiento en [`wpf-desktop-tasks.md`](wpf-desktop-tasks.md) §Bitácora y en `docs/todos.md` (UI-01…UI-37).

Este documento cierra T5.3. `Domain/Domain.csproj` compila limpio (`0 Errores`); `EDUSIS.sln` no. Acá está el inventario completo de por qué, para que la siguiente sesión no tenga que redescubrirlo. La distinción importante en todo el documento es entre errores **confirmados por el compilador** (MSBuild los imprimió) y errores **enmascarados** (inferidos por inspección estática porque MSBuild nunca llegó a compilar ese proyecto: su dependencia `Core` falla primero y corta la cadena, así que `Core.dll` no existe y no hay forma de forzar la compilación aislada de lo que depende de él).

## 1. Errores de `Core` — 20, confirmados por el compilador

Todos `CS1061` (miembro inexistente), ninguno de sintaxis propia. Verificados con `dotnet build EDUSIS.sln --no-incremental` y, por separado, compilando `Infrastructure`, `WPF_Desktop` y los cinco proyectos de `tests` de forma individual: los cinco reproducen exactamente estos mismos 20 errores y ninguno propio, porque ninguno llega a compilar su propio código — se cortan en la referencia a `Core`.

| Archivo:línea | Símbolo faltante | Recorte que lo causó |
|---|---|---|
| `Core/ServicioCursos/ServicioCurso.cs:53` | `Curso.Divisiones` | T2.1 |
| `Core/ServicioCursos/ServicioCurso.cs:54` | `Curso.CantidadAlumnos` | T2.1 |
| `Core/ServicioCursos/ServicioCurso.cs:139` | `ICursoRepository.DivisionesDelCurso` | T2.4 |
| `Core/ServicioCursos/ServicioCurso.cs:207` | `Curso.AsignarPreceptor` | T2.1 |
| `Core/ServicioCursos/ServicioCurso.cs:226` | `Curso.QuitarPreceptor` | T2.1 |
| `Core/ServicioCursos/ServicioCurso.cs:244` | `ICursoRepository.CursoConDivisiones` | T2.4 |
| `Core/ServicioCursos/ServicioCurso.cs:263` | `ICursoRepository.CursoConDivisiones` | T2.4 |
| `Core/ServicioCursos/ServicioCurso.cs:286` | `Curso.AgregarAlumnoEnDivision` | T2.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:80,163,203,322` | `Curricula.Materias` (×4) | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:257` | `Curricula.AgregarMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:277` | `Curricula.ActualizarMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:299` | `Curricula.QuitarMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:362` | `Curricula.AsignarDocenteEnMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:383` | `Curricula.RelevarDocenteDeMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:403` | `Curricula.RescindirDocenteDeMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:422` | `Curricula.EstablecerDocenteEnFuncionesEnMateria` | T4.1 |
| `Core/ServicioCurriculas/ServicioCurricula.cs:442` | `Curricula.EliminarCargoDocenteDeMateria` | T4.1 |

## 2. Errores de `Infrastructure` — enmascarados, probados por inspección estática

**Inferidos, no observados por el compilador.** Leí cada archivo fuente para confirmar que la línea es código vivo (no comentado) antes de listarla acá. Donde encontré una discrepancia con lo que se me había reportado inicialmente, la corrijo explícitamente y marco la fuente (inspección propia).

- **`Infrastructure/UnitOfWork.cs:15`** — `public class UnitOfWork : IUnitOfWork` no es `partial`. Su `#region Repositories` (líneas 22–27) declara seis propiedades (`Alumnos`, `Docentes`, `Licencias`, `Cursos`, `Curriculas`, `Usuarios`) — ninguna de las cuatro que T5.1 agregó a la interfaz. **`CS0535` ×4**: `Divisiones`, `Cursantes`, `Materias`, `SituacionesDeRevista`.
- **`Infrastructure/Repository/CursoRepository.cs`** — sólo tiene `using Domain.Cursos;`.
  - Líneas 17, 23, 30, 32 — `CS1061` ×4 sobre `.Divisiones` / `x.Divisiones` (`Curso` ya no tiene esa navegación).
  - Línea 27 — `public IEnumerable<Division> DivisionesDelCurso(...)`: **`CS0246`**, no `CS1061`. `Division` se mudó a `Domain.Divisiones` y el archivo no importa ese namespace. **Ojo acá**: no alcanza con reescribir el método contra `IDivisionRepository`; hace falta agregar `using Domain.Divisiones;` (o quitar el método si la responsabilidad se muda del todo al `DivisionRepository` nuevo).
- **`Infrastructure/Repository/CurriculaRepository.cs:17,25`** — `CS1061` ×2 sobre `.Include(x => x.Materias)` (`Curricula` ya no expone esa colección).
- **`Infrastructure/EntityConfigurations/CursosConfiguration.cs`**:
  - Líneas 55–56 — `builder.Ignore(x => x.CantidadDivisiones)` / `.Ignore(x => x.CantidadAlumnos)` → `CS1061` ×2.
  - Línea 66 — `builder.OwnsMany(x => x.Divisiones, divisionBuilder => {...})` → `CS1061`. Al no poder inferirse el tipo de `divisionBuilder`, el compilador va a arrastrar errores secundarios de tipo indeterminado en el cuerpo del lambda — en particular líneas 87 y 93 (`divisionBuilder.Property(x => x.Preceptor)`, `.HasForeignKey(x => x.Preceptor)`). El conteo exacto de esa cascada depende de cómo Roslyn recupere el árbol tras el primer error; no lo doy por cerrado en un número.
  - Línea 208 — `.Navigation(nameof(Curso.Divisiones))` → error sobre `Divisiones` como miembro de `Curso`.
  - **Excluidas por estar comentadas** (no se compilan, no son errores): líneas 28–32, 46–51 y 58–61 (los tres bloques `/* ... */` que tocan `Curricula`/`Divisiones`/`FindNavigation`). También todo el bloque de `Cursantes`/`Calificacion` (líneas 97–206) está comentado.
- **`Infrastructure/EntityConfigurations/CurriculasConfiguration.cs`** — **corrección sobre lo reportado inicialmente**: verifiqué los delimitadores `/* */` del archivo (`grep -n '/\*\|\*/'`) y sólo hay **un** sitio vivo, no tres.
  - Línea 56 — `builder.Navigation(nameof(Curricula.Materias))` → error real, vivo.
  - Línea 196 y línea 276 **NO son errores**: ambas caen dentro del bloque comentado de las líneas 193–279 (el método completo `ConfigureTableMaterias`, que además se invoca comentado en la línea 13: `//ConfigureTableMaterias(builder);`). Ese método nunca se compila. No hace falta tocarlo para que el proyecto compile, aunque sí conviene revisarlo cuando se diseñe el mapeo real de `Materia` como agregado propio, porque documenta la intención original del mapeo `OwnsMany`.
- **`Infrastructure/EntityConfigurations/MateriasConfiguration.cs:48`** — `builder.HasOne<Curricula>().WithMany(x => x.Materias)` → `CS1061`, vivo (el resto del archivo con referencias a `Docentes`/`Horarios` está comentado, líneas 68–127 y 185–232).
- **`Infrastructure/EntityConfigurations/DivisionesConfiguration.cs`** — archivo entero comentado (línea 10 abre `/*public class DivisionesConfiguration...`, línea 161 cierra `*/`). Cero errores porque no se compila nada. Queda como referencia histórica del mapeo que hay que rehacer para el `DivisionRepository` nuevo (tabla propia, ya no `OwnsMany` de `Curso`).

## 3. Errores de `tests/` — enmascarados, misma salvedad que la sección 2

Ubicaciones señaladas para la sesión siguiente. **A diferencia de la sección 2, acá no verifiqué cada línea contra los delimitadores de comentario** — un grep rápido de sitios candidatos en `CurriculaTests.cs` y `MateriaTests.cs` da más coincidencias que las que se me habían apuntado inicialmente (que puede deberse a asserts múltiples por método de prueba o a que mi patrón de búsqueda es más amplio que los sitios que realmente fallan). Tratar los conteos como orientativos, no como lista cerrada:

- `EDUSIS.TestSupport/Builders/CursoBuilder.cs:46` — `curso.AgregarDivision()`, confirmado vivo.
- `EDUSIS.TestSupport/Builders/CurriculaBuilder.cs:52` — `curricula.AgregarMateria(...)`, confirmado vivo.
- `EDUSIS.TestSupport/Fakes/CursoRepositorioFake.cs` — **corrección sobre lo reportado inicialmente**: las líneas 8 (`CursoConDivisiones`) y 12 (`CambiarAlumnoDeCurso`) ya no son errores por sí mismas — al vaciarse `ICursoRepository` (T2.4), un método público que ya no está en la interfaz sigue siendo legal en C#, simplemente deja de ser una implementación de interfaz. Los errores reales de este archivo están en **línea 15** (`IEnumerable<Division> DivisionesDelCurso(...)` → `CS0246`, mismo problema de `using` que en `CursoRepository.cs`) y **línea 16** (`.Divisiones` sobre `Curso` → `CS1061`).
- `Domain.UnitTests/Curriculas/CurriculaTests.cs` — múltiples sitios sobre `Curricula.Materias`/`AgregarMateria`/`ActualizarMateria`/`QuitarMateria`/las siete operaciones de docente y `HorasSemanales`/`TotalEspacios`/`TotalHorasSemanales`. Grep amplio: ~27 coincidencias candidatas.
- `Domain.UnitTests/Curriculas/MateriaTests.cs` — múltiples sitios sobre `Materia.Docentes`/`Docente`/`DocenteID`/las operaciones de cargo docente y calificaciones. Grep amplio: ~24 coincidencias candidatas.
- `Infrastructure.IntegrationTests/Repositorios/CursoRepositorioTests.cs` — sitios reportados en líneas 42, 45, 56, 71, 74 (no re-verificados línea por línea en esta sesión).
- `EDUSIS.EndToEndTests/Flujos/RegistroDeCursoConDivisionesTests.cs` — sitios reportados en líneas 37, 54 (no re-verificados línea por línea en esta sesión).

## 4. `WPF_Desktop` — indeterminado, no cero

El barrido no encontró accesos directos de `WPF_Desktop` a miembros eliminados de `Domain` (no tiene lógica de dominio propia, según `CLAUDE.md`). Pero sus ViewModels consumen los servicios de `Core` que van a cambiar de forma cuando se repare `ServicioCurso`/`ServicioCurricula`, así que su conjunto de errores real depende de esa reparación y no se puede afirmar que sea cero hasta que `Core` compile.

> **Cerrado 2026-10-03 — el número real no era cero y tampoco los ~41 sitios que predecía [`core.md`](core.md).** `WPF_Desktop` falló en **tres** etapas encadenadas y cada una enmascaraba a la siguiente: 1 `MC3066` de markup compile de XAML (UI-09, `GestionSituacionRevistaView.xaml:25`), 18 errores de etapa de declaración (1 `CS0234` + 17 `CS0246`) y, al limpiarlos, **40** errores de cuerpos de método (31 `CS1061`, 6 `CS0246`, 2 `CS0029`, 1 `CS7036`); de ahí bajó a 32 → 25 → 20 → 1 → **0** al cerrar las olas W3 a W6 de [`wpf-desktop-tasks.md`](wpf-desktop-tasks.md). Dos hechos corrigen lo que decía el párrafo de arriba: (1) **sí** había accesos a tipos de `Domain` movidos —en XAML (`clr-namespace` de cinco vistas) y en `using` de los converters—, es decir, la afirmación "no tiene lógica de dominio propia" valía para la *lógica*, no para las *referencias*; (2) seis de los errores de cuerpos de método eran `CS0246` por requests que `Core` **eliminó** al renombrar los casos de uso, así que ningún `using` los resolvía. El detalle por archivo y la comparación contra la predicción están en la nota de cabecera de `core.md`.

## 5. Correcciones a `docs/plan/validaciones.md`

- **Línea 971**: predice `CS0535` sobre `CursoRepository` "por los cuatro métodos que T2.4 quitó de `ICursoRepository`". Es incorrecto — quitar miembros de una interfaz nunca rompe a quien la implementa en C#; los métodos públicos que sobran siguen siendo legales (mismo razonamiento que la corrección de la sección 3 sobre `CursoRepositorioFake.cs`). Los diagnósticos reales en `CursoRepository.cs` son los `CS1061`/`CS0246` de la sección 2.
- **Línea base de advertencias**: el documento asume 2–3 advertencias. La real es ~51 (confirmada en H2–H5), preexistente — el `CS0234` de H0 abortaba la compilación en etapa de declaración y enmascaraba todo el análisis de nulabilidad (`CS8618`/`CS8602`/`CS8629`) igual que enmascaraba los 6 `CS0246` originales.

## 6. Caveat de entorno

`EDUSIS.sln` referencia los proyectos de test como `Tests\...` (mayúscula). Es un cambio preexistente, ajeno a este refactor. En esta máquina Windows el filesystem es case-insensitive, así que la solución carga y compila con normalidad. En Linux o en un CI case-sensitive la solución no cargaría en absoluto, y este listado saldría vacío — por la razón equivocada, no porque el refactor esté completo.

## 7. Orden de ataque sugerido para la sesión siguiente

> **Cumplido al 2026-09-26** salvo el punto 4 (`Core`), ver nota de cabecera. El agregado nuevo que este documento llama `SituacionRevista` como raíz propia se resolvió después con un diseño distinto (`SituacionRevista` como entidad interna de `Catedra`, no repositorio propio) — los puntos 1 y 3 se cerraron contra ese diseño final, no contra el literal de acá.

1. ~~Las cuatro propiedades de `UnitOfWork` (`Divisiones`, `Cursantes`, `Materias`, `SituacionesDeRevista`) + implementar los cuatro repositorios concretos nuevos (`DivisionRepository`, `CursanteRepository`, `MateriaRepository`, `SituacionRevistaRepository`) en `Infrastructure/Repository/`.~~ Hecho en `527b1ba`: cinco repositorios (`DivisionRepository`, `CursanteRepository`, `MateriaRepository`, `CatedraRepository`, `PlanillaAsistenciaRepository`) y `UnitOfWork` con los once repos de `IUnitOfWork`.
2. ~~`CursoRepository.cs` y `CurriculaRepository.cs` — recortar los métodos que ya no existen en la interfaz y, si se conserva alguno que devuelva `Division`, agregar `using Domain.Divisiones;` (no alcanza con reescribir la lógica).~~ Hecho en `3cf17b7`.
3. ~~Las `EntityConfigurations` (`CursosConfiguration`, `CurriculasConfiguration`, `MateriasConfiguration`) — quitar los `Ignore`/`OwnsMany`/`Navigation` sobre las colecciones eliminadas, y diseñar el mapeo nuevo de `Division` como tabla propia (`DivisionesConfiguration.cs`, hoy comentado entero, sirve de punto de partida) y de `Materia`/`SituacionRevista` como agregados propios.~~ Hecho antes, en `ff2de55` (2026-09-25).
4. `ServicioCurso`/`ServicioCurricula` en `Core` — reescribir contra los repositorios nuevos. **Pendiente**: es la tarea siguiente (ver tabla de handoff en `C:\Users\P0lybius\.claude\plans\vamos-a-diagramar-un-drifting-mitten.md`).
5. ~~Los builders y fakes de `EDUSIS.TestSupport`, y luego los tests de `Domain.UnitTests`/`Infrastructure.IntegrationTests`/`EDUSIS.EndToEndTests` que dependen de la API vieja.~~ Hecho en `58705d8` (fakes) y `0f39316` (tests de integración); `Domain.UnitTests` ya estaba al día desde la Fase 0 del refactor.
