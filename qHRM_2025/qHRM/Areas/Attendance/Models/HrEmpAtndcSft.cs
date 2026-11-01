using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.WorkShift.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAtndcSft
    {
        public int AtndcSftId { get; set; }
        public DateTime AtndcDate { get; set; }
        public int EmpSysId { get; set; }
        public int SftId { get; set; }
        public TimeSpan? InTime { get; set; }
        public string InSource { get; set; }
        public TimeSpan? OutTime { get; set; }
        public string OutSource { get; set; }
        public int WrkMnt { get; set; }
        public int OtMnt { get; set; }
        public int ExtOtMnt { get; set; }
        public int LateInMnt { get; set; }
        public int ErlyLvMnt { get; set; }
        public int AtndcStatId { get; set; }
        public decimal SlryDctFct { get; set; }
        public bool MnlMdfd { get; set; }
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int? CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual SysAtndcStat AtndcStat { get; set; }
        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrWkShift Sft { get; set; }
    }
}
