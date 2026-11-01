using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class AdmUsrGrpEmpGrp
    {
        public int AutoId { get; set; }
        public int GrpId { get; set; }
        public int EmpGrpId { get; set; }
        public bool RecView { get; set; }
        public bool RecAdd { get; set; }
        public bool RecEdit { get; set; }
        public bool RecDelete { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGroup EmpGrp { get; set; }
        public virtual AdmUserGroup Grp { get; set; }
    }
}
