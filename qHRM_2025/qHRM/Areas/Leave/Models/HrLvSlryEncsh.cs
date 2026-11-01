using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Leave.Models
{
    public partial class HrLvSlryEncsh
    {
        public HrLvSlryEncsh()
        {
            HrLvTktDtl = new HashSet<HrLvTktDtl>();
        }

        public int LvSlryId { get; set; }
        public string PrcScr { get; set; }
        public int? LvId { get; set; }
        public int? EosId { get; set; }
        public DateTime PrdStDate { get; set; }
        public DateTime PrdEndDate { get; set; }
        public int ElgblDays { get; set; }
        public bool IncldPrjAlwnc { get; set; }
        public decimal LvSlryAmt { get; set; }
        public string Rmks { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrLvRegister Lv { get; set; }
        public virtual ICollection<HrLvTktDtl> HrLvTktDtl { get; set; }
    }
}
