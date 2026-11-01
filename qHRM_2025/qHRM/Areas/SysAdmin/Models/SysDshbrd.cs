using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysDshbrd
    {
        public SysDshbrd()
        {
            AdmUserGroup = new HashSet<AdmUserGroup>();
        }

        public int DshbrdId { get; set; }
        public string DshbrdIndxTxt { get; set; }
        public string DshbrdName { get; set; }
        public string Desc { get; set; }
        public bool FlAccss { get; set; }
        public bool EnbldOpt1 { get; set; }
        public bool EnbldOpt2 { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<AdmUserGroup> AdmUserGroup { get; set; }
    }
}
