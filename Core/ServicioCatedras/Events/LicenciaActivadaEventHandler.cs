using Domain.Licencias.DomainEvents;
using Domain.Shared;
using MediatR;

namespace Core.ServicioCatedras.Events;

/// <summary>
/// Al activarse la licencia de un docente, las cátedras donde está en funciones quedan sin
/// docente en funciones (cada una emite <c>CatedraSinDocenteEnFuncionesEvent</c>). La designación
/// sigue vigente: al volver, se lo pone en funciones de nuevo; mientras tanto se pone en funciones
/// al suplente.
/// </summary>
/// <remarks>
/// No guarda por su cuenta, a diferencia de <c>MateriaEliminadaEventHandler</c>: el despacho corre
/// dentro de <c>GuardarCambiosAsync</c>, antes de <c>SaveChangesAsync</c>, y las cátedras que trae
/// el repositorio quedan rastreadas, así que el relevo se persiste en el mismo guardado que aprueba
/// la licencia. Si ese guardado falla, no queda ni la licencia activa ni el relevo.
/// <para>
/// Se filtra por <c>DocenteEnFunciones()</c> y no sólo por la consulta: el repositorio devuelve
/// toda cátedra donde el docente tiene alguna designación, y <c>RelevarDeFunciones</c> sobre una
/// cátedra con otro docente en funciones lo sacaría a él.
/// </para>
/// </remarks>
internal class LicenciaActivadaEventHandler : INotificationHandler<LicenciaActivadaEvent>
{
	private readonly IUnitOfWork _unitOfWork;


	public LicenciaActivadaEventHandler(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task Handle(LicenciaActivadaEvent notification, CancellationToken cancellationToken)
	{
		var catedras = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(notification.DocenteID);

		foreach (var catedra in catedras.Where(catedra => notification.DocenteID.Equals(catedra.DocenteEnFunciones())))
		{
			catedra.RelevarDeFunciones();
		}
	}
}
