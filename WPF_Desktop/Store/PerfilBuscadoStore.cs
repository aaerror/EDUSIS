using System;

namespace WPF_Desktop.Store;

public class PerfilBuscadoStore
{
    private Guid _personaID;
    private string _documento;

    public event Action PerfilStoreChanged;


    public Guid PersonaID
    {
        get
        {
            return _personaID;
        }

        set
        {
            _personaID = value;
            PerfilStoreChanged?.Invoke();
        }
    }

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
