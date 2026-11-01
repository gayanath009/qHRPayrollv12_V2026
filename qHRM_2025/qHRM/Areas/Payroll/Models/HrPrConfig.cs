using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static qHRM.Areas.Payroll.Data.PayrollContext;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrConfig
    {
        public HrPrConfig()
        {
            HrPrCnfGrtvtyLvl = new HashSet<HrPrCnfGrtvtyLvl>();
        }
        public int PrCnfId { get; set; }

        [Display(Name = "Salary Process Starting Year")]
        [Required(ErrorMessage = "Salary process starting year cannot be blank")]
        public int SlryPrcStYear { get; set; }

        [Display(Name = "Salary Process Starting Month")]
        [Required(ErrorMessage = "Salary process starting month cannot be blank")]
        public int SlryPrcStMonth { get; set; }

        [Display(Name = "Attendance Cutoff at End of the Month")]
        public bool AttnCtEom { get; set; }

        [Display(Name = "Attendance Cutoff Date")]
        //[Remote(action: "ValidateAttnCtDay", controller: "HrPrConfig", ErrorMessage = "Out of range (1 - 28)", AdditionalFields = "AttnCtEom")]
        public int AttnCtDay { get; set; }

        [Display(Name = "Salary Process at End of the Month")]
        public bool SlryPrcEom { get; set; }

        [Display(Name = "Salary Process Date")]
        [Remote(action: "ValidateSlryPrcDay", controller: "HrPrConfig", ErrorMessage = "Out of range (1 - 28)", AdditionalFields = "SlryPrcEom")]
        public int SlryPrcDay { get; set; }


        [Display(Name = "Use Actual Working Hours in Month")]
        public bool OtClcActMntHrs { get; set; }

        [Display(Name = "No of Effective Hours per Month")]
        [Range(1, 365, ErrorMessage = "Out of range (1 - 240)")]
        public int OtClcMntHrsVal { get; set; }

        [Display(Name = "Payer Employer ID")]
        [Required(ErrorMessage = "Payer Employer ID cannot be blank")]
        public string PyrEid { get; set; }

        [Display(Name = "Payer QID")]
        [Required(ErrorMessage = "Payer QID cannot be blank")]
        public string PyrQid { get; set; }

        [Display(Name = "Payer Bank Short Name")]
        [Required(ErrorMessage = "Payer Bank Short Name cannot be blank")]
        public string PyrBnkShtNm { get; set; }

        [Display(Name = "Payer IBAN")]
        [Required(ErrorMessage = "Payer IBAN cannot be blank")]
        public string PyrIban { get; set; }

        [Display(Name = "Payer Bank Name")]
        public string PyrBnkNm { get; set; }

        [Display(Name = "Payer Bank Branch")]
        public string PyrBrnch { get; set; }

        [Display(Name = "Include Last (Final) salary in salary process")]
        public bool SlryPrcLstMnt { get; set; }

        [Display(Name = "Minimum working days to eligible to leave encashment (in Months)")]
        public int? EsLvSMnWkPrd { get; set; }

        public bool EsLvSActDiY { get; set; }

        [Display(Name = "Effective no of days in year for leave encashment calculation")]
        [Range(1, 365, ErrorMessage = "Out of range (1 - 365)")]
        public int EsLvSDiYVal { get; set; }

        public bool EsLmsActDiM { get; set; }

        [Display(Name = "Salary divide by no of days")]
        [Remote(action: "ValidateEsLmsDiMVal", controller: "HrPrConfig", ErrorMessage = "Out of range (1 - 31)", AdditionalFields = "EsLmsDiMTyp")]
        public int EsLmsDiMVal { get; set; }

        public bool EsGrtActDiM { get; set; }

        [Display(Name = "Effective Days in month for gratuity")]
        [Range(1, 31, ErrorMessage = "Out of range (1 - 31)")]
        public int EsGrtDiMVal { get; set; }

        public bool EsGrtActDiY { get; set; }

        [Display(Name = "Fixed on of Days in year")]
        [Range(1, 365, ErrorMessage = "Out of range (1 - 365)")]
        public int EsGrtDiYVal { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [Display(Name = "Ext 1")]
        public string Ext1 { get; set; }
        [Display(Name = "Ext 2")]
        public string Ext2 { get; set; }

        [Display(Name = "Ext 3")]
        public string Ext3 { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrPrCnfGrtvtyLvl> HrPrCnfGrtvtyLvl { get; set; }
    }
}
