<p align="center">
	<img src="docs/assets/edusis-logo.svg" alt="EDUSIS" width="420">
</p>

<p align="center">
	<b>Sistema de gestión escolar de escritorio</b><br>
	Docentes, alumnos, cursos, divisiones, currículas y licencias.
</p>

<p align="center">
	<img src="https://img.shields.io/badge/.NET-7.0-512BD4" alt=".NET 7">
	<img src="https://img.shields.io/badge/UI-WPF-0C54C2" alt="WPF">
	<img src="https://img.shields.io/badge/BD-SQL%20Server-CC2927" alt="SQL Server">
	<img src="https://img.shields.io/badge/ORM-EF%20Core-6DB33F" alt="EF Core">
	<img src="https://img.shields.io/badge/arquitectura-DDD%20por%20capas-2B7489" alt="DDD por capas">
</p>

---

## Índice

- [Descripción](#descripción)
- [Arquitectura](#arquitectura)
- [Módulos](#módulos)
- [Stack](#stack)
- [Requisitos](#requisitos)
- [Puesta en marcha](#puesta-en-marcha)
- [Migraciones EF Core](#migraciones-ef-core)
- [Pruebas](#pruebas)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Convenciones](#convenciones)

## Descripción

**EDUSIS** es una aplicación de escritorio **WPF sobre .NET 7** con persistencia en **SQL Server** para la gestión administrativa de una institución educativa: legajos de
docentes y alumnos, puestos y situación de revista, cursos y divisiones, currículas y materias, inscripción de cursantes, licencias y usuarios.

El dominio está modelado con **DDD táctico** (modelo rico, invariantes en las entidades, eventos de dominio) y la aplicación se organiza en **casos de uso** por módulo.


## Arquitectura

Cuatro proyectos en capas con la **dependencia invertida**: `Infrastructure` referencia `Core`, no al revés. Los puertos (`IUnitOfWork`, `IRepository<T>`, `I<Agregado>Repository`) viven en `Domain.Shared`; `Infrastructure` los implementa.

```
WPF_Desktop (net7.0-windows, UI)  ──►  Core (Application)  ──►  Domain
        │                                     ▲                   ▲
        └──►  Infrastructure  ────────────────┴───────────────────┘
```

> ⚠️ La carpeta es `Application/` pero el assembly y el namespace raíz son **`Core`**.

| Proyecto | Rol |
| :--- | :--- |
| `Domain` | Entidades, value objects, excepciones de dominio y eventos. Solo referencia MediatR. |
| `Application` (`Core`) | Un servicio por caso de uso (`Servicio<X>` + `IServicio<X>`), DTOs `record` y handlers de eventos. |
| `Infrastructure` | EF Core (`EdusisDBContext`), `UnitOfWork`, repositorios `internal` y despacho de eventos de dominio. |
| `WPF_Desktop` | MVVM con CommunityToolkit. Composition root (`App.xaml.cs`), navegación por `NavigationStore` + `DataTemplate`. |


## Módulos

`Alumnos` · `Docentes` (+ `Puestos`) · `Cursos` (con sus divisiones) · `Curriculas` (+ `Materias`) ·
`Cursantes` · `Licencias` · `Personas` · `Usuarios`

`Persona` es la base de `Alumno` y `Docente` (jerarquía mapeada con TPT).

## Stack

- **.NET 7** (`net7.0` / `net7.0-windows`)
- **WPF** + [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Entity Framework Core** sobre **SQL Server**
- **MediatR** para eventos de dominio
- **iText** para generación de PDF, aislado tras `Core.Shared.Documentos.IGeneradorDocumentos`
- Pruebas: **xUnit** + **Testcontainers** (SQL Server)

## Requisitos

- **SDK de .NET** (los proyectos apuntan a `net7.0`; un SDK más nuevo compila por *roll-forward*, con warning `NETSDK1138` de EOL que **no** hay que silenciar cambiando el TFM).
- **SQL Server** accesible en `localhost` con una base `EdusisDB` (la cadena de conexión está **hardcodeada** en `Infrastructure/InfrastructureDI.cs`, con `Integrated Security`).
- **Ejecutar la app: solo en Windows.** El resto (build de toda la solución, incluido `WPF_Desktop`, y la suite de pruebas) corre también en Linux gracias a `<EnableWindowsTargeting>true</EnableWindowsTargeting>`.
- Para pruebas de integración / E2E: **Docker** o la variable `EDUSIS_TEST_SQLSERVER`. Sin eso, esas categorías se **omiten** (skip, salida 0).

## Puesta en marcha

```bash
# Compilar la solución completa (Linux o Windows)
dotnet build EDUSIS.sln

# Ejecutar la aplicación (SOLO Windows)
dotnet run --project WPF_Desktop/WPF_Desktop.csproj
```

Rutas y credenciales fijas en código a tener en cuenta:

| Qué | Dónde | Valor |
| :--- | :--- | :--- |
| Cadena de conexión | `Infrastructure/InfrastructureDI.cs` | `localhost` / `EdusisDB`, Integrated Security |
| Salida de PDFs | `Infrastructure/InfrastructureDI.cs` | `C:\edusis\docs\` |

## Migraciones EF Core

El proyecto de migraciones es `Infrastructure`, pero el *startup project* es `WPF_Desktop` (es quien registra el DI).

Desde Windows:

```bash
dotnet ef migrations add <Nombre> --project Infrastructure --startup-project WPF_Desktop
dotnet ef database update --project Infrastructure --startup-project WPF_Desktop
```

## Pruebas

La suite vive en `tests/` (5 proyectos) y está segmentada por `[Trait("Categoria", ...)]` en `Unidad` / `Integracion` / `E2E`. `Unidad` corre siempre; `Integracion` y `E2E` se omiten si no hay Docker ni `EDUSIS_TEST_SQLSERVER`.

```bash
dotnet test EDUSIS.sln            # todas las categorías aplicables al entorno
./tests/run-tests.sh --rapidas    # sólo Categoria=Unidad (dominio + Core, sin BD, < 60 s)
./tests/run-tests.sh --todas      # = dotnet test EDUSIS.sln
./tests/run-tests.sh --cobertura  # + reporte HTML en tests/CoverageReport/
```

**Puertas de calidad:** `dotnet build EDUSIS.sln` **y** `dotnet test EDUSIS.sln` (no hay linter configurado).

## Estructura del repositorio

```
EDUSIS/
├─ Domain/            # Modelo de dominio (DDD táctico)
├─ Application/       # Assembly "Core": servicios de caso de uso + DTOs
├─ Infrastructure/    # EF Core, UnitOfWork, repositorios, eventos
├─ WPF_Desktop/       # UI WPF (MVVM + CommunityToolkit)
├─ tests/             # EDUSIS.TestSupport, Domain.UnitTests, Core.UnitTests, Infrastructure.IntegrationTests, EDUSIS.EndToEndTests
├─ specs/             # Especificaciones de features (spec-kit)
├─ docs/              # Recursos de documentación (logo, imágenes)
└─ EDUSIS.sln
```
