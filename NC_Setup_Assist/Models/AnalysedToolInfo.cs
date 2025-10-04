using System.Collections.Generic;

namespace NC_Setup_Assist.Models
{
    public class AnalysedToolInfo
    {
        public int RevolverStation { get; set; }
        public int KorrekturNummer { get; set; }
        public string Kommentar { get; set; } = string.Empty;

        // --- NEUE EIGENSCHAFT ---
        // Speichert die Start-Position jedes Vorkommens des Werkzeugs in der Datei
        public List<int> FileIndices { get; set; } = new List<int>();
    }
}