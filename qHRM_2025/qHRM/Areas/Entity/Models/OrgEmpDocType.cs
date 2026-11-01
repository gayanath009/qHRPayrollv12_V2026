using qHRM.Areas.General.Models;
using System;

namespace qHRM.Areas.Entity.Models
{
    public partial class OrgEmpDocType
    {
        public int DocTypId { get; set; }
        public string DocTypCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string DescEng { get; set; }
        public string DescArb { get; set; }
        public int EntId { get; set; }
        public bool IsMandatory { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
    }
}
