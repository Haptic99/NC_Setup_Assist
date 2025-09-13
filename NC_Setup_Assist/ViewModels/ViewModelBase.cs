using CommunityToolkit.Mvvm.ComponentModel;

namespace NC_Setup_Assist.ViewModels
{
    /// <summary>
    /// Eine Basisklasse für alle ViewModels in der Anwendung.
    /// Sie implementiert ObservableObject, was die INotifyPropertyChanged-Funktionalität bereitstellt.
    /// Dadurch kann die UI automatisch auf Änderungen von Properties im ViewModel reagieren.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
    }
}