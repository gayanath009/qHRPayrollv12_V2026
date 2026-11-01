using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.General.Models
{
    public partial class OrgNltyType
    {
        public OrgNltyType()
        {
            OrgNationality = new HashSet<OrgNationality>();
            OrgNltyEmpRatio = new HashSet<OrgNltyEmpRatio>();
        }

        public int NltyTypId { get; set; }
        public string NltyTypNameEng { get; set; }
        public string NltyTypNameArb { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<OrgNationality> OrgNationality { get; set; }
        public virtual ICollection<OrgNltyEmpRatio> OrgNltyEmpRatio { get; set; }
    }
}
