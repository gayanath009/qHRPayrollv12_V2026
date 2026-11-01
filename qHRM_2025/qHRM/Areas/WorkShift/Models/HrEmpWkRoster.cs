using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.WorkShift.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.WorkShift.Models
{
    public partial class HrEmpWkRoster
    {

        [Key]
        public int RecId { get; set; }        
        [Display(Name = "Employee Name")]
        [Required(ErrorMessage = "Please specify the Employee Name")]        
        public int EmpSysId { get; set; }

        [Display(Name = "Roster")]
        [Required(ErrorMessage = "Please specify the Roster.")]
        public int RstId { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [Remote(action: "IsRosterAssign", controller: "HrEmpWkRoster", ErrorMessage = "Roster is already assigned to the date range or Invalid date range", AdditionalFields = "StDate,EndDate,EmpSysId,RecId")]        
        public DateTime? EndDate { get; set; }
        
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }
        public bool CndrAtndc { get; set; }

        [NotMapped]
        public string EmpTyp { get; set; }

        [NotMapped]
        public string SkpRstVldtn { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
        [JsonIgnore]
        public virtual HrWkRoster Rst { get; set; }
    }
}
