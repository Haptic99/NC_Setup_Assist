// Models/Werkzeug.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class Werkzeug
    {
        [Key]
        public int WerkzeugID { get; set; }

        public string Name { get; set; } = null!;
        public string? Beschreibung { get; set; }

        // --- EIGENSCHAFTEN BASIEREND AUF CSV ---

        /// <summary>
        /// z.B. Eckenradius
        /// </summary>
        public double? Radius { get; set; }

        /// <summary>
        /// z.B. Gewindesteigung
        /// </summary>
        public double? Steigung { get; set; }

        /// <summary>
        /// z.B. 60, 90, 118, 120 Grad
        /// </summary>
        public double? Spitzenwinkel { get; set; }

        /// <summary>
        /// Durchmesser für Fräser, Bohrer etc.
        /// </summary>
        public double? Durchmesser { get; set; }

        /// <summary>
        /// z.B. Stechbreite oder Fräserbreite
        /// </summary>
        public double? Breite { get; set; }

        /// <summary>
        /// z.B. für Einstech- oder Abstechstähle
        /// </summary>
        public double? MaxStechtiefe { get; set; }


        // Beziehung zur Unterkategorie
        public int WerkzeugUnterkategorieID { get; set; }
        [ForeignKey("WerkzeugUnterkategorieID")]
        public WerkzeugUnterkategorie Unterkategorie { get; set; } = null!;

        // Beziehungen zu den Verwendungen
        public ICollection<StandardWerkzeugZuweisung> StandardWerkzeugZuweisungen { get; set; } = new List<StandardWerkzeugZuweisung>();
        public ICollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; } = new List<WerkzeugEinsatz>();
    }
}