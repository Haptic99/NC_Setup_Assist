// NC_Setup_Assist/Models/Hersteller.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NC_Setup_Assist.Models
{
    public class Hersteller
    {
        [Key]
        public int HerstellerID { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public ICollection<Maschine> Maschinen { get; set; } = new List<Maschine>();
    }
}