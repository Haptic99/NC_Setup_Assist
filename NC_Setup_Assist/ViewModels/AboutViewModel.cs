using System;
using System.Reflection;

namespace NC_Setup_Assist.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {
        public string AppName => "NC-Setup-Assist";

        public string AppVersion { get; }

        // KORREKTUR HIER:
        public string Copyright => $"© {DateTime.Now.Year} Daniel Martinez";

        public AboutViewModel()
        {
            // Liest die Version (z.B. 1.0.0) aus der Projektdatei
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }
}