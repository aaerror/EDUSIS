using Domain.Licencias.DomainEvents;
using Domain.Shared;
using MediatR;

namespace Core.ServicioCatedras.Events;

/// <summary>
/// Al finalizar la licencia de un docente, vuelve a funciones en cada cátedra que lo esperaba: la
/// que quedó sin nadie al frente o la que cubre su suplente. Es la contracara de
/// <see cref="LicenciaActivadaEventHandler"/>.
/// </summary>
/// <remarks>
/// <para>
/// Qué cátedra lo espera lo decide <c>Catedra.ReponerTrasLicencia</c>: el handler sólo recorre las
/// cátedras donde el docente tiene alguna designación. No hace falta mirar otras licencias del
/// docente: un docente no puede tener dos licencias en el mismo período (lo controla
/// <c>ServicioLicencia</c>), así que al terminar una no hay otra en curso.
/// </para>
/// <para>
/// Mismo molde transaccional que el de activación: no guarda por su cuenta, y el regreso se persiste
/// en el mismo guardado que finaliza la licencia.
/// </para>
/// </remarks>
internal class LicenciaFinalizadaEventHandler : INotificationHandler<LicenciaFinalizadaEvent>
{
	private readonly IUnitOfWork _unitOfWork;


	public LicenciaFinalizadaEventHandler(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task Handle(LicenciaFinalizadaEvent notification, CancellationToken cancellationToken)
	{
		var catedras = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(notification.DocenteID);

		foreach (var catedra in catedras)
		{
			catedra.ReponerTrasLicencia(notification.DocenteID);
		}
	}
}
