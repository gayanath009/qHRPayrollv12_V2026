using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStrucStage
    {
        public WfStrucStage()
        {
            WfStrucStageAlert = new HashSet<WfStrucStageAlert>();
        }

        public int StgId { get; set; }
        public int WfStrcId { get; set; }
        public int WfStgId { get; set; }
        public short StgLvl { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Ins { get; set; }
        public string InsArb { get; set; }
        public int? PreStgId { get; set; }
        public bool PreCmpRqd { get; set; }
        public int? PstStgId { get; set; }
        public bool PstCmpRqd { get; set; }
        public bool PreAlt { get; set; }
        public bool ActionAlt { get; set; }
        public bool SpclAlt { get; set; }
        public int? DesigId { get; set; }
        public int? EmpId { get; set; }
        public short LvlIndx { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual WfStage WfStg { get; set; }
        public virtual WfStructure WfStrc { get; set; }
        public virtual ICollection<WfStrucStageAlert> WfStrucStageAlert { get; set; }
    }
}
