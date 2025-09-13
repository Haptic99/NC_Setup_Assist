using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NC_Setup_Assist.Models
{
    public class Projekt
    {
        [Key]
        public int ProjektID {  get; set; }

        public DateTime AnalyseDatum { get; set; }

        public int NCProgrammID {  get; set; }
        [ForeignKey("NCProgrammID")]
        public NCProgramm ZugehoerigesNCProgramm {  get; set; } = null!;

    }
}