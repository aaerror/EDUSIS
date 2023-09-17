using System;
using System.ComponentModel;
using System.Diagnostics;

namespace WPF_Desktop.Shared;

public abstract class ViewModel : INotifyPropertyChanged, IDisposable
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
