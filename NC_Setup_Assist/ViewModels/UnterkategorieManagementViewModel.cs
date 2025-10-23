// NC_Setup_Assist/ViewModels/UnterkategorieManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class UnterkategorieManagementViewModel : ViewModelBase
    {
        private readonly Action? _onDataChangedCallback;

        // Listen
        public ObservableCollection<WerkzeugKategorie> Kategorien { get; } = new();
        public ObservableCollection<WerkzeugUnterkategorie> FilteredUnterkategorien { get; } = new();

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorieFilter;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditUnterkategorieCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteUnterkategorieCommand))]
        private WerkzeugUnterkategorie? _selectedUnterkategorie;

        [ObservableProperty]
        private WerkzeugUnterkategorie? _editingUnterkategorie;

        [ObservableProperty]
        private bool _isInEditMode;

        public UnterkategorieManagementViewModel(Action? onDataChangedCallback = null)
        {
            _onDataChangedCallback = onDataChangedCallback;
            LoadKategorien();
        }

        private void LoadKategorien()
        {
            Kategorien.Clear();
            using var context = new NcSetupContext();
            var katsFromDb = context.WerkzeugKategorien.OrderBy(k => k.Name).ToList();
            foreach (var kat in katsFromDb)
            {
                Kategorien.Add(kat);
            }
        }

        partial void OnSelectedKategorieFilterChanged(WerkzeugKategorie? value)
        {
            FilteredUnterkategorien.Clear();
            SelectedUnterkategorie = null;

            if (value != null)
            {
                using var context = new NcSetupContext();
                var subsFromDb = context.WerkzeugUnterkategorien
                                        .Where(u => u.WerkzeugKategorieID == value.WerkzeugKategorieID)
                                        .OrderBy(u => u.Name)
                                        .ToList();
                foreach (var sub in subsFromDb)
                {
                    FilteredUnterkategorien.Add(sub);
                }
            }
        }

        [RelayCommand]
        private void NewUnterkategorie()
        {
            EditingUnterkategorie = new WerkzeugUnterkategorie
            {
                // Wähle die gefilterte Kategorie vor, falls eine ausgewählt ist
                Kategorie = SelectedKategorieFilter ?? Kategorien.FirstOrDefault()
            };
            IsInEditMode = true;
        }

        private bool CanExecuteEditDelete() => SelectedUnterkategorie != null;

        [RelayCommand(CanExecute = nameof(CanExecuteEditDelete))]
        private void EditUnterkategorie()
        {
            if (SelectedUnterkategorie == null) return;

            using var context = new NcSetupContext();
            EditingUnterkategorie = context.WerkzeugUnterkategorien
                                           .Include(u => u.Kategorie)
                                           .FirstOrDefault(u => u.WerkzeugUnterkategorieID == SelectedUnterkategorie.WerkzeugUnterkategorieID);

            // Stelle sicher, dass die Kategorie-Instanz aus der ComboBox-Liste stammt
            if (EditingUnterkategorie != null)
            {
                EditingUnterkategorie.Kategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == EditingUnterkategorie.WerkzeugKategorieID);
            }

            IsInEditMode = true;
        }

        [RelayCommand]
        private void SaveUnterkategorie()
        {
            if (EditingUnterkategorie == null ||
                string.IsNullOrWhiteSpace(EditingUnterkategorie.Name) ||
                EditingUnterkategorie.Kategorie == null)
            {
                MessageBox.Show("Bitte Name und Hauptkategorie angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var context = new NcSetupContext();

            // Wichtig: Die Kategorie-ID setzen, nicht die Navigationseigenschaft
            EditingUnterkategorie.WerkzeugKategorieID = EditingUnterkategorie.Kategorie.WerkzeugKategorieID;
            EditingUnterkategorie.Kategorie = null!; // Navigationseigenschaft nullen, um Konflikte zu vermeiden

            if (EditingUnterkategorie.WerkzeugUnterkategorieID == 0) // Neu
            {
                context.WerkzeugUnterkategorien.Add(EditingUnterkategorie);
            }
            else // Bearbeiten
            {
                context.WerkzeugUnterkategorien.Update(EditingUnterkategorie);
            }
            context.SaveChanges();

            IsInEditMode = false;
            EditingUnterkategorie = null;
            // Liste neu laden
            OnSelectedKategorieFilterChanged(SelectedKategorieFilter);
            _onDataChangedCallback?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingUnterkategorie = null;
            IsInEditMode = false;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteEditDelete))]
        private void DeleteUnterkategorie()
        {
            if (SelectedUnterkategorie == null) return;

            using (var context = new NcSetupContext())
            {
                bool isUsed = context.Werkzeuge.Any(w => w.WerkzeugUnterkategorieID == SelectedUnterkategorie.WerkzeugUnterkategorieID);
                if (isUsed)
                {
                    MessageBox.Show($"Die Unterkategorie '{SelectedUnterkategorie.Name}' kann nicht gelöscht werden, da sie noch Werkzeugen zugewiesen ist.",
                                    "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show($"Möchten Sie die Unterkategorie '{SelectedUnterkategorie.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new NcSetupContext())
                {
                    var subKatToDelete = context.WerkzeugUnterkategorien.Find(SelectedUnterkategorie.WerkzeugUnterkategorieID);
                    if (subKatToDelete != null)
                    {
                        context.WerkzeugUnterkategorien.Remove(subKatToDelete);
                        context.SaveChanges();
                    }
                }
                OnSelectedKategorieFilterChanged(SelectedKategorieFilter);
                _onDataChangedCallback?.Invoke();
            }
        }
    }
}