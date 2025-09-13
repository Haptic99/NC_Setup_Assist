using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class StandardWerkzeugZuweisung
    {
        [Key]
        public int StandardWerkzeugZuweisungID { get; set; }

        public int RevolverStation {  get; set; }

        public int MaschineID { get; set; }

        [ForeignKey("MaschineID")]
        public Maschine ZugehoerigeMaschine {  get; set; } = null!;

        public int WerkzeugID { get; set; }

        [ForeignKey("WerkzeugID")]
        public Werkzeug ZugehoerigesWerkzeug { get; set; } = null!;

    }
}