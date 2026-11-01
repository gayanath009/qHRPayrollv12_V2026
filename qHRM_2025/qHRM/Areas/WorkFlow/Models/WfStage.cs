using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStage
    {
        public WfStage()
        {
            WfStageStatus = new HashSet<WfStageStatus>();
            WfStrucStage = new HashSet<WfStrucStage>();
        }

        public int WfStgId { get; set; }
        public string WfStgCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string GenIns { get; set; }
        public string Color { get; set; }
        public byte[] Icon { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<WfStageStatus> WfStageStatus { get; set; }
        public virtual ICollection<WfStrucStage> WfStrucStage { get; set; }
    }
}
