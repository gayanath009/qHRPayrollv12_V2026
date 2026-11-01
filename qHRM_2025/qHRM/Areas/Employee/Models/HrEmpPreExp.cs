using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using qHRM.Areas.Employee.CustomValidation;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpPreExp
    {
        public int ExpId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Company Name")]
        //[Remote(action: "IsExistCompanyName", controller: "HrEmpPreExp", ErrorMessage = "Company is already existing", AdditionalFields = "ex_CompName")]
        [Required(ErrorMessage = "Please specify Company Name")]
        [StringLength(300)]
        public string CompName { get; set; }        

        //[NotMapped]
        //public string ex_CompName { get; set; }

        [Display(Name = "Position")]
        [Required(ErrorMessage = "Please specify Position")]
        [StringLength(30)]
        public string Position { get; set; }

        [Display(Name = "Service Certificate")]
        public byte[] SvcCtfct { get; set; }

        public string SvcFileName { get; set; }

        [NotMapped]
        public string ex_SvcFileName { get; set; }        
        public string SvcFileMime { get; set; }
        [NotMapped]
        public string ex_SvcFileMime { get; set; }

        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Please specify Start Date")]        
        [DataType(DataType.Date)]        
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Please specify End Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateDateRange", controller: "HrEmpPreExp", ErrorMessage = "Invalid Date Range", AdditionalFields = "StDate")]        
        public DateTime EndDate { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }


        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
