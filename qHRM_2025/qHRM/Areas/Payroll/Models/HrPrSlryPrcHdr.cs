using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrSlryPrcHdr
    {
        public HrPrSlryPrcHdr()
        {
            HrPrSlryAtnAdj = new HashSet<HrPrSlryAtnAdj>();
            HrPrSlryPrcDtlActvVw = new HashSet<HrPrSlryPrcDtlActvVw>();
            HrPrSlryPrcDtlPsv = new HashSet<HrPrSlryPrcDtlPsv>();
            HrPrSlryPrcDtlPsvVw = new HashSet<HrPrSlryPrcDtlPsvVw>();
            HrPrSlryPrcLog = new HashSet<HrPrSlryPrcLog>();
            HrPrStfLoanPmt = new HashSet<HrPrStfLoanPmt>();
        }

        public int PrcId { get; set; }
        public int EntId { get; set; }
        public int EftYear { get; set; }
        public int EftMonth { get; set; }
        public DateTime InitDateTime { get; set; }
        public int InitBy { get; set; }
        public short? PrcStat { get; set; }
        public DateTime? CmpdDateTime { get; set; }
        public int? CmpdBy { get; set; }
        public DateTime? VrfdDateTime { get; set; }
        public int? VrfdBy { get; set; }

        [NotMapped] 
        public string lstEmps { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrPrSlryAtnAdj> HrPrSlryAtnAdj { get; set; }
        public virtual ICollection<HrPrSlryPrcDtlActvVw> HrPrSlryPrcDtlActvVw { get; set; }
        public virtual ICollection<HrPrSlryPrcDtlPsv> HrPrSlryPrcDtlPsv { get; set; }
        public virtual ICollection<HrPrSlryPrcDtlPsvVw> HrPrSlryPrcDtlPsvVw { get; set; }
        public virtual ICollection<HrPrSlryPrcLog> HrPrSlryPrcLog { get; set; }
        public virtual ICollection<HrPrStfLoanPmt> HrPrStfLoanPmt { get; set; }
    }
}
