using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public class HrEmpAtndcVw
    {
        [Key]
        public Int64 Indx { get; set; }
        public int EmpSysId { get; set; }
        public string EmpName { get; set; }
        public string AtndcDate { get; set; }
        public string AtndcStat { get; set; }
    }
}
