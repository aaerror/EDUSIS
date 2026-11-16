using Core.Shared.Documentos;
using Infrastructure.Documentos;
using Infrastructure.Documentos.Configuracion;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureDI
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		// DBContext
		// Sin LogTo: EF toma el ILoggerFactory del contenedor, así que cada composition root decide
		// nivel y destino con AddFilter. El LogTo(Console.WriteLine, Debug) que había escribía por
		// fuera de ese factory y nadie podía silenciarlo (el seeder tenía que re-registrar el
		// contexto sólo para esquivarlo).
		services.AddDbContext<EdusisDBContext>(options =>
			{
				options.UseSqlServer("Data Source=localhost;Initial Catalog=EdusisDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Application Name='Edusis';Command Timeout=0")
					   .EnableDetailedErrors(true);
			},
			ServiceLifetime.Scoped);

		// MEDIATOR
		//services.AddScoped<IMediator, Mediator>();
		services.AddMediatR(x =>
			x.RegisterServicesFromAssemblies(typeof(EdusisDBContext).Assembly, typeof(IGeneradorDocumentos).Assembly));

		// UNIT OF WORK
		services.AddScoped<IUnitOfWork>(provider =>
			new UnitOfWork(provider.GetRequiredService<EdusisDBContext>(), 
			provider.GetRequiredService<IMediator>()));

		// DOCUMENTOS
		// Los datos del establecimiento salen de la sección <institucion> de App.config y no
		// cambian durante la ejecución: Singleton para leer el archivo una sola vez.
		services.AddSingleton<IDatosInstitucionales, DatosInstitucionalesConfigurados>();

		services.AddScoped<IPortableDocumentFormat>(provider =>
			new PortableDocumentFormat(provider.GetRequiredService<IDatosInstitucionales>()));

		services.AddScoped<IGeneradorDocumentos>(provider =>
			new GeneradorDocumentos(provider.GetRequiredService<IPortableDocumentFormat>(),
									provider.GetRequiredService<IDatosInstitucionales>()));

		return services;
	}
}