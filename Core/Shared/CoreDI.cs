using Core.ServicioAlumnos;
using Core.ServicioAsistencias;
using Core.ServicioAutenticaciones;
using Core.ServicioCatedras;
using Core.ServicioCurriculas;
using Core.ServicioCursantes;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDocentes;
using Core.ServicioDocumentos;
using Core.ServicioLicencias;
using Core.ServicioMaterias;
using Core.ServicioReportes;
using Core.ServicioSeguridad;
using Core.ServicioUsuarios;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Shared;

public static class CoreDI
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IServicioAlumno, ServicioAlumno>();
        services.AddScoped<IServicioAsistencia, ServicioAsistencia>();
        services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        services.AddScoped<IServicioCurricula, ServicioCurricula>();
        services.AddScoped<IServicioCursante, ServicioCursante>();
        services.AddScoped<IServicioCurso, ServicioCurso>();
        services.AddScoped<IServicioDivision, ServicioDivision>();
        services.AddScoped<IServicioDocente, ServicioDocente>();
        services.AddScoped<IServicioLicencia, ServicioLicencia>();
        // Calificado: el nombre simple `ServicioSeguridad` se resuelve al NAMESPACE
        // `Core.ServicioSeguridad` —visible desde acá por el contenedor `Core`— y no al tipo
        // homónimo que vive dentro (CS0118). Es el único módulo donde el segmento del namespace
        // coincide con su clase: el resto pluraliza (`Core.ServicioAlumnos` → `ServicioAlumno`) y
        // "Seguridad" no pluraliza con naturalidad.
        services.AddScoped<IServicioSeguridad, Core.ServicioSeguridad.ServicioSeguridad>();
        services.AddScoped<IServicioUsuario, ServicioUsuario>();
        services.AddScoped<IServicioDocumento, ServicioDocumento>();
        services.AddScoped<IServicioReporte, ServicioReporte>();
        services.AddScoped<IServicioMateria, ServicioMateria>();
        services.AddScoped<IServicioCatedra, ServicioCatedra>();

        return services;
    }
}
