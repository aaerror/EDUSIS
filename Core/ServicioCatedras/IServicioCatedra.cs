using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;

namespace Core.ServicioCatedras;

public interface IServicioCatedra
{
	Task<Guid> CrearCatedraAsync(CrearCatedraRequest request);

	Task<Guid> DesignarDocenteAsync(DesignarDocenteRequest request);

	Task PonerEnFuncionesAsync(PonerEnFuncionesRequest request);
	Task RelevarDeFuncionesAsync(RelevarDeFuncionesRequest request);

	Task EstablecerFinDeDesignacionAsync(EstablecerFinDeDesignacionRequest request);
	Task FinalizarDesignacionAsync(FinalizarDesignacionRequest request);

	Task AgregarHorarioAsync(AgregarHorarioRequest request);
	Task QuitarHorarioAsync(QuitarHorarioRequest request);

	Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunMateriaAsync(ListarCatedrasSegunMateriaRequest request);
	Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunDivisionAsync(ListarCatedrasSegunDivisionRequest request);
	Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunDocenteAsync(ListarCatedrasSegunDocenteRequest request);

	Task<IReadOnlyCollection<SituacionRevistaResponse>> ListarSituacionesRevistaAsync(ListarSituacionesRevistaRequest request);
	Task<IReadOnlyCollection<HorarioResponse>> ListarHorariosSegunCatedraAsync(ListarHorariosRequest request);
}
