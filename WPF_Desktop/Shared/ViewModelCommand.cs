using System;
using System.Windows.Input;

namespace WPF_Desktop.Shared;

public class ViewModelCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    // Event
    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public ViewModelCommand(Action<object> executeAction) : this(executeAction, null) { }

    public ViewModelCommand(Action<object> executeAction, Predicate<object> canExecuteAction)
    {
        _execute = executeAction;
        _canExecute = canExecuteAction;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null ? true : _canExecute(parameter);
    }

    public virtual void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute(parameter);
        }
    }
}
