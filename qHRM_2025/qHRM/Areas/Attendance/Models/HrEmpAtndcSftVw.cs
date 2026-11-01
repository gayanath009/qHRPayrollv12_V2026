using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAtndcSftVw
    {
        [Key]
        public int AtndcSftId { get; set; }
        public DateTime AtndcDate { get; set; }
        public int EmpSysId { get; set; }
        public int SftId { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public int AtndcStatId { get; set; }
    }
}
