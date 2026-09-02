using System;
using System.Windows;
using WPF_Desktop.Shared.Converters;

namespace WPF_Desktop.Windows;

public partial class MainWindow : Window
{
	public class BooleanToStringConverter : BooleanConverter<String> { }

	public MainWindow()
	{
		InitializeComponent();
		//var img = new Uri($"pack://application:,,,../Resources/Icons/bell-ring-icon.png");
		//this.Icon = BitmapFrame.Create(img);
	}
}