using System;
using System.Threading.Tasks;

namespace WPF_Desktop.Shared.Commands;

internal class ViewModelAsyncRelayCommand : ViewModelAsyncCommandBase
{
	private readonly Func<Task> _callback;


	public ViewModelAsyncRelayCommand(Func<Task> callback, Action<Exception> onException)
		: base(onException)
	{
		_callback = callback;
	}

	protected async override Task ExecuteAsync(object parameter) =>
		await _callback();
}