using Domain.Cursos;
using Domain.Docentes.DomainEvents;
using MediatR;

namespace Core.ServicioCursos.Events;

internal class LicenciaSolicitadaEventHandler : INotificationHandler<LicenciaSolicitadaEvent>
{
    private readonly ICursoRepository _cursoRepository;


    public LicenciaSolicitadaEventHandler(ICursoRepository cursoRepository)
    {
        _cursoRepository = cursoRepository;
    }

    public Task Handle(LicenciaSolicitadaEvent notification, CancellationToken cancellationToken)
    {
        // TODO: COMPLETAR EVENTO DE LICENCIAS SOLICITADAS
        //var docentes = _cursoRepository.
        return Task.CompletedTask;
    }
}
