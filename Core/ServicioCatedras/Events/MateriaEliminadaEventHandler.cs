using Domain.Materias.DomainEvents;
using Domain.Shared;
using MediatR;

namespace Core.ServicioCatedras.Events;

internal class MateriaEliminadaEventHandler : INotificationHandler<MateriaEliminadaEvent>
{
	private readonly IUnitOfWork _unitOfWork;


	public MateriaEliminadaEventHandler(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task Handle(MateriaEliminadaEvent notification, CancellationToken cancellationToken)
	{
		var catedrasHuerfanas = await _unitOfWork.Catedras.CatedrasSegunMateriaAsync(notification.MateriaID);

		_unitOfWork.Catedras.EliminarRango(catedrasHuerfanas);

		await _unitOfWork.GuardarCambiosAsync();
	}
}
