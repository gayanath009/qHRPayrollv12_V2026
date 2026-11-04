using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Leave.Models
{
    /// <summary>
    /// Maps each Leave Type to a Dynamic Workflow Definition code in the Workflow Engine.
    /// Administrators configure this via the Leave Workflow Configuration screen.
    /// </summary>
    public partial class HrLvWfConfig
    {
        [Key]
        public int WfCfgId { get; set; }

        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Please select a Leave Type")]
        public int LvTypId { get; set; }

        [Display(Name = "Workflow Code")]
        [Required(ErrorMessage = "Please enter the Workflow Code")]
        [StringLength(50)]
        public string WfCode { get; set; }

        [Display(Name = "Workflow Enabled")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime? EffDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpDate { get; set; }

        /// <summary>
        /// JSON: stores specific employee IDs for named approvers
        /// e.g. {"OutsourceRecordOwnerEmpId":97,"ReportingManagerEmpId":405,"HrManagerEmpId":6,"FinanceEmpId":452,"CeoEmpId":11,"BuHeadEmpId":2}
        /// </summary>
        public string ApproverConfig { get; set; }

        /// <summary>
        /// JSON: stores configurable policy rules for the leave type
        /// e.g. {"MedCertRequired":true,"PayrollCutoffDay":20,"FullPayDays":14}
        /// </summary>
        public string PolicyRules { get; set; }

        // Audit fields
        public short RecStat { get; set; } = 1;
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; } = DateTime.Now;
        public int? MdfId { get; set; }
        public DateTime? MdfDateTime { get; set; }

        // Navigation
        [NotMapped]
        public virtual HrLvType LvTyp { get; set; }

        // Deserialized approver config (not mapped to DB)
        [NotMapped] public int? OutsourceRecordOwnerEmpId { get; set; }
        [NotMapped] public int? ReportingManagerEmpId     { get; set; }
        [NotMapped] public int? HrManagerEmpId            { get; set; }
        [NotMapped] public int? FinanceEmpId              { get; set; }
        [NotMapped] public int? CeoEmpId                  { get; set; }
        [NotMapped] public int? BuHeadEmpId               { get; set; }

        // Deserialized policy rules (not mapped to DB)
        [NotMapped] public bool   MedCertRequired  { get; set; }
        [NotMapped] public int    PayrollCutoffDay  { get; set; } = 20;
        [NotMapped] public int    FullPayDays       { get; set; }
        [NotMapped] public int    HalfPayDays       { get; set; }
        [NotMapped] public bool   NoPay             { get; set; }
        [NotMapped] public int    MaxDays           { get; set; }
        [NotMapped] public bool   OnceInService     { get; set; }
        [NotMapped] public int    AnnualEntitlement { get; set; }
        [NotMapped] public int    EligibleDays      { get; set; }
        [NotMapped] public int    MinServiceYears   { get; set; }
        [NotMapped] public string GenderRequired    { get; set; }
        [NotMapped] public int    HoursPerDay       { get; set; }
        [NotMapped] public int    EligibilityDaysFromMaternity { get; set; } = 365;

        // Display helpers
        [NotMapped] public string LvTypName { get; set; }
        [NotMapped] public string StatusText => IsEnabled ? "Active" : "Inactive";
    }
}
