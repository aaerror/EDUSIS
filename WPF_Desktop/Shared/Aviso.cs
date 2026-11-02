using CommunityToolkit.Mvvm.Input;
using System;

namespace WPF_Desktop.Shared;

/// <summary>Qué clase de aviso es. Define el color de la barra y cuánto tarda en irse solo.</summary>
internal enum TipoDeAviso
{
	Exito,
	Advertencia
}

/// <summary>
/// Un aviso al pie: lo que la aplicación responde cuando una operación sale bien o sale
/// regular, sin interrumpir lo que el usuario está haciendo.
/// </summary>
/// <remarks>
/// No lleva el error: un error exige acuse y va al diálogo. Un aviso que se va solo sirve para
/// confirmar lo que el usuario acaba de pedir —ya sabe qué hizo, sólo necesita saber que pasó—,
/// y es exactamente lo contrario de lo que hace falta cuando algo falló.
/// </remarks>
internal class Aviso
{
	public Aviso(TipoDeAviso tipo, string titulo, string mensaje, Action<Aviso> descartar)
	{
		Tipo = tipo;
		Titulo = titulo;
		Mensaje = mensaje;
		DescartarCommand = new RelayCommand(() => descartar(this));
	}

	public TipoDeAviso Tipo { get; }

	public string Titulo { get; }

	/// <summary>Detalle opcional. Vacío cuando el título ya lo dice todo.</summary>
	public string Mensaje { get; }

	public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

	public IRelayCommand DescartarCommand { get; }
}
