// Models/WerkzeugKategorie.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NC_Setup_Assist.Models
{
    public class WerkzeugKategorie
    {
        [Key]
        public int WerkzeugKategorieID { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<WerkzeugUnterkategorie> Unterkategorien { get; set; } = null!;
    }
}