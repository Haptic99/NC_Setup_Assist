using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.ViewModels;
using NC_Setup_Assist.Views;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System; // Hinzugefügt für Exception Handling

namespace NC_Setup_Assist
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // --- NEU: Datenbankmigration beim Start ---
            // Stellt sicher, dass die Datenbank existiert und alle Migrationen
            // (inklusive der neuen Seed-Daten) angewendet wurden.
            using (var context = new NcSetupContext())
            {
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ein Fehler ist beim Initialisieren der Datenbank aufgetreten:\n{ex.Message}\n\nDie Anwendung wird möglicherweise nicht korrekt funktionieren.", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            // --- ENDE NEU ---

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }

    }
}