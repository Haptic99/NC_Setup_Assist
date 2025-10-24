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

        // --- NEUE EIGENSCHAFT (statt EditingUnterkategorie.Kategorie) ---
        [ObservableProperty]
        private WerkzeugKategorie? _editingKategorie;

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
            EditingUnterkategorie = new WerkzeugUnterkategorie();

            // Setze die separate ViewModel-Eigenschaft
            EditingKategorie = SelectedKategorieFilter ?? Kategorien.FirstOrDefault();

            IsInEditMode = true;
        }

        private bool CanExecuteEditDelete() => SelectedUnterkategorie != null;

        [RelayCommand(CanExecute = nameof(CanExecuteEditDelete))]
        private void EditUnterkategorie()
        {
            if (SelectedUnterkategorie == null) return;

            using var context = new NcSetupContext();
            // 1. Lade das Objekt, das wir bearbeiten wollen
            EditingUnterkategorie = context.WerkzeugUnterkategorien
                                           .FirstOrDefault(u => u.WerkzeugUnterkategorieID == SelectedUnterkategorie.WerkzeugUnterkategorieID);

            // Stelle sicher, dass die Kategorie-Instanz aus der ComboBox-Liste stammt
            if (EditingUnterkategorie != null)
            {
                // 2. KORREKTUR: Setze die separate ViewModel-Eigenschaft für die ComboBox
                EditingKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == EditingUnterkategorie.WerkzeugKategorieID);
            }

            IsInEditMode = true;
        }

        [RelayCommand]
        private void SaveUnterkategorie()
        {
            if (EditingUnterkategorie == null ||
                string.IsNullOrWhiteSpace(EditingUnterkategorie.Name) ||
                EditingKategorie == null) // <-- Prüfe die ViewModel-Eigenschaft
            {
                MessageBox.Show("Bitte Name und Hauptkategorie angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var context = new NcSetupContext();

            // Wichtig: Die Kategorie-ID von der ViewModel-Eigenschaft setzen
            EditingUnterkategorie.WerkzeugKategorieID = EditingKategorie.WerkzeugKategorieID;
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
            EditingKategorie = null; // <-- Setze die VM-Eigenschaft zurück

            // Liste neu laden
            OnSelectedKategorieFilterChanged(SelectedKategorieFilter);
            _onDataChangedCallback?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingUnterkategorie = null;
            EditingKategorie = null; // <-- Setze die VM-Eigenschaft zurück
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