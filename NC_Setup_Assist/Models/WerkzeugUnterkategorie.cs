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
    }
}