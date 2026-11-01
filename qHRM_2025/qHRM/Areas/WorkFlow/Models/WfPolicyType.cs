using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfPolicyType
    {
        public WfPolicyType()
        {
            WfStructure = new HashSet<WfStructure>();
        }

        public int WfPlcTypId { get; set; }
        public string WfPlcTypCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Desc { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<WfStructure> WfStructure { get; set; }
    }
}
