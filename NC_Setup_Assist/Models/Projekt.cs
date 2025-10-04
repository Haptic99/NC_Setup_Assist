using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class Projekt
    {
        [Key]
        public int ProjektID { get; set; }

        public string Name { get; set; } = null!;

        public int MaschineID { get; set; }

        [ForeignKey("MaschineID")]
        public Maschine ZugehoerigeMaschine { get; set; } = null!; // Korrigiert für Fehler CS8618 und CS0246

        public ICollection<NCProgramm> NCProgramme { get; set; } = new List<NCProgramm>();
    }
}