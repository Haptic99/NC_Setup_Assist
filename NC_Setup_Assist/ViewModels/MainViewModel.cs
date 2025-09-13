// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        // Das "Gedächtnis" für den Zurück-Button
        private Stack<ViewModelBase> _navigationHistory = new Stack<ViewModelBase>();

        public MainViewModel()
        {
            // Die Start-View festlegen
            CurrentViewModel = new MainMenuViewModel(this); // Wir übergeben eine Referenz!
        }

        // Die zentrale Navigationsmethode
        public void NavigateTo(ViewModelBase viewModel)
        {
            if (CurrentViewModel != null)
            {
                _navigationHistory.Push(CurrentViewModel); // Aktuelle View im Gedächtnis speichern
            }
            CurrentViewModel = viewModel;
            // Wichtig: Dem "Zurück"-Button sagen, dass er sich eventuell ändern muss
            NavigateBackCommand.NotifyCanExecuteChanged();
        }

        // Command für den Zurück-Button
        [RelayCommand(CanExecute = nameof(CanNavigateBack))]
        private void NavigateBack()
        {
            if (_navigationHistory.Any())
            {
                CurrentViewModel = _navigationHistory.Pop(); // Letzte View aus dem Gedächtnis holen
                NavigateBackCommand.NotifyCanExecuteChanged();
            }
        }

        // Logik, die bestimmt, ob der Zurück-Button klickbar ist
        private bool CanNavigateBack()
        {
            return _navigationHistory.Any(); // Klickbar, wenn etwas im Gedächtnis ist
        }
    }
}