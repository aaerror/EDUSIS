using Domain.Shared;
using MediatR;

namespace Infrastructure.Extensions;

internal static class MediatrExtension
{
	public static async Task DispatchDomainEventsAsync(this IMediator mediator, EdusisDBContext context)
	{
		while (true)
		{
			var entidades = context.ChangeTracker.Entries<Entity>()
				.Select(x => x.Entity)
				.Where(x => x.Eventos is not null && x.Eventos.Any())
				.ToList();

			if (!entidades.Any())
			{
				break;
			}

			var eventos = entidades.SelectMany(x => x.Eventos).ToList();
			entidades.ForEach(x => x.LiberarEventos());

			foreach (var evento in eventos)
			{
				await mediator.Publish(evento);
			}
		}
	}
}
