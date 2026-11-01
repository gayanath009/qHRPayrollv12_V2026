using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStatus
    {
        public WfStatus()
        {
            WfStageStatus = new HashSet<WfStageStatus>();
        }

        public int WfStatId { get; set; }
        public string WfStatCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Desc { get; set; }
        public string Color { get; set; }
        public byte[] Icon { get; set; }
        public int RsltType { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<WfStageStatus> WfStageStatus { get; set; }
    }
}
