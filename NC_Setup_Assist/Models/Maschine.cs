using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public enum MaschinenTyp
    {
        Okuma,
        DMT
    }


    public class Maschine
    {
        [Key]
        public int MaschineID { get; set; }

        public MaschinenTyp Hersteller { get; set; }

        public string Name { get; set; } = null!;

        public string? Seriennummer { get; set; }

        public int StandortID { get; set; }

        [ForeignKey("StandortID")]
        public Standort ZugehoerigerStandort { get; set; } = null!;

        public ICollection<StandardWerkzeugZuweisung> StandardWerkzeuge { get; set; } = null!;
    }
}