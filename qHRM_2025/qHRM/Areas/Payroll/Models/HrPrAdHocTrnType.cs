using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrAdHocTrnType
    {
        public HrPrAdHocTrnType()
        {
            HrPrAdHocTrns = new HashSet<HrPrAdHocTrns>();
        }

        public int TrnTypId { get; set; }
        [Display(Name = "Index")]
        public int Indx { get; set; }

        [Display(Name = "Type")]
        [StringLength(6)]
        public string TrnType { get; set; } // PAY / DEDUCT

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Type Name cannot be blank")]
        [StringLength(15)]
        public string TrnTypTxt { get; set; }

        [Display(Name = "Description")]
        [StringLength(200)]
        public string Desc { get; set; }
        public bool AutoGen { get; set; }

        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrPrAdHocTrns> HrPrAdHocTrns { get; set; }
    }
}
