// Models/WerkzeugUnterkategorie.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class WerkzeugUnterkategorie
    {
        [Key]
        public int WerkzeugUnterkategorieID { get; set; }
        public string Name { get; set; } = null!;

        // Beziehung zur übergeordneten Kategorie
        public int WerkzeugKategorieID { get; set; }
        [ForeignKey("WerkzeugKategorieID")]
        public WerkzeugKategorie Kategorie { get; set; } = null!;

        // --- NEUE EIGENSCHAFTEN BASIEREND AUF CSV ---

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe eines Radius erfordert (z.B. Eckenradius).
        /// </summary>
        public bool BenötigtRadius { get; set; }

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe einer Steigung erfordert (z.B. Gewindebohrer).
        /// </summary>
        public bool BenötigtSteigung { get; set; }

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe eines Spitzenwinkels erfordert (z.B. Bohrer, Fasenfräser).
        /// </summary>
        public bool BenötigtSpitzenwinkel { get; set; }

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe eines Durchmessers erfordert.
        /// </summary>
        public bool BenötigtDurchmesser { get; set; }

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe einer Breite erfordert (z.B. Stechbreite).
        /// </summary>
        public bool BenötigtBreite { get; set; }

        /// <summary>
        /// Definiert, ob dieser Werkzeugtyp die Angabe einer maximalen Stechtiefe erfordert.
        /// </summary>
        public bool BenötigtMaxStechtiefe { get; set; }
    }
}