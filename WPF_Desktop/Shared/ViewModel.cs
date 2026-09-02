using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace WPF_Desktop.Shared;

/**
 * https://learn.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern
 */
public abstract class ViewModel : ObservableObject, IDisposable
{
	public virtual event PropertyChangedEventHandler? PropertyChanged;


	protected virtual void OnPropertyChanged(string propertyName)
	{
		VerificarPropertyName(propertyName);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	[Conditional("DEBUG")]
	private void VerificarPropertyName(string propertyName)
	{
		// Verify that the property name matches a real, public, instance property on this object.
		if (TypeDescriptor.GetProperties(this)[propertyName] == null)
		{
			throw new ArgumentNullException(GetType().Name + " does not contain property: " + propertyName);
		}
	}

	public virtual void Dispose() { }
}