using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.ViewModels;
using NC_Setup_Assist.Views;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace NC_Setup_Assist
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }

    }
}