using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStructure
    {
        public WfStructure()
        {
            WfStrucAlert = new HashSet<WfStrucAlert>();
            WfStrucStage = new HashSet<WfStrucStage>();
        }

        public int WfStrcId { get; set; }
        public string WfStrcpCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Desc { get; set; }
        public int WfPlcTypId { get; set; }
        public bool PreAlt { get; set; }
        public bool ActionAlt { get; set; }
        public bool SpclAlt { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual WfPolicyType WfPlcTyp { get; set; }
        public virtual ICollection<WfStrucAlert> WfStrucAlert { get; set; }
        public virtual ICollection<WfStrucStage> WfStrucStage { get; set; }
    }
}
