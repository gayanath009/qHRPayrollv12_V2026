using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStrucStageAlert
    {
        public int StgAltId { get; set; }
        public int StgId { get; set; }
        public short AltType { get; set; }
        public int? EmpId { get; set; }
        public string Email { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual WfStrucStage Stg { get; set; }
    }
}
