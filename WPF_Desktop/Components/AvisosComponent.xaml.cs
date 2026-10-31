using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class AvisosComponent : UserControl
{
	public static readonly DependencyProperty AvisosProperty = DependencyProperty.Register(nameof(Avisos), typeof(IEnumerable), typeof(AvisosComponent), new PropertyMetadata(null));


	public AvisosComponent()
	{
		InitializeComponent();
	}

	public IEnumerable? Avisos
	{
		get
		{
			return (IEnumerable?) GetValue(AvisosProperty);
		}

		set
		{
			SetValue(AvisosProperty, value);
		}
	}
}
