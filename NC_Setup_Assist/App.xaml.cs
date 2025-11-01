// App.xaml.cs
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.ViewModels;
using NC_Setup_Assist.Views;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows.Threading; // <-- NEU
using NC_Setup_Assist.Services; // <-- NEU
using System.IO; // <-- NEU

namespace NC_Setup_Assist
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // --- NEU: Globalen Handler registrieren ---
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            // ------------------------------------------


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
                    // --- NEU: Verbessertes Fehlerhandling beim Start ---
                    // --- ÄNDERUNG HIER: Verwende LoggingService.LogFilePath ---
                    LoggingService.LogException(ex, "Kritischer Datenbank-Migrationsfehler beim Start");
                    MessageBox.Show($"Ein kritischer Datenbankfehler ist aufgetreten:\n{ex.Message}\n\n" +
                                    $"Ein Fehlerbericht wurde in '{LoggingService.LogFilePath}' gespeichert.\n" + // NEU
                                    $"Die Anwendung wird beendet.",
                                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    // --- ENDE ÄNDERUNG ---

                    // Bei einem DB-Fehler beim Start ist ein Shutdown sinnvoll
                    Application.Current.Shutdown(1);
                    return; // Beendet die Methode hier
                    // --- ENDE NEU ---
                }
            }

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }


        // --- NEUE METHODE: Der globale "Safety Net"-Handler ---
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 1. Den Fehler sofort loggen
            LoggingService.LogException(e.Exception, "Global Unhandled Exception (Dispatcher)");

            // --- ÄNDERUNG HIER: Verwende LoggingService.LogFilePath ---
            // 2. Benutzer freundlich informieren
            MessageBox.Show(
                "Ein unerwarteter Fehler ist aufgetreten.\n\n" +
                "Ein Fehlerbericht wurde gespeichert in:\n" +
                LoggingService.LogFilePath + // NEU
                $"\n\nFehler: {e.Exception.Message}",
                "Unerwarteter Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            // --- ENDE ÄNDERUNG ---

            // 3. Verhindern, dass Windows den "Programm abstürzen"-Dialog zeigt
            e.Handled = true;

            // Optional: Anwendung kontrolliert beenden, um Datenkorruption zu vermeiden
            // Application.Current.Shutdown(1);
        }
        // --- ENDE NEUE METHODE ---
    }
}