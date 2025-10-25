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

        /// <summary>
        /// Ein optionaler Kommentar, z.B. für Spindeldrehzahl-Anpassungen.
        /// </summary>
        public string? Kommentar { get; set; }

        // --- NEU (UMBENANNT VON BearbeitungsArt) ---
        /// <summary>
        /// Speichert die Ausrichtung des Fräsers (z.B. "←" oder "↓").
        /// Wird vom Parser (G101/SB) gesetzt.
        /// </summary>
        public string? FräserAusrichtung { get; set; }
        // ------------------------------------

        // --- NEU (BASIEREND AUF PARSER-ERKENNUNG) ---
        /// <summary>
        /// Die vom Parser empfohlene Hauptkategorie (z.B. "Fräser").
        /// </summary>
        public string? FavoritKategorie { get; set; }

        /// <summary>
        /// Die vom Parser empfohlene Unterkategorie (z.B. "Gewindedrehstahl Innen").
        /// </summary>
        public string? FavoritUnterkategorie { get; set; }
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