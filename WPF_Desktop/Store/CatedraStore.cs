using System;

namespace WPF_Desktop.Store;

internal class CatedraStore
{
	private Guid _catedra = Guid.Empty;

	public event Action CatedraStoreChanged;


	public Guid Catedra
	{
		get
		{
			return _catedra;
		}

		set
		{
			_catedra = value;
			CatedraStoreChanged?.Invoke();
		}
	}
}
