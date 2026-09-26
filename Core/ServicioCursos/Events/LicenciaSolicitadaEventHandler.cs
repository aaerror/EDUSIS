using Domain.Licencias.DomainEvents;
using Domain.Shared;
using MediatR;

namespace Core.ServicioCursos.Events;

internal class LicenciaSolicitadaEventHandler : INotificationHandler<LicenciaSolicitadaEvent>
{
    private readonly IUnitOfWork _unitOfWork;


    public LicenciaSolicitadaEventHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task Handle(LicenciaSolicitadaEvent notification, CancellationToken cancellationToken)
    {
        // TODO: COMPLETAR EVENTO DE LICENCIAS SOLICITADAS
        //var docentes = _unitOfWork.Cursos.
        return Task.CompletedTask;
    }
}
