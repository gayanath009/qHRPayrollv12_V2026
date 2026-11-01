using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpResidInfo
    {
        public int ResidId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Resident Type")]
        [Required(ErrorMessage = "Please specify Resident Type")]
        public string ResidType { get; set; }

        [Display(Name = "Flat/Villa No")]
        [StringLength(10)]
        public string PropNo { get; set; }

        [Display(Name = "Building No")]
        [StringLength(20)]
        public string BldNo { get; set; }

        [Display(Name = "Street")]
        [StringLength(50)]
        [Required(ErrorMessage = "Please specify Street")]
        public string Strt { get; set; }

        [Display(Name = "Area")]
        [Required(ErrorMessage = "Please specify Area")]
        [StringLength(30)]
        public string Area { get; set; }

        [Display(Name = "State")]
        [StringLength(20)]
        public string State { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Please specify Country")]
        public int? CntryId { get; set; }

        [Display(Name = "Telephone")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [StringLength(20)]
        public string TelNo { get; set; }

        [Display(Name = "Mobile")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [StringLength(20)]
        public string MobNo { get; set; }

        [Display(Name = "Move-in Date")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Please specify Move-In Date")]
        public DateTime InDate { get; set; }

        [Display(Name = "Move-out Date")]
        [Remote(action: "ValidateDateRange", controller: "HrEmpResidInfo", ErrorMessage = "Invalid Date Range", AdditionalFields = "InDate,IsToDate")]
        [DataType(DataType.Date)]
        public DateTime? OutDate { get; set; }

        [Display(Name = "Note")]
        [Required(ErrorMessage = "Please specify Notes")]
        [StringLength(300)]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [Display(Name = "To Date")]
        public bool IsToDate { get; set; }

        [JsonIgnore]
        public virtual OrgCountry Cntry { get; set; }
        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
