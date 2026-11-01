using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStageStatus
    {
        public int WfStgId { get; set; }
        public int WfStatId { get; set; }
        public string GenIns { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual WfStatus WfStat { get; set; }
        public virtual WfStage WfStg { get; set; }
    }
}
