using Infrastructure.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class InfrastructureDI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // DBContext
        services.AddDbContext<EdusisDBContext>(options =>
        {
            options.UseSqlServer("Data Source=localhost; Initial Catalog=EdusisDB; Integrated Security=True; Encrypt=True; TrustServerCertificate=True")
                   .LogTo(Console.WriteLine, LogLevel.Information);
        });

        // MEDIATOR
        services.AddScoped<IMediator, Mediator>();

        // UNIT OF WORK
        services.AddScoped<IUnitOfWork>(provider =>
            new UnitOfWork(provider.GetRequiredService<EdusisDBContext>(), 
            provider.GetRequiredService<IMediator>()));

        return services;
    }
}
