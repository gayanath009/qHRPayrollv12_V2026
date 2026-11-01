using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.Entity.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Leave.Models
{
    public partial class HrLvRegister
    {
        public HrLvRegister()
        {
            HrLvRegDocs = new HashSet<HrLvRegDocs>();
        }

        public int LvId { get; set; }

        //[NotMapped]
        //public int exLvId { get; set; }11


        [Display(Name = "Employee Name")]
        [Required(ErrorMessage = "Please choose an employee")]
        public int? EmpSysId { get; set; }

        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Please choose the leave type")]
        public int? LvTypId { get; set; }

        [Display(Name = "Leave Year")]
        public int LvYear { get; set; }
        public int Incident { get; set; }

        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Please specify the Start Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Please specify the End Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateDateRange", controller: "HrLvRegister", ErrorMessage = "You already have a leave within the selected date range.", AdditionalFields = "StDate,EmpSysId,LvId")]
        public DateTime EndDate { get; set; }

        [Display(Name = "No. of Leave Days")]
        public int Days { get; set; }

        [Display(Name = "Eligible Days")]
        public int CurrAvblDays { get; set; }

        [Display(Name = "Rejoin Date")]
        [DataType(DataType.Date)]
        public DateTime? RjnDate { get; set; }

        [Display(Name = "Rejoin Status")]
        public short RjnStat { get; set; }
        public int? LvSdlId { get; set; }
        public int? DocId { get; set; }
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        // ── Workflow Integration ──────────────────────────────────────────────
        /// <summary>
        /// Workflow status from the Dynamic Workflow Engine.
        /// Values: null (legacy) | Draft | Submitted | InProgress | PendingResubmission |
        ///         Approved | Rejected | Cancelled
        /// </summary>
        public string WfStat { get; set; }

        /// <summary>FK to HrLvWfInstance.WfInstId — populated when a workflow is started.</summary>
        public int? WfInstId { get; set; }

        // Display helpers (not mapped)
        [NotMapped]
        public string WfStatBadgeClass =>
            WfStat switch {
                "Approved"             => "badge badge-success",
                "Rejected"             => "badge badge-danger",
                "Cancelled"            => "badge badge-secondary",
                "PendingResubmission"  => "badge badge-warning",
                "Leave Initiated"      => "badge badge-primary",
                "Submitted"            => "badge badge-primary",
                "InProgress"           => "badge badge-info",
                _                      => "badge badge-light"
            };

        [NotMapped]
        public string WfStatDisplay =>
            string.IsNullOrEmpty(WfStat) ? "Not Submitted" : WfStat;

        [NotMapped]
        public bool HasWorkflow => !string.IsNullOrEmpty(WfStat);

        [NotMapped]
        public DateTime ex_StDate { get; set; }

        [NotMapped]
        public DateTime ex_EndDate { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
        [JsonIgnore]
        public virtual HrLvAnlSchedule LvSdl { get; set; }
        [JsonIgnore]
        public virtual HrLvType LvTyp { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvRegDocs> HrLvRegDocs { get; set; }
    }
}
