using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32; // <-- NEU
using NC_Setup_Assist.Services; // <-- NEU
using NC_Setup_Assist.Views;
using System; // <-- NEU
using System.Diagnostics; // <-- NEU
using System.IO; // <-- NEU
using System.Windows; // <-- NEU
// --- NEUER IMPORT ---
using NC_Setup_Assist.Data;

namespace NC_Setup_Assist.ViewModels
{
    public partial class SettingsDashboardViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        // --- ÄNDERUNG HIER ---
        // private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nc_setup.db"); // ALT
        private readonly string _dbPath = NcSetupContext.DatabasePath; // NEU
        // --- ENDE ÄNDERUNG ---

        public SettingsDashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void OpenToolManagement()
        {
            // --- ANGEPASST: MainViewModel wird jetzt übergeben ---
            _mainViewModel.NavigateTo(new ToolManagementViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void OpenMachineManagement()
        {
            _mainViewModel.NavigateTo(new MachineManagementViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void OpenStandortManagement()
        {
            _mainViewModel.NavigateTo(new StandortManagementViewModel(_mainViewModel));
        }

        // --- NEUE METHODE: Backup ---
        [RelayCommand]
        private void BackupDatabase()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Datenbank-Backup (*.db)|*.db",
                    FileName = $"nc_setup_backup_{DateTime.Now:yyyyMMdd}.db",
                    Title = "Datenbank-Backup speichern"
                };

                if (sfd.ShowDialog() == true)
                {
                    File.Copy(_dbPath, sfd.FileName, true);
                    MessageBox.Show($"Backup erfolgreich gespeichert unter:\n{sfd.FileName}", "Backup erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Erstellen des Backups");
                MessageBox.Show($"Fehler beim Erstellen des Backups:\n{ex.Message}", "Backup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- NEUE METHODE: Restore ---
        [RelayCommand]
        private void RestoreDatabase()
        {
            var result = MessageBox.Show(
                "WARNUNG:\n\n" +
                "Sie sind im Begriff, die aktuelle Datenbank mit einem Backup zu überschreiben.\n" +
                "Alle aktuellen Daten gehen dabei verloren.\n\n" +
                "Die Anwendung wird danach neu gestartet.\n\n" +
                "Möchten Sie fortfahren?",
                "Wiederherstellung bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Datenbank-Backup (*.db)|*.db|Alle Dateien (*.*)|*.*",
                    Title = "Backup-Datei auswählen"
                };

                if (ofd.ShowDialog() == true)
                {
                    // Kopiere das Backup über die aktive DB-Datei
                    File.Copy(ofd.FileName, _dbPath, true);

                    MessageBox.Show("Wiederherstellung erfolgreich.\nDie Anwendung wird jetzt neu gestartet.", "Wiederherstellung", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Anwendung neu starten, um die DB neu zu laden
                    Application.Current.Shutdown();
                    Process.Start(Application.ResourceAssembly.Location);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Wiederherstellen der Datenbank");
                MessageBox.Show($"Fehler beim Wiederherstellen:\n{ex.Message}\n\nStellen Sie sicher, dass die Datei ein gültiges Backup ist.", "Restore-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}