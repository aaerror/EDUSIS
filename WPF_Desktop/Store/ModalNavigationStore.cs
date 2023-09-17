using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.Store;

public class ModalNavigationStore
{
    private ViewModel _viewModelActual;

    public bool EstaAbierto => _viewModelActual != null;

    public event Action ViewModelActualChanged;


    public ViewModel ViewModelActual
    {
        get
        {
            return _viewModelActual;
        }

        set
        {
            _viewModelActual?.Dispose();
            _viewModelActual = value;
            OnVistaActualChanged();
        }
    }

    public void Cerrar()
    {
        ViewModelActual = null;
    }

    public void OnVistaActualChanged()
    {
        ViewModelActualChanged?.Invoke();
    }
}
