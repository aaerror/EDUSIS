using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class GestionDocentesViewModel : ViewModel, INotifyDataErrorInfo
{
    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    #region Commands
    public ViewModelCommand RegistrarDocenteCommand { get; }
    #endregion



    public GestionDocentesViewModel(INavigationService registrarDocenteNavigationService)
    {
        RegistrarDocenteCommand = new ViewModelCommand(commnad =>
        {
            registrarDocenteNavigationService.Navigate();
        });
    }
    
    
    

    
    
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;



    public IEnumerable GetErrors(string? propertyName)
    {
        throw new NotImplementedException();
    }
}
