using Core.ServicioAlumnos;
using Core.ServicioAutenticaciones;
using Core.ServicioCursos;
using Core.ServicioDocentes;
using Core.ServicioMaterias;
using Core.ServicioSeguridades;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Shared;

public static class CoreDI
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        services.AddScoped<IServicioSeguridad, ServicioSeguridad>();
        services.AddScoped<IServicioAlumno, ServicioAlumno>();
        services.AddScoped<IServicioDocente, ServicioDocente>();
        services.AddScoped<IServicioCurso, ServicioCurso>();
        services.AddScoped<IServicioMateria, ServicioMateria>();

        return services;
    }
}
