using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAtndcExpSmryVw
    {
        [Key]
        public int EmpSysId { get; set; }
        public string EmpId { get; set; }
        public string EmpName { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; }
        public int DesigId { get; set; }
        public string DesigName { get; set; }
        public int AbsntCnt { get; set; }
        public int RqstCnt { get; set; }
        public int AprvdCnt { get; set; }
        public int RjctdCnt { get; set; }
        public int PndCnt { get; set; }
    }
}
