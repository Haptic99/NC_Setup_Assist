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
        /// Zählt, wie oft dieses Werkzeug nacheinander aufgerufen wurde.
        /// </summary>
        public int Anzahl { get; set; }

        public int RevolverStation { get; set; }

        /// <summary>
        /// Korrekturnummer kann fehlen (z.B. bei reinen Kommentar-Einträgen).
        /// </summary>
        public int? KorrekturNummer { get; set; }

        /// <summary>
        /// Ein optionaler Kommentar, z.B. für Spindeldrehzahl-Anpassungen.
        /// </summary>
        public string? Kommentar { get; set; }

        public int NCProgrammID { get; set; }

        [ForeignKey("NCProgrammID")]
        public NCProgramm? ZugehoerigesProgramm { get; set; }

        public int? WerkzeugID { get; set; }

        [ForeignKey("WerkzeugID")]
        public Werkzeug? ZugehoerigesWerkzeug { get; set; }
    }
}