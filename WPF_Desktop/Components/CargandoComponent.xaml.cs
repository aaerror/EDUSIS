using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class CargandoComponent : UserControl
{
	public static readonly DependencyProperty EstaCargandoProperty = DependencyProperty.Register(nameof(EstaCargando), typeof(bool), typeof(CargandoComponent), new PropertyMetadata(false));
	public static readonly DependencyProperty LeyendaProperty = DependencyProperty.Register(nameof(Leyenda), typeof(string), typeof(CargandoComponent), new PropertyMetadata("CARGANDO"));


	public CargandoComponent()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Se liga a <c>IAsyncRelayCommand.IsRunning</c> del comando de carga de la pantalla.
	/// </summary>
	/// <remarks>
	/// CommunityToolkit ya expone esa propiedad en todos los <c>AsyncRelayCommand</c>, que es
	/// lo que usa casi toda la capa: no hace falta una bandera nueva en cada ViewModel.
	/// </remarks>
	public bool EstaCargando
	{
		get
		{
			return (bool) GetValue(EstaCargandoProperty);
		}

		set
		{
			SetValue(EstaCargandoProperty, value);
		}
	}

	/// <summary>Qué se está esperando: "CARGANDO", "GUARDANDO", "EMITIENDO EL PDF".</summary>
	public string Leyenda
	{
		get
		{
			return (string) GetValue(LeyendaProperty);
		}

		set
		{
			SetValue(LeyendaProperty, value);
		}
	}
}
