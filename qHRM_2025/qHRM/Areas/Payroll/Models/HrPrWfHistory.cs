using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    [Table("HR_Pr_Wf_History")]
    public partial class HrPrWfHistory
    {
        [Key]
        [Column("History_Id")]
        public int HistoryId { get; set; }

        [Column("Wf_Type")]
        [StringLength(50)]
        public string WfType { get; set; } // 'Salary Process' or 'Leave Salary & Air Ticket Encashment'

        [Column("Ref_Id")]
        public int RefId { get; set; } // PrcId or EncashHdrId

        [Column("Prc_Year")]
        public int PrcYear { get; set; }

        [Column("Prc_Month")]
        public int PrcMonth { get; set; }

        [Column("Step_Number")]
        public int StepNumber { get; set; }

        [Column("Step_Name")]
        [StringLength(100)]
        public string StepName { get; set; }

        [Column("Action")]
        [StringLength(50)]
        public string Action { get; set; } // 'Submitted', 'Verified', 'Approved', 'Rejected'

        [Column("Actor_Emp_Sys_Id")]
        public int ActorEmpSysId { get; set; }

        [Column("Actor_Name")]
        [StringLength(150)]
        public string ActorName { get; set; }

        [Column("Comments")]
        public string Comments { get; set; }

        [Column("Action_DateTime")]
        public DateTime ActionDateTime { get; set; } = DateTime.Now;
    }
}
