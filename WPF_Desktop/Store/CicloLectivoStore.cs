using System;

namespace WPF_Desktop.Store;

internal class CicloLectivoStore
{
	private string _cicloLectivo = DateTime.Today.Year.ToString();

	public event Action CicloLectivoStoreChanged;


	public string CicloLectivo
	{
		get
		{
			return _cicloLectivo;
		}

		set
		{
			_cicloLectivo = value;
			CicloLectivoStoreChanged?.Invoke();
		}
	}
}
