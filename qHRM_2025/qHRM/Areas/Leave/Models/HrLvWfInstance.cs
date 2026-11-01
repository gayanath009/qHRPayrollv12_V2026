using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Leave.Models
{
    /// <summary>
    /// Tracks the Dynamic Workflow Engine instance ID for each Leave Application.
    /// Created when an employee submits a leave application and the workflow starts.
    /// </summary>
    public partial class HrLvWfInstance
    {
        [Key]
        public int WfInstId { get; set; }

        [Required]
        public int LvId { get; set; }

        /// <summary>
        /// The GUID primary key of the WorkflowInstance in the Dynamic Workflow Engine database.
        /// Used for all API calls to the Workflow Engine.
        /// </summary>
        [Required]
        public Guid WfInstanceId { get; set; }

        /// <summary>
        /// Human-readable instance number, e.g. "LV_SICK-20260819-4521"
        /// </summary>
        [Required]
        [StringLength(100)]
        public string WfInstanceNo { get; set; }

        /// <summary>
        /// Current workflow status: InProgress | Approved | Rejected | Cancelled | Returned
        /// Synced from the Workflow Engine via the callback endpoint.
        /// </summary>
        [Required]
        [StringLength(30)]
        public string WfStatus { get; set; } = "InProgress";

        public DateTime CrtDateTime { get; set; } = DateTime.Now;
        public DateTime? MdfDateTime { get; set; }

        // Navigation
        [NotMapped]
        public virtual HrLvRegister LvReg { get; set; }

        // Display helpers
        [NotMapped] public string StatusBadgeClass =>
            WfStatus switch {
                "Approved"        => "badge-success",
                "Rejected"        => "badge-danger",
                "Cancelled"       => "badge-secondary",
                "Returned"        => "badge-warning",
                "Leave Initiated" => "badge-primary",
                _                 => "badge-info"
            };
    }
}
