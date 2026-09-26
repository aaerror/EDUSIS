using Core.ServicioAlumnos;
using Core.ServicioAutenticaciones;
using Core.ServicioCatedras;
using Core.ServicioCurriculas;
using Core.ServicioCursos;
using Core.ServicioDocentes;
using Core.ServicioDocumentos;
using Core.ServicioLicencias;
using Core.ServicioMaterias;
using Core.ServicioSecurity;
using Core.ServicioUsuarios;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Shared;

public static class CoreDI
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IServicioAlumno, ServicioAlumno>();
        services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        services.AddScoped<IServicioCurricula, ServicioCurricula>();
        services.AddScoped<IServicioCurso, ServicioCurso>();
        services.AddScoped<IServicioDocente, ServicioDocente>();
        services.AddScoped<IServicioLicencia, ServicioLicencia>();
        services.AddScoped<IServicioSeguridad, ServicioSeguridad>();
        services.AddScoped<IServicioUsuario, ServicioUsuario>();
        services.AddScoped<IServicioDocumento, ServicioDocumento>();
        services.AddScoped<IServicioMateria, ServicioMateria>();
        services.AddScoped<IServicioCatedra, ServicioCatedra>();

        return services;
    }
}
