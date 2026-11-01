using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Leave.Models
{
    /// <summary>
    /// Approval Delegation record.
    /// Allows an approver to delegate their leave approval responsibility to another employee
    /// for a specified period (e.g. during annual leave or sick leave).
    /// The original approver's role/designation is NOT permanently changed.
    /// </summary>
    public partial class HrLvWfDelegation
    {
        [Key]
        public int DelegId { get; set; }

        [Display(Name = "Delegating Employee")]
        [Required(ErrorMessage = "Please select the delegating employee")]
        public int DlgtrEmpId { get; set; }

        [Display(Name = "Delegate Employee")]
        [Required(ErrorMessage = "Please select the delegate employee")]
        public int DlgteEmpId { get; set; }

        [Display(Name = "Leave Type")]
        public int? LvTypId { get; set; }  // NULL = applies to all leave types

        [Display(Name = "Delegation From Date")]
        [Required(ErrorMessage = "Please specify From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Display(Name = "Delegation To Date")]
        [Required(ErrorMessage = "Please specify To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Display(Name = "Reason for Delegation")]
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// GUID of the corresponding delegation record in the Dynamic Workflow Engine.
        /// Set after successfully creating the delegation in the WF engine.
        /// </summary>
        public Guid? WfDelegationId { get; set; }

        // 1=Active, 0=Inactive/Cancelled
        public short RecStat { get; set; } = 1;

        // Audit
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; } = DateTime.Now;
        public int? MdfId { get; set; }
        public DateTime? MdfDateTime { get; set; }

        // Navigation (NotMapped to avoid EF circular issues)
        [NotMapped] public virtual HrEmpGenInfo Delegator { get; set; }
        [NotMapped] public virtual HrEmpGenInfo Delegate  { get; set; }
        [NotMapped] public virtual HrLvType LvTyp         { get; set; }

        // Display helpers
        [NotMapped] public string DelegatorName  { get; set; }
        [NotMapped] public string DelegateName   { get; set; }
        [NotMapped] public string LvTypName      { get; set; }
        [NotMapped] public string StatusText     => RecStat == 1 ? "Active" : "Inactive";

        /// <summary>
        /// Returns true if delegation is currently active (within date range and status is active).
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyActive =>
            RecStat == 1 &&
            DateTime.Today >= FromDate &&
            DateTime.Today <= ToDate;
    }
}
