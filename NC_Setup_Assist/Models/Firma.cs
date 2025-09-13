using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NC_Setup_Assist.Models
{
    public class Firma
    {
        [Key]
        public int FirmaID { get; set; }

        public string Name { get; set; } = null!;

        public ICollection<Standort> Standorte { get; set; } = null!;
    }
}