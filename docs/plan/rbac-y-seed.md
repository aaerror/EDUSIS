# Plan — RBAC funcional y poblado de la base de datos

> **Alcance**: hacer que los roles determinen qué funcionalidad tiene disponible el usuario (`Domain` → `Core` → `WPF_Desktop`) y poblar la base con un dataset de escuela demo. Incluye los datos de referencia (`rol`) como parte del esquema, no como datos de demo. **Fuera de alcance**: cambiar el `TargetFramework`, reescribir el flujo de autenticación más allá de lo que el RBAC exige, y corregir los defectos preexistentes que la suite de integración destapó al ejercitarse por primera vez (se registran en [`../todos.md`](../todos.md), no se arreglan acá).
>
> Documentos hermanos: [`core.md`](core.md) y [`wpf-desktop.md`](wpf-desktop.md) (bloques anteriores), [`../todos.md`](../todos.md) (deuda con IDs), [`../roadmap.md`](../roadmap.md) Fase 6 (dónde encaja esto).

> **Estado al 2026-10-13 — las siete fases ejecutadas.** Trabajo en la rama `db/migraciones`, **sin commitear**. Verificado: `dotnet build EDUSIS.sln --no-incremental` → **0 errores**; `dotnet test EDUSIS.sln` → **513 pasan, 0 fallan, 78 omitidas**; `dotnet test Tests/WPF_Desktop.UnitTests` → **320 pasan, 0 fallan**; `dotnet run --project Tools/EDUSIS.Seed` deja la escuela demo completa en ~4 s. La migración `20261011000305_InitCreate` está aplicada sobre `EdusisDB` y siembra los 4 roles: **cierra H-023**.
>
> **Lo único que queda sin verificar es lo que exige una persona delante de la pantalla**: ingresar con cada una de las cuatro cuentas y comprobar que el menú habilita exactamente lo que ese rol permite. La app arranca y muestra la ventana «Login» (verificado lanzándola), y el seeder comprueba **contra la base** que las cuatro cuentas validen su clave con el algoritmo real y tengan su rol asociado; lo que no se puede automatizar desde acá es tipear las credenciales. UI-39 queda cerrado.
>
> **Cinco predicciones de este plan que la ejecución corrigió, y conviene no repetirlas como si siguieran vigentes.**
> 1. Se anticipó que la columna `rol.descripcion` (`varchar(15)`, `IsUnicode(false)`) degradaría el acento de "Dirección". **No ocurre en este servidor**: `ASCII(SUBSTRING(descripcion,8,1))` devuelve **243** (`0xF3`, 'ó' en CP1252) y la colación es `Modern_Spanish_CI_AS`. El `Direcci?n` de `sqlcmd` es codificación de salida, un falso positivo que volvió a aparecer al verificar `usuario_rol` el 2026-10-13. La decisión #6 (viajar por Id) se sostiene por portabilidad, no por un defecto observado.
> 2. Se anticipó que `Usuario.AgregarRol` chocaría con el change tracker (§C). **El riesgo era real y el `AttachRange` lo evitó**: el seeder adjunta los cuatro roles antes de usarlos y las 4 filas de `usuario_rol` se insertaron sin que EF intentara un `INSERT INTO rol`. Verificado por consulta directa. El seeder además esquiva el problema por el otro lado: usa el constructor `Usuario(…, List<Rol> roles)` en lugar de `AgregarRol(string)`, que resuelve por descripción.
> 3. **El seeder no necesitó `ISesionActual` para sembrar.** La Fase 5 anticipaba que haría falta una `SesionDeSistema` porque los servicios cambiaban de constructor; pero el seeder construye por el dominio y guarda con `IUnitOfWork` (decisión #8), así que no ejecuta ningún caso de uso con guarda. La clase existe igual —`CoreDI.AddServices()` registra los catorce servicios y cualquiera que se resuelva la pide— pero hoy no se consulta.
> 4. **El alcance "sólo escritura" de la Fase 5 no fue prudencia: era obligatorio.** `ServicioAutenticacion.Login` delega en `IServicioUsuario.BuscarUsuarioPorIDAsync`. Si las lecturas llevaran guarda, iniciar sesión exigiría `AdministrarUsuarios`, o sea **tener sesión para poder iniciarla**. Está afirmado en `AutorizacionDeCasosDeUsoTests.El_login_no_exige_permiso_porque_es_quien_abre_la_sesion`.
> 5. **Sembrar resultó ser la primera prueba de carga del mapeo de EF, y encontró tres defectos que ningún build ni la suite detectan** (INFRA-16/17/18): el `LogTo` insilenciable de `InfrastructureDI`, el ciclo de FK `catedra` ↔ `situacion_revista` —que impide insertar *y* borrar en un paso— y el `OwnsOne` de `CicloLectivo`, que puesto a compartir instancia escribe NULL en silencio. Los tres estaban latentes desde antes; producción no los sufre porque hace todo de a uno. Detalle en [`../todos.md`](../todos.md).

## Context

`Rol` existe en `Domain/Usuarios/Rol.cs` desde antes de este trabajo, con cuatro valores (`Administrador`, `Dirección`, `Docente`, `Secretaria`), tabla propia (`rol`), tabla de unión (`usuario_rol`) y repositorio que los carga con `Include`. `Usuario.AgregarRol`/`QuitarRol` están implementados, y `ServicioUsuario.AsignarRolAsync`/`QuitarRolAsync` también (H-015, cerrado en `b3ac87b`).

Nada de eso tenía efecto. La cadena estaba cortada en tres puntos simultáneos: la tabla `rol` nunca se sembraba, `RegistrarUsuarioAsync` creaba usuarios con el constructor sin roles, y `BuscarUsuarioPorIDAsync` devolvía una lista fija `{"Basic","Custom"}` con el mapeo real comentado una línea más abajo. Aun si los tres se arreglaran, no había un solo `IsInRole` en la solución: los claims se construían y nadie los consultaba.

El requisito es que **los roles determinen la funcionalidad disponible**. Eso convierte a los roles en una decisión de autorización, y una decisión de autorización no puede apoyarse en lo que había: `Thread.CurrentPrincipal` como almacenamiento de sesión (ver Diagnóstico §E) ni una comparación por descripción contra una columna no unicode.

Resultado buscado: un usuario entra, el sistema sabe qué permisos tiene, la interfaz le habilita sólo lo que puede hacer, los casos de uso rechazan el resto, y la base tiene datos suficientes para recorrer todo eso.

---

## Decisiones tomadas

1. **`Rol → Permiso` vive en código, no en base de datos.** La atribución es un `IReadOnlyDictionary<int, Permiso[]>` en `Rol.cs`. Queda versionada en git, auditable y sin tablas, repositorio ni pantalla de administración. Se puede migrar a tabla más adelante **sin tocar a ningún consumidor**, porque todos preguntan por permiso y no por rol. Alternativa descartada: tablas `permiso`/`rol_permiso` con `HasData` más pantalla para reconfigurar — es el RBAC completo del libro, pero agrega dos tablas, un repositorio, casos de uso y una pantalla a un sistema que todavía no recorrió sus flujos existentes en la app.
2. **La autorización se aplica en la interfaz y en `Core`.** La interfaz guía (comandos deshabilitados) y `Core` garantiza (el caso de uso rechaza), sin importar desde dónde se lo llame — incluido el seeder y las pruebas. Sólo-UI deja los casos de uso alcanzables sin control y una pantalla nueva que olvide el chequeo abre el agujero en silencio; sólo-Core muestra botones que fallan al apretarlos.
3. **`Permiso` no se persiste.** No tiene tabla. `RolesConfiguration` hace `builder.Ignore(x => x.Permisos)`: sin eso EF alcanza `Permiso` desde la navegación, lo descubre como entidad y reclama un mapeo. Verificado — la migración regenerada sigue teniendo **21 tablas**, las mismas que antes.
4. **La atribución se resuelve por `Id`, no por instancia.** EF materializa `Rol` con el constructor privado sin parámetros, así que un rol leído de `usuario_rol` **no es** la instancia estática: es otro objeto con el mismo `Id`. Guardar los permisos como estado de instancia haría que la autorización negara todo, en silencio, para cualquier usuario leído de la base. El diccionario indexado por `Id` hace que el singleton y el materializado respondan igual.
5. **La sesión es un `Singleton` en el contenedor, no `Thread.CurrentPrincipal`.** Ver Diagnóstico §E para el motivo técnico. Se registra la clase concreta y las dos interfaces resuelven hacia ella; registrarlas por separado crearía dos instancias y el login escribiría en una mientras `Core` leería la otra.
6. **Los roles viajan en el DTO como `Id`, no como descripción.** `UsuarioResponse.Roles: List<string>` → `RolesIDs: List<int>`. El Id es canónico; la descripción es presentación y vive en una columna `varchar(15)` no unicode. Para mostrar el nombre: `Enumeration.FromValue<Rol>(id).Descripcion`. Costo de la decisión: cero — el único consumidor del campo era el bloque que este plan reemplaza.
7. **El seeder es un proyecto consola aparte, fuera de `EDUSIS.sln`.** Mismo criterio que `Tests/WPF_Desktop.UnitTests`. Referencia `Infrastructure`, `EDUSIS.TestSupport.Domain` y `Bogus`; así `Bogus` no entra como dependencia de un ensamblado de producción ni el código de demo viaja en el binario final.
8. **Los datos de demo se construyen por el dominio, no con `HasData` ni con SQL.** `HasData` queda reservado para datos de referencia (`rol`, `ciclo_lectivo`): inmutables, con PK conocida, parte del esquema. Los datos de demo pasan por los constructores públicos de los agregados, así validan invariantes y encolan sus eventos de dominio; un script SQL se saltea todo eso y nada garantiza que lo sembrado sea representable por el modelo.

---

## Diagnóstico

### A. La cadena de roles antes de este trabajo

| # | Capa | Estado |
|---|---|---|
| 1 | Tabla `rol` | **vacía** — único config con `ValueGeneratedNever()` y sin `HasData` |
| 2 | `Usuario.AgregarRol`/`QuitarRol` | implementado (`Usuario.cs:64,76`) |
| 3 | `AsignarRolAsync`/`QuitarRolAsync` | implementado (`ServicioUsuario.cs:205,227`) |
| 4 | `RegistrarUsuarioAsync` | usa el ctor **sin roles** (`ServicioUsuario.cs:93`) |
| 5 | `BuscarUsuarioPorIDAsync` | devolvía `{"Basic","Custom"}` hardcodeado, mapeo real comentado |
| 6 | `UsuarioRepository.Consulta()` | correcto: `Include(x => x.Roles)`, y `BuscarPorIDAsync` lo usa por la clase base |
| 7 | Claims + autorización | el claim se creaba (`BasicIdentity.cs:16`); **cero `IsInRole`** en la solución |
| 8 | Pantalla de roles | no existe (UI-19) |
| 9 | Login en el arranque | comentado; `App.xaml.cs:73-76` abre `MainWindow` directo |

`"Basic"` y `"Custom"` no son descripciones válidas de `Rol`: eran dos vocabularios conviviendo, y el de la interfaz no tenía correspondencia con el del dominio.

### B. Datos de referencia vs. datos de demo

`ciclo_lectivo` ya se sembraba con `HasData` (`CicloLectivoConfiguration.cs:39`, períodos 2020–2035, INFRA-14). `rol` no, aunque `usuario_rol` tiene `FK_USUARIO_ROL_ROL` contra `rol.rol_id`. El patrón correcto ya estaba en el repo; faltaba aplicarlo.

### C. La trampa del change tracker (riesgo, no defecto observado)

`Rol.Admin` y sus hermanos son `static readonly`, compartidos por todo el proceso, y `AgregarRol` agrega esa instancia. Para EF eso deja dos caminos y ninguno es bueno: si el `DbContext` no consultó `rol`, el singleton entra como `Added` y EF intenta `INSERT INTO rol`, que choca con la PK sembrada; si sí la consultó, EF rechaza el singleton con *"another instance with the same key value is already being tracked"*.

**No se reprodujo** en la suite. El motivo es §D, no que el riesgo no exista. El patrón que lo evita es adjuntar los cuatro roles al contexto antes de usarlos (`AttachRange` → quedan `Unchanged`), con lo que EF sólo inserta las filas de `usuario_rol`. Hay que aplicarlo en el seeder (Fase 7) y verificar si `AsignarRolAsync` lo necesita: sus pruebas de `Core` usan dobles de `IUnitOfWork`, que no ejercitan el change tracker.

### D. `Respawn` borra los datos de referencia (defecto nuevo, TEST-09)

`SqlServerFixture` inicializa `Respawner` con `TablesToIgnore = new Table[] { "__EFMigrationsHistory" }` y nada más, así que entre pruebas **borra también `rol` y `ciclo_lectivo`**. Consecuencia medida al correr la suite de integración por primera vez: `The INSERT statement conflicted with the FOREIGN KEY constraint "FK_CURSANTE_CICLO_LECTIVO"` en 12 pruebas.

Y explica por qué §C no se dispara: la tabla `rol` termina vacía, así que el `INSERT` del singleton **funciona**. La prueba de roles pasa apoyándose en que EF inserte un dato de referencia, que es justo lo que no debería pasar.

### E. `Thread.CurrentPrincipal` no sirve como sesión en .NET

La documentación de `ExecutionContext` dice que en .NET Framework *"wherever the compressed stack flows, the managed principal... also flow"*, y marca explícitamente que **en .NET Core el security context no está soportado**. El principal no viaja con las continuaciones `async`.

Hoy sobrevivía de casualidad: los `await` de los ViewModels no usan `ConfigureAwait(false)`, así que el `SynchronizationContext` del Dispatcher los devuelve al hilo de UI, donde el principal sigue estando. Una decisión de autorización no puede depender de eso. Es la violación #19 de `todos.md`, que `plan/wpf-desktop.md` había decidido no tocar (su decisión #4) — esa decisión queda **superada** por el requisito de RBAC.

De paso: `UsuarioViewModel.cs:44-45` hacía `principal.Identity` sobre el resultado de un `as ClaimsPrincipal` sin comprobar null, un `NullReferenceException` latente.

---

## Fase 0 — Línea base

- `dotnet ef` no estaba instalado; se fijó en **7.0.9** para coincidir con el major de los paquetes del proyecto, siguiendo la doc de MS (*"always use the version of the tools package that matches the major version of the runtime packages"*).
- Descubierto al primer intento: no hay runtime 7.x en la máquina (sólo `NETCore.App` 8.0.31/10.x y `WindowsDesktop` 10.x), y la política de roll-forward por defecto es `Minor`, que **no cruza de major**. Ni `dotnet ef` ni `dotnet run` arrancaban.
- Se agregó `<RollForward>Major</RollForward>` a `WPF_Desktop.csproj`, mismo criterio que `Tests/Directory.Build.props:22`, que ya tenía el problema resuelto para el testhost. No se toca el `TargetFramework` (restricción de la constitución). Verificado: `dotnet ef migrations list` funciona **sin** la variable de entorno.
- Se quitó la regla `/Infrastructure/Migrations/` de `.gitignore`: las migraciones son código fuente. El snapshot estaba trackeado **sin** las migraciones que lo produjeron, un estado incoherente (un snapshot versionado que nadie puede reconstruir).

## Fase 1 — Dominio

- `Domain/Usuarios/Permiso.cs` (nuevo): `Enumeration` con 10 acciones — `GestionarDocentes`, `GestionarAlumnos`, `GestionarCursos`, `GestionarCurriculas`, `GestionarCatedras`, `InscribirCursantes`, `CargarCalificaciones`, `TomarAsistencia`, `AdministrarUsuarios`, `AsignarRoles`.
- `Domain/Usuarios/Rol.cs`: matriz rol → permisos indexada por `Id` (decisión #4), más `Permisos` y `Tiene(Permiso)`. Un `Id` desconocido devuelve vacío, no excepción: un rol que la base tenga y el código no, no debe habilitar nada.
- `Domain/Usuarios/Usuario.cs`: `TienePermiso(Permiso)` → `_roles.Any(rol => rol.Tiene(unPermiso))`. Sin roles no hay permisos; se niega por omisión.

**Atribución elegida** (es una propuesta de arranque, no una regla de negocio relevada):

| Rol | Permisos |
|---|---|
| Administrador | los 10 |
| Dirección | los 8 académicos (todos menos `AdministrarUsuarios` y `AsignarRoles`) |
| Secretaria | `GestionarDocentes`, `GestionarAlumnos`, `InscribirCursantes` |
| Docente | `CargarCalificaciones`, `TomarAsistencia` |

## Fase 2 — Esquema y migración

- `RolesConfiguration`: `builder.Ignore(x => x.Permisos)` (decisión #3) y `builder.HasData(Rol.Admin, Rol.Direccion, Rol.Docente, Rol.Secretaria)`.
- Migración regenerada desde cero: se borró la vieja (`20250519233424_InitCreate`, de mayo 2025, previa a cátedras/cursantes/asistencias — el snapshot regenerado cambió **+548/−245** líneas) y se creó `20261011000305_InitCreate`. **21 tablas**, mapeo en `snake_case` español, `InsertData` de los 4 roles y de los 14 ciclos lectivos.
- Aplicada sobre `EdusisDB`. **Cierra H-023.**

## Fase 3 — Puerto y sesión

- `Core/Shared/Seguridad/ISesionActual.cs` (público): `UsuarioID`, `EstaAutenticado`, `TienePermiso(Permiso)`. Mismo patrón de puerto que `IGeneradorDocumentos`: lo define `Core`, lo implementa el composition root. Sincrónico y sin consultas a propósito — si fuera a la base en cada pregunta, el `CanExecute` de cada comando haría una consulta por repintado; la contrapartida es que un cambio de roles recién aplica al volver a entrar.
- `Core/Shared/Seguridad/PermisoDenegadoException.cs` (`internal`): misma forma que las excepciones de dominio (constante `ERROR`, tres constructores, mensaje en español).
- `WPF_Desktop/Security/ISesionUsuario.cs` + `SesionUsuario.cs`: el puerto más los mutadores que sólo la presentación necesita (`Iniciar`, `Cerrar`, `event Action? Cambio`). Separadas en dos interfaces para que un caso de uso pueda consultar la sesión pero no abrirla ni cerrarla.
- `ServicioUsuario.BuscarUsuarioPorIDAsync`: devuelve los roles reales (`usuario.Roles.Select(x => x.Id)`), no `{"Basic","Custom"}`.
- `UsuarioResponse`: `List<string> Roles` → `List<int> RolesIDs` (decisión #6).

## Fase 4 — Interfaz

- `MainViewModel`: los tres comandos de menú pasan a `new RelayCommand(execute, canExecute)` con `canExecute` por permiso, y `OnSesionCambio()` llama a `NotifyCanExecuteChanged()` en los tres. Sin eso el menú queda congelado en el estado que tenía al construirse el ViewModel.
- `LoginUsuarioViewModel`: llama a `_sesion.Iniciar(response.UsuarioID, response.RolesIDs)` en lugar de armar `BasicIdentity`/`CustomPrincipal` y asignar `Thread.CurrentPrincipal`.
- `UsuarioViewModel`: lee `ISesionActual` (la interfaz angosta, sólo lectura) en lugar del principal; de paso desaparece el `NullReferenceException` latente de §E.
- `WPF_DesktopDI`: `SesionUsuario` como `Singleton` y las dos interfaces resolviendo hacia esa instancia (decisión #5).

---

## Fase 5 — Autorización en `Core`

Es la mitad que faltaba de la decisión #2.

- **58 guardas** en **12 de los 14 servicios**. Quedan fuera `ServicioAutenticacion` (es quien *abre* la sesión) y `ServicioSeguridad` (hashing técnico, no un caso de uso).
- `Core/Shared/Seguridad/SesionActualExtensions.cs` (nuevo): `Exigir(Permiso)` deja la guarda en **una línea** por caso de uso. El nombre de la operación lo completa `[CallerMemberName]`, así que no se escribe a mano ni queda desactualizado al renombrar el método. Importa para algo concreto: una guarda que cuesta cuatro líneas es una guarda que alguien omite en la pantalla siguiente.
- **Alcance decidido: sólo los casos de uso de escritura.** Las ~30 consultas quedan abiertas, y es decisión escrita y afirmada por una prueba, no un olvido — ver CORE-16 en [`../todos.md`](../todos.md) y la predicción corregida #4 del encabezado.
- Mapeo módulo → permiso. Cuatro de las doce filas **no tienen un permiso propio en el dominio** y se resolvieron por pertenencia; es una decisión tomada por omisión y está anotada como violación #32:

| Servicio | Permiso | ¿Directo? |
|---|---|---|
| `ServicioAlumno` | `GestionarAlumnos` | sí |
| `ServicioDocente` | `GestionarDocentes` | sí |
| `ServicioCurso` | `GestionarCursos` | sí |
| `ServicioCurricula` | `GestionarCurriculas` | sí |
| `ServicioCatedra` | `GestionarCatedras` | sí |
| `ServicioAsistencia` | `TomarAsistencia` | sí |
| `ServicioCursante` | `InscribirCursantes` / `CargarCalificaciones` | sí (se parte en dos) |
| `ServicioUsuario` | `AdministrarUsuarios` / `AsignarRoles` | sí (se parte en dos) |
| `ServicioLicencia` | `GestionarDocentes` | **no** — la licencia es de un docente |
| `ServicioDivision` | `GestionarCursos` | **no** — la división es de un curso |
| `ServicioMateria` | `GestionarCurriculas` | **no** — la materia es espacio de la currícula |
| `ServicioDocumento` | `GestionarAlumnos` | **no** — el certificado es de un alumno |

- `ServicioUsuario.SolicitarAccesoAsync` queda **sin guarda** a propósito: es auto-servicio de quien todavía no tiene cuenta.
- El costo previsto se confirmó: hubo que proveer `ISesionActual` a `HostDeServicios` y a `ComposicionDePruebaE2E` (ambos con `SesionDePrueba`, nueva en `Tests/EDUSIS.TestSupport/Fakes/`, que concede todo por omisión) y a `ServicioDocumentoTests`, que construye el servicio a mano en vez de resolverlo.
- **18 pruebas** en `Tests/Core.UnitTests/Shared/AutorizacionDeCasosDeUsoTests.cs`: una por permiso, más los bordes de la sesión y las dos decisiones de alcance. Todas invocan con `request: null!` a propósito, así afirman **la guarda y su posición**: si alguien la moviera detrás de la validación de datos, fallarían con `NullReferenceException` en lugar de `PermisoDenegadoException`.
- De paso se corrigió el doble punto del mensaje de `PermisoDenegadoException` (`ERROR` ya terminaba en punto y el formato agregaba otro).

**Listo.** Un caso de uso con permiso faltante lanza `PermisoDenegadoException`, verificado con un doble de `ISesionActual`.

## Fase 6 — Arranque de sesión

Se tomó la **salida 1, reactivar el flujo de login**, que es lo que el RBAC presupone. Pasó a ser viable porque la Fase 7 siembra las cuentas.

- `App.xaml.cs` muestra `StartupWindow` con **`ShowDialog()`** —modal y bloqueante— y, al cerrarse, abre `MainWindow` **sólo si `ISesionUsuario.EstaAutenticado`**; si no, `Shutdown(0)`.
- Se eligió el modal sobre `Show()` + `IsVisibleChanged` (que era la forma del bloque comentado) porque el arranque queda lineal y la condición de salida se lee de un vistazo. Con `Show()` + evento hay que coordinar además el `ShutdownMode`: cerrar la única ventana abierta da por terminada la aplicación antes de alcanzar a abrir la siguiente.
- `MainWindow` pasa a ser `Application.MainWindow` **recién después** del login. Hasta ahí lo es la de acceso, que es la que `LoginUsuarioViewModel` cierra con `Application.Current.MainWindow.Close()`; reasignarla antes haría que esa línea cerrara la ventana principal.
- `MainViewModel` se construye **después** del login, así que lee la sesión ya iniciada y su menú arranca con el `CanExecute` correcto, sin depender del evento `Cambio`. (`MainViewModel.Receive(UsuarioLogeadoMessage)` sólo hace `Console.WriteLine`, así que no importa que el mensaje se emita antes de que exista el destinatario.)

**Verificado**: la app arranca y muestra la ventana «Login». **Sin verificar**: el ingreso con credenciales y el menú por rol — requiere tipear a mano. Bloqueo encontrado en el camino: `LoginUsuarioViewModel` valida el campo usuario como correo electrónico, ver UI-40.

## Fase 7 — Seeder

`Tools/EDUSIS.Seed` (consola, `net7.0`, `RollForward` `Major`), fuera de `EDUSIS.sln`, con referencia a `Infrastructure`, `Core`, `EDUSIS.TestSupport.Domain` y `Bogus` 35.6.1.

Las tres capas, sin mezclarlas:

| Capa | Qué | Con qué |
|---|---|---|
| Referencia | `rol`, `ciclo_lectivo` | `HasData` (Fase 2); el seeder sólo los **adjunta** |
| Construcción | agregados válidos, invariantes, eventos | los **18 builders** de `EDUSIS.TestSupport.Domain` |
| Volumen | nombres, domicilios, fechas | `Bogus` locale `es`, `Randomizer.Seed` fijo en `20261013` |

**Lo que el dataset tiene** (una corrida completa, ~4 s): 51 docentes (2 directivos, 2 secretarias, 12 preceptores, 35 profesores), 300 alumnos, 6 cursos de secundaria, 6 currículas vigentes, 48 materias, 12 divisiones **todas con preceptor**, 96 cátedras con titular en funciones (12 además con suplente, que es quien queda en funciones), **384 horas con horario y 0 sin asignar**, 300 cursantes (1 de cada 10 recursante), 1292 calificaciones, 60 planillas de asistencia (la del día más viejo cerrada, para tener los dos estados) y 4 usuarios, uno por rol.

Decisiones de implementación que conviene conocer antes de tocarlo:

- **Las identidades únicas no salen de Bogus.** DNI, CUIL y legajos vienen de contadores secuenciales: el dominio y la base exigen unicidad, y un generador aleatorio la rompe por colisión tarde o temprano, de forma intermitente y por lo tanto difícil de diagnosticar. Bogus se usa sólo para lo que puede repetirse (nombres, calles, localidades).
- **Los puestos se siembran con estado `Activo`, no con el `Pendiente` del builder.** `DocenteRepository.BuscarSegunPosicionAsync` filtra por `Estado == EstadoPuesto.Activo` (`:29`), así que un puesto pendiente no aparece en `ListarPreceptoresActivosAsync` y la pantalla de divisiones quedaría sin preceptores para elegir.
- **La edad de los docentes es asimétrica por sexo.** `Docente` rechaza la edad jubilatoria con límites distintos (`:43`): más de 64 para varones, más de 59 para mujeres. Un rango único rompe la invariante la mitad de las veces — lo descubrió el propio dominio en la primera corrida, que es exactamente para lo que la decisión #8 eligió sembrar por constructores.
- **Dos workarounds del mapeo**: las cátedras se guardan en dos pasos (`PonerEnFuncionesDeTodasAsync`) y cada cursante lleva su propia instancia de `CicloLectivo`. Causa y alcance en [`../todos.md`](../todos.md), INFRA-17 y INFRA-18.
- **Los horarios respetan dos reglas que no viven en ningún agregado**: una división no tiene dos cátedras en el mismo slot, y un docente no está en dos aulas a la vez. El seeder lleva dos `HashSet` de ocupación y toma el primer slot libre de los 45 del turno mañana (5 días × 9 horas cátedra de 40' desde las 07:20, que es lo que entra en los límites 07:00–13:00 del turno).
- **`--limpiar` borra sólo los datos de demo**, en orden de hoja a raíz y dentro de una transacción, conservando `rol` y `ciclo_lectivo`. Es explícitamente lo contrario de TEST-09, donde `Respawn` borra los datos de referencia y hace fallar 12 pruebas. Pide confirmación escrita (`borrar`) salvo que se pase `--si`.
- **El seeder se autoverifica**: relee las cuatro cuentas de la base, valida la contraseña con el `IServicioSeguridad` real y comprueba que tengan rol. Un seeder que informa "4 usuarios sembrados" y deja cuentas con las que no se puede entrar es peor que uno que falla, porque el problema aparece lejos de su causa. De paso, verifica el `AttachRange` de §C: si hubiera fallado, se leería un usuario sin roles.
- Se confirmó lo previsto sobre eventos: sembrar con `GuardarCambiosAsync()` los publica, y los dos handlers existentes (`LicenciaSolicitadaEvent`, `MateriaEliminadaEvent`) no generan PDFs, así que sembrar cientos de agregados no tiene efectos colaterales.

**Las cuentas** (contraseña `Edusis2026` para las cuatro): `admin@edu.ar`, `direccion@edu.ar`, `secretaria@edu.ar`, `docente@edu.ar`. Tienen forma de correo y dominio corto por UI-40: la interfaz valida ese campo como email y la columna es `varchar(18)`.

**Listo.** `dotnet run --project Tools/EDUSIS.Seed` deja la escuela demo completa y es re-ejecutable con `--limpiar`. Queda sin verificar entrar a la app con cada rol, que es manual.

---

## Verificación

Ejecutado al 2026-10-11:

```bash
dotnet build EDUSIS.sln --no-incremental          # 0 errores
dotnet test EDUSIS.sln                            # 495 pasan, 0 fallan, 73 omitidas
dotnet test Tests/WPF_Desktop.UnitTests           # 320 pasan, 0 fallan
dotnet ef migrations list --project Infrastructure --startup-project WPF_Desktop
```

Las 599 advertencias de un build completo son preexistentes y de nullable (`CS8618` 488, `CS8622` 320, `CS8625` 118…); los archivos nuevos de este plan **no agregan ninguna**. Ojo con medirlas: un build incremental reporta ~2 porque sólo re-emite lo que recompila, y el build completo imprime todo dos veces (WPF compila un `*_wpftmp.csproj`), así que todo conteo se deduplica con `sort -u`.

**Pendiente de verificar**: la app corriendo. No se ejecutó en Windows; con la Fase 6 abierta, arrancarla hoy muestra el menú deshabilitado.

### Suite de integración ejercitada por primera vez

Con el motor instalado se levantaron las 73 pruebas omitidas apuntando a una base desechable:

```bash
EDUSIS_TEST_SQLSERVER="Data Source=localhost;Initial Catalog=EdusisDB_Test;Integrated Security=True;Encrypt=False;TrustServerCertificate=True" \
  dotnet test Tests/Infrastructure.IntegrationTests
```

Resultado: **44 pasan, 20 fallan, 2 omitidas**. Las 20 son defectos preexistentes que estaban invisibles porque la suite siempre se omitía — ninguna toca roles, permisos ni sesión. Causas identificadas: 12 por TEST-09 (`Respawn` borrando datos de referencia, §D), 6 por un legajo de docente que no cumple `^(\d{6})$` (TEST-10; el default de `DocenteBuilder`, `"100200"`, sí es válido, así que son las pruebas las que pasan otro valor), más una planilla con fecha futura y una aserción de `MaxLength` sobre `Alumno.Legajo`.

**Limitación metodológica, escrita a propósito**: no se estableció una línea base previa a estos cambios. En `HEAD` las migraciones estaban gitignoreadas, así que no hay migración que aplicar y la comparación no sería válida. La afirmación "son preexistentes" se sostiene en las causas raíz identificadas, no en un diff de resultados.

---

## Violaciones DDD / Clean Architecture detectadas

1. **El seeder depende de un proyecto alojado bajo `Tests/`** (Fase 7, ya materializado). `EDUSIS.TestSupport.Domain` declara `<IsTestProject>false</IsTestProject>` y sólo depende de `Domain`, así que no es código de prueba sino una biblioteca de construcción de agregados; pero su nombre y su ubicación dejan de describir su rol. Se aceptó documentándolo en el `.csproj` del seeder; mover el proyecto fuera de `Tests/` sigue siendo la salida limpia. **No es violación de capas** — no invierte ninguna dependencia.
1b. **El seeder referencia `Core` y necesitó un tercer `InternalsVisibleTo`** para usar `IServicioSeguridad`, que es `internal` y es quien hashea. El motivo es tener **una sola fuente de verdad** del algoritmo: reimplementar el PBKDF2 en el seeder divergiría sin aviso del `ValidatePassword` que después valida el login. Costo: sube a tres los consumidores de los internos de `Core`, y el tercero no es una suite. La salida limpia sería un contrato público de hashing en `Core.Shared`.
1c. **Cuatro módulos quedaron mapeados a permisos de otro módulo** porque el dominio no tiene permiso propio para ellos: licencias → `GestionarDocentes`, divisiones → `GestionarCursos`, materias → `GestionarCurriculas`, documentos → `GestionarAlumnos`. Cada uno es defendible por pertenencia, pero son decisiones tomadas por omisión: nadie dijo que quien gestiona cursos deba poder asignar el preceptor de una división. Se resuelve en la misma conversación que el punto 4.
2. **`PermisoDenegadoException` vive en `Core`, no en `Domain`.** Deliberado: no es un invariante de entidad sino una política de aplicación sobre la sesión, y `Domain` no conoce el concepto de sesión. Se señala porque rompe la convención de "una excepción por regla en `Domain/<Módulo>/Exceptions/`".
3. **Código muerto que parece el mecanismo de seguridad** (UI-38). `BasicIdentity`, `CustomPrincipal`, `AnonymousIdentity` y `AuthType` quedaron sin uso al salir `Thread.CurrentPrincipal`. No se borraron (no estaba pedido), y es peligroso dejarlos: alguien puede recablearlos creyendo que son el camino vigente. La Fase 6 **no los volvió a usar**: el arranque va por `ISesionUsuario`.
4. **La atribución de permisos no está relevada.** La matriz de la Fase 1 es una propuesta razonable de arranque, no una regla de negocio validada con el dominio del problema. Conviene confirmarla antes de que alguien la tome como especificación.

---

## Fuera de alcance (no hacer)

- Cambiar el `TargetFramework`. El upgrade a .NET 10 se discutió el 2026-10-11 y quedó como decisión aparte: la constitución declara "Stack fijo: WPF sobre .NET 7", así que es una enmienda, no un cambio incidental. Si se hace, el momento es **antes** de estabilizar migraciones, porque EF Core 7 → 10 cambia el snapshot.
- Corregir los 20 fallos de integración. Se registran en `todos.md` (TEST-09, TEST-10); son trabajo propio, no de este plan.
- Tablas `permiso`/`rol_permiso` y pantalla de administración de roles (decisión #1 y UI-19).
- Descomentar `ConfigureTableAccesos` en `UsuariosConfiguration.cs:104-135`. Es un vestigio de un diseño viejo: `Usuario.Accesos` y `Permiso` como propiedad de entidad **no existen** en el dominio. No es un modelo a medio construir que convenga revivir.
