using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF_Desktop.Shared.Commands;

internal abstract class ViewModelAsyncCommandBase : ICommand
{
	private readonly Action<Exception> _onException;
	private bool _isExecuting;

	public event EventHandler? CanExecuteChanged;


	public ViewModelAsyncCommandBase(Action<Exception> onException)
	{
		_onException = onException;
	}

	public bool IsExecuting
	{
		get
		{
			return _isExecuting;
		}

		set
		{
			_isExecuting = value;
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}

	public bool CanExecute(object? parameter)
	{
		return !IsExecuting;
	}

	public async void Execute(object? parameter)
	{
		IsExecuting = true;

		try
		{
			await ExecuteAsync(parameter);
		}
		catch (Exception ex)
		{
			_onException?.Invoke(ex);
		}

		IsExecuting = false;
	}

	protected abstract Task ExecuteAsync(object parameter);
}