using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class WerkzeugEinsatz
    {
        [Key]
        public int WerkzeugEinsatzID {  get; set; }

        public int RevolverStation {  get; set; }

        public int KorrekturNummer { get; set; }

        public int NCProgrammID {  get; set; }
        [ForeignKey("NCProgrammID")]
        public NCProgramm ZugehoerigesProgramm { get; set; } = null!;

        public int WerkzeugID { get; set; }
        [ForeignKey("WerkzeugID")]
        public Werkzeug ZugehoerigesWerkzeug { get; set; } = null!;
    }
}