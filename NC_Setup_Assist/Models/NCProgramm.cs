// NC_Setup_Assist/Models/NCProgramm.cs
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

        // NEU: Fremdschlüssel zur optionalen Verknüpfung mit einem Projekt
        public int? ProjektID { get; set; }

        // Die Navigationseigenschaft zum Projekt wird nicht direkt benötigt,
        // da sie implizit durch ProjektID und die Definition in Projekt.cs und DbContext
        // sowie den Migrations-Snapshot gehandhabt wird.
        // Die Migration/EF Core weiss, dass NCProgramm optional zu Projekt gehört.

        public ICollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; } = new List<WerkzeugEinsatz>();

    }
}