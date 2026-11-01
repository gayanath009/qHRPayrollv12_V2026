using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Models
{
    public partial class EmpSrtOrdVw
    {
        [Key]
        public int EmpSysId { get; set; }

        [Display(Name = "Emp Id")]
        public string EmpId { get; set; }

        [Display(Name = "Employee Name")]
        public string EmpName { get; set; }

        [Display(Name = "Sort Order")]
        public int EmpSrtOrd { get; set; }

        [Display(Name = "New Sort Order, Note, Maximum 6 Digits only")]
        [Range(0,999999)]
        public int ex_EmpSrtOrd { get; set; }

        public int DeptId { get; set; }

        [Display(Name = "Dept Sort Order Prefix")]
        public int DeptSrtOrd { get; set; }

        [Display(Name = "Department")]
        public string DeptName { get; set; }

        public int DesigId { get; set; }

        [Display(Name = "Designation")]
        public string DesigName { get; set; }
    }
}
