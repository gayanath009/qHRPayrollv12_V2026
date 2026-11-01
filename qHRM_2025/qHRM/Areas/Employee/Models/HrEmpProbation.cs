using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Entity.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpProbation
    {
        public int ProbId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Assigned Date")]
        [Required(ErrorMessage = "Enter Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Enter End Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateDateRange", controller: "HrEmpProbation", ErrorMessage = "Invalid Date Range", AdditionalFields = "StDate")]
        public DateTime EndDate { get; set; }
        public int DeptId { get; set; }
        public int DesigId { get; set; }
        public short WkStat { get; set; }
        [Display(Name = "Status")]
        [NotMapped]
        public string WkStatName { get { return (WkStat == 1) ? "Active " : "Closed"; } }

        public bool IsExtended { get; set; }

        [Display(Name = "Extended")]
        [NotMapped]
        public string Extended { get { return (IsExtended == true) ? "Yes" : "No"; } }

        [Display(Name = "Special Notes")]
        public string Note { get; set; }

        public int? RptMngId { get; set; }
        public string OfcPhone { get; set; }
        public string OfcEmail { get; set; }

        public int CrtId { get; set; }

        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual OrgDepartment Dept { get; set; }
        [JsonIgnore]
        public virtual OrgDesignation Desig { get; set; }

        [NotMapped]
        [IgnoreDataMember]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
