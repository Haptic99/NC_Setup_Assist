// NC_Setup_Assist/ViewModels/KategorieManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class KategorieManagementViewModel : ViewModelBase
    {
        private readonly Action? _onDataChangedCallback;

        public ObservableCollection<WerkzeugKategorie> Kategorien { get; } = new();

        [ObservableProperty]
        private string? _newKategorieName;

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorie;

        public KategorieManagementViewModel(Action? onDataChangedCallback = null)
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

        [RelayCommand]
        private void AddKategorie()
        {
            if (string.IsNullOrWhiteSpace(NewKategorieName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen für die Kategorie ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new NcSetupContext())
            {
                var newKat = new WerkzeugKategorie { Name = NewKategorieName };
                context.WerkzeugKategorien.Add(newKat);
                context.SaveChanges();
            }

            NewKategorieName = string.Empty;
            LoadKategorien();
            _onDataChangedCallback?.Invoke();
        }

        [RelayCommand]
        private void DeleteKategorie()
        {
            if (SelectedKategorie == null) return;

            using (var context = new NcSetupContext())
            {
                bool isUsed = context.WerkzeugUnterkategorien.Any(u => u.WerkzeugKategorieID == SelectedKategorie.WerkzeugKategorieID);
                if (isUsed)
                {
                    MessageBox.Show($"Die Kategorie '{SelectedKategorie.Name}' kann nicht gelöscht werden, da ihr noch Unterkategorien zugewiesen sind.",
                                    "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show($"Möchten Sie die Kategorie '{SelectedKategorie.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new NcSetupContext())
                {
                    var katToDelete = context.WerkzeugKategorien.Find(SelectedKategorie.WerkzeugKategorieID);
                    if (katToDelete != null)
                    {
                        context.WerkzeugKategorien.Remove(katToDelete);
                        context.SaveChanges();
                    }
                }
                LoadKategorien();
                _onDataChangedCallback?.Invoke();
            }
        }
    }
}