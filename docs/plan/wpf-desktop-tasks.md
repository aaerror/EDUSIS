# Tareas — Adaptar `WPF_Desktop` al refactor de agregados

Disección ejecutable de [`wpf-desktop.md`](wpf-desktop.md). Alcance idéntico al del plan: **sólo `WPF_Desktop/`**, más dos excepciones acordadas (un proyecto de pruebas nuevo bajo `tests/` y la tarea `TEST-07`, ver "Decisiones de disección").

Este documento es la **lista de trabajo para agentes implementadores**. Cada tarea declara los archivos que posee, la acción y un criterio de aceptación verificable sin ayuda humana. El plan hermano sigue siendo la fuente de la *justificación*: acá no se repite el diagnóstico, se repite sólo lo que hace falta para ejecutar.

> **Leer antes de tomar cualquier tarea**: §Reglas de ejecución, §Convenciones y §Correcciones al plan. La tercera contiene cinco hechos que el plan predijo mal o no cubrió, verificados con el compilador.

> **Estado al 2026-10-03 — todas las olas ejecutadas; la tarea NO está cerrada.** Las olas `W0.T` a `W7`, las revisiones `R1` a `R7` y `W8.A` están hechas (42 commits sobre `f4f57fb`, `dd89f93`…`e8d5043`; la §Bitácora es la fuente de verdad). **Criterio alcanzado: builds + pruebas + guardas.** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` y `dotnet build EDUSIS.sln --no-incremental` → 0 errores; `dotnet test tests/WPF_Desktop.UnitTests` → 320 pruebas, 320 pasan, 0 omitidas; `./tests/run-tests.sh --rapidas` sin regresiones (`Domain` 303/308, `Core` 191/192, omitidas preexistentes); advertencias de `WPF_Desktop` 328 → 282 y `CS4014` 7 → 1. **Pendiente: la §Verificación funcional en Windows, que este documento llama "lo único que cierra la tarea de verdad".** Requiere la base de datos, que no está instalada (`H-023`: la migración sigue siendo sólo `InitCreate`), así que los pasos 0 a 5 —incluido el paso 0, que verifica `W7.A`— están **sin ejecutar**. Es el caso que la propia §Verificación prevé ("la verificación funcional queda anotada como pendiente — sin declarar la tarea cerrada"). No debe leerse que la feature esté terminada.
>
> **Se ejecutó tal cual**: la disección completa (olas, propiedad exclusiva de archivos, un commit por tarea, `*.DI` al final de cada ola, reviewer por ola), las cinco decisiones de disección y las tareas `W1` a `W7` con sus archivos propios. **Cambió de ruta**: 13 correcciones al plan en §"Correcciones al plan", de las cuales las cuatro más importantes son (a) **`UI-01` estaba mal diagnosticado** —el offset era el síntoma, no la causa—, (b) **seis `CS0246` no se resolvían con un `using`** porque `Core` había eliminado esos tipos (corrección 8), (c) **el criterio de `W2.C` era inalcanzable en su ola** (corrección 12) y (d) **cuatro criterios medían una propiedad global desde un alcance que no la controlaba** (`W2.C`, `W3.D`, `W5.B` y la cuarta guarda de §Verificación final, corrección 13). Además: la decisión #6 (las revisiones `R2`…`R5` escribieron pruebas sin ejecutarlas), `UI-13` cerrado por verificación y no por cambio, y varias tareas devueltas por no cumplir su criterio (`W1.C`, `W3.C`, `W4.B`, `W5.A`). **Quedó abierto**: la verificación funcional, `H-023` y la deuda de `docs/todos.md` UI-17…UI-37 y violaciones 23…27 (el `DbContext` único de vida de aplicación, los servicios sin pantalla, `CLAUDE.md` desactualizado, entre otras). Narrativa completa en la nota de cabecera de [`wpf-desktop.md`](wpf-desktop.md).

---

## Decisiones de disección

Tomadas con el usuario antes de escribir las tareas. Modifican el plan:

1. **Verificación por pruebas automatizadas, acotada a lo que no depende de un hilo de UI.** Se crea `tests/WPF_Desktop.UnitTests` (`net7.0-windows`) y **no se agrega a `EDUSIS.sln`**: `dotnet build EDUSIS.sln`, `dotnet test EDUSIS.sln` y `./tests/run-tests.sh --rapidas` quedan intactos y multiplataforma. El reviewer lo corre explícitamente en Windows con `dotnet test tests/WPF_Desktop.UnitTests`. Requiere `InternalsVisibleTo` en `WPF_Desktop.csproj` (los ViewModels son `internal`), igual que `Core.csproj` ya hace para su suite.
2. **`IDialogService` entra en alcance para los cinco `Gestion*` que se reescriben** (Divisiones, Curriculas, Catedras, SituacionRevista, Cursantes). Cierra la violación #12 del plan en esos archivos y es lo que vuelve testeables los remapeos de la tabla D.2, que es la parte más riesgosa del trabajo. Los `Gestion*` que el plan no reescribe (Alumnos, Cursos, Docentes, Licencias, Puestos, Usuarios) **conservan `MessageBox.Show`**.
3. **Misma rama (`002-refactor-agregados-dominio`), tareas con propiedad exclusiva de archivos.** Ningún par de tareas de la misma ola toca el mismo archivo. Ver §Reglas de ejecución.
4. **`TEST-07` entra como ola propia e independiente** (`W0.T`), para que el criterio de cierre pueda ser `dotnet build EDUSIS.sln` en verde de verdad.
5. **Los contadores que los DTO dejaron de traer se eliminan de la vista, no se recomponen.** Se quitan "Cant. Divisiones" y "Cant. Alumnos" de `GestionCursosView`/`CursoView` y "Cant. Materias" de `GestionCurriculasView`. Recomponerlos exigiría una consulta por fila (N+1) y sumar dependencias de DI a dos ViewModels más. Se registra un ID nuevo en `docs/todos.md` por si se quieren recuperar como feature.

---

## W0 — Línea base: **ya medida**, no repetir

Ejecutada el 2026-10-02 sobre `002-refactor-agregados-dominio` (`f4f57fb`). Reemplaza la predicción de la Fase 0 del plan. Ningún agente necesita volver a correrla:

| Comando | Resultado real |
|---|---|
| `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` | **1 error**: `MC3066` en `GestionSituacionRevistaView.xaml(25,13)`. 86 warnings, **1 solo de `WPF_Desktop`** (`NETSDK1138`); los otros 85 son de otras capas: `Domain` 50, `Core` 28, `Infrastructure` 7 |
| `dotnet msbuild WPF_Desktop/WPF_Desktop.csproj -t:ResolveReferences;CoreCompile -v:q -nologo` | **18 errores** de etapa de declaración (1 `CS0234` + 17 `CS0246`) y **7 warnings de `WPF_Desktop`**: 3×`CS8765`, 2×`CS8612`, 2×`CS0108` |
| `dotnet build tests/EDUSIS.EndToEndTests --no-incremental` | **6 `CS1061`** (`TEST-07`), en **2 archivos** |

**Línea base de warnings de `WPF_Desktop` = 7, y es parcial**: se midió con los 18 errores presentes, así que Roslyn nunca ligó los cuerpos de método. El número real sube al cerrar la ola W1 y eso **no es una regresión** — es la primera vez que la capa compila C# desde el refactor. La regla operativa es: a partir de la medición de W1, el conteo no debe crecer por código nuevo.

**Baseline de errores de las otras capas = 0.** `Domain`, `Core` e `Infrastructure` compilan limpio: cualquier error en esos proyectos durante este trabajo es una violación del alcance, no un arrastre.

### Los 18 errores, por archivo (dueño de la tarea que los cierra)

| Archivo | Errores | Símbolo | Tarea |
|---|---|---|---|
| `Shared/Converters/CargoConverter.cs(1,25)` | 1 `CS0234` | namespace `Domain.Curriculas.Materias` | `W1.B` |
| `ViewModels/Alumnos/RegistrarAlumnoViewModel.cs(28,10)` | 1 | `RegistrarCursanteRequest` | `W1.B` |
| `ViewModels/Cursos/Curriculas/Materias/HorarioViewModel.cs(8,19)(17,26)` | 2 | `HorarioResponse` | `W1.C` |
| `ViewModels/Cursos/Curriculas/Materias/MateriaViewModel.cs(13,19)(42,26)` | 2 | `MateriaResponse` | `W1.C` |
| `ViewModels/.../SituacionRevista/SituacionRevistaViewModel.cs(10,19)(40,35)` | 2 | `SituacionRevistaResponse` | `W1.C` |
| `ViewModels/Cursos/Divisiones/CalificacionViewModel.cs(17,10)(28,31)(36,62)(164,30)` | 4 | `CalificacionResponse`, `MateriaResponse` | `W1.D` |
| `ViewModels/Cursos/Divisiones/CursanteViewModel.cs(9,10)(17,27)` | 2 | `CursanteResponse` | `W1.D` |
| `ViewModels/Cursos/Divisiones/DivisionViewModel.cs(10,19)(29,27)` | 2 | `DivisionResponse` | `W1.D` |
| `ViewModels/Cursos/Divisiones/GestionCursantesViewModel.cs(32,10)` | 1 | `BuscarListadoRequest` | `W1.D` |
| `ViewModels/Cursos/Divisiones/GestionDivisionesViewModel.cs(36,10)` | 1 | `EliminarDivisionRequest` | `W1.D` |

---

## Reglas de ejecución

### Paralelismo por propiedad de archivos

- Cada tarea lista sus **archivos propios**. Dentro de una misma ola, **ningún archivo aparece en dos tareas**. Un agente no abre en modo escritura un archivo que no figura en su tarea; si cree que lo necesita, **para y pregunta** — es señal de que la disección tiene un hueco.
- Las olas son **puntos de sincronización**: no se empieza una ola hasta que todas las tareas de la anterior pasaron su criterio. Entre olas sí puede repetirse un archivo.
- **Archivos calientes** (serializados a propósito, un solo dueño por ola, y ese dueño va **último** dentro de su ola):
  - `WPF_Desktop/Shared/WPF_DesktopDI.cs` — lo toca la tarea `*.DI` de cada ola, ninguna otra.
  - `WPF_Desktop/Shared/DataTemplate.xaml` — `W3.B` y `W7.C`.
  - `WPF_Desktop/App.xaml` / `App.xaml.cs` — sólo `W7.A`.
- `W0.T` (TEST-07) no comparte ningún archivo con `WPF_Desktop/`: puede correr **en paralelo con cualquier ola**, desde el minuto cero.

### Criterio de aceptación por tarea, sin build verde

El proyecto no compila hasta la ola W6, así que "build limpio" no sirve como criterio por tarea. El criterio es **cero errores cuyo `file:line` caiga en los archivos que la tarea posee**:

```bash
# Desde la raíz del repo. Reemplazar <MiArchivo> por cada archivo propio de la tarea.
dotnet msbuild WPF_Desktop/WPF_Desktop.csproj -t:ResolveReferences\;CoreCompile -v:q -nologo 2>&1 | grep ': error' | grep '<MiArchivo>'
```

Debe imprimir **nada**. Que el comando siga mostrando errores de *otros* archivos es lo esperado y no invalida la tarea. A partir de W2 (cuando `MC3066` ya no corta el markup compile) sirve igual `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental`, **siempre con `--no-incremental`**: un build incremental de un proyecto WPF cachea el paso de XAML y devuelve `0 Errores` de forma engañosa.

### Reporte de cierre de tarea

Cada agente cierra su tarea informando, en tres líneas: (1) archivos tocados, (2) salida del comando de criterio, (3) cualquier violación de DDD/Clean Architecture que haya visto al pasar, **aunque esté fuera de su tarea y no la corrija** (`CLAUDE.md` lo exige). No marca el checkbox de otra tarea ni "arregla de paso" un archivo que no posee.

---

## Convenciones para los implementadores

- **Tabs** para indentar, llaves Allman, UTF-8 **sin BOM**, CRLF. En Git Bash, `sed -i` reescribe el archivo entero en LF: usar `perl -0777 -pi` o la herramienta de edición nativa.
- Todo en **español**: clases, métodos, propiedades, mensajes, `#region`, comentarios.
- **No borrar código comentado ni `TODO` preexistentes** salvo que la tarea lo nombre explícitamente (`CLAUDE.md`, FR-013).
- **Antes de dar por roto un sitio que el compilador no imprimió, verificar los delimitadores de comentario**: `grep -n '/\*\|\*/' <archivo>`. `WPF_Desktop` tiene bloques `/* */` largos y un `rg` cuenta lo comentado como vivo. Casos conocidos y **sanos**: `RegistrarDocenteViewModel` 131→164, `GestionPuestosViewModel` 159→189, `LoginUsuarioViewModel` 184→211, `RegistrarAlumnoViewModel` 100→157.
- **Prohibido editar `Domain/`, `Core/`, `Infrastructure/`.** Si una tarea parece necesitarlo, se detiene y se consulta. Única excepción: `W0.T`, que sí toca `tests/`.
- `async void` sólo si es un handler de evento de WPF. Un caso de uso devuelve `Task` y se espera con `await`.
- Los `Gestion*` que la ola W4/W5/W6 reescribe **no llaman a `MessageBox.Show`**: usan el `IDialogService` de `W2.B`. Los demás ViewModels quedan como están.

---

## Procedimiento de ejecución

Esta sección es el contrato operativo. Está escrita para que una sesión **sin ningún contexto previo** pueda arrancar leyendo sólo este documento y `CLAUDE.md`.

### Arranque en frío

1. **Verificar el punto de partida**: `git rev-parse --short HEAD` y `git status --short`. La línea base de §W0 se midió sobre `f4f57fb` en la rama `002-refactor-agregados-dominio`. Si hay commits encima que **no** sean de estas tareas, re-medir W0 antes de arrancar (comandos en §W0).
2. **Leer**: este documento completo. [`wpf-desktop.md`](wpf-desktop.md) **sólo** si hace falta la justificación de una decisión; no es necesario para ejecutar.
3. **Mirar §Bitácora** para saber qué olas están cerradas y dónde retomar. La Bitácora es la única fuente de verdad del progreso: si una tarea no está anotada ahí, se considera **no hecha**, aunque el código parezca tocado.
4. **No volver a medir W0.** Sus números están en este documento. Reconfirmarlos cuesta dos builds completos y no aporta nada si el `HEAD` coincide.

### Reparto de tareas

- **La sesión principal es el orquestador: no implementa.** Reparte tareas, verifica criterios, commitea y actualiza la Bitácora. Si implementa tareas mientras coordina, pierde la capacidad de verificar con ojos limpios lo que entregaron los agentes.
- **Hasta 3 agentes en vuelo, uno por tarea.** Nunca dos agentes sobre la misma tarea, y **nunca** dos agentes cuyas listas de archivos propios se solapen — la disección ya garantiza que las tareas de una misma ola no se solapan, así que basta con respetar las olas.
- **Las tareas `*.DI` van solas**, al final de su ola, cuando todas las demás de la ola pasaron su criterio. Poseen el archivo caliente `WPF_DesktopDI.cs`.
- **Modelo por tipo de tarea**: `haiku` alcanza para las mecánicas (W1 completa, `W2.A`, W3, `W8.A`) — son repuntes de `using`, renombres de propiedad y bindings, con el destino ya escrito en la tarea. `sonnet` para las que exigen diseño o reprogramación (`W2.B`, `W2.C`, W4, W5, W6, W7).
- **W7 y W8 son seriales.** W7 toca el composition root y su criterio es que la app arranque; no tiene sentido paralelizarla.

### Prompt para el agente implementador

El orquestador rellena los dos huecos y no agrega nada más — todo lo demás ya vive en el documento:

```
Implementá la tarea <ID> de docs/plan/wpf-desktop-tasks.md.

Leé primero, de ese documento: §Reglas de ejecución, §Convenciones para los implementadores,
§Procedimiento de ejecución y la tarea <ID> completa. No leas otras tareas salvo que la tuya
las nombre.

Podés LEER cualquier archivo del repo. Sólo podés ESCRIBIR en los archivos que tu tarea
declara como propios. Si necesitás escribir en otro, PARÁ y reportalo: es un hueco de la
disección, no algo que debas resolver por tu cuenta.

No corras `git add`, `git commit` ni ningún comando que escriba en el índice de git:
hay otros agentes trabajando en la misma rama y commitea el orquestador.

Al terminar, reportá exactamente tres cosas:
1. Los archivos que tocaste.
2. La salida literal del comando de criterio de tu tarea.
3. Toda violación de DDD o Clean Architecture que hayas visto al pasar, aunque esté fuera
   de tu tarea y no la corrijas (CLAUDE.md lo exige).
```

### Si un agente se bloquea

- **Necesita escribir un archivo que no es suyo** → para y reporta. El orquestador decide si reasigna el archivo o si la tarea se parte.
- **Un DTO no tiene la forma que dice la tarea** → **gana la forma real del DTO**. Las firmas de este documento son las que relevó el plan, no las que validó el compilador. Adaptar y anotarlo en el reporte.
- **Necesita tocar `Domain/`, `Core/` o `Infrastructure/`** → para y pregunta. Está fuera de alcance por decisión explícita, y la salida correcta casi siempre es que el puerto se define donde ya existe.
- **El criterio no pasa después de dos intentos** → reporta la salida literal del compilador y se detiene. No seguir improvisando: un tercer intento a ciegas sobre un archivo caliente es más caro de revertir que de rehacer.

### Commits

- **Commitea el orquestador, nunca los agentes.** Con tres agentes en la misma rama, un `git add` concurrente choca en `.git/index.lock` y deja el índice a medias. Los agentes editan archivos; el orquestador commitea de a una tarea.
- **Un commit por tarea cerrada**, después de verificar su criterio. Conventional Commits en español, con el ID de la tarea al final del asunto:
  - `refactor(wpf): repuntar los xmlns de Domain a los namespaces nuevos (W1.A)`
  - `fix(wpf): unificar el offset de carga horaria de materia (W5.A, UI-01)`
  - `feat(wpf): agregar la pantalla de gestión de cátedras (W4.B)`
- **Cuerpo en soft wrap** (sin hard-wrap), un párrafo por línea.
- **Sin trailers**: ni `Co-Authored-By`, ni `Claude-Session`, ni atribución de agente.
- **No se commitea una tarea cuyo criterio no pasó.** El build roto es esperado entre olas, pero el criterio *de la tarea* es por archivo propio y ése sí tiene que estar en verde.

### Cierre de ola

El orquestador, antes de abrir la ola siguiente: verifica el criterio de ola, anota en §Bitácora una línea por tarea (fecha, ID, commit, nota) y lanza la tarea `R*` de revisión correspondiente. **Las tareas de una ola nueva no se reparten con una `R*` pendiente**: el reviewer es el único que puede decir que lo entregado era lo pedido, y encontrar un remapeo mal hecho dos olas después cuesta mucho más que esperarlo.

---

## Mapa de olas

| Ola | Qué consigue | Tareas (paralelizables) | Archivos calientes |
|---|---|---|---|
| `W0.T` | `TEST-07` cerrado | 1 — corre en paralelo con todo | — |
| **W1** | Etapa de declaración limpia | `W1.A` `W1.B` `W1.C` `W1.D` | — |
| **W2** | Plomería nueva lista | `W2.A` `W2.B` `W2.C` → `W2.DI` | `WPF_DesktopDI.cs` |
| **W3** | Item ViewModels contra los DTO nuevos | `W3.A` `W3.B` `W3.C` `W3.D` | `DataTemplate.xaml` (`W3.B`) |
| **W4** | Divisiones + pantalla `GestionCatedras` | `W4.A` `W4.B` → `W4.DI` | `WPF_DesktopDI.cs` |
| **W5** | Materias/Currículas y Cursantes/Calificaciones | `W5.A` `W5.B` `W5.C` → `W5.DI` | `WPF_DesktopDI.cs` |
| **W6** | Situación de revista + deuda `UI` → **build verde** | `W6.A` `W6.B` `W6.C` `W6.D` → `W6.DI` | `WPF_DesktopDI.cs` |
| **W7** | Composition root: la app **funciona** | `W7.A` `W7.B` `W7.C` | `App.xaml*`, `DI`, `DataTemplate` |
| **W8** | Documentación de estado | `W8.A` | `docs/` |
| `R1`…`R7` | Revisión por pruebas y guardas | una por ola, para el agente reviewer | `tests/WPF_Desktop.UnitTests/**` |

Con tres agentes, el reparto natural es: un carril toma `W0.T` y después los carriles `.A`, otro los `.B`/`.C`, el tercero los `.D` y las tareas `.DI` de cierre. W7 y W8 son prácticamente seriales.

---

# W0.T — `TEST-07` (independiente, arrancar ya)

**Archivos propios**: `tests/EDUSIS.EndToEndTests/Flujos/RegistroDeCursoConDivisionesTests.cs`, `tests/EDUSIS.EndToEndTests/Infraestructura/ColeccionE2E.cs`, y el `CursoBuilder` de `tests/EDUSIS.TestSupport/` (localizarlo con `fd CursoBuilder tests/`).

Los 6 `CS1061` exactos:

| Archivo | Línea | Miembro inexistente | Dueño hoy |
|---|---|---|---|
| `Flujos/RegistroDeCursoConDivisionesTests.cs` | 37, 54 | `CursoResponse.Divisiones` | `IServicioDivision.ListarDivisionesAsync` devuelve las divisiones |
| `Flujos/RegistroDeCursoConDivisionesTests.cs` | 42, 43 | `IServicioCurso.AgregarDivisionAlCurso` | `IServicioDivision.AgregarDivisionAsync` |
| `Flujos/RegistroDeCursoConDivisionesTests.cs` | 46 | `IServicioCurso.BuscarDivisionesAsync` | `IServicioDivision.ListarDivisionesAsync` |
| `Infraestructura/ColeccionE2E.cs` | 139 | `CursoBuilder.ConDivision` | el builder ya no puede anidar divisiones: `Division` es raíz propia |

- [x] **W0.T.1** — Reprogramar `RegistroDeCursoConDivisionesTests` contra `IServicioDivision`. El flujo que la prueba describe (registrar curso → agregarle divisiones → listarlas) sigue siendo válido; cambia el servicio dueño y el request lleva `CicloLectivo`.
- [x] **W0.T.2** — `ColeccionE2E.cs:139` y el `CursoBuilder`: la división se siembra como agregado propio (`Division.Siguiente(cursoID, …)` o el repositorio de divisiones), no como hija del curso. **No reintroducir `ConDivision`** en el builder: eso resucitaría el límite de agregado que el refactor rompió.

**Criterio**: `dotnet build tests/EDUSIS.EndToEndTests --no-incremental` → 0 errores. `./tests/run-tests.sh --rapidas` sigue en verde. Si la prueba E2E queda sin poder ejecutarse por falta de Docker/`EDUSIS_TEST_SQLSERVER`, **omitirla es correcto** (salida 0): el criterio es que compile y que la lógica del flujo quede reprogramada, no que corra en esta máquina.

---

# W1 — Repunte mecánico: llegar a la etapa de cuerpos de método

Cuatro tareas, sin solape. **Al cerrar esta ola el conteo de errores sube** de 18 a ~45-60: son los `CS1061`/`CS0117`/`CS7036` que Roslyn no podía reportar mientras hubiera tipos sin resolver. Es el resultado esperado de la ola, no una regresión.

Tabla de repunte de namespaces, común a `W1.C` y `W1.D` (el DTO existe con el **mismo nombre**, en otro namespace):

| Tipos | Namespace viejo | Namespace nuevo |
|---|---|---|
| `MateriaResponse`, `RegistrarMateriaRequest`, `ModificarMateriaRequest`, `EliminarMateriaRequest`, `ListarMateriasSegunCurriculaRequest`, `MateriaRequest`, `NombreDuplicadoRequest` | `Core.ServicioCurriculas.DTOs.*` | `Core.ServicioMaterias.DTOs.*` |
| `HorarioResponse`, `SituacionRevistaResponse`, `ListarHorariosRequest` | `Core.ServicioCurriculas.DTOs.*` | `Core.ServicioCatedras.DTOs.*` |
| `DivisionResponse`, `EliminarDivisionRequest`, `EliminarPreceptorRequest`, `RegistrarPreceptorRequest` | `Core.ServicioCursos.DTOs.*` | `Core.ServicioDivisiones.DTOs.*` |
| `CursanteResponse`, `CalificacionResponse`, `RegistrarCursanteRequest`, `BuscarListadoRequest`, `CrearCalificationRequest` | `Core.ServicioCursos.DTOs.*` | `Core.ServicioCursantes.DTOs.*` |

> El sufijo correcto (`.Requests` / `.Responses`) se confirma abriendo el DTO en `Core/`, no se adivina. Y ojo con la trampa de `Core/Shared/DTOs/Personas/`, que declara **dos** namespaces para la misma carpeta (`...Personas.Response` singular y `...Personas.Responses` plural): algunos archivos necesitan importar los dos. Es la violación #10 del plan, es de `Core` y está fuera de alcance.

- [x] **W1.A** — **`UI-09` y los otros XAML con `clr-namespace` roto.**
  **Archivos propios**: `Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml`, `Views/Cursos/Curriculas/Materias/SituacionRevista/SituacionRevistaView.xaml`, `Views/Alumnos/RegistrarAlumnoView.xaml`, `Views/Cursos/RegistrarCursoView.xaml`, `Views/Cursos/Divisiones/CalificacionView.xaml`.
  - `GestionSituacionRevistaView.xaml:9` y `SituacionRevistaView.xaml:9`: `clr-namespace:Domain.Curriculas.Materias.CargosDocentes` → `clr-namespace:Domain.Catedras.SituacionesRevista` (es donde vive `Cargo`). Éste es el error que hoy corta el markup compile.
  - `SituacionRevistaView.xaml:8` declara además `xmlns:domainMaterias="clr-namespace:Domain.Curriculas"`. Ese namespace **sigue existiendo**: verificar si el prefijo se usa en el archivo y, si no, eliminar la línea; si se usa, dejarla.
  - `RegistrarAlumnoView.xaml:6`: el prefijo apunta a `Domain.Cursos` para `NivelEducativo`, que se mudó a `Domain.Shared`. Repuntarlo.
  - `RegistrarCursoView.xaml:9`: necesita **dos** xmlns. `NivelEducativo` se fue a `Domain.Shared`, pero `Grado` (usado en la línea 22) **sigue en `Domain.Cursos`**: agregar un prefijo nuevo para `Domain.Shared` y **conservar** `cursoDomain`.
  - `CalificacionView.xaml:4`: `Domain.Curriculas.Materias` → `Domain.Cursantes.Calificaciones` (es donde vive `Instancia`).
  - **No tocar** los xmlns sanos: `Domain.Personas`, `Domain.Personas.Domicilios`, `Domain.Licencias`, `Domain.Docentes.Puestos`, `Domain.Cursantes`, `Domain.Curriculas`.
  **Criterio**: `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` ya **no imprime `MC3066`** y empieza a imprimir errores `CS`. La guarda `rg -n "clr-namespace:Domain\.Curriculas\.Materias" WPF_Desktop/` da **cero**.

- [x] **W1.B** — **Converters y el error huérfano de Alumnos.**
  **Archivos propios**: `Shared/Converters/CargoConverter.cs`, `Shared/Converters/NivelEducativoConverter.cs`, `Shared/Converters/ViviendaConverter.cs`, `ViewModels/Alumnos/RegistrarAlumnoViewModel.cs`.
  - `CargoConverter.cs:1`: `using Domain.Curriculas.Materias.CargosDocentes;` → `using Domain.Catedras.SituacionesRevista;`.
  - `NivelEducativoConverter.cs:1`: `using Domain.Cursos;` → `using Domain.Shared;`. El namespace viejo existe, el tipo ya no vive ahí.
  - `ViviendaConverter.cs:5`: eliminar el `using Domain.Personas;` redundante (la línea 1 ya importa `Domain.Personas.Domicilios`).
  - `RegistrarAlumnoViewModel.cs:28`: agregar `using Core.ServicioCursantes.DTOs.Requests;` por `RegistrarCursanteRequest`. **Sólo el `using`**: el resto del archivo es de `W6.B`.
  **Criterio**: el comando de criterio filtrado por estos cuatro archivos imprime nada. `GradoConverter.cs` sigue con `using Domain.Cursos;` y eso es **correcto**.

- [x] **W1.C** — **`using` de DTOs en `ViewModels/Cursos/Curriculas/**`.**
  **Archivos propios**: `ViewModels/Cursos/Curriculas/CurriculaViewModel.cs`, `ViewModels/Cursos/Curriculas/GestionCurriculasViewModel.cs`, `ViewModels/Cursos/Curriculas/Materias/MateriaViewModel.cs`, `ViewModels/Cursos/Curriculas/Materias/HorarioViewModel.cs`, `ViewModels/Cursos/Curriculas/Materias/SituacionRevista/SituacionRevistaViewModel.cs`, `ViewModels/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaViewModel.cs`.
  Aplicar la tabla de repunte. **Sólo `using`**: nada de cuerpos de método, firmas ni propiedades — eso es de W3/W5/W6. Un archivo puede necesitar varios namespaces nuevos a la vez.
  **Criterio**: en estos archivos no queda ningún `CS0234`/`CS0246`. Sí van a aparecer `CS1061` nuevos, y es correcto: los cierran las olas siguientes.

- [x] **W1.D** — **`using` de DTOs en `ViewModels/Cursos/Divisiones/**` y `ViewModels/Cursos/`.**
  **Archivos propios**: `ViewModels/Cursos/Divisiones/DivisionViewModel.cs`, `CursanteViewModel.cs`, `CalificacionViewModel.cs`, `GestionDivisionesViewModel.cs`, `GestionCursantesViewModel.cs`, y `ViewModels/Cursos/CursoViewModel.cs`, `GestionCursosViewModel.cs`, `RegistrarCursosViewModel.cs`.
  Igual que `W1.C`. Casos que necesitan **más de un** namespace: `GestionCursantesViewModel` requiere `Core.ServicioCursantes.DTOs.Requests`, `Core.ServicioCursantes.DTOs.Responses` **y** los de `Core.ServicioMaterias.DTOs.*`; `CalificacionViewModel` requiere los de `Cursantes` **y** `MateriaResponse` de `Materias`.
  **Criterio**: igual que `W1.C`.

**Criterio de la ola W1**: `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **cero `MC3066`, cero `CS0234`, cero `CS0246`**. Todo lo que quede es `CS1061`/`CS0117`/`CS7036`/`CS1739` de cuerpos de método. **Medir acá la línea base real de warnings de `WPF_Desktop` y anotarla en este documento**: es la primera vez que la capa compila C# desde el refactor y el número sustituye al "7 parcial" de W0.

---

# W2 — Plomería nueva (todo aditivo, cero conflictos)

- [x] **W2.A** — **Stores nuevos y `CatedraViewModel`.**
  **Archivos propios (nuevos)**: `Store/CicloLectivoStore.cs`, `Store/CatedraStore.cs`, `ViewModels/Cursos/Curriculas/Materias/Catedras/CatedraViewModel.cs`.
  - `CicloLectivoStore`: mismo patrón que `Store/CursoStore.cs` (campo privado, propiedad pública, `event Action ...Changed` invocado en el setter). Inicializa en `DateTime.Today.Year.ToString()`. **Guarda el valor, no un ViewModel.**
  - `CatedraStore`: idéntico patrón, guarda el **`Guid` de la cátedra** seleccionada.
  - `CatedraViewModel`: `internal partial class ... : ObservableObject` sobre `CatedraResponse(CatedraID, MateriaID, DivisionID, CargaHoraria, HorasAsignadas, HorasSinAsignar, DocenteEnFuncionesID)` — **confirmar la forma exacta del record abriendo el DTO en `Core/ServicioCatedras/DTOs/Responses/`**. Agregar una propiedad `Division` (string) que el ViewModel contenedor completa desde `DivisionResponse.Descripcion`.
  > Los stores existentes guardan **ViewModels** (`MateriaStore.Materia` es un `MateriaViewModel`), contra lo que dice `CLAUDE.md`. Los dos nuevos siguen la convención **documentada** (valor/`Guid`), no la del vecino. Los tres viejos no se migran: es la violación #4 del plan.
  **Criterio**: los tres archivos compilan (criterio filtrado por archivo → nada). No se registran todavía en DI: eso es `W2.DI`.

- [x] **W2.B** — **`IDialogService` de verdad.**
  **Archivos propios**: `Shared/IDialogService.cs`, `Shared/DialogService.cs`.
  Hoy la interfaz tiene un único método `OpenDialogService()` y la implementación tiene el cuerpo **entero comentado**: es un stub muerto, no una abstracción. Reemplazar el contrato por lo que los `Gestion*` realmente hacen con `MessageBox.Show` (relevarlo en los cinco archivos a reescribir antes de fijar la firma; son ~95 llamadas en total). Forma mínima esperada:
  - `void MostrarInformacion(string mensaje, string titulo)`
  - `void MostrarError(string mensaje, string titulo)`
  - `bool Confirmar(string mensaje, string titulo)` — devuelve si el usuario aceptó.
  `DialogService` es la **única** clase de la capa que envuelve `MessageBox.Show`. Mantener `IDialogService` `internal` (los ViewModels que lo consumen también lo son) y la implementación coherente con eso.
  **No tocar** los ViewModels: la migración de cada uno va en su propia tarea (W4/W5/W6).
  **Criterio**: compila; `rg -n "MessageBox" WPF_Desktop/Shared/` da como única coincidencia `DialogService.cs`.

- [x] **W2.C** — **Proyecto de pruebas de la capa.**
  **Archivos propios (nuevos)**: `tests/WPF_Desktop.UnitTests/` completo, y `WPF_Desktop/WPF_Desktop.csproj` (una sola línea).
  - `WPF_Desktop.UnitTests.csproj`: `net7.0-windows`, `<UseWPF>true</UseWPF>`, `<EnableWindowsTargeting>true</EnableWindowsTargeting>`, `ProjectReference` a `WPF_Desktop`. Hereda `TargetFramework`/`Nullable`/`RollForward` de `tests/Directory.Build.props` — **sobrescribir sólo el TFM**. Paquetes: los mismos que `tests/Core.UnitTests` (xUnit + el doble de prueba que ya use el repo; **mirarlo, no elegir uno nuevo**).
  - **No agregarlo a `EDUSIS.sln`** (decisión #1). Se corre con `dotnet test tests/WPF_Desktop.UnitTests`.
  - `WPF_Desktop.csproj`: agregar `InternalsVisibleTo` para `WPF_Desktop.UnitTests`, copiando la forma exacta que usa `Core/Core.csproj`.
  - Marcar todas las pruebas con `[Trait("Categoria", "Unidad")]` y una prueba trivial de humo, para validar que el proyecto corre.
  **Criterio**: `dotnet test tests/WPF_Desktop.UnitTests` ejecuta y pasa. `dotnet build EDUSIS.sln --no-incremental` **no cambia de conteo** (el proyecto nuevo no está en el .sln). `./tests/run-tests.sh --rapidas` sigue en verde.

- [x] **W2.DI** — **Registros de la ola.** *(última de la ola; posee el archivo caliente)*
  **Archivo propio**: `Shared/WPF_DesktopDI.cs`.
  Agregar en `AddStores` (junto a las líneas 54-57): `services.AddSingleton<CicloLectivoStore>();` y `services.AddSingleton<CatedraStore>();`. `IDialogService` ya está registrado en la línea 174 — **verificar que siga apuntando a `DialogService`** después de `W2.B` y no tocar nada más.
  **Criterio**: compila; `rg -n "CicloLectivoStore|CatedraStore" WPF_Desktop/Shared/WPF_DesktopDI.cs` da dos coincidencias.

---

# W3 — Item ViewModels contra los DTO nuevos (hojas del árbol)

Mapeos acotados. Van antes de los `Gestion*` para no arrastrar errores hacia arriba. **Confirmar la forma de cada `record` abriendo el DTO en `Core/`**: las que siguen son las que el plan relevó, no las que el compilador validó.

- [x] **W3.A** — **Curso: eliminar los contadores.**
  **Archivos propios**: `ViewModels/Cursos/CursoViewModel.cs`, `Views/Cursos/CursoView.xaml`, `Views/Cursos/GestionCursosView.xaml`.
  - `CursoViewModel.cs:38,39`: eliminar las propiedades `Divisiones` y `Alumnos`. `CursoResponse` hoy es `(CursoID, Grado, NivelEducativo)`.
  - `CursoView.xaml:71-84`: quitar los dos pares `Label`+`TextBox` de "Divisiones" y "Alumnos".
  - `GestionCursosView.xaml:220-225`: quitar las dos `DataGridTextColumn` "Cant. Divisiones" y "Cant. Alumnos". **No tocar** la línea 81 (`TextBlock Text="Divisiones"`, que es el botón de navegación) ni el subtítulo de la línea 41.
  - Dejar un `//TODO` con el ID nuevo que `W8.A` registre en `docs/todos.md` (decisión #5: los contadores se recuperan como feature, no como parte de este refactor).
  **Criterio**: criterio filtrado por los tres archivos → nada. `rg -n "Path=Divisiones|Path=Alumnos" WPF_Desktop/Views/Cursos/` da cero.

- [x] **W3.B** — **Currícula y Materia: la materia deja de conocer a sus docentes.**
  **Archivos propios**: `ViewModels/Cursos/Curriculas/CurriculaViewModel.cs`, `ViewModels/Cursos/Curriculas/Materias/MateriaViewModel.cs`, `Views/Cursos/Curriculas/Materias/MateriaView.xaml`, `Shared/DataTemplate.xaml`.
  - `CurriculaViewModel.cs:40`: eliminar la propiedad `Materias` (el conteo). `CurriculaResponse` es `(CursoID, CurriculaID, FechaInicio, FechaFin)` — aplanado a propósito. **Conservar `EstaActiva`**, que se calcula con `FechaFin is null` y lo necesita `W5.B`.
  - `MateriaViewModel.cs:53,54`: eliminar `CargosOcupados` y `SituacionRevista`. `MateriaResponse` es `(CursoID, CurriculaID, MateriaID, Descripcion, HorasCatedra)`. Es la corrección de las violaciones DDD #2 y #3 de `core.md` llegando a la UI.
  - `MateriaView.xaml:137` (binding a `SituacionRevista.Docente`) y `:152` (binding a `CargosOcupados`): quitar esos dos bloques. El docente de una materia ya no existe como concepto: existe el docente **en funciones de una cátedra**, y eso se ve en la pantalla de cátedras.
  - `DataTemplate.xaml`: verificar que ningún `DataTemplate` quede apuntando a un tipo inexistente. El de `HorarioViewModel` (líneas 169-173) está **comentado** y `HorarioView` no existe: **dejarlo así**, es un hueco preexistente.
  - **No tocar `GestionCurriculasView.xaml`**: su columna "Cant. Materias" la quita `W5.B`, que es dueña del archivo.
  **Criterio**: criterio filtrado por los cuatro archivos → nada. `rg -n "CargosOcupados|SituacionRevista\.Docente" WPF_Desktop/` da cero (salvo lo que `W6.B` todavía deba limpiar en `GestionSituacionRevistaView.xaml:194`).

- [x] **W3.C** — **División, Cursante y Calificación.**
  **Archivos propios**: `ViewModels/Cursos/Divisiones/DivisionViewModel.cs`, `CursanteViewModel.cs`, `CalificacionViewModel.cs`, `Views/Cursos/Divisiones/DivisionView.xaml`, `CursanteView.xaml`, `CalificacionView.xaml`.
  - `DivisionViewModel.cs:37-39`: `DocenteID`→`PreceptorID`, `Docente`→`Preceptor`, `Alumnos`→`Cursantes`. `DivisionResponse` es `(DivisionID, Descripcion, PreceptorID, Preceptor, Cursantes)`. Actualizar los bindings de `DivisionView.xaml`.
  - `CursanteViewModel.cs:30`: `Edad` es `int` en el DTO y hoy se asigna a un `string` — tiparlo `int` o mapear con `.ToString()`. Agregar `CursanteID` y `EsRecursante`. **`CursanteID` es obligatorio**: todos los requests de calificación y de asistencia lo usan como clave.
  - `CalificacionViewModel.cs`: `Asistencia`→`Rindio`; `Instancia` pasa de `int` a `string`; `Fecha` deja de ser nullable; agregar `CalificacionID`, `Aprobado` y `Observacion`. `CalificacionResponse` es `(CalificacionID, MateriaID, Materia, Fecha, Instancia:string, Rindio, Nota, Aprobado, Observacion)`. Además: la colección interna pasa de `ObservableCollection<MateriaResponse>` a `ObservableCollection<MateriaViewModel>` (hoy bindea un DTO de `Core` directo en la vista — violación #5 del plan) y el `INotifyDataErrorInfo` manual se migra a `ObservableValidator`, como el resto del repo.
  - `CalificacionView.xaml:77-81`: el binding `Path=Asistencia` pasa a `Path=Rindio` y el `Label` "Asistencia" a un texto que diga qué significa (rindió / no rindió). El xmlns de la línea 4 ya lo arregló `W1.A`.
  - **No tocar** `GestionDivisionesViewModel.cs` ni `GestionDivisionesView.xaml` (son de `W4.A`), ni `GestionCursantesViewModel.cs` (de `W5.C`).
  - **No tocar** `Views/Docentes/DocenteView.xaml:56` ni `LegajoDocenteView.xaml:127-142`: también bindean `FechaAlta`/`FechaBaja`, pero son del legajo del docente y no tienen nada que ver con este refactor.
  **Criterio**: criterio filtrado por los seis archivos → nada. `rg -n "Path=Asistencia" WPF_Desktop/Views/Cursos/` da cero.

- [x] **W3.D** — **Situación de revista (item).**
  **Archivos propios**: `ViewModels/Cursos/Curriculas/Materias/SituacionRevista/SituacionRevistaViewModel.cs`, `Views/Cursos/Curriculas/Materias/SituacionRevista/SituacionRevistaView.xaml`.
  - `MateriaID`→`CatedraID`, `FechaAlta`/`FechaBaja`→`FechaInicio`/`FechaFin`, agregar `ReemplazaA` y `EnFunciones`. `SituacionRevistaResponse` es `(SituacionRevistaID, CatedraID, DocenteID, Estado, Cargo, FechaInicio, FechaFin, ReemplazaA, EnFunciones)`.
  - **`Docente` (el nombre) ya no viene en el DTO.** Conservar la propiedad en el ViewModel y **no intentar llenarla acá**: la completa el contenedor en `W6.A`. Es la violación #3 del plan, mitigada y no resuelta.
  - `SituacionRevistaView.xaml`: actualizar los bindings por los renombres. El xmlns ya lo arregló `W1.A`.
  **Criterio**: criterio filtrado por los dos archivos → nada. `rg -n "FechaAlta|FechaBaja" WPF_Desktop/Views/Cursos/` da cero.

---

# W4 — Divisiones y la pantalla nueva de cátedras

- [x] **W4.A** — **`GestionDivisiones`: cambiar de servicio y cerrar tres defectos.**
  **Archivos propios**: `ViewModels/Cursos/Divisiones/GestionDivisionesViewModel.cs`, `Views/Cursos/Divisiones/GestionDivisionesView.xaml`, `Store/DivisionStore.cs`.
  - Constructor: `IServicioCurso` → `IServicioDivision`. **Conservar `IServicioDocente`** (lo usa la búsqueda de preceptores). Agregar `CicloLectivoStore` e `IDialogService`.
  - Remapear las cinco llamadas: `BuscarDivisionesAsync`→`ListarDivisionesAsync`, `AgregarDivisionAlCurso`→`AgregarDivisionAsync`, `QuitarDivisiosDelCurso`→`EliminarDivisionAsync`, `RegistrarPreceptorEnDivision`→`AsignarPreceptorAsync`, `EliminarPreceptorDeDivision`→`QuitarPreceptorAsync`. `ListarDivisionesRequest(CursoID, CicloLectivo)` y `EliminarDivisionRequest(CursoID, DivisionID, CicloLectivo)` llevan ciclo lectivo: sale del `CicloLectivoStore`, **nunca de `DateTime.Today` dentro del ViewModel**.
  - **Bug de navegación**: el constructor recibe `DivisionStore divisionStore` y lo **descarta** (no hay campo), y la línea 345 `//_divisionStore.Division = Division;` está comentada. Guardarlo en un campo y asignar la división **antes** de `Navigate()`. Hoy navegar a cursantes pierde la división elegida.
  - **`E.5`**: en `Store/DivisionStore.cs`, la propiedad `Curso` está tipada **`CursoStore`** en lugar de `CursoViewModel` — es un store que guarda otro store y nadie puede asignarle un curso. **Eliminar la propiedad**: quien necesite el curso ya tiene `CursoStore` inyectado.
  - Los tres `CargarDivisionesAsync()` sin `await` (líneas 211, 236, 383): el `Task` se descarta y las excepciones de recarga se pierden en silencio dentro de un `try/catch` que parecía cubrirlas. Hacer `await` y pasar los métodos contenedores a `Task`.
  - El `CanExecute` de la línea 185 lee `Division.DocenteID` → `Division.PreceptorID` (renombrado por `W3.C`).
  - Reemplazar los `MessageBox.Show` (19) por el `IDialogService` de `W2.B`, **conservando los textos**.
  - Actualizar los bindings de `GestionDivisionesView.xaml` afectados por `W3.C`.
  **Criterio**: criterio filtrado por los tres archivos → nada. `rg -n "MessageBox|async void" <los tres>` da cero. `rg -n "DateTime.Today|DateTime.Now" GestionDivisionesViewModel.cs` da cero.

- [x] **W4.B** — **Pantalla nueva `GestionCatedras`** (todo archivos nuevos salvo `DataTemplate.xaml`).
  **Archivos propios (nuevos)**: `ViewModels/Cursos/Curriculas/Materias/Catedras/GestionCatedrasViewModel.cs`, `Views/Cursos/Curriculas/Materias/Catedras/GestionCatedrasView.xaml(.cs)`, `Navigation/NavigationServices/Cursos/GestionCatedrasNavigationService.cs`. **Archivo existente propio**: `Shared/DataTemplate.xaml`.
  Razón de ser: `SituacionRevista` dejó de colgar de `Materia` y ahora es entidad interna de `Catedra` = **materia + división**. `IServicioCatedra` exige `CatedraID` en nueve de sus trece métodos, así que hace falta un paso intermedio entre materias y situación de revista. Es la única pantalla nueva del plan.
  - Recibe `IServicioCatedra`, `IServicioDivision`, `MateriaStore`, `CatedraStore`, `CicloLectivoStore`, `IDialogService` y **dos** `INavigationService` (volver a materias, ir a situación de revista).
  - Lista las cátedras de la materia con `ListarCatedrasSegunMateriaAsync(MateriaID)`, cruza con `ListarDivisionesAsync(CursoID, CicloLectivo)` para mostrar la letra de división, y ofrece `CrearCatedraAsync(MateriaID, DivisionID)` para las divisiones que todavía no tienen cátedra de esa materia.
  - Al seleccionar una cátedra, escribe su `Guid` en `CatedraStore` **antes** de navegar.
  - Comandos `IAsyncRelayCommand` con su par `Execute`/`CanExecute`, construidos en el constructor. **Cero `async void`**, cero `MessageBox`.
  - `GestionCatedrasNavigationService`: copiar la forma de `GestionSituacionRevistaNavigationService`.
  - `DataTemplate.xaml`: agregar el template de `GestionCatedrasViewModel` (y de `CatedraViewModel` si la vista lo usa como item). **Toda vista nueva necesita su `DataTemplate` acá** o la navegación muestra el nombre del tipo en pantalla.
  > **Heredás la violación #7 del plan**: los dos `INavigationService` se distinguen **sólo por su orden** en la factory lambda. Invertirlos compila y rompe la navegación en silencio. No se corrige en esta tanda; dejar el orden documentado en un comentario sobre el constructor.
  **Criterio**: criterio filtrado por los archivos nuevos → nada. El `DataTemplate` existe y apunta a un tipo que existe.

- [x] **W4.DI** — **Registros de la ola.** *(última; posee el archivo caliente)*
  **Archivo propio**: `Shared/WPF_DesktopDI.cs`.
  - Actualizar la factory lambda de `GestionDivisionesViewModel` (hoy líneas 131-137) por la firma nueva de `W4.A`.
  - Registrar `GestionCatedrasViewModel` (`AddTransient` con factory lambda explícita) y `CreateGestionCatedrasNavigationService`, siguiendo la forma de las líneas 265-273.
  - Agregar los `using` de `Core.ServicioDivisiones` y `Core.ServicioCatedras`.
  **Criterio**: `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` no imprime **ningún** error en los archivos de `W4.A`, `W4.B` ni en `WPF_DesktopDI.cs`.

---

# W5 — Materias/Currículas y Cursantes/Calificaciones

- [x] **W5.A** — **Partir la dependencia de `GestionCurriculas` y cerrar `UI-01`, `UI-03`, `UI-08`.**
  **Archivo propio**: `ViewModels/Cursos/Curriculas/GestionCurriculasViewModel.cs`.
  La pantalla mezcla dos agregados (`Curricula` y `Materia`) y hoy los pide a un solo servicio. **No hace falta partir la pantalla, sí la dependencia.**
  - Agregar `IServicioMateria` al constructor y mover a él `ListarMateriasSegunCurriculaAsync`, `RegistrarMateriaAsync`, `ModificarMateriaAsync`, `EliminarMateriaAsync`. `IServicioCurricula` queda sólo con `ListarCurriculasSegunCursoAsync`, `RegistrarCurriculaAsync` y `DesafectarCurriculaAsync`. Agregar `IDialogService`.
  - **Quitar `IServicioDocente`**, que se inyecta y no se usa en código vivo. Renombrar el parámetro `servicioMateria` de la línea 108, que en realidad recibe la currícula.
  - **`UI-01`, el offset de carga horaria**: el XAML liga `CargaHoraria` al `SelectedIndex` (0-based) del combo; el diálogo de confirmación (línea 240) muestra `CargaHoraria + 1` y el request (línea 252) envía `CargaHoraria` **sin** el `+1`. Unificar: el valor que se muestra es el que se persiste. Hoy elegir el índice 0 intenta registrar `HorasCatedra = 0`, que `Materia` rechaza con `ArgumentException`. Y el flujo "Update" (líneas 283-288) reenvía `Materia.HorasCatedra` sin cambios, así que **editar la carga horaria no tiene ningún efecto**: debe usar el valor elegido en el combo.
  - **`UI-08`, la duplicación**: descomentar el bloque de las líneas 379-426 (que ya tiene el offset bien) y **fusionarlo** con el flujo activo de `ExecuteGuardarCommandAsync` caso `"Materia"`, reescribiendo sus referencias muertas: `_servicioMateria` → el campo nuevo, `_registrarMateriaRequest` → variable local, `LoadMaterias()` → `CargarMaterias()`, `NivelEducativoDescripcion` → `NivelEducativo`. Queda **un** flujo de alta de materia, no dos.
  - **`UI-03`**: los dos `//TODO: Verificar id de curricula` (líneas 248, 354) son **obsoletos** y se eliminan. El de alta de materia usa `Curricula.CursoID`/`Curricula.CurriculaID` de la currícula seleccionada, que es correcto; el de alta de currícula usa `_cursoStore.Curso.CursoID` y no necesita `CurriculaID`.
  - Convertir a `Task` los cuatro `async void` (`CargarCurriculas`, `CargarMaterias`, `ExecuteRegistrarCommand`, `ExecuteCancelarCommand`) y exponer los dos primeros como `IAsyncRelayCommand` (el XAML los dispara hoy con `behaviors:CallMethodAction` sobre `Loaded`, que exige un método sin retorno observable).
  - Reemplazar los `MessageBox.Show` (18) por `IDialogService`.
  - `ExecuteNavigationCommand` caso `"SituacionRevista"` pasa a navegar a **`GestionCatedras`** (creada en `W4.B`) y el caso se renombra a `"Catedras"`.
  **Criterio**: criterio filtrado por el archivo → nada. `rg -n "MessageBox|async void|IServicioDocente|TODO: Verificar id de curricula" <el archivo>` da cero. Debe quedar **un solo** sitio que construya un `RegistrarMateriaRequest`.

- [x] **W5.B** — **`GestionCurriculasView.xaml`.**
  **Archivo propio**: `Views/Cursos/Curriculas/GestionCurriculasView.xaml`.
  - **Línea 353**: quitar la `DataGridTextColumn` "Cant. Materias" (`Binding Path=Materias` sobre la fila de currícula), cuya propiedad eliminó `W3.B`. **Cuidado: la línea 378 también bindea `Path=Materias`, y ésa se queda** — es el `ItemsSource` de la colección propia del ViewModel, no el contador de la fila. Son dos cosas distintas con el mismo nombre.
  - Líneas 357-362: el `behaviors:CallMethodAction MethodName="CargarMaterias"` pasa a `behaviors:InvokeCommandAction` con el comando de `W5.A`. Mismo tratamiento para el trigger de `CargarCurriculas`. Patrón ya usado en `GestionDivisionesView` con `CargarDivisionesCommandAsync`.
  - Línea 476: `CommandParameter="SituacionRevista"` → `CommandParameter="Catedras"`, y el texto del botón pasa a hablar de cátedras.
  - Líneas 438 y siguientes (`MateriaView` embebida): verificar que no bindee nada que `W3.B` eliminó.
  - El xmlns de la línea 9 (`Domain.Curriculas`) es **sano**: no tocarlo.
  **Criterio**: criterio filtrado por el archivo → nada. `rg -n 'CommandParameter="SituacionRevista"' WPF_Desktop/Views/` da cero. La línea del `ItemsSource` de materias sigue existiendo.

- [x] **W5.C** — **`GestionCursantes` y las calificaciones: cerrar `UI-02`.**
  **Archivos propios**: `ViewModels/Cursos/Divisiones/GestionCursantesViewModel.cs`, `Views/Cursos/Divisiones/GestionCursantesView.xaml`.
  Es el ViewModel más desactualizado del proyecto: usa `ViewModelCommand` (el command base legado de `Shared/Commands/`) e `INotifyDataErrorInfo` manual en lugar de CommunityToolkit.
  - Reescribirlo como `ObservableValidator` con `IAsyncRelayCommand`, recibiendo `IServicioCursante` + `IServicioMateria` + `IServicioCurricula` + `IDialogService` en lugar de `IServicioCurso` + `IServicioCurricula`.
  - **`UI-02`, primera mitad**: el constructor llama `ListarMateriasSegunCurriculaAsync` con `CurriculaID: Guid.Empty`, **sin `await`**, y descarta el resultado — `_materias` queda **siempre vacía**. Mover la carga a un `CargarCommandAsync` disparado en `Loaded`, y obtener la currícula vigente con `IServicioCurricula.ListarCurriculasSegunCursoAsync` + `FechaFin is null` (el criterio que `CurriculaViewModel.EstaActiva` ya usa).
  - **`UI-02`, segunda mitad**: `BuscarCursantes` tiene la llamada real comentada y devuelve `new List<CursanteResponse>()` hardcodeado. Reemplazar por `IServicioCursante.ListarCursantesAsync(new BuscarListadoRequest(CursoID, DivisionID, Periodo))`.
  - El `Periodo` local con regex `^(20)\d{2}$` se reemplaza por `CicloLectivoStore`. La validación de formato ya la hace `CicloLectivo.Crear` en el dominio: **no duplicarla en la UI**.
  - Conectar las calificaciones, que hoy no llegan a `Core`: `ExecuteABMCommand` caso `"Calificacion"` pasa a `RegistrarCalificacionAsync(CrearCalificationRequest(...))`, y se agregan los comandos de `QuitarCalificacionAsync`, `ModificarObservacionCalificacionAsync` y `RegistrarInasistenciaAExamenAsync`, que el dominio y `Core` ya soportan y la UI no alcanza.
  - **El dominio no permite corregir una nota**: se quita y se vuelve a cargar. La pantalla **no debe ofrecer "editar nota"**, sólo "editar observación".
  - Actualizar los bindings de `GestionCursantesView.xaml`. El xmlns de la línea 5 (`Domain.Cursantes`) es **sano**.
  **Criterio**: criterio filtrado por los dos archivos → nada. `rg -n "Guid.Empty|new List<CursanteResponse>|ViewModelCommand|async void|MessageBox" <los dos>` da cero. `rg -n '\^\(20\)' <el VM>` da cero.

- [x] **W5.DI** — **Registros de la ola.** *(última; posee el archivo caliente)*
  **Archivo propio**: `Shared/WPF_DesktopDI.cs`.
  Actualizar las factory lambdas de `GestionCurriculasViewModel` (líneas 141-147) y `GestionCursantesViewModel` (líneas 120-124) por las firmas nuevas, y agregar los `using` de `Core.ServicioMaterias` y `Core.ServicioCursantes`.
  **Criterio**: ningún error en los archivos de `W5.*` ni en `WPF_DesktopDI.cs`.

---

# W6 — Situación de revista, deuda `UI` restante, y **build verde**

- [x] **W6.A** — **`GestionSituacionRevista`: reescritura contra `IServicioCatedra`.**
  **Archivo propio**: `ViewModels/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaViewModel.cs`.
  - `IServicioCurricula` **sale**, entra `IServicioCatedra`. `MateriaStore` se complementa con `CatedraStore`. Agregar `IDialogService`.
  - Remapeo de los seis casos de uso:

    | Antes | Ahora |
    |---|---|
    | `ListarCargosDocenteSegunMateriaAsync(CursoID, CurriculaID, MateriaID)` | `ListarSituacionesRevistaAsync(CatedraID)` |
    | `RegistrarDocenteEnMateriaAsync(...)` | `DesignarDocenteAsync(CatedraID, DocenteID, Cargo, FechaInicio, FechaFin, ReemplazaA)` |
    | `EstablecerDocenteDeAulaAsync(...)` | `PonerEnFuncionesAsync(CatedraID, SituacionRevistaID)` |
    | `RelevarDocenteDeFuncionesEnMateriaAsync(...)` | `RelevarDeFuncionesAsync(CatedraID)` |
    | `RescindirCargoDocenteDeMateriaAsync(...)` | `EstablecerFinDeDesignacionAsync(CatedraID, SituacionRevistaID, FechaFin)` |
    | `EliminarCargoDocenteAsync(...)` | `FinalizarDesignacionAsync(CatedraID, SituacionRevistaID)` |

  - **Dos cambios semánticos que hay que propagar a los textos de los diálogos**: (a) "eliminar definitivamente el cargo docente" **ya no existe** — el dominio *finaliza* la designación, no la borra; (b) al elegir el cargo `Suplente`, el formulario debe pedir **a quién reemplaza** entre las situaciones de revista vigentes de la misma cátedra, porque `DesignarDocenteRequest` lleva `ReemplazaA` y es la cadena de suplencias. Sin eso, designar un suplente falla con `SuplenciaSinReemplazoException`.
  - **Nombre del docente**: `SituacionRevistaResponse` trae `DocenteID` pero **no** el nombre, y la pantalla lo muestra en seis diálogos. Resolverlo con **una sola** llamada a `IServicioDocente.ListarDocentesActivosAsync()` y cruce en memoria por `DocenteID` — **no una llamada por fila**. Con eso se llena la propiedad `Docente` que `W3.D` dejó en el item ViewModel.
  - **Conservar tal cual** el `Query` de búsqueda de docentes y `SeleccionarCommand`: usan `IServicioDocente.BuscarDocenteSegunNombreCompletoAsync`, que no cambió.
  - Convertir los cuatro `async void` a `IAsyncRelayCommand` y reemplazar los `MessageBox.Show` (25) por `IDialogService`.
  **Criterio**: criterio filtrado por el archivo → nada. `rg -n "MessageBox|async void|IServicioCurricula" <el archivo>` da cero. Debe haber **exactamente una** llamada a `ListarDocentesActivosAsync` y debe estar fuera de todo bucle.

- [x] **W6.B** — **`GestionSituacionRevistaView.xaml`.**
  **Archivo propio**: `Views/Cursos/Curriculas/Materias/SituacionRevista/GestionSituacionRevistaView.xaml`.
  - **Línea 194**: `DataContext="{ Binding Path=Materia.SituacionRevista }"` apunta a la propiedad que `W3.B` eliminó de `MateriaViewModel`. Repuntarla a la situación de revista seleccionada de la cátedra activa. **Éste es el binding que el plan no nombra y que rompe la pantalla en silencio** (un binding roto en WPF no falla el build: deja el control vacío).
  - Línea 33: `behaviors:CallMethodAction MethodName="CargarSituacionRevistas"` → `InvokeCommandAction` con el comando de `W6.A`.
  - Agregar el control para elegir **a quién reemplaza** un suplente (ver `W6.A`).
  - Actualizar los bindings por los renombres de `W3.D` y los textos de los diálogos por el cambio semántico de "eliminar" → "finalizar".
  - El xmlns de la línea 9 ya lo arregló `W1.A`.
  **Criterio**: criterio filtrado por el archivo → nada. `rg -n "Materia\.SituacionRevista" WPF_Desktop/` da cero.

- [x] **W6.C** — **Alumnos: `UI-04` y `UI-05`.**
  **Archivos propios**: `ViewModels/Alumnos/RegistrarAlumnoViewModel.cs`, `ViewModels/Alumnos/InscripcionAlumnoViewModel.cs`, `Views/Alumnos/RegistrarAlumnoView.xaml`, `Views/Alumnos/InscripcionAlumnoView.xaml`.
  - **`UI-04`**: resolver el `//TODO: Refactorizar` (línea 21), la `#region TODO:` (línea 99) y el `//TODO: Refactorizar inscripción de alumno` (línea 323) de `RegistrarAlumnoViewModel`. El alta usa `IServicioAlumno.RegistrarAlumnoAsync`, que **no cambió**; lo que hay que separar es la **inscripción**, que ahora es un caso de uso de otro agregado (`IServicioCursante.InscribirCursanteAsync`). El bloque `/* */` de 100→157 es código comentado preexistente: **no se descomenta ni se borra** salvo que sea justo el flujo de inscripción que esta tarea reescribe.
  - **`UI-05`**: completar `InscripcionAlumnoViewModel` con `InscribirCursanteAsync(RegistrarCursanteRequest(CursoID, DivisionID, AlumnoID, Periodo, EsRecursante))`. El `TODO` de la línea 37 pide "alumno/división/período de origen": el período sale de `CicloLectivoStore`, la división de un combo alimentado por `IServicioDivision.ListarDivisionesAsync`, y el alumno de `LegajoStore`. **El cupo lo valida `Core`** (`Division.ValidarCupoDisponible` + `ContarCursantesDeDivisionAsync`): la UI sólo muestra la excepción, no reimplementa la regla.
  - El `using` de `RegistrarCursanteRequest` ya lo agregó `W1.B`.
  - Estos dos ViewModels **conservan `MessageBox.Show`**: no están en la lista de los cinco `Gestion*` reescritos (decisión #2).
  **Criterio**: criterio filtrado por los cuatro archivos → nada. Inscribir un alumno llega a `IServicioCursante`, verificable por lectura: debe existir una llamada a `InscribirCursanteAsync`.

- [x] **W6.D** — **Cabos sueltos: `UI-06`, `Models/` y `D.3`.**
  **Archivos propios**: `ViewModels/Shared/DomicilioViewModel.cs`, `Models/` (carpeta completa), `ViewModels/Docentes/GestionDocentesViewModel.cs`, `Navigation/NavigationServices/Docentes/GestionLicenciasNavigationService.cs`.
  - **`UI-06`**: `DomicilioViewModel.cs:51`, `//TODO:Verificar string`. Revisar la validación señalada y cerrarla, **o** dejar escrito en el código por qué queda abierta. Las dos salidas son aceptables; lo que no es aceptable es dejar el `TODO` sin decidir.
  - **`Models/`**: las tres clases están **huérfanas**, nadie importa `WPF_Desktop.Models`. `PersonaDTO.cs` declara una clase llamada `AlumnoDTO` (nombre de archivo y clase no coinciden) que sólo usa `DomicilioDTO`, y `Usuario.cs` no tiene consumidores. Duplican `PersonaConDetallesResponse`, `DomicilioResponse` y `UsuarioResponse` de `Core`. **Eliminar la carpeta**, después de confirmar con `rg -n "WPF_Desktop.Models|AlumnoDTO|DomicilioDTO" WPF_Desktop/ --glob '!obj' --glob '!bin'` que no hay consumidores (ni comentados: si hay comentados, se dejan y se anota).
  - **`D.3`**: `GestionDocentesViewModel.cs:178` llama `_servicioDocentes.QuitarDocente(...)`, que `Core` renombró a `QuitarDocenteAsync` al corregir `H-018` (era `async void`). Y la llamada **no tiene `await`**, así que descarta el `Task`. **Corregir las dos cosas juntas**: `await _servicioDocentes.QuitarDocenteAsync(...)`, y pasar el método contenedor a `Task`. Renombrar sin `await` deja vivo exactamente el defecto que `H-018` cerró. Es el único error real fuera del clúster de cursos. Este ViewModel **conserva `MessageBox.Show`**.
  - **`9.4`**: `GestionLicenciasNavigationService.cs` **no declara `namespace`** — la clase queda en el namespace global y compila por accidente (`WPF_DesktopDI` importa `...NavigationServices.Docentes`, que resuelve igual porque el global es visible). Agregar el namespace que le corresponde.
  **Criterio**: criterio filtrado por los archivos → nada. `rg -n "QuitarDocente\(" WPF_Desktop/` da cero. `fd . WPF_Desktop/Models` no devuelve nada.

- [x] **W6.DI** — **Registros de la ola y build verde.** *(última; posee el archivo caliente)*
  **Archivo propio**: `Shared/WPF_DesktopDI.cs`.
  - Actualizar la factory de `GestionSituacionRevistaViewModel` (líneas 151-156) y las de `RegistrarAlumnoViewModel` / `InscripcionAlumnoViewModel`.
  - **`9.2`, riesgo latente de DI**: `INavigationService` está registrado **una sola vez** (línea 175), como `MainNavigationService<MainViewModel>`. Los ViewModels registrados **sin** factory lambda (`BuscarViewModel`, `GestionPuestosViewModel`, `GestionLicenciasViewModel`, `RegistrarUsuarioViewModel`, `RegistrarAlumnoViewModel`, `InscripcionAlumnoViewModel`) reciben por inyección **ese** servicio, no el que su pantalla necesita: cualquiera que navegue, navega a `MainViewModel`. Darles factory lambda explícita como el resto, **o** verificar uno por uno que no dependan de `INavigationService` y dejarlo anotado.
  **Criterio de la ola W6 — el corte grande**: `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **0 errores**. Con `W0.T` cerrada, `dotnet build EDUSIS.sln --no-incremental` → **0 errores**. Y las cuatro guardas de §Verificación final, salvo la de `BuildServiceProvider`, que la cierra W7.

---

# W7 — Composition root: que la app **funcione**

El build verde **no** cierra la tarea. Esta ola tiene el defecto más grave de la capa y a la vez el que ningún compilador señala: con dos contenedores de DI, la app compila, arranca y muestra una ventana que no responde a la navegación — y el síntoma se confundiría con un error en los ViewModels recién reescritos.

- [x] **W7.A** — **Un solo contenedor, un solo punto de arranque** (`E.1`…`E.4`).
  **Archivos propios**: `App.xaml`, `App.xaml.cs`.
  - **`E.1`**: `App.xaml:5` declara `Startup="Application_Startup"` **y** la clase sobrescribe `OnStartup`. Los dos se ejecutan en cada arranque. Elegir **uno**: lo idiomático con `IHost` es `OnStartup` — quitar el `Startup=` del XAML y mover ahí el cuerpo de `Application_Startup`.
  - **`E.2`**: `App.xaml.cs:62` llama `services.BuildServiceProvider()` **dentro** del `ConfigureServices` del `IHost`, así que quedan **dos contenedores raíz** sobre la misma `IServiceCollection`: `AppHost.Services` y `_serviceProvider`. Los `*NavigationStore` son `Singleton`, de modo que hay **dos** instancias de cada uno. Eliminar el `BuildServiceProvider()` y el campo `_serviceProvider`: todo se resuelve de `AppHost.Services`.
  - **`E.3`** es la consecuencia: `App.xaml.cs:111-114` resuelve `MainWindow` del contenedor A y el `INavigationService` del B, así que la navegación inicial escribe el ViewModel activo en el store que la ventana **no** observa. Se cierra solo al cerrar `E.2`.
  - **`E.4`**: `OnStartup` hace `await AppHost.StopAsync()` **al arrancar**, sobre un host al que nadie le hizo `StartAsync()`. Reemplazarlo por `await AppHost.StartAsync()` y **conservar** el `StopAsync()` de `OnExit`. `OnStartup`/`OnExit` son `async void`: una excepción ahí termina el proceso sin pasar por `Application_DispatcherUnhandledException` — envolver los cuerpos en `try/catch`, o pasar a `OnStartup` sincrónico con `AppHost.Start()`.
  - **Anotar, no resolver**: los servicios de `Core` están registrados `Scoped` en `CoreDI` y se resuelven del proveedor **raíz**, así que cada `GetRequiredService` devuelve una instancia que vive lo que vive la aplicación — incluido el `DbContext` detrás de `IUnitOfWork`. Con un único contenedor esto se vuelve visible. Un `IServiceScope` por operación de ViewModel es lo correcto, pero **es un cambio de diseño que excede esta tarea**: registrarlo como ID nuevo en `docs/todos.md` (tarea `W8.A`) y verificar al menos que no haya dos `DbContext` concurrentes sobre el mismo agregado.
  **Criterio**: `rg -n "BuildServiceProvider" WPF_Desktop/` da **cero**. `rg -n "Startup=" WPF_Desktop/App.xaml` da cero. `rg -n "StopAsync" WPF_Desktop/App.xaml.cs` da **una** coincidencia, en `OnExit`.

- [x] **W7.B** — **Limpieza de DI y navegación.**
  **Archivos propios**: `Shared/WPF_DesktopDI.cs`, `Navigation/NavigationServices/Modal/BuscarNavigationService.cs`, `WPF_Desktop/WPF_Desktop.csproj`.
  Todo preexistente y ya verificado sin consumidores:
  - `CreateBuscarModalNavigationService` (`WPF_DesktopDI.cs:183`) nunca se llama.
  - `Navigation/NavigationServices/Modal/BuscarNavigationService.cs` no se usa **y además está tipado contra `MainWindowNavigationStore`** en lugar del store modal: si alguien lo activara, abriría el modal en la ventana principal. Eliminar o corregir el tipo; **no dejarlo como está**.
  - `Navigation/NavigationService.cs` y `Navigation/NavigationWithParameterService.cs` no participan del contenedor: **anotar, no borrar** (código muerto preexistente, FR-013).
  - `IDialogService`/`DialogService`: después de `W2.B` **sí** tienen consumidores. Verificarlo y no tocarlos.
  - `WPF_Desktop.csproj` tiene un `PackageReference` a `MediatR` que la capa no debería necesitar (los handlers viven en `Core`). **Verificar con `rg -n "MediatR" WPF_Desktop/ --glob '!obj' --glob '!bin'` antes de quitarlo.** Si hay un solo uso vivo, se queda y se anota.
  **Criterio**: build limpio. `rg -n "CreateBuscarModalNavigationService" WPF_Desktop/` da cero.

- [x] **W7.C** — **`DataTemplate` y autenticación: verificación, no reescritura.**
  **Archivos propios**: `Shared/DataTemplate.xaml`, `ViewModels/Usuarios/LoginUsuarioViewModel.cs`, `ViewModels/Usuarios/UsuarioViewModel.cs`.
  - `DataTemplate.xaml`: verificar que **ningún** template apunte a un tipo inexistente después de W3-W6, y que `GestionCatedrasViewModel` (de `W4.B`) esté. Huecos **preexistentes que se dejan así**: `CurriculaViewModel` (no existe `CurriculaView`), `HorarioViewModel` (template comentado en las líneas 169-173, `HorarioView` no existe) y `UsuarioViewModel` (`UsuarioView` existe pero se instancia directo en `MainWindow.xaml:128`; funciona y no se toca).
  - **El flujo de autenticación no se toca** (decisión #4 del plan): `App.xaml.cs` sigue arrancando en `MainWindow` con el login comentado y la sesión sigue en `Thread.CurrentPrincipal`. Sólo **verificar** que `LoginUsuarioViewModel` y `UsuarioViewModel` compilen contra `IServicioAutenticacion` sin `Logout` y sigan coherentes con `UsuarioResponse(UsuarioID, DocenteID, Usuario, NombreCompleto, Puesto, Roles)`. `UsuarioViewModel` se construye con `new` dentro de `MainViewModel.cs:72` y no está en el contenedor: **dejarlo así**, anotado.
  **Criterio**: build limpio. Ningún `DataTemplate` activo apunta a un tipo que no existe.

---

# W8 — Documentación de estado

- [x] **W8.A** — **Actualizar el registro vivo.** **Hecha 2026-10-03** (sólo estados y documentación, cero líneas de código; el orquestador commitea). Esta tarea cerrada **no** significa que la adaptación de `WPF_Desktop` esté cerrada: la verificación funcional sigue pendiente.
  **Archivos propios**: `docs/todos.md`, `docs/plan/wpf-desktop.md`, `docs/plan/core.md`, `docs/plan/handoff-errores.md`, `docs/revision-arquitectura.md`, `docs/roadmap.md`, y este archivo.
  Las violaciones y los IDs nuevos **ya están registrados** (`docs/todos.md`, `UI-10`…`UI-16` y violaciones 11-22, anotados 2026-10-02 junto con el plan), así que acá sólo se cambian **estados**:
  - `docs/todos.md` §WPF_Desktop: `UI-01`…`UI-05`, `UI-08`, `UI-09`, `UI-12`, `UI-13`, `UI-15`, `UI-16` → `[RESUELTO]` con su commit; `UI-10`/`UI-11` también, si `W7.A` se completó. Quedan **vigentes por decisión**: `UI-14` (diálogos, es UI/UX) y lo que el plan deja afuera.
  - `docs/todos.md` §Violaciones: marcar las que este trabajo cierra (items 15, 16, 20) y actualizar las **mitigadas-no-resueltas** (13, 14). Agregar las violaciones que los agentes hayan reportado al cerrar sus tareas.
  - **IDs nuevos a registrar**: (a) los contadores eliminados de `CursoView`/`GestionCursosView`/`GestionCurriculasView` (decisión #5); (b) el `IServiceScope` por operación de ViewModel que `W7.A` deja anotado; (c) `PlanillaAsistencia` sigue sin pantalla y `ServicioAsistencias` sin un solo caso de uso alcanzable, igual que `IServicioCatedra.AgregarHorarioAsync`/`QuitarHorarioAsync`/`ListarHorariosSegunCatedraAsync` e `IServicioUsuario.AsignarRolAsync`/`QuitarRolAsync`.
  - `docs/plan/core.md`: corregir la predicción de los 41 sitios de llamada contra lo que el compilador terminó imprimiendo.
  - `docs/plan/wpf-desktop.md` y este archivo: agregar la nota de cabecera de estado, como la tienen `core.md`, `tasks.md` y `handoff-errores.md` — **qué se ejecutó tal cual, qué cambió de ruta y qué quedó abierto**.
  - `docs/plan/handoff-errores.md` §4 (que decía "indeterminado, no cero") y `docs/roadmap.md` Fase 2.1.
  **Criterio**: ningún ID de `todos.md` queda con estado que contradiga el código. Lectura humana.

---

# Tareas de revisión (agente reviewer)

Una por ola. El reviewer **no corrige**: reporta. Todas las pruebas van a `tests/WPF_Desktop.UnitTests/` (creado en `W2.C`), con `[Trait("Categoria", "Unidad")]`, y se corren con `dotnet test tests/WPF_Desktop.UnitTests`.

**Lo que no se puede cubrir con pruebas y por qué**: los `Gestion*` que conservan `MessageBox.Show` (Alumnos, Cursos, Docentes, Licencias, Puestos, Usuarios) abren un diálogo real y cuelgan al test esperando un click. Para ésos la verificación es build + guardas `rg` + lectura de diff. Los cinco `Gestion*` reescritos **sí** son testeables, porque la decisión #2 les puso `IDialogService` por delante: se les pasa un doble que registra los mensajes en lugar de mostrarlos.

- [x] **R1 (tras W1)** — Sin pruebas todavía (no existe el proyecto). Verificar: cero `MC3066`/`CS0234`/`CS0246`; `rg -n "clr-namespace:Domain\.Curriculas\.Materias" WPF_Desktop/` → cero; `rg -n "using Domain\." WPF_Desktop/Shared/Converters/` → sólo namespaces que existen. **Medir y anotar la línea base de warnings de `WPF_Desktop`.**
- [x] **R2 (tras W2)** — Pruebas de `CicloLectivoStore` (inicializa en el año actual; el setter dispara el `event`), `CatedraStore` (guarda un `Guid`, dispara el `event`), `CatedraViewModel` (mapea cada campo de `CatedraResponse`), y de los **converters** repuntados en `W1.B` (cada valor del enum produce el texto esperado, y un valor fuera de rango no tira). Verificar que `DialogService` sea el único sitio de `Shared/` con `MessageBox`.
- [x] **R3 (tras W3)** — **La ola más valiosa de cubrir**: siete ViewModels de mapeo puro, sin servicios ni diálogos. Una prueba por ViewModel que construya el `Response` real de `Core` y afirme **campo por campo**, incluidas las trampas: `CursanteViewModel.Edad` es `int` (no `string`) y trae `CursanteID`; `CalificacionViewModel.Instancia` es `string` y `Fecha` **no** es nullable; `SituacionRevistaViewModel.Docente` queda vacío a propósito (lo llena el contenedor). Más una prueba que afirme que `CursoViewModel`, `CurriculaViewModel` y `MateriaViewModel` **no** exponen las propiedades eliminadas (por reflexión, o simplemente que el proyecto compile sin ellas). Guardas: `rg -n "Path=Divisiones|Path=Alumnos|Path=Asistencia|CargosOcupados|FechaAlta" WPF_Desktop/Views/Cursos/` → cero.
- [x] **R4 (tras W4)** — Pruebas de `GestionDivisionesViewModel` y `GestionCatedrasViewModel` con `IServicio*` dobles y un `IDialogService` doble. Lo que importa afirmar es el **remapeo de la tabla D.2**, que es el riesgo real: que eliminar una división llame a `IServicioDivision.EliminarDivisionAsync` con el `CicloLectivo` del store (y **no** con `DateTime.Today`), que crear una cátedra llame a `CrearCatedraAsync(MateriaID, DivisionID)`, y que navegar a cursantes deje la división escrita en `DivisionStore` **antes** del `Navigate()`. Guardas: `rg -n "async void|MessageBox" WPF_Desktop/ViewModels/Cursos/Divisiones/GestionDivisionesViewModel.cs` → cero.
- [x] **R5 (tras W5)** — `GestionCurriculasViewModel`: **`UI-01` es la prueba clave** — elegir el índice 0 del combo debe registrar `HorasCatedra = 1`, y el valor que el diálogo muestra debe ser el que viaja en el request (afirmarlo sobre el mismo doble de `IDialogService`); y editar la carga horaria debe enviar el valor **nuevo**, no `Materia.HorasCatedra`. Más: existe un solo camino de alta de materia (`UI-08`), y las altas/bajas de materia pegan en `IServicioMateria`, no en `IServicioCurricula`. `GestionCursantesViewModel`: **`UI-02`** — después del comando de carga, `Materias` **no** está vacía y la currícula pedida es la de `FechaFin is null`, nunca `Guid.Empty`; y `BuscarCursantes` llega a `IServicioCursante.ListarCursantesAsync` en lugar de devolver una lista vacía. Y que no exista comando de "editar nota".
- [x] **R6 (tras W6)** — `GestionSituacionRevistaViewModel`: los seis casos de uso remapeados pegan en `IServicioCatedra` con el `CatedraID` del store; designar un `Suplente` **sin** `ReemplazaA` no llega al servicio (lo frena la UI o lo reporta el diálogo, pero no falla con `SuplenciaSinReemplazoException` sin explicación); y `ListarDocentesActivosAsync` se llama **exactamente una vez** por carga, no una por fila — es afirmable contando invocaciones en el doble. Guardas: build de la solución en **0 errores**; `rg -n "Materia\.SituacionRevista|QuitarDocente\(" WPF_Desktop/` → cero; `fd . WPF_Desktop/Models` → vacío. **Hecha**: ver la Bitácora (`dd12037`, `42323aa`).
- [ ] **R7 (tras W7)** — Acá las pruebas no alcanzan: el composition root necesita la app corriendo. Verificar `rg -n "BuildServiceProvider" WPF_Desktop/` → cero y `rg -n "Startup=" WPF_Desktop/App.xaml` → cero, y después **recorrer la app en Windows** siguiendo §Verificación funcional. Lo único afirmable automáticamente, y vale la pena: construir el contenedor del `IHost` y resolver cada `*NavigationStore` dos veces, afirmando que es **la misma instancia** — la prueba que habría detectado `E.2` sin levantar la UI. **Hecha sólo en parte** (`0aa3cf0`): están las guardas y la prueba de los `Singleton`, pero **no** el recorrido de la app en Windows, que sigue pendiente. Por eso queda sin marcar.

---

## Verificación final

```bash
# Criterio de cierre
dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental   # 0 errores
dotnet build EDUSIS.sln --no-incremental                        # 0 errores (requiere W0.T cerrada)
./tests/run-tests.sh --rapidas                                  # sin regresiones en las otras capas
dotnet test tests/WPF_Desktop.UnitTests                         # suite nueva de la capa (sólo Windows)

# Guardas de arquitectura
rg -n "using Infrastructure|EntityFrameworkCore|IQueryable|\.Include\(" WPF_Desktop/ --glob '!obj' --glob '!bin'
rg -n "clr-namespace:Domain\.Curriculas\.Materias" WPF_Desktop/
rg -n "async void" WPF_Desktop/ViewModels/
rg -n "BuildServiceProvider" WPF_Desktop/
rg -n "MessageBox" WPF_Desktop/ViewModels/Cursos/
```

- La primera guarda debe dar como **única** coincidencia `App.xaml.cs:2`: el composition root es el único lugar legítimo con `using Infrastructure`.
- La segunda debe dar **cero**. **Ojo**: la guarda que propone el plan (`clr-namespace:Domain\.(Curriculas\.Materias|Cursos\.)`) está mal escrita — `Domain.Cursos.` con punto final no aparece en ningún XAML, y `Domain.Cursos` **sin** punto es legítimo en `RegistrarCursoView.xaml` porque `Grado` sigue ahí. La versión de arriba es la correcta.
- La tercera no tiene por qué llegar a cero en esta tanda, pero **no debe crecer**: cada `async void` que quede tiene que ser un handler de evento de WPF, no un caso de uso.
- La cuarta debe dar **cero** después de `W7.A`.
- **[Corregido 2026-10-03, ver corrección 13 y UI-31]** La guarda, tal como está escrita, **no da cero y está mal especificada**: `ViewModels/Cursos/` incluye `GestionCursosViewModel` y `RegistrarCursosViewModel`, que la decisión #2 manda conservar con `MessageBox.Show` (5 coincidencias cada uno; las 7 de `GestionSituacionRevistaViewModel` caen dentro de bloques `/* */`). La forma correcta apunta sólo a los cinco `Gestion*` reescritos o excluye esos dos archivos. Texto original: la quinta debía dar **cero**: los cinco `Gestion*` del clúster de cursos pasan por `IDialogService`.
- **Warnings**: la línea base se mide al cerrar W1 (la de W0, 7, es parcial: se tomó con 18 errores presentes y Roslyn nunca ligó los cuerpos). No debe crecer por código nuevo. `Domain` 50 / `Core` 28 / `Infrastructure` 7 no deben cambiar **en absoluto**: están fuera de alcance.

### Verificación funcional en Windows — lo único que cierra la tarea de verdad

`dotnet run --project WPF_Desktop/WPF_Desktop.csproj`. Requiere la base de datos, que **todavía no está instalada** (`H-023`: la migración sigue siendo sólo `InitCreate` y no refleja el mapeo de `ff2de55`).

0. **La app arranca, muestra `MainWindow` y la navegación inicial pinta la pantalla de inicio.** Es la verificación de `W7.A`: si la ventana sale vacía o los botones del menú no cambian la vista, el problema es el composition root, **no** los ViewModels reescritos. Comprobar este paso **antes** de cualquier otro y antes de culpar a las olas W3-W6.
1. Regenerar la migración: `dotnet ef migrations add <Nombre> --project Infrastructure --startup-project WPF_Desktop` y `dotnet ef database update`. La cadena está hardcodeada en `Infrastructure/InfrastructureDI.cs` (`localhost`/`EdusisDB`, Integrated Security).
2. Curso → Divisiones: agregar, asignar y quitar preceptor, eliminar.
3. Curso → Diseño curricular: crear currícula, alta/edición/baja de materia. Verifica `UI-01`: la carga horaria que muestra el diálogo es la que se persiste, y editarla **tiene efecto**.
4. Materia → Cátedras: crear cátedra para una división, designar titular, poner en funciones, relevar, designar suplente con `ReemplazaA`.
5. División → Cursantes: inscribir alumno, listar (verifica `UI-02`), cargar y quitar calificación.

Si la base no se puede instalar en esta tanda, el criterio de aceptación se limita a los builds, las pruebas y las guardas, y la verificación funcional queda **anotada como pendiente — sin declarar la tarea cerrada**.

---

## Bitácora

**Única fuente de verdad del progreso.** Una línea por tarea cerrada, agregada por el orquestador después de verificar el criterio y commitear. Si una tarea no está acá, **está sin hacer**, aunque el código parezca tocado — un agente puede haber dejado trabajo a medias y haber sido interrumpido.

| Fecha | Tarea | Commit | Nota |
|---|---|---|---|
| 2026-10-02 | `W0` (línea base) | — | Medida sobre `f4f57fb`: 1 `MC3066` + 18 errores de declaración + 6 `CS1061` de `TEST-07`. Warnings de `WPF_Desktop`: 7, **parcial**. Otras capas: `Domain` 50, `Core` 28, `Infrastructure` 7, **0 errores** |
| 2026-10-02 | `W0.T` | `dd89f93` | `TEST-07` cerrado. `dotnet build tests/EDUSIS.EndToEndTests --no-incremental` → **0 errores**, 85 advertencias (todas de otras capas, = línea base). `CursoBuilder` **no** se tocó: ya no tenía `ConDivision`, así que la corrección 6 del plan era obsoleta. Se agregó `ColeccionE2E.SembrarDivisionAsync`. La prueba E2E no se ejecutó (sin Docker ni `EDUSIS_TEST_SQLSERVER`), que es el resultado correcto |
| 2026-10-02 | `W1.A` | `cae617f` | `UI-09` cerrado. **Cero `MC3066`**: el proyecto llega por primera vez a la etapa de C# desde el refactor. Guarda `rg -n "clr-namespace:Domain\.Curriculas\.Materias" WPF_Desktop/` → cero. `SituacionRevistaView` tenía el xmlns a `Domain.Curriculas` sin usar y se eliminó |
| 2026-10-02 | `W1.B` | `ea2f2d3` | Los 2 errores de los archivos propios cerrados (18 → 16 errores de declaración), verificado con el comando de criterio filtrado. `GradoConverter` conserva `using Domain.Cursos` a propósito |
| 2026-10-02 | `W1.D` | `942cedb` | Ocho repuntes de `using` de DTOs, sin tocar ningún namespace de servicio. `CursoViewModel`, `GestionCursosViewModel` y `RegistrarCursosViewModel` no necesitaron cambios |
| 2026-10-02 | `W1.C` | `0f50d32` | Cerrada **al segundo intento**. El primero reemplazó `using Core.ServicioCurriculas;` (namespace del **servicio**, fuera de alcance) y dejó 2 `CS0246` por `IServicioCurricula` en las líneas 25 y 109, con reporte de criterio en verde: el agente midió antes de su última edición. Devuelta y corregida. Ver corrección 8 por los 6 `CS0246` que quedan en ese archivo y **no** son de esta tarea |

**Cierre de ola W1 (2026-10-02).** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **cero `MC3066`**, **cero `CS0234`**, 40 errores distintos: 31 `CS1061`, 6 `CS0246`, 2 `CS0029`, 1 `CS7036`. Los 6 `CS0246` son los de la corrección 8 y los cierra `W6.A`, así que el criterio de ola se cumple con esa salvedad documentada. La secuencia prevista (`1` → `18` → `~45-60` → `0`) se cumple en la franja baja: 40, no 45-60.

**Línea base real de advertencias de `WPF_Desktop` = 328** (sustituye al "7 parcial" de W0; es la primera vez que la capa liga cuerpos de método). Desglose de los códigos con más peso: 130 `CS8618`, 76 `CS8622`, 32 `CS8625`, 16 `CS8603`, 14 `CS8602`, 11 `CS0169`, 9 `CS8604`, 7 `MVVMTK0034`, **7 `CS4014`**, 5 `CS0168`, 5 `CS0108`. Las otras capas quedaron intactas: `Domain` 50, `Core` 28, `Infrastructure` 7.

| 2026-10-02 | `R1` | — | **Sin defectos que obliguen a rehacer nada de W1.** Verificó tipo por tipo, abriendo cada DTO en `Core/` y cada enum en `Domain/`, que los 21 archivos del diff coinciden con los archivos propios de cada tarea y que ningún repunte apunta a un namespace equivocado. Confirmó de forma independiente los 40 errores, los 6 `CS0246` de la corrección 8 y las cuatro guardas. Cuatro hallazgos de severidad baja, abajo |

| 2026-10-02 | `W2.A` | `8482e35` | `CicloLectivoStore` (año en curso), `CatedraStore` (`Guid`) y `CatedraViewModel`. Verificado contra el DTO real: `CatedraResponse` tiene los 7 campos que la tarea predijo, mapeados con el tipo correcto (incluido `Guid?` en `DocenteEnFuncionesID`). Los dos stores calcan el patrón de `CursoStore` pero por valor/`Guid`; el ctor del ViewModel usa el mismo guard `if (x is not null)` que `MateriaViewModel` |
| 2026-10-02 | `W2.B` | `fcc5c8b` | Contrato de **cuatro** métodos, no tres: se agregó `MostrarAdvertencia` porque 9 de las llamadas vivas usan `MessageBoxImage.Warning`. Relevamiento: **62 llamadas vivas** a `MessageBox.Show` en los `Gestion*` a reescribir (20 error, 18 información, 15 confirmación, 9 advertencia), que coincide con los 19+18+25 del plan. `DialogService` pasó a `internal` y es la única clase de la capa con `MessageBox`. Cero consumidores del `OpenDialogService` viejo; el registro de DI de la línea 174 sigue válido. **Ojo**: las 11 confirmaciones que hoy usan ícono `Information` pasan a `Question` al migrar |
| 2026-10-02 | `W2.C` | `5d3bde2` | Proyecto creado y `InternalsVisibleTo` agregado con la forma de `Core.csproj`. **Criterio parcialmente pendiente, no es un defecto**: ver corrección 12. Git ajustó la ruta de disco `Tests/` a `tests/` en el índice, como anticipa `CLAUDE.md` |

| 2026-10-02 | `W2.DI` | `8d12f42` | Los dos stores registrados como `AddSingleton` en `AddStores`. `IDialogService` ya apuntaba a `DialogService` y no hizo falta tocarlo |

**Cierre de ola W2 (2026-10-02).** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **40 errores únicos, sin cambio** respecto del cierre de W1: la ola fue íntegramente aditiva, que es lo esperado. `dotnet build EDUSIS.sln --no-incremental` → los mismos 40, o sea que `TEST-07` sigue cerrado y `WPF_Desktop` es el único proyecto con errores. `./tests/run-tests.sh --rapidas` en verde (191 pasadas, 1 omitida, 0 con error).

| 2026-10-02 | `R2` | `5b8b47e` | Sin defectos que obliguen a rehacer W2. Primera revisión con el mandato doble de la decisión #6: revisó por lectura **y** escribió 37 pruebas en 7 archivos (los dos stores, `CatedraViewModel` campo por campo más una prueba de reflexión contra `CatedraResponse`, y los dos converters de `W1.B`), sin ejecutarlas. Encontró un **bloqueo real para `R4`…`R6`**, resuelto en este mismo commit: ver abajo |

| 2026-10-02 | `W3.A` | `c99ca83` | Contadores de curso eliminados del ViewModel, de `CursoView` y de las dos columnas de `GestionCursosView`. `CursoResponse(CursoID, Grado, NivelEducativo)` confirmado contra el DTO real |
| 2026-10-02 | `W3.B` | `cb265b6` | `CargosOcupados` y `SituacionRevista` fuera de `MateriaViewModel`, contador `Materias` fuera de `CurriculaViewModel`, dos bloques fuera de `MateriaView`. `EstaActiva` conservada. **`DataTemplate.xaml` no requirió cambios**: ningún template apuntaba a un tipo eliminado |
| 2026-10-02 | `W3.C` | `4522005` | Cerrada **al segundo intento**: ver abajo. Cierra la violación #5 del plan (`ObservableCollection<MateriaResponse>` → `<MateriaViewModel>`) y migra `CalificacionViewModel` a `ObservableValidator` |
| 2026-10-02 | `W3.D` | `d59d319` | Los 9 campos de `SituacionRevistaResponse` mapeados; `Docente` conservada sin llenar para W6.A. `EnFunciones` ya existía como propiedad |

**Cierre de ola W3 (2026-10-02).** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **32 errores únicos** (40 → 32; 50 `CS1061` + 12 `CS0246` + 2 `CS7036` en 64 líneas, deduplicadas). Primera ola que baja el conteo. **Los 32 restantes tienen todos un dueño asignado, sin huérfanos**: 19 en `GestionSituacionRevistaViewModel` (`W6.A`), 7 en `GestionDivisionesViewModel` (`W4.A`), 4 en `GestionCurriculasViewModel` (`W5.A`), 1 en `GestionCursantesViewModel` (`W5.C`) y 1 en `GestionDocentesViewModel` (`W6.D`). Los 13 archivos tocados son exactamente los de las cuatro tareas (3+3+5+2) y todos quedaron en CRLF consistente.

| 2026-10-02 | `R3` | `7fc4bf5` | **Sin defectos que obliguen a rehacer W3.** Verificó campo por campo, contra los `record` de `Core/`, que los siete ViewModels asignan todos los campos de su `Response` — ningún otro caso como el de `CursanteID`. Escribió 7 archivos de prueba, uno por ViewModel, sin ejecutarlos. Aprobó los dos cambios de XAML que `W3.C` hizo fuera de guion. **Dos hallazgos accionables para las olas siguientes, abajo** |

| 2026-10-02 | `W4.A` | `d985e09` | Las 5 llamadas remapeadas a `IServicioDivision`, 19 `MessageBox` a `IDialogService`, y **cuatro defectos cerrados**: el `DivisionStore` descartado que perdía la división al navegar, los 3 `CargarDivisionesAsync()` sin `await`, el `CanExecute` roto y la propiedad inasignable `DivisionStore.Curso`. Las firmas reales de `IServicioDivision` coincidieron con la tarea. **El agente rechazó con evidencia una instrucción del orquestador** y tenía razón: ver la corrección al hallazgo 1 de `R3` |
| 2026-10-02 | `W4.B` | `9fd6b6b` | La pantalla nueva `GestionCatedras` completa. Cerrada **al segundo intento**: la primera versión exponía `ObservableCollection<DivisionResponse>` y `DivisionResponse` como propiedades bindeables, reintroduciendo la violación #5 del plan en la única pantalla nueva, tres commits después de que `W3.C` la cerrara en `CalificacionViewModel`. Corregida a `DivisionViewModel`, que ya existía. La letra de división se resuelve con **un solo** listado más un diccionario, sin N+1 |
| 2026-10-02 | `W4.DI` | `30a3753` | Factory de `GestionDivisionesViewModel` actualizada y `GestionCatedrasViewModel` registrado con su `NavigationService`. El orden de los dos `INavigationService` se confirmó contra el `<remarks>` del constructor |

**Cierre de ola W4 (2026-10-02).** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **25 errores únicos** (32 → 25). Todos con dueño: 19 en `GestionSituacionRevistaViewModel` (`W6.A`), 4 en `GestionCurriculasViewModel` (`W5.A`), 1 en `GestionCursantesViewModel` (`W5.C`) y 1 en `GestionDocentesViewModel` (`W6.D`). Guardas de `W4.A` en cero: ni `MessageBox`, ni `async void`, ni `DateTime.Today`/`DateTime.Now` en el ViewModel.

| 2026-10-02 | `R4` | `b57834c` + `99f627b` | **Tres defectos reales, todos invisibles al compilador**, corregidos en `b57834c`. Escribió 34 pruebas en 2 archivos más un doble manual de `INavigationService` con captura de orden. Verificó el remapeo D.2 línea por línea contra las firmas de `Core/ServicioDivisiones/` y auditó los bindings **grilla por grilla** |

| 2026-10-02 | `W5.C` | `0059ebe` | `UI-02` cerrado, las dos mitades. Reescrito como `ObservableValidator` con `IAsyncRelayCommand`: era el último consumidor de `ViewModelCommand` y del `INotifyDataErrorInfo` manual. Las calificaciones llegan a `Core` por primera vez (alta de nota, inasistencia a examen, quitar, editar observación; **sin** "editar nota"). Quitó el `IServicioCurso` muerto de `CalificacionViewModel`, sus dos sitios de llamada y la prueba de `R3` que lo construía. **Paró y preguntó** al topárselo, en lugar de salirse de carril: ver abajo |
| 2026-10-02 | `W5.A` | `86da194` | Dependencia partida (`IServicioMateria` entra, `IServicioDocente` sale), `UI-03` y `UI-08` cerrados (un solo `RegistrarMateriaRequest` en el archivo), 4 `async void` a `Task`, 18 `MessageBox` a `IDialogService`, primer consumidor de `GestionCatedrasNavigationService`. Cerrada **al segundo intento**: los tres `+1` se quitaron cuando `W5.B` arregló el binding. Agregó los `[NotifyCanExecuteChangedFor]` que faltaban en cuatro propiedades |
| 2026-10-02 | `W5.B` | `003b0ac` | El arreglo de fondo de `UI-01`, con alcance ampliado por el orquestador a `MateriaView.xaml` (huérfano de `W3.B`). Auditó **50** usos de `Style`/`StaticResource` (todos con el `TargetType` correcto) y los bindings grilla por grilla |
| 2026-10-02 | `W5.DI` | `160d37a` | Las dos firmas registradas; el segundo `INavigationService` de currículas pasa a cátedras. Orden confirmado leyendo el constructor |

**Cierre de ola W5 (2026-10-02).** `dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → **20 errores únicos** (25 → 20). Los 20 son 19 en `GestionSituacionRevistaViewModel` (`W6.A`) y 1 en `GestionDocentesViewModel` (`W6.D`): **W6 es el build verde**. Guardas de `W5.A` y `W5.C` en cero (ni `MessageBox`, ni `async void`, ni `IServicioDocente`, ni `Guid.Empty`, ni `ViewModelCommand`, ni el regex `^(20)`).

**`UI-01` era un diagnóstico equivocado del plan, no una tarea mal ejecutada.** El plan lo describía como "unificar el offset de carga horaria" entre el diálogo (`CargaHoraria + 1`) y el request (`CargaHoraria`). Pero el `+1` existía para compensar que el `ComboBox` ligaba **`SelectedIndex` —0-based, un detalle del control— a `HorasCatedra`, que son horas reales**: una materia de 3 horas mostraba "4". Unificar el offset en `+1` arregla la visualización y rompe la edición (guardar sin tocar suma una hora cada vez); unificarlo en 0 hace lo inverso. **No existe valor de offset que arregle las dos cosas, porque el offset es el síntoma.** La corrección real fue cambiar qué propiedad bindea el combo: `ItemsSource` sobre un `x:Array` de `Int32` 1-5 más `SelectedItem`, y el offset desaparece de toda la cadena. Invariante que queda: *el número que el combo muestra es el que viaja en el request y el que se persiste, sin suma ni resta en ningún punto*.

> **Y el arreglo destapó un defecto que el binding roto venía tapando.** Con `SelectedIndex`, el `IsSelected="True"` del primer ítem empujaba un valor al ViewModel al cargar la vista, así que el `0` inicial de `CargaHoraria` nunca se veía. Con binding por valor, 0 queda fuera del rango 1-5: el combo aparecería vacío y registraría `HorasCatedra = 0`, que `Materia` rechaza con `ArgumentException`. Lo detectó `W5.B` y lo cerró `W5.A` poniendo el valor inicial y el reset en 1. **Es el patrón de toda esta capa: cada corrección revela lo que la anterior enmascaraba.**

**Contraste de proceso que vale conservar.** `W5.C` se topó con el mismo dilema que `W3.A` —necesitaba editar un archivo ajeno para terminar— y resolvió al revés: **paró y preguntó**. El resultado concreto es que encontró un consumidor que el orquestador no había visto (la prueba `CalificacionViewModelTests.cs:31`, escrita por `R3`), y que habría quedado roto si el cambio se hubiera hecho a ciegas sobre los tres archivos previstos. Parar no sólo respeta la regla: pone a la vista el grafo de dependencias que el que reparte las tareas no tiene completo.

**Deuda nueva registrada en W5:**

1. **`CalificacionView.xaml` quedó sin consumidores.** `W5.C` armó el formulario de alta en línea porque esa vista tiene `Materia` como `TextBox` libre y el ViewModel necesita un `MateriaID`. Su `DataTemplate` (`Shared/DataTemplate.xaml:140-141`) sigue vivo y sólo se activaría si algún `ContentControl` mostrara un `CalificacionViewModel`. **No se borra** (FR-013); queda como deuda para `W8.A`.
2. **`CLAUDE.md` quedó desactualizado**: describe `INotifyDataErrorInfo` manual y `ViewModelCommand` como patrón vigente, y `GestionCursantes` era su último consumidor. Candidato para `W8.A`.
3. **La UI conoce el enum `Instancia` de `Domain`** vía `ObjectDataProvider`, y `Core` no expone las instancias válidas: viajan como `string` y `Core` hace `Enum.Parse` en `ServicioCursante`. Es la violación #1 del plan en acción. Preexistente.
4. `CommandParameter="SituacionRevista"` sigue vivo en `GestionSituacionRevistaView.xaml:64`, que es de **`W6.B`**: el criterio literal de `W5.B` no llega a cero hasta esa ola.

| 2026-10-03 | `W6.A` | `17ece52` + `ab4cfbe` | Los **19** errores cerrados: 6 eran `CS0246` por requests que `Core` **eliminó al renombrar** los casos de uso (corrección 8), así que había que reprogramar las seis llamadas, no repuntarlas. Nombre del docente con **una** llamada y diccionario. Cadena de suplencias con guarda previa. Cerró además el bloque "Docente en Funciones", que estaba **permanentemente oculto** |
| 2026-10-03 | `W6.B` | `ab4cfbe` | Control de suplencia agregado (sin él, designar un suplente fallaba con `SuplenciaSinReemplazoException`), binding de la línea 193 repuntado, 49 `StaticResource` auditados. **Dejó intacto el `FechaAlta` de la línea 482** tras verificar que su grilla bindea `LegajoDocenteViewModel`: es el falso positivo de la nota de `R3`, ahora evitado |
| 2026-10-03 | `W6.C` | `7d5167b` | `UI-04` y `UI-05`. **La inscripción de alumno llega a `Core` por primera vez**: antes el request se construía y nunca se usaba. Las dos vistas estaban peor que lo descrito — `InscripcionAlumnoView.xaml` estaba copiada de una pantalla de búsqueda y usaba un converter no definido que habría lanzado excepción al cargar |
| 2026-10-03 | `W6.D` | `1681f7f` | `D.3` (último error fuera del clúster), `UI-06` cerrado con decisión, `Models/` eliminada, `namespace` agregado, y un botón de **baja** que quedaba habilitado sin selección |
| 2026-10-03 | `W6.DI` | `4b7165b` | **BUILD VERDE.** Punto `9.2` cerrado **por verificación y no por cambio**: ninguno de los seis ViewModels señalados recibe `INavigationService` por constructor |
| 2026-10-03 | (orquestador) | `6202b72` | Dos `await` que faltaban, en métodos que ya eran `async`. `CS4014` baja de 3 a 1 |

| 2026-10-03 | `W7.A` | `0a24f2d` | **`E.1` a `E.4` cerrados**: un solo contenedor y un solo punto de arranque. Cadena verificada de punta a punta por el orquestador — ver abajo |
| 2026-10-03 | `W7.B` | `081f92f` | `CreateBuscarModalNavigationService` y `BuscarNavigationService.cs` eliminados, `PackageReference` de `MediatR` fuera. `NavigationService.cs` y `NavigationWithParameterService.cs` **se conservan** (FR-013). Build y advertencias sin cambio |
| 2026-10-03 | `W7.C` | — | **Verificación pura: ningún archivo tocado**, que es el resultado correcto. Los **34** `DataTemplate` activos apuntan a tipos que existen. Autenticación coherente con `UsuarioResponse` campo por campo, sin `Logout`. Hallazgos abajo |

## CIERRE DE OLA W7 — EL COMPOSITION ROOT (2026-10-03)

**La cadena que cierra `E.3`, verificada a mano por el orquestador y no por el agente** (que declaró honestamente no haber abierto los dos últimos eslabones):

1. `OnStartup` resuelve `MainWindow` de `AppHost.Services` (`App.xaml.cs:73`).
2. `OnStartup` resuelve `INavigationService` del **mismo** proveedor (`:74`).
3. La factory de ese servicio es `CreateMainNavigationService`, que pide `MainWindowNavigationStore` (`WPF_DesktopDI.cs:220`).
4. Ese store está registrado `AddSingleton` (`WPF_DesktopDI.cs:53`), así que hay **una sola instancia por raíz**.
5. `MainViewModel` recibe ese mismo store por constructor (`MainViewModel.cs:42`).

Una raíz, un Singleton: **la ventana observa lo que `Navigate()` escribe**, y `Navigate()` corre **antes** de `Show()`. Antes la ventana salía del contenedor A y el servicio del B, con dos stores distintos, y por eso la ventana podía quedar vacía.

**Guardas de `W7.A`**: `rg "BuildServiceProvider" WPF_Desktop/` → **0**. `rg "Startup=" WPF_Desktop/App.xaml` → **0**. `rg "StopAsync" WPF_Desktop/App.xaml.cs` → **1**, en `OnExit`. Build en **0 errores**; advertencias de `WPF_Desktop` **287 → 282**.

### Intercambio que `W7.A` introduce, y que hay que saber

Con **un** contenedor, los servicios `Scoped` de `Core` resueltos del proveedor raíz comparten **un solo `DbContext` de vida de aplicación** entre todos los ViewModels. Antes había dos contenedores y por lo tanto dos contextos: igual de mal, pero distinto. El riesgo ahora es `"a second operation was started on this context"` si dos cargas se solapan, y un `ChangeTracker` que crece sin límite dejando datos obsoletos en pantalla. **Arreglar la navegación hizo visible un problema de ciclo de vida que estaba tapado por uno peor.** El arreglo correcto —un `IServiceScope` por operación de ViewModel— es un cambio de diseño que el plan excluye explícitamente, y `W8.A` lo registra como ID nuevo.

### Hallazgos de `W7.C`, ninguno corregido por decisión

1. **`LoginUsuarioViewModel`: `CanExecuteRegistrarCommand` lee `!HasErrors`, pero `_usuario` y `_clave` no notifican al comando.** El botón arranca habilitado (`HasErrors == false`) y **nunca se reevalúa**. No es un botón muerto sino un `CanExecute` obsoleto; el método valida los campos vacíos igual. **No se corrige**: el login está fuera del flujo y la decisión #4 prohíbe tocar autenticación. El arreglo sería un `ErrorsChanged += (_, _) => RegistrarCommandAsync.NotifyCanExecuteChanged()` en el constructor.
2. **`LoginRequest` recibe un `NetworkCredential` de `System.Net`**: un detalle de plataforma dentro de un DTO de `Core`. Preexistente, de `Core`, fuera de alcance.
3. **`LoginUsuarioViewModel` construye `NetworkCredential`, `BasicIdentity`/`CustomPrincipal` y asigna `Thread.CurrentPrincipal` dentro del ViewModel**: mezcla gestión de sesión con presentación. Cubierto por la decisión #4.
4. **`CalificacionView` confirmada sin consumidores**: ningún `.cs` ni `.xaml` muestra un `CalificacionViewModel` en un `ContentControl`. Su `DataTemplate` (`DataTemplate.xaml:140-141`) sigue vivo. `GestionCursantesViewModel` sí usa el ViewModel, pero como datos de su formulario en línea. No se borra (FR-013).
5. Huecos preexistentes de `DataTemplate`, dejados tal cual: `CurriculaViewModel` (no existe `CurriculaView`), `HorarioViewModel` (template comentado, no existe `HorarioView`) y `UsuarioViewModel` (`UsuarioView` existe, pero el ViewModel se instancia con `new` en `MainViewModel.cs:72` y no está en el contenedor).
6. **`BuscarViewModel` y `BuscarView` quedan sin ningún camino de navegación** tras la limpieza de `W7.B`. No es regresión: el único método que los alcanzaba nunca se invocaba. El borrado hace explícito lo que ya era inalcanzable.

## CIERRE DE OLA W6 — BUILD VERDE (2026-10-03)

**`dotnet build WPF_Desktop/WPF_Desktop.csproj --no-incremental` → 0 errores. `dotnet build EDUSIS.sln --no-incremental` → 0 errores.** Verificado de forma independiente por el orquestador. Secuencia completa: `1` (markup) → `18` → `40` → `32` → `25` → `20` → `1` → **`0`**.

**Advertencias de `WPF_Desktop`: 287**, contra los 328 de la línea base de W1. Peso: 101 `CS8618`, 80 `CS8622`, 29 `CS8625`, 15 `CS8603`, 14 `CS8602`, 11 `CS0169`, 8 `CS8604`. **`CS4014` bajó de 7 a 1**, que era la métrica que el cierre de W1 pidió vigilar. El que queda es `GestionCursosViewModel:198`, dentro de `ExecuteListarCommand(object)`, que es `void`: esperarlo exige convertir el método a `Task` y cambiar el tipo del comando, lo que cascadea al XAML y al contenedor. **No es una omisión de `await` sino un comando mal tipado**, y queda como deuda.

**Guardas de §Verificación final, medidas al cerrar W6:**

| Guarda | Resultado |
|---|---|
| `using Infrastructure`, `EntityFrameworkCore`, `IQueryable`, `.Include(` | **1**, `App.xaml.cs:2` — el composition root, que es lo esperado |
| `clr-namespace:Domain.Curriculas.Materias` | **0** |
| `async void` en `ViewModels/` | **6**. Cinco son handlers de comando (`GestionAlumnosViewModel:82` y `:141`, `PerfilAlumnoViewModel:168`, `RegistrarAlumnoViewModel:176`, `RegistrarDocenteViewModel:67`). El sexto, `PerfilAlumnoViewModel:74` (`public async void CargarPerfil()`), **no es un handler**: es un caso de uso público que no se puede esperar y se traga sus excepciones. Queda como deuda |
| `MessageBox` en `ViewModels/Cursos/` | **no da cero, y la guarda está mal especificada**: ver corrección 13 |

### Corrección 13 — la cuarta guarda de §Verificación final contradice la decisión de disección #2

La §Verificación final pide que `rg -n "MessageBox" WPF_Desktop/ViewModels/Cursos/` dé **cero**, "porque los cinco `Gestion*` del clúster de cursos pasan por `IDialogService`". Pero la **decisión #2** dice que los cinco reescritos son Divisiones, Curriculas, Catedras, SituacionRevista y Cursantes, y que los `Gestion*` que el plan **no** reescribe —entre ellos **Cursos**— *conservan* `MessageBox.Show`. La ruta `ViewModels/Cursos/` incluye `GestionCursosViewModel.cs` y `RegistrarCursosViewModel.cs`, que son precisamente los que la decisión excluye.

**El código es correcto; la guarda es inconsistente con el propio documento.** La guarda correcta excluye esos dos archivos, o apunta sólo a los cinco reescritos. Es el **cuarto** criterio mal especificado de la tanda (con los de `W2.C`, `W3.D` y `W5.B`), y los cuatro por el mismo motivo: **miden una propiedad global desde un alcance que no la controla**.

Estado real verificado: `MessageBox` vivo queda sólo en `RegistrarCursosViewModel` (5) y `GestionCursosViewModel` (5), los dos permitidos por la decisión #2. Las 7 coincidencias de `GestionSituacionRevistaViewModel` caen dentro de los bloques `/* */` de 293-325 y 565-593, verificados con `grep -n` sobre los delimitadores.

### Deuda nueva registrada en W6

1. **`DesignarDocenteAsync` + `PonerEnFuncionesAsync` son dos transacciones.** El método de `Core` no recibe `EnFunciones`, así que `W6.A` llama a `PonerEnFuncionesAsync` con el `Guid` devuelto. Si la segunda falla, la designación queda persistida y el usuario ve un error. Lo correcto sería un solo caso de uso en `Core`.
2. **`SituacionRevistaUPDATE` cargaba dos roles incompatibles** —`DataContext` del bloque de solo lectura y `SelectedItem` de la grilla—, así que el bloque afirmaba "en funciones" sobre la fila que el usuario clickeara. Corregido repuntando el bloque a `SituacionRevistaEnFunciones`; el ViewModel **sigue** asignando `SituacionRevistaUPDATE = SituacionRevistaEnFunciones` tras cada carga, lo que preselecciona la fila en funciones. Es coherente pero mantiene la mezcla de roles.
3. **`LegajoStore.PersonaID` es un `Guid` no nulable que arranca en `Guid.Empty`**: cuarto centinela de la tanda con la misma forma. Si alguien llega a `InscripcionAlumnoView` sin alumno elegido, el request viaja con `Guid.Empty` y `Core` lo rechaza; la UI no lo valida.
4. **`ExecuteContinuarCommand` de `RegistrarAlumnoViewModel` es `async void`** y construye `RegistrarDatosPersonalesRequest`, `RegistrarDomicilioRequest` y `RegistrarContactoRequest` que **nunca usa**. `ContinuarCommand` tampoco tiene `[NotifyCanExecuteChangedFor]`, y su `CanExecute` lee `HasErrors` de tres sub-ViewModels: nunca se reevalúa.
5. **El alta de alumno y su inscripción son dos agregados en dos pasos sin transacción conjunta**: si falla la inscripción, el alumno queda registrado y sin curso.
6. **En "finalizar designación", el reset posterior a la recarga se ejecuta también si el usuario cancela** la confirmación, dejando seleccionada la situación en funciones. Cambio menor de comportamiento, aceptado.

**Los tres defectos que `R4` encontró, ninguno detectable por el build:**

1. **`GestionCatedrasView.xaml:112` aplicaba el estilo `Body` (`TargetType=Label`) a un `TextBlock`.** WPF lanza `InvalidOperationException` al instanciar la plantilla — y **sólo cuando hay al menos una cátedra**: con la lista vacía el `ItemTemplate` no se instancia, así que una prueba de humo de navegación lo deja pasar. Corregido a `TextBlock_Body`. **Convención del repo descubierta acá: hay un `TextBlock_*` por cada estilo de `Label`** (`Theme/EDUSIS_TextBlock.xaml`), así que aplicar un estilo de `Label` a un `TextBlock` siempre tiene un equivalente correcto. Se auditaron los 10 usos de recurso del archivo; el resto coincidía.
2. **`GestionDivisionesView.xaml:387` bindeaba `HabilitarListaDocente`** y la propiedad es `HabilitarListaDocentes`. Preexistente (venía de `377b12a`). Un binding roto no falla el build: `IsEnabled` quedaba en su valor por defecto.
3. **`Division` no tenía `[NotifyCanExecuteChangedFor]`, y eso volvía inalcanzable el arreglo de `W4.A`.** Los `IRelayCommand` de CommunityToolkit **no** se enganchan a `CommandManager.RequerySuggested`: su `CanExecute` se reevalúa sólo si alguien llama a `NotifyCanExecuteChanged()`. Los botones de eliminar y navegar quedaban deshabilitados aunque el usuario seleccionara una fila, así que escribir el store antes de `Navigate()` era correcto **y a la vez imposible de ejercitar**. Agregadas las notificaciones a `EliminarCommandAsync` y `NavigationCommand`, los dos únicos `CanExecute` que leen `Division`.

> **Método de revisión de XAML que funcionó, y conviene repetir en `W5.B` y `W6.B`**: `R4` no barrió el archivo por nombre de propiedad —el error que produjo el falso positivo de la línea 401— sino **grilla por grilla**, determinando el `ItemsSource` de cada `DataGrid` antes de juzgar sus columnas. Con ese método confirmó que la 401 es correcta y encontró la 387, que un barrido por nombre no ve.

**Defecto pendiente que `R4` y `W4.A` reportaron y NO se corrigió** (fuera del alcance de la ola, para quien tome el archivo): `CanExecuteListarCommand` lee `!HasErrors` en la rama "Buscar", pero `Query` tiene `[NotifyDataErrorInfo]` sin `[NotifyCanExecuteChangedFor(nameof(ListarCommandAsync))]`. Con un texto de búsqueda inválido, el botón Buscar **no se deshabilita**. Es el mismo modo de fallo del defecto 3. Hay además una prueba con `Skip` en `GestionDivisionesViewModelTests` que documenta el defecto 3 ya corregido: **quitarle el `Skip` en la pasada de W6**.

**Defecto latente corregido por el orquestador, fuera de los archivos de W4**: `DivisionViewModel._preceptorID` (de `W3.C`) era `Guid?` pero se inicializaba en `Guid.Empty`, así que una división sin preceptor daba `PreceptorID is not null == true` y habilitaba "quitar preceptor" sin preceptor. Un `Guid?` inicializado en `Guid.Empty` crea un segundo valor que significa lo mismo que `null`. Corregido en una línea, commiteado con `W4.A`. Lo encontró el agente de `W4.A` al escribir su `CanExecute`.

**Corrección al método de medición**, reportada por `W4.DI` y verificada: un `grep error` **laxo** captura 14 líneas de warnings `MVVMTK0034`, porque su texto contiene la palabra "error". El filtro correcto es `grep ': error'` con dos puntos, que es el que usa §Reglas de ejecución y el que el orquestador usó en toda la tanda.

**Pendientes que `W5.DI` tiene que absorber**, reportados por `W4.DI`: `CreateGestionCatedrasNavigationService` todavía no tiene consumidores — `W5.A` debe hacer que `GestionCurriculasViewModel` navegue a cátedras y no a situación de revista, y el registro de `GestionCurriculasViewModel` sigue inyectando el `IServicioDocente` que `W5.A` manda quitar. Hay además 7 warnings `MVVMTK0034` preexistentes (accesos directos a campos `[ObservableProperty]`, que se saltean la notificación de cambio) en `GestionAlumnosViewModel`, `PerfilAlumnoViewModel` y `GestionCurriculasViewModel`.

**Hallazgos de `R3` que las olas siguientes tienen que absorber — leer antes de tomar `W4.A` y `W5.C`:**

1. **`W4.A`: hay un binding roto que ninguna guarda detecta.** `Views/Cursos/Divisiones/GestionDivisionesView.xaml:229` hace `Binding Path=Docente` en la columna "Preceptor" de un `DataGrid` cuyo `ItemsSource` son `DivisionViewModel`. La propiedad pasó a llamarse `Preceptor` en `W3.C`, así que **la columna queda vacía en silencio**: un binding roto en WPF no falla el build. La guarda `rg` de `R3` no lo ve porque busca `Path=Alumnos`, que está en la línea 232. `W4.A` tiene que cambiar **las dos**: `Docente`→`Preceptor` (línea 229) y `Alumnos`→`Cursantes` (línea 232).

   > **Corrección al hallazgo, verificada al ejecutar `W4.A`**: la versión original de esta nota incluía también el `FechaAlta` de la línea 401, y **era un falso positivo**. Esa columna pertenece a un `DataGrid` anidado de **docentes**, cuyos items son `LegajoDocenteViewModel`, que expone `FechaAlta` mapeada desde `LegajoDocenteResponse.FechaInicio` y **no** tiene `FechaInicio`. El binding es correcto y cambiarlo lo habría roto; el agente de `W4.A` se negó a aplicarlo y tenía razón. **Lección de método**: `GestionDivisionesView.xaml` tiene varios `DataGrid` anidados con `ItemsSource` distintos, así que un barrido por nombre de propiedad no alcanza — la pregunta no es "¿este nombre cambió?" sino "¿sobre qué tipo bindea esta grilla?". El `FechaAlta` de `GestionSituacionRevistaView.xaml:482` sigue siendo de `W6.B` y hay que verificarlo con el mismo criterio antes de tocarlo.
2. **`W5.C`: `Instancia` se quedó sin valor por defecto, y eso rompe el alta de calificación.** Antes el `ComboBox` usaba `SelectedIndex` y arrancaba en "Parcial"; ahora `CalificacionViewModel.Instancia` arranca en `string.Empty` y `SelectedItem` no selecciona nada (`CalificacionView.xaml:59`). Si el usuario no elige, el request viaja con `""` y `Core` lo rechaza con `ArgumentException("La instancia especificada no existe.")` (`ServicioCursante.cs:158`). Es un cambio de comportamiento que el plan no anticipa: `W5.C` tiene que inicializar en `"Parcial"` o validar antes de enviar.
3. `W5.C` además: `CrearCalificationRequest.Nota` es `double` y `CalificacionViewModel.Nota` es `double?` — hay que resolver la conversión. Y queda por quitar el `IServicioCurso` que `W3.C` conservó para no romper el sitio de llamada.
4. Preexistentes, de severidad baja: `SituacionRevistaViewModel.Docente` es `string` sin inicializar, así que vale `null` y no `""`; el `DatePicker` de `SituacionRevistaView.xaml` tiene `DisplayDateStart=DateTime.Now`, que impide ver fechas pasadas de un response existente, y tiene `SelectedDate` y `DisplayDate` bindeados a la vez. `MateriaView.xaml` y `CursoView.xaml` quedaron con filas de `Grid` vacías (cosmético).
5. **Riesgo conocido en las pruebas, para la pasada de W6**: `CatedraViewModelTests.cs:119` (de `R2`) conserva la forma ambigua `Should.NotThrow(() => x = ...)`. `R3` la evitó usando lambda con bloque.

**Dos fallas de proceso en W3, para corregir el reparto de las olas siguientes:**

1. **`W3.A` escribió en un archivo que no era suyo.** Reparó los finales de línea de `Views/Cursos/Divisiones/CalificacionView.xaml`, que es de `W3.C`, porque el estado LF mixto que `W3.C` había dejado rompía el markup compile con `MC3000` y le impedía medir su propio criterio. Lo reportó, pero la regla es parar y avisar, no arreglar. El resultado quedó correcto y el archivo se commiteó con `W3.C`, que es su dueña. **Causa de fondo**: el criterio de una tarea depende de un build global que otra tarea puede romper, así que en una ola paralela un agente bloqueado tiene incentivo a salirse de su carril.
2. **`W3.C` reportó dos veces un cambio que no estaba en disco.** Afirmó haber agregado `CursanteID` y la edición con `perl` nunca se aplicó; lo admitió al ser devuelta. Es el segundo caso de la tanda, después de `W1.C`. **El defecto que dejaba era invisible al compilador**: `CursanteID` quedaba en `Guid.Empty` para siempre, y es la clave de todos los requests de calificación y de asistencia. Ni el build ni una prueba de reflexión como la de `R2` lo detectan — hace falta una prueba de mapeo campo por campo, que es lo que `R3` tiene mandato de escribir.

**Correcciones al DTO que la disección no documentaba** (gana el DTO real, §Si un agente se bloquea): `DivisionResponse` tiene `Guid? PreceptorID` y `string? Preceptor`; `CalificacionResponse` tiene `double? Nota` y `string? Observacion`. La nulabilidad importa para el `CanExecute` de `GestionDivisionesViewModel:185` que reescribe `W4.A`.

**Hallazgo de `R2` resuelto por el orquestador — `InternalsVisibleTo("DynamicProxyGenAssembly2")`:**

`IDialogService` es `internal`, y NSubstitute no puede crear un doble de una interfaz `internal` sin que el ensamblado se lo declare visible al ensamblado dinámico donde Castle DynamicProxy genera el proxy. El `InternalsVisibleTo` de `W2.C` sólo habilitaba al proyecto de pruebas. Los `IServicio*` de `Core` **son públicos**, así que no había precedente en el repo y el problema aparecía por primera vez con el puerto que `W2.B` creó justamente para volver testeables los `Gestion*`. Se agregó la línea a `WPF_Desktop.csproj` (fuera del alcance literal de `W2.C`, decisión del orquestador) y además quedó `tests/WPF_Desktop.UnitTests/Dobles/DialogServiceFalso.cs`, un doble manual que registra los mensajes, como alternativa. Sin esto, `R4`, `R5` y `R6` habrían escrito decenas de pruebas con `Substitute.For<IDialogService>()` que fallan **todas juntas en ejecución** al llegar a W6, con un síntoma que no apunta a su causa.

**Otros hallazgos de `R2`, de severidad baja, ninguno bloqueante:**

1. `NivelEducativoConverter.ConvertBack` (`Shared/Converters/NivelEducativoConverter.cs:26`) hace `value.ToString()` sin guarda y lanza `NullReferenceException` con `null`; `CargoConverter` sí tolera `null`. **Preexistente**, fuera de la ola, y deliberadamente **no** cubierto por prueba para no fijar ese comportamiento como correcto.
2. Ningún converter lanza con un valor fuera de rango: `Enum.Parse` acepta `"99"` y devuelve `(NivelEducativo)99`. Las pruebas documentan ese comportamiento tal como es.
3. `CatedraViewModel` conserva `_catedraResponse` sin usarlo después del constructor — mismo patrón que `MateriaViewModel`, así que queda como observación.
4. **Riesgo conocido en las pruebas**: `Should.NotThrow(() => x = ...)` puede ser ambiguo entre `Action` y `Func<T>`. Nadie lo compiló. Mirarlo en la pasada de corrección de W6.

**Hallazgos de `R1`, ninguno bloqueante:**

1. **`using Core.ServicioCatedras.DTOs.Requests;` (`GestionSituacionRevistaViewModel.cs:3`) no resuelve ningún tipo vivo**: compila por vacío. El destino es correcto (es el namespace de `IServicioCatedra`), pero queda sin verificar hasta `W6.A`. **`W6.A` tiene que decidir si se queda o se retira** al reprogramar los 6 requests de la corrección 8.
2. `using Core.ServicioCursos.DTOs.Requests;` y `Core.ServicioCursos;` en `RegistrarAlumnoViewModel.cs:5-6` pueden quedar obsoletos — lo resuelve `W6.C`.
3. `ColeccionE2E.SembrarDivisionAsync` (líneas 147-159) todavía no tiene llamadores, y `tests/Infrastructure.IntegrationTests` tiene un helper homónimo que devuelve `Guid` en lugar de `Division`. Inconsistencia menor entre las dos bases de prueba.
4. **Los 19 archivos de `WPF_Desktop` tocados tienen BOM UTF-8**, contra la convención de `CLAUDE.md`. Es **preexistente** (ya lo tenían en `f4f57fb`): la ola no lo introdujo ni lo corrigió. `CLAUDE.md` dice que sólo `Domain/` está normalizado, así que es coherente con el estado del repo.

> **Métrica a vigilar aparte del total**: los **7 `CS4014`** ("no se esperó esta llamada") son el detector gratuito de la familia de defectos de `H-018` — los `Task` descartados que `W4.A` (tres `CargarDivisionesAsync`) y `W6.D` (`QuitarDocenteAsync`) corrigen a mano. Ese subconteo **tiene que bajar** ola a ola; si al cerrar W6 sigue en 7, algún `await` quedó sin poner.

## CIERRE DE LA TANDA — R5, R6, R7, suite en verde y `W8.A` (2026-10-03)

Filas que faltaban en la tabla de arriba, agregadas por `W8.A` a partir de los mensajes de commit:

| Fecha | Tarea | Commit | Nota |
|---|---|---|---|
| 2026-10-03 | `R5` | `66ef3f9` + `0b1398b` | `UI-01` y `UI-02` cubiertos. Escribió las pruebas de `GestionCurriculas` (el invariante de `UI-01` en alta y edición, incluida la regresión "guardar sin tocar no cambia la carga") y de `GestionCursantes`, y **`GuardasEstaticasW5Tests`**, que convierte guardas `rg` en pruebas (ningún `ComboBox` liga `SelectedIndex` a un dato de dominio, un solo `RegistrarMateriaRequest`, el combo ofrece exactamente 1 a 5, cada `Style` coincide con el `TargetType` de su elemento). **Encontró un defecto real invisible al compilador**: `GestionCursantesViewModel` no se podía construir (NRE: el setter generado por `[NotifyCanExecuteChangedFor]` llama al comando sin comprobar null y el constructor asignaba `CicloLectivo` antes de crearlo). Es el modo de fallo inverso al de la tanda (antes era el atributo **ausente**). Corregido en `0b1398b` |
| 2026-10-03 | `R6` | `dd12037` + `42323aa` | **Dos defectos reales, ninguno detectable por el build** (`dd12037`): la UI reimplementaba la vigencia y copió sólo la mitad de `RangoFechas.EstaVigente()`, así que una designación con fecha futura aparecía como reemplazable y `Catedra.Designar` la rechazaba sin explicar la causa; y "Volver" saltaba un nivel de navegación (a materias, no a cátedras). Unas 68 pruebas: los seis casos de uso remapeados contra el `CatedraID` del store, la cadena de suplencias en los dos sentidos, `ListarDocentesActivosAsync` con `Received(1)` sobre cinco filas (una consulta, no N+1), y `GuardasEstaticasW6Tests` |
| 2026-10-03 | `R7` | `0aa3cf0` | **Hecha sólo en parte.** Construye el contenedor con la misma cadena que `App.xaml.cs` y resuelve cada store dos veces afirmando la misma instancia (los tres `*NavigationStore`, `CicloLectivoStore` y `CatedraStore`): es la prueba que habría detectado `E.2`. No hizo falta mockear `Infrastructure` (registrar un `DbContext` no abre conexión). **No cubre** que `App` use ese mismo host (exige un `Application` de WPF) ni el recorrido de la app: eso es el paso 0 de la verificación funcional, **pendiente** |
| 2026-10-03 | suite en verde | `e8d5043` | **Primera ejecución de la suite desde que existe**: las ~170 pruebas de `R2`…`R7` nunca se habían podido correr. Cinco correcciones, ninguna de producción. Los **32 errores de compilación eran una sola causa**: el SDK de WPF **excluye `System.IO` de los implicit usings** y las guardas estáticas leen archivos. Dos pruebas fallaban por error de la prueba (`ShouldNotContain("MessageBox")` ignora mayúsculas y chocaba con la variable `messageBoxText`). Los dos `Skip` documentaban defectos ya corregidos y se habilitaron. **320 pruebas, 320 pasan, 0 omitidas, 0 con error.** Sin regresiones: `Domain.UnitTests` 303/308, `Core.UnitTests` 191/192 |
| 2026-10-03 | `W8.A` | — (lo commitea el orquestador) | Estados y registro vivo: `docs/todos.md` (`UI-01`…`UI-13`, `UI-15`, `UI-16` y `TEST-07` a `[RESUELTO]`; `UI-14` vigente por decisión; `UI-17`…`UI-37`, `TEST-08` y violaciones 23…27 nuevos; violaciones 15, 16 y 20 cerradas), `plan/core.md` (corrección de la predicción de los 41 sitios), `plan/handoff-errores.md` §4, `plan/wpf-desktop.md` y este archivo (nota de cabecera), `revision-arquitectura.md` y `roadmap.md` Fase 2.1. Cero líneas de código |

**Estado final verificado.** Los tres builds y las guardas de §Verificación final: `using Infrastructure` → **1** (`App.xaml.cs:2`); `clr-namespace:Domain.Curriculas.Materias` → **0**; `async void` en `ViewModels/` → **6** (cinco handlers de comando; `PerfilAlumnoViewModel.CargarPerfil` es un caso de uso, UI-22); `BuildServiceProvider` → **0**, `Startup=` → **0**, `StopAsync` → **1** en `OnExit`; `MessageBox` en `ViewModels/Cursos/` → 5 + 5 en los dos `Gestion*` que la decisión #2 excluye y 7 comentadas (corrección 13, UI-31).

**Lo que sigue pendiente, para que el siguiente que abra este documento no lo dé por cerrado.** (1) La §Verificación funcional en Windows, pasos 0 a 5, sin ejecutar; (2) `H-023`: instalar la base de datos y regenerar la migración (`InitCreate` no refleja el mapeo de `ff2de55`); (3) la deuda UI-17…UI-37 y las violaciones 23…27 de `docs/todos.md`; (4) `CLAUDE.md` quedó desactualizado y **no lo edita `W8.A`**: el usuario decide (UI-30). Hasta que (1) se haga, el estado correcto de esta tarea es **"builds, pruebas y guardas en verde; verificación funcional pendiente"**.

Al cerrar cada ola, agregar además una línea de cierre con el conteo de errores resultante, que es lo que permite verificar que la secuencia esperada (`1` → `18` → `~45-60` → `0`) se está cumpliendo:

| Ola | Errores al cerrar | Warnings de `WPF_Desktop` |
|---|---|---|
| W0 | 1 (markup) / 18 (declaración) | 7 (parcial) |
| W1 | **40** distintos (31 `CS1061`, 6 `CS0246` de la corrección 8, 2 `CS0029`, 1 `CS7036`) | **328** (de los cuales 7 `CS4014`) |
| W2 | **40** (sin cambio: la ola fue aditiva) | no medido |
| W3 | **32** (64 líneas impresas, 32 únicas) | no medido |
| W4 | **25** | no medido |
| W5 | **20** (19 en `GestionSituacionRevistaViewModel`, 1 en `GestionDocentesViewModel`) | no medido |
| W6 | **0** (pasando por 1) | **287** (`CS4014`: 7 → 1) |
| W7 | **0** | **282** |

---

## Correcciones al plan detectadas al diseccionar

Verificadas con el compilador y con lectura de archivo, no inferidas. El plan es correcto en lo demás:

1. **Un error vive fuera de `ViewModels/Cursos/**`.** La tabla D.2 y la nota metodológica del plan dan por sentado que todo lo roto en Alumnos/Docentes/Usuarios está comentado. Pero `ViewModels/Alumnos/RegistrarAlumnoViewModel.cs:28` declara `private RegistrarCursanteRequest _crearCursanteRequest;` en **código vivo** (el bloque `/* */` de ese archivo empieza en la línea 100), y es uno de los 18 errores. Lo cierra `W1.B`.
2. **`IDialogService` no es una abstracción reutilizable, es un stub muerto.** Su único método es `OpenDialogService()` y el cuerpo de la implementación está **entero comentado**. "Usar el `IDialogService` que ya existe y nadie consume" (violación #12 del plan) exige **diseñar el contrato**, no sólo inyectarlo. De ahí la tarea `W2.B`.
3. **`GestionSituacionRevistaView.xaml:194` bindea `Materia.SituacionRevista`**, la propiedad que la Fase 3.3 del plan elimina de `MateriaViewModel`. El plan no lo nombra, y es el peor tipo de defecto de esta capa: **un binding roto no falla el build**, deja el control vacío en silencio. Lo cierra `W6.B`.
4. **`GestionCurriculasView.xaml` bindea `Materias` dos veces con significados distintos**: línea 353 es la columna contador de la fila de currícula (se elimina) y línea 378 es el `ItemsSource` de la colección del ViewModel (se queda). Una instrucción genérica de "quitar los bindings a `Materias`" rompe la pantalla.
5. **La segunda guarda `rg` del plan no detecta lo que busca.** `rg -n "clr-namespace:Domain\.(Curriculas\.Materias|Cursos\.)"` no matchea `clr-namespace:Domain.Cursos;` (sin punto final), que es la forma real de los dos XAML con `NivelEducativo` roto; y `Domain.Cursos` es legítimo en `RegistrarCursoView.xaml` por `Grado`. La guarda correcta es sólo sobre `Domain\.Curriculas\.Materias`.
6. **`TEST-07` toca `tests/EDUSIS.TestSupport`, no sólo el proyecto E2E.** Uno de los 6 errores es `CursoBuilder.ConDivision`, y el builder vive en el proyecto de soporte. El plan lo describe como "6 `CS1061` de `tests/EDUSIS.EndToEndTests`".
7. **El `DataTemplate` de `HorarioViewModel` está efectivamente comentado** (`DataTemplate.xaml:169-173`), tal como dice el plan — confirmado, porque un `rg` lo reporta como vivo.

Detectadas al **ejecutar** (verificadas con el compilador el 2026-10-02):

8. **La tabla de los 18 errores de W0 subcontó: `GestionSituacionRevistaViewModel` tiene 6 `CS0246` más**, por `ListarCargosDocenteSegunMateriaRequest`, `RelevarDocenteDeAulaRequest`, `RescindirCargoDocenteRequest`, `EliminarCargoDocenteRequest`, `EstablecerDocenteDeAulaRequest` y `RegistrarDocenteEnMateriaRequest`. Esos tipos **no existen en ningún namespace de `Core`**: son los requests de los casos de uso que el refactor renombró al moverlos a `IServicioCatedra`, así que **ningún `using` los resuelve** y `W1.C` no podía cerrarlos. No aparecían en la línea base porque son referencias **dentro de cuerpos de método** (declaraciones de variable local), y Roslyn liga los cuerpos recién cuando el grafo de declaraciones está limpio — un `CS0246` no es necesariamente de etapa de declaración. **Los cierra `W6.A`**, que reprograma esas seis llamadas. El criterio de `W1.C` ("cero `CS0234`/`CS0246` en estos archivos") queda amendado con esta salvedad.
9. **El build completo imprime cada error y cada advertencia dos veces.** WPF compila un proyecto temporal (`WPF_Desktop_*_wpftmp.csproj`) para el segundo paso de markup, y Roslyn liga los archivos en los dos pases. Todo conteo tomado de `dotnet build` hay que **deduplicar** (`sort -u` sobre `file:line: code`) antes de compararlo con una línea base, o el número sale al doble. Es la razón por la que el cierre de W1 son 40 errores y no las 80 líneas que imprime el build.
10. **Correr `-t:ResolveReferences;CoreCompile` después de un `--no-incremental` agrega ~45 errores falsos.** Sin el paso de markup no se generan los `*.g.cs`, así que aparecen `CS0103` por `InitializeComponent` en cada code-behind y un `CS5001` por falta de `Main`. El comando de criterio por archivo del §Reglas de ejecución sigue siendo válido —esos falsos positivos caen en archivos `*.xaml.cs`, que ninguna tarea posee— pero **a partir de W1 conviene usar `dotnet build --no-incremental`**, que es exactamente lo que el documento ya anticipaba para W2 en adelante.
11. **La corrección 6 era obsoleta**: `CursoBuilder` ya no tenía `ConDivision` cuando `W0.T` la tomó, así que la tarea tocó dos archivos de `tests/`, no tres, y `tests/EDUSIS.TestSupport` quedó sin cambios.
12. **El criterio de `W2.C` es inalcanzable en W2, y arrastra a `R2`…`R5`.** `tests/WPF_Desktop.UnitTests` tiene un `ProjectReference` a `WPF_Desktop`, así que `dotnet test tests/WPF_Desktop.UnitTests` **no puede ejecutar ni la prueba de humo hasta que la ola W6 deje el proyecto en cero errores**. La decisión de disección #1 ("verificación por pruebas automatizadas") sólo se vuelve operativa en W6, no en W2. Consecuencia para las revisiones: `R2`, `R3`, `R4` y `R5` **no pueden correr las pruebas que sus textos describen**. **Resuelto con el usuario el 2026-10-02 (decisión de disección #6)**: cada `R*` de las olas W2 a W5 hace las dos cosas — revisa por lectura de diff más guardas `rg`, que es lo que mantiene la puerta por ola, **y escribe sus pruebas sin ejecutarlas**. Las pruebas de las cuatro olas se ejecutan todas juntas al cerrar W6, en el mismo momento en que el criterio de `W2.C` por fin se puede medir. Se acepta el riesgo de que una prueba nunca ejecutada esté mal escrita: la contrapartida es que se escriben con el contexto fresco de su ola, en vez de reconstruirlo cuatro olas después. **Al cerrar W6 hay que presupuestar una pasada de corrección de las pruebas**, no sólo de ejecución. Lo verificable de `W2.C` sí se verificó: `EDUSIS.sln` sigue en 40 errores, el proyecto no figura en el `.sln`, y `./tests/run-tests.sh --rapidas` sigue en verde con 191 pasadas y 1 omitida.
