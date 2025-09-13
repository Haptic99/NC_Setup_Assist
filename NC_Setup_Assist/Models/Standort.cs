using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{

    public class Standort
    {
        [Key]
        public int StandortID { get; set; }

        public string Name { get; set; } = null!;

        public string PLZ { get; set; } = null!;

        public string Stadt { get; set; } = null!;

        public string Strasse { get; set; } = null!;

        public string Hausnummer { get; set; } = null!;

        // --- Beziehung zur Firma ---
        // Ein Standort gehört zu EINER Firma.
        public int FirmenID { get; set; } // Der Fremdschlüssel

        [ForeignKey("FirmenID")]
        public Firma ZugehoerigeFirma { get; set; } = null!; // Die Navigations-Eigenschaft


        // --- Beziehung zu den Maschinen ---
        // Ein Standort hat VIELE Maschinen.
        // Das ist die "andere Seite" der Beziehung, die Sie gerade bauen.
        public ICollection<Maschine> Maschinen { get; set; } = null!;
    }
}