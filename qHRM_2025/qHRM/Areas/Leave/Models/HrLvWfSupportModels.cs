using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Leave.Models
{
    /// <summary>
    /// Logs every email notification sent for leave workflow events.
    /// Supports audit and deferred retry without depending on the external Notification DB.
    /// </summary>
    public partial class HrLvWfNotifyLog
    {
        [Key]
        public int LogId { get; set; }

        public int LvId { get; set; }

        public int? WfInstId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        public int ToEmpSysId { get; set; }

        [Required]
        [StringLength(100)]
        public string ToEmail { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsSent { get; set; } = false;

        public DateTime? SentDateTime { get; set; }

        [StringLength(500)]
        public string ErrorMsg { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CrtDateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Maps qHRM EmpSysId (int) to Dynamic Workflow Engine User GUID.
    /// Created on first use for each employee.
    /// </summary>
    public partial class HrWfUserMapping
    {
        [Key]
        public int EmpSysId { get; set; }

        [Required]
        public Guid WfUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string WfUsername { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}
