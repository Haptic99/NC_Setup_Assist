// App.xaml.cs
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.ViewModels;
using NC_Setup_Assist.Views;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System;

namespace NC_Setup_Assist
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var context = new NcSetupContext())
            {
                try
                {
                    // --- Schritt 1: Stellt sicher, dass DB existiert & Migrationen angewendet sind ---
                    context.Database.Migrate();

                    // --- Schritt 2: Datenbank mit Standarddaten befüllen (nur wenn leer) ---
                    DataSeeder.Initialize(context);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ein Fehler ist beim Initialisieren der Datenbank aufgetreten:\n{ex.Message}\n\nDie Anwendung wird möglicherweise nicht korrekt funktionieren.", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }

    }
}