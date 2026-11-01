using System;

namespace qHRM.Areas.Employee.Models
{
    public class HrEmpProbationView
    {
        public int ProbId { get; set; }

        public int EmpSysId { get; set; }

        public DateTime StDate { get; set; }

        public DateTime EndDate { get; set; }

        public int DeptId { get; set; }
        public string DeptName { get; set; }

        public int DesigId { get; set; }
        public string DesigName { get; set; }

        public short WkStat { get; set; }
        public string WkStatName { get; set; }

        public int? RptMngId { get; set; }
        public string RptMngName { get; set; }

        public bool IsExtended { get; set; }
        public string Extended { get; set; }
    }
}
