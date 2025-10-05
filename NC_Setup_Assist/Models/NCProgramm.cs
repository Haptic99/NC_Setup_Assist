using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class NCProgramm
    {
        [Key]
        public int NCProgrammID { get; set; }

        public string ZeichnungsNummer { get; set; } = null!;

        public string Bezeichnung { get; set; } = null!;

        public string DateiPfad { get; set; } = null!;

        public int MaschineID { get; set; }

        [ForeignKey("MaschineID")]
        public Maschine ZugehoerigeMaschine { get; set; } = null!;

        public ICollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; } = new List<WerkzeugEinsatz>();

    }
}