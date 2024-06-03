using Domain.Cursos.DomainEvents;
using Domain.Materias;
using MediatR;

namespace Core.ServicioMaterias.Events;

internal class MateriaEliminadaEventHandler : INotificationHandler<MateriaEliminadaEvent>
{
    private readonly IMateriaRepository _materiaRepository;


    public MateriaEliminadaEventHandler(IMateriaRepository materiaRepository)
    {
        _materiaRepository = materiaRepository;
    }

    public Task Handle(MateriaEliminadaEvent notification, CancellationToken cancellationToken)
    {
        var materia = _materiaRepository.BuscarPorID(notification.materiaID);

        _materiaRepository.Eliminar(materia.Id);

        return Task.CompletedTask;
    }
}
