using System;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace NC_Setup_Assist.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public string AppName => "NC-Setup-Assist";

        public string AppVersion { get; }

        // KORREKTUR HIER:
        public string Copyright => $"© {DateTime.Now.Year} Daniel Martinez";

        public AboutViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            // Liest die Version (z.B. 1.0.0) aus der Projektdatei
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _mainViewModel.NavigateBack();
        }
    }
}