using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.Store;

public class NavigationStore
{
    private ViewModel _viewModelActual;

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

    public void OnVistaActualChanged()
    {
        ViewModelActualChanged?.Invoke();
    }

}
