using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class WerkzeugEinsatz
    {
        [Key]
        public int WerkzeugEinsatzID { get; set; }

        public int Reihenfolge { get; set; }

        /// <summary>
        /// NEU: Zählt, wie oft dieses Werkzeug nacheinander aufgerufen wurde.
        /// </summary>
        public int Anzahl { get; set; }

        public string? RevolverStation { get; set; }

        public string? KorrekturNummer { get; set; }

        // --- NEUE EIGENSCHAFT HINZUGEFÜGT ---
        /// <summary>
        /// Ein optionaler Kommentar, z.B. für Spindeldrehzahl-Anpassungen.
        /// </summary>
        public string? Kommentar { get; set; }

        // --- NEU (basierend auf G101/SB-Anfrage) ---
        /// <summary>
        /// Speichert die Bearbeitungsart, z.B. "Fräsen" oder "Drehen".
        /// Wird vom Parser (G101/SB) gesetzt.
        /// </summary>
        public string? BearbeitungsArt { get; set; }
        // ------------------------------------

        // --- NEU (BASIEREND AUF KORB-ANFRAGE) ---
        /// <summary>
        /// True, wenn der Teilefänger (Korb) bei diesem Werkzeug verwendet wurde (M76).
        /// </summary>
        public bool VerwendetKorb { get; set; }
        // ------------------------------------

        public int NCProgrammID { get; set; }
        [ForeignKey("NCProgrammID")]
        public NCProgramm ZugehoerigesProgramm { get; set; } = null!;

        public int? WerkzeugID { get; set; }
        [ForeignKey("WerkzeugID")]
        public Werkzeug? ZugehoerigesWerkzeug { get; set; }
    }
}