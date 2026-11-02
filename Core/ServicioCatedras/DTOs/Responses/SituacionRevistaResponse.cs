namespace Core.ServicioCatedras.DTOs.Responses;

/// <summary>
/// Una designación de la cátedra, vigente o histórica.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Vigente"/>, <see cref="EsOcupante"/> y <see cref="TieneSuplenteVigente"/> son cálculos
/// del DOMINIO proyectados acá, no datos nuevos: salen de <c>SituacionRevista.EsCargoVigente()</c>,
/// <c>SituacionRevista.EsOcupante()</c> y <c>Catedra.TieneSuplenteVigente()</c>. Viajan en el response
/// porque la pantalla los necesitaba y los estaba reimplementando: la vigencia se deducía comparando
/// <c>Estado</c> contra el texto "Finalizado" —que no es lo mismo, una designación aceptada con
/// período terminado también está vencida— y la regla del suplente no se conocía en absoluto.
/// </para>
/// <para>
/// <see cref="ReemplazaA"/> es el identificador de la DESIGNACIÓN reemplazada, no del docente.
/// <see cref="ReemplazaADocenteID"/> resuelve ese salto, porque mostrar "suple a" pide un nombre y
/// hacer el salto en la vista obligaría a tener el listado entero a mano para recorrerlo.
/// </para>
/// </remarks>
public record SituacionRevistaResponse(
	Guid SituacionRevistaID,
	Guid CatedraID,
	Guid DocenteID,
	string Estado,
	string Cargo,
	DateTime FechaInicio,
	DateTime? FechaFin,
	Guid? ReemplazaA,
	bool EnFunciones,
	bool Vigente,
	bool EsOcupante,
	Guid? ReemplazaADocenteID,
	bool TieneSuplenteVigente);
