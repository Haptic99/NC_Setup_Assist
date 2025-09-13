using CommunityToolkit.Mvvm.Input;
using NC_Setup_Assist.Views;

namespace NC_Setup_Assist.ViewModels
{
    public partial class SettingsDashboardViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public SettingsDashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void OpenToolManagement()
        {
            _mainViewModel.NavigateTo(new ToolManagementViewModel());
        }
    }
}