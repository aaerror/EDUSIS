using Domain.Shared;
using MediatR;

namespace Infrastructure.Extensions;

internal static class MediatrExtension
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, EdusisDBContext context)
    {
        var domainEvents = context.ChangeTracker.Entries<Entity>()
                                                .Where(x => x.Entity.Eventos is not null && x.Entity.Eventos.Any())
                                                .Select(x => x.Entity)
                                                .SelectMany(x =>
                                                {
                                                    var domainEvents = x.Eventos.ToList();
                                                    x.LiberarEventos();

                                                    return domainEvents;
                                                }).ToList();

        if (domainEvents.Any())
        {
            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent);
            }
        }
    }
}
