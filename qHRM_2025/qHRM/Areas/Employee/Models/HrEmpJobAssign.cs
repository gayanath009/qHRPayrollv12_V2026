using qHRM.Areas.Entity.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpJobAssign
    {
        public int JbAsngId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Assigned Date")]
        [Required(ErrorMessage = "Enter Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        //[Display(Name = "Assignment End Date")]
        //[NotMapped]
        //public string EndDateText { get { return (EndDate == null) ? "" : EndDate.ToString(); } set { } }

        [Display(Name = "Department")]
        [Required(ErrorMessage = "Select Department")]
        public int DeptId { get; set; }

        [Display(Name = "Designation")]
        [Required(ErrorMessage = "Select Designation")]
        public int DesigId { get; set; }

        [Display(Name = "Employement Type")]
        [Required(ErrorMessage = "Select Employement Type")]
        public int EmpTypId { get; set; }

        public short WkStat { get; set; }

        //[Display(Name = "Status")]
        //[NotMapped]
        //public string WkStatText { get { return (WkStat == 1) ? "Active" : "Closed"; } }

        [Display(Name = "Reporting to")]
        public int? RptMngId { get; set; }

        [Display(Name = "Phone")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid number")]
        [StringLength(20)]
        public string OfcPhone { get; set; }

        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        [StringLength(50)]
        public string OfcEmail { get; set; }

        [Display(Name = "Special Notes")]
        [Required(ErrorMessage = "Enter the change comment")]
        public string Note { get; set; }

        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgDepartment Dept { get; set; }
        [NotMapped]
        public virtual OrgDesignation Desig { get; set; }
        [NotMapped]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
