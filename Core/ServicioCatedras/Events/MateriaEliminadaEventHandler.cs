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

		// Entre catedra y situacion_revista hay dos FK cruzadas: FK_SITUACION_REVISTA_CATEDRA
		// (situacion_revista.catedra_id → catedra, en cascada) y FK_CATEDRA_SITUACION_REVISTA
		// (catedra.situacion_en_funciones_id → clave alterna de situacion_revista). Si las dos filas
		// caen en Deleted dentro del mismo SaveChangesAsync, EF no encuentra un orden válido para los
		// DELETE y aborta con "circular dependency was detected". Hay que soltar la referencia y
		// guardarla ANTES de marcar el borrado: el mismo recurso que Tools/EDUSIS.Seed/Limpiador.cs
		// aplica por SQL. El Where no es opcional — RelevarDeFunciones lanza DocenteSinCargoException
		// cuando no hay nadie en funciones, que es el caso de las cátedras que hoy sí se borran bien.
		foreach (var catedra in catedrasHuerfanas.Where(catedra => catedra.SituacionEnFuncionesID.HasValue))
		{
			catedra.RelevarDeFunciones();
		}

		await _unitOfWork.GuardarCambiosAsync();

		_unitOfWork.Catedras.EliminarRango(catedrasHuerfanas);

		await _unitOfWork.GuardarCambiosAsync();
	}
}
