using Domain.Shared;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Contrato de <see cref="Entity"/>: identidad por <see cref="Entity.Id"/> y cola de eventos de
/// dominio (<c>AgregarEvento</c> / <c>QuitarEvento</c> / <c>LiberarEventos</c> / <c>Eventos</c>).
/// Se ejercita con una entidad de prueba mínima definida en este archivo, sin tocar agregados
/// reales.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class EntityTests
{
	#region Identidad
	[Fact]
	public void Dos_entidades_con_el_mismo_Id_son_iguales()
	{
		var id = Guid.NewGuid();
		var una = new EntidadDePrueba(id);
		var otra = new EntidadDePrueba(id);

		una.Equals(otra).ShouldBeTrue();
		(una == otra).ShouldBeTrue();
		(una != otra).ShouldBeFalse();
		una.GetHashCode().ShouldBe(otra.GetHashCode());
	}

	[Fact]
	public void Dos_entidades_con_distinto_Id_no_son_iguales()
	{
		var una = new EntidadDePrueba(Guid.NewGuid());
		var otra = new EntidadDePrueba(Guid.NewGuid());

		una.Equals(otra).ShouldBeFalse();
		(una == otra).ShouldBeFalse();
		(una != otra).ShouldBeTrue();
	}

	[Fact]
	public void Una_entidad_no_es_igual_a_null()
	{
		var una = new EntidadDePrueba(Guid.NewGuid());
		EntidadDePrueba? ninguna = null;

		una.Equals(ninguna).ShouldBeFalse();
		(una == ninguna).ShouldBeFalse();
		(ninguna == una).ShouldBeFalse();
	}

	[Fact]
	public void La_igualdad_ignora_el_estado_no_identitario()
	{
		var id = Guid.NewGuid();
		var una = new EntidadDePrueba(id) { Etiqueta = "árbol" };
		var otra = new EntidadDePrueba(id) { Etiqueta = "camión" };

		una.Equals(otra).ShouldBeTrue();
	}

	/// <remarks>
	/// Comparar sólo por <see cref="Entity.Id"/> hacía iguales a todas las entidades sin
	/// identidad asignada, porque comparten <c>Guid.Empty</c>.
	/// </remarks>
	[Fact]
	public void Dos_entidades_transitorias_distintas_no_son_iguales()
	{
		var una = new EntidadDePrueba(Guid.Empty);
		var otra = new EntidadDePrueba(Guid.Empty);

		una.Equals(otra).ShouldBeFalse();
		(una == otra).ShouldBeFalse();
	}

	[Fact]
	public void Una_entidad_transitoria_es_igual_a_si_misma()
	{
		var una = new EntidadDePrueba(Guid.Empty);

		una.Equals(una).ShouldBeTrue();
		una.GetHashCode().ShouldBe(una.GetHashCode());
	}
	#endregion

	#region Cola de eventos
	[Fact]
	public void Una_entidad_recien_creada_expone_una_cola_vacia_y_no_null()
	{
		var entidad = new EntidadDePrueba(Guid.NewGuid());

		entidad.Eventos.ShouldNotBeNull();
		entidad.Eventos.ShouldBeEmpty();
	}

	[Fact]
	public void AgregarEvento_encola_el_evento_y_queda_visible_en_Eventos()
	{
		var entidad = new EntidadDePrueba(Guid.NewGuid());
		var evento = new EventoDePrueba();

		entidad.Encolar(evento);

		entidad.Eventos.ShouldNotBeNull();
		entidad.Eventos.ShouldHaveSingleItem().ShouldBeSameAs(evento);
	}

	[Fact]
	public void QuitarEvento_remueve_solo_el_evento_indicado()
	{
		var entidad = new EntidadDePrueba(Guid.NewGuid());
		var primero = new EventoDePrueba();
		var segundo = new EventoDePrueba();
		entidad.Encolar(primero);
		entidad.Encolar(segundo);

		entidad.Descolar(primero);

		entidad.Eventos.ShouldHaveSingleItem().ShouldBeSameAs(segundo);
	}

	[Fact]
	public void LiberarEventos_vacia_la_cola()
	{
		var entidad = new EntidadDePrueba(Guid.NewGuid());
		entidad.Encolar(new EventoDePrueba());
		entidad.Encolar(new EventoDePrueba());

		entidad.LiberarEventos();

		entidad.Eventos.ShouldBeEmpty();
	}

	[Fact]
	public void Eventos_no_permite_modificar_la_cola_interna_desde_afuera()
	{
		var entidad = new EntidadDePrueba(Guid.NewGuid());
		entidad.Encolar(new EventoDePrueba());

		var vista = (ICollection<IDomainEvent>) entidad.Eventos;

		Should.Throw<NotSupportedException>(() => vista.Add(new EventoDePrueba()));
		Should.Throw<NotSupportedException>(() => vista.Clear());
		entidad.Eventos.Count.ShouldBe(1);
	}
	#endregion
}

#region Dobles de prueba
internal sealed class EntidadDePrueba : Entity
{
	public EntidadDePrueba(Guid id)
		: base(id)
	{
	}

	public string Etiqueta { get; set; } = string.Empty;

	public void Encolar(IDomainEvent evento) =>
		AgregarEvento(evento);

	public void Descolar(IDomainEvent evento) =>
		QuitarEvento(evento);
}

internal sealed class EventoDePrueba : IDomainEvent
{
}
#endregion
