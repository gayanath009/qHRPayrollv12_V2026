using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Entity.Models
{
    public partial class OrgHoliday
    {
        public int HdId { get; set; }

        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Select Start Date")]
        [DataType(DataType.Date)]
        public DateTime StDate { get; set; }

        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Select End Date")]
        [DataType(DataType.Date)]        
        [Remote(action: "ValidateDateRange", controller: "OrgHoliday", ErrorMessage = "Invalid Date Range", AdditionalFields = "StDate")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Recurring")]
        public bool Recurring { get; set; }

        [NotMapped]
        public string Recurring_Txt { get { if (Recurring) return "Yes"; else return "No"; }}

        [Display(Name = "Holiday Name")]
        [Required(ErrorMessage = "Enter Holiday Name")]
        [StringLength(50)]
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }
    }
}
