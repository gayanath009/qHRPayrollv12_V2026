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
        [Remote(action: "Validate", controller: "HrEmpAttendExp", ErrorMessage = "Invalid Date Range", AdditionalFields = "EmpSysId,StDate")]
        public DateTime EndDate { get; set; }

        [NotMapped]
        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Please specify Start Date")]
        [DataType(DataType.Date)]
        public DateTime Del_StDate { get; set; }

        [NotMapped]
        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Please specify End Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateDateRange", controller: "HrEmpAttendExp", ErrorMessage = "Invalid Date Range", AdditionalFields = "Del_StDate")]
        public DateTime Del_EndDate { get; set; }

        [Display(Name = "Note")]
        [Required(ErrorMessage = "Please specify Note")]
        public string Note { get; set; }
        public int? RqstId { get; set; }
        public int? AprvdId { get; set; }
        public int? AprvdStatId { get; set; }

        [Display(Name = "Date")]
        public DateTime? AprvdDateTime { get; set; }
        public string AprvdRmks { get; set; }
        public short RecStat { get; set; }
        public int? CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

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
