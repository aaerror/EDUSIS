using System;

namespace WPF_Desktop.Store;

public class PerfilBuscadoStore
{
    private string _documento;

    public event Action PerfilStoreChanged;


    public string Documento
    {
        get
        {
            return _documento;
        }

        set
        {
            _documento = value;
            PerfilStoreChanged?.Invoke();
        }
    }
}
