using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAttendExp 
    {
        public int AtnExpId { get; set; }

        [Display(Name = "Employee Name")]
        [Required(ErrorMessage = "Please choose the Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Please specify Start Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Please specify End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [NotMapped]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime Del_StDate { get; set; }

        [NotMapped]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime Del_EndDate { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }
        public int? RqstId { get; set; }
        public int? AprvdId { get; set; }
        public int? AprvdStatId { get; set; }

        [Display(Name = "Date")]
        public DateTime? AprvdDateTime { get; set; }
        public string AprvdRmks { get; set; }
        public short RecStat { get; set; } = 1;
        public int? CrtId { get; set; }
        public DateTime CrtDateTime { get; set; } = DateTime.Now;

        // Attendance Regularization Fields
        [Column("Req_No")]
        public string ReqNo { get; set; }

        [Column("Req_In_Time")]
        public TimeSpan? ReqInTime { get; set; }

        [Column("Req_Out_Time")]
        public TimeSpan? ReqOutTime { get; set; }

        [Column("Actual_In_Time")]
        public TimeSpan? ActualInTime { get; set; }

        [Column("Actual_Out_Time")]
        public TimeSpan? ActualOutTime { get; set; }

        [Column("Total_Hours")]
        public decimal? TotalHours { get; set; }

        [Column("Reason_Category")]
        public string ReasonCategory { get; set; }

        [Column("Reason_Description")]
        public string ReasonDescription { get; set; }

        [Column("Attachment_Path")]
        public string AttachmentPath { get; set; }

        [Column("Attachment_FileName")]
        public string AttachmentFileName { get; set; }

        [Column("Period_Type")]
        public string PeriodType { get; set; }

        [Column("Wf_Status")]
        public string WfStatus { get; set; } = "PendingManager";

        [Column("Wf_Step")]
        public int? WfStep { get; set; } = 1;

        [Column("Manager_Emp_Id")]
        public int? ManagerEmpId { get; set; }

        [Column("Manager_Action")]
        public string ManagerAction { get; set; }

        [Column("Manager_Action_DateTime")]
        public DateTime? ManagerActionDateTime { get; set; }

        [Column("Manager_Comments")]
        public string ManagerComments { get; set; }

        [Column("Hr_Emp_Id")]
        public int? HrEmpId { get; set; }

        [Column("Hr_Action")]
        public string HrAction { get; set; }

        [Column("Hr_Action_DateTime")]
        public DateTime? HrActionDateTime { get; set; }

        [Column("Hr_Comments")]
        public string HrComments { get; set; }

        [Column("Is_Lop_Applied")]
        public bool? IsLopApplied { get; set; }

        [Column("Mdf_Id")]
        public int? MdfId { get; set; }

        [Column("Mdf_DateTime")]
        public DateTime? MdfDateTime { get; set; }

        [NotMapped]
        public string EmpName { get; set; }
        [NotMapped]
        public string EmpIdStr { get; set; }
        [NotMapped]
        public string DeptName { get; set; }
        [NotMapped]
        public string DesigName { get; set; }
        [NotMapped]
        public string ManagerName { get; set; }
        [NotMapped]
        public string HrName { get; set; }
        [NotMapped]
        public string ActualWorkedHoursText { get; set; }

        [NotMapped]
        public int? empSysId  { get; set; }
        [NotMapped]
        public int? dptId { get; set; }
        [NotMapped]
        public string empTyp { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo Aprvd { get; set; }
        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
        [JsonIgnore]
        public virtual HrEmpGenInfo Rqst { get; set; }
    }
}
