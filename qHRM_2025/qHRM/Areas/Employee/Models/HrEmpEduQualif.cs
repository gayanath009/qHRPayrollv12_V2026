using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpEduQualif
    {
        public int QualifId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Certificate No")]
        [Required(ErrorMessage = "Please specify the Certificate No")]
        [StringLength(30)]
        public string CertNo { get; set; }

        [Display(Name = "Desciption")]
        [Required(ErrorMessage = "Please specify the Certificate Description")]
        [StringLength(30)]
        public string Desc { get; set; }

        [Display(Name = "Institute")]
        [Required(ErrorMessage = "Please specify the Institute")]
        [StringLength(300)]
        public string Institute { get; set; }
        public byte[] Cert { get; set; }

        [Display(Name = "Certificate Name")]
        [StringLength(50)]
        public string CertName { get; set; }

        [NotMapped]
        public string ex_CertName { get; set; }
        public string CertMime { get; set; }

        [NotMapped]
        public string ex_CertMime { get; set; }

        [Display(Name = "Result")]
        [StringLength(50)]
        [Required(ErrorMessage = "Please specify the Result")]
        public string Result { get; set; }

        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Please specify the Start Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Please specify the End Date")]
        [Remote(action: "ValidateDateRange", controller: "HrEmpEduQualif", ErrorMessage = "Invalid Date Range", AdditionalFields = "StDate")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
