using Core.Shared.Documentos;
using Infrastructure.Documentos;
using Domain.Shared;
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
					   .LogTo(Console.WriteLine, LogLevel.Debug)
					   .EnableDetailedErrors(true);
			},
			ServiceLifetime.Scoped);

		// MEDIATOR
		//services.AddScoped<IMediator, Mediator>();
		services.AddMediatR(x => 
			x.RegisterServicesFromAssemblyContaining<EdusisDBContext>());

		// UNIT OF WORK
		services.AddScoped<IUnitOfWork>(provider =>
			new UnitOfWork(provider.GetRequiredService<EdusisDBContext>(), 
			provider.GetRequiredService<IMediator>()));

		services.AddScoped<IPortableDocumentFormat>(provider =>
			new PortableDocumentFormat(@"C:\\edusis\docs\"));

		services.AddScoped<IGeneradorDocumentos>(provider =>
			new GeneradorDocumentos(provider.GetRequiredService<IPortableDocumentFormat>()));

		return services;
	}
}