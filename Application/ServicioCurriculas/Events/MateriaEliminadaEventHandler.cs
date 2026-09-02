using MediatR;
using Domain.Curriculas;
using Domain.Cursos.DomainEvents;

namespace Core.ServicioCurriculas.Events;

internal class MateriaEliminadaEventHandler : INotificationHandler<MateriaEliminadaEvent>
{
    private readonly ICurriculaRepository _materiaRepository;


    public MateriaEliminadaEventHandler(ICurriculaRepository materiaRepository)
    {
        _materiaRepository = materiaRepository;
    }

    public Task Handle(MateriaEliminadaEvent notification, CancellationToken cancellationToken)
    {
        var materia = _materiaRepository.BuscarPorIDAsync(notification.MateriaID);

        _materiaRepository.Eliminar(materia.Id);

        return Task.CompletedTask;
    }
}
