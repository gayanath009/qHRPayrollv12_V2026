using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Entity.Models
{
    [Table("ORG_Religion")]
    public partial class OrgReligion
    {
        public OrgReligion()
        {
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
        }

        [Key]
        [Column("Rel_Id")]
        public int RelId { get; set; }

        [Column("Rel_Name_Eng")]
        [Display(Name = "Religion")]
        [Required(ErrorMessage = "Please specify Religion")]
        [StringLength(50)]
        public string RelNameEng { get; set; }

        [Column("Rel_Name_Arb")]
        [StringLength(50)]
        public string RelNameArb { get; set; }

        [Column("Rec_Stat")]
        public short RecStat { get; set; }

        [Column("Crt_DateTime")]
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
    }
}
