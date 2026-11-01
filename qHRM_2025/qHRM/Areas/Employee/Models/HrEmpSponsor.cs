using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpSponsor
    {
        public int SpnsrId { get; set; }
        public int EmpSysId { get; set; }

        [Required(ErrorMessage = "Please specify Sponsorship Type")]
        [Display(Name = "Sponsorship Type")]
        public string SpnsrType { get; set; }

        [Required(ErrorMessage = "Please specify Relationship Type")]
        [Display(Name = "Relationship Type")]
        public string RltnType { get; set; }

        [Display(Name = "Labor Card")]
        [StringLength(20)]
        public string LbrCard { get; set; }

        [Display(Name = "Company Details")]
        [StringLength(200)]
        public string CmpDtl { get; set; }

        [Display(Name = "Secondment (ERA) No.")]
        [StringLength(20)]
        public string EraNo { get; set; }

        [Display(Name = "From Date")]
        [Required(ErrorMessage = "Please specify From Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "To Date")]
        //[Required(ErrorMessage = "Please specify To Date (Expiry)")]
        [Remote(action: "ValidateDateRange", controller: "HrEmpSponsor", ErrorMessage = "Invalid Date Range", AdditionalFields = "StDate")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Note")]
        [StringLength(300)]
        public string Note { get; set; }

        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
