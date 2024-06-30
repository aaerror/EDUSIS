﻿using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.Navigation.NavigationServices.Modal;

public class BuscarNavigationService<TViewModel> : INavigationService
    where TViewModel : ViewModel
{
    private Func<TViewModel> _viewModelFactory;
    private NavigationStore _navigationStore;


    public BuscarNavigationService(Func<TViewModel> viewModelFactory, NavigationStore navigationStore)
    {
        _viewModelFactory = viewModelFactory;
        _navigationStore = navigationStore;
    }

    public void Navigate()
    {
        _navigationStore.ViewModelActual = _viewModelFactory();
    }
}
