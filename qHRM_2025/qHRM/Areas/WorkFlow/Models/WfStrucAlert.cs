using System;
using System.Collections.Generic;

namespace qHRM.Areas.WorkFlow.Models
{
    public partial class WfStrucAlert
    {
        public int StrcAltId { get; set; }
        public int WfStrcId { get; set; }
        public short AltType { get; set; }
        public int? EmpId { get; set; }
        public string Email { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual WfStructure WfStrc { get; set; }
    }
}
