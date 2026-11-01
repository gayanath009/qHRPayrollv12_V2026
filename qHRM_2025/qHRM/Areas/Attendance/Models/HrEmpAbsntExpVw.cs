using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAbsntExpVw
    {
        [Key]
        public int AtndcSftId { get; set; }
        public DateTime AtndcDate { get; set; }
        public int EmpSysId { get; set; }
        public int AtndcStatId { get; set; }
        public string ActionStat { get; set; }
        public int? AtnExpId { get; set; }
        public string Note { get; set; }
        public int? AprvdStatId { get; set; }
        public string AprvdStatTxt { get; set; }
        public int? AprvdId { get; set; }
        public string AprvdName { get; set; }
        public DateTime? AprvdDateTime { get; set; }
        public string AprvdRmks { get; set; }
        public int? CrtId { get; set; }
        public string CrtName { get; set; }
        public DateTime? CrtDateTime { get; set; }
    }
}
