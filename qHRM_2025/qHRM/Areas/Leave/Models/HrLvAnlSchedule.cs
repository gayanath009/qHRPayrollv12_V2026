using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;

namespace qHRM.Areas.Leave.Models
{
    public partial class HrLvAnlSchedule
    {
        public HrLvAnlSchedule()
        {
            HrLvRegister = new HashSet<HrLvRegister>();
        }

        public int LvSdlId { get; set; }
        public int EmpSysId { get; set; }
        public int LvTypId { get; set; }
        public int LvYear { get; set; }
        public int Incident { get; set; }
        public DateTime StDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Note { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrLvType LvTyp { get; set; }
        public virtual ICollection<HrLvRegister> HrLvRegister { get; set; }
    }
}
