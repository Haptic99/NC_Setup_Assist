using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <-- Wichtig für [ForeignKey]

namespace NC_Setup_Assist.Models
{
    public class Werkzeug
    {
        [Key]
        public int WerkzeugID { get; set; }

        public string Name { get; set; } = null!; // <-- Behebt die Warnung für Name
        public string? Beschreibung { get; set; }

        // Beziehung zur Unterkategorie
        public int WerkzeugUnterkategorieID { get; set; }
        [ForeignKey("WerkzeugUnterkategorieID")]
        public WerkzeugUnterkategorie Unterkategorie { get; set; } = null!; // <-- Behebt die Warnung für Unterkategorie

        // Beziehungen zu den Verwendungen
        public ICollection<StandardWerkzeugZuweisung> StandardWerkzeugZuweisungen { get; set; } = new List<StandardWerkzeugZuweisung>();
        public ICollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; } = new List<WerkzeugEinsatz>();
    }
}