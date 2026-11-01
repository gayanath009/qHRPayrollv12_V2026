using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.General.Models
{
    public partial class OrgNltyEmpRatio
    {
        public int EmpRtoId { get; set; }
        public int NltyTypId { get; set; }
        public int? NltyId { get; set; }
        public decimal StfRto { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual OrgNationality Nlty { get; set; }
        public virtual OrgNltyType NltyTyp { get; set; }
    }
}
