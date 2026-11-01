using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Areas.Leave.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrOtrate
    {
        public HrPrOtrate()
        {
            HrPrEmpSlryStruc = new HashSet<HrPrEmpSlryStruc>();
        }

        [Key]
        public int OtId { get; set; }
        [Display(Name = "OT Code")]
        [Required(ErrorMessage = "Please enter Unique OT Code")]
        [Remote(action: "IsAlreadyExisting", controller: "HrPrOtrate", ErrorMessage = "OT Code is already existing", AdditionalFields = "ex_OtCode")]
        [StringLength(10)]
        public string OtCode { get; set; }

        [NotMapped]
        public string ex_OtCode { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please enter Description")]
        [StringLength(50)]
        public string DescEng { get; set; }

        [Display(Name = "Description - Arabic")]
        [StringLength(50)]
        public string DescArb { get; set; }

        [Display(Name = "Normal Hour Rate")]
        [Range(0.00, 10.00)]
        [Required(ErrorMessage = "Enter Normal Hour Rate")]
        public decimal NmlRate { get; set; }

        [Display(Name = "Extended Hour Rate")]
        [Range(0.00, 10.00)]
        [Required(ErrorMessage = "Enter Extended Hour Rate")]
        public decimal ExtRate { get; set; }

        [Display(Name = "Apply only for basic salary")]
        public bool OnlyBasic { get; set; }

        [NotMapped]
        public string ex_OnlyBasic { get { return (OnlyBasic == true) ? "Yes " : "No"; } }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrPrEmpSlryStruc> HrPrEmpSlryStruc { get; set; }
    }
}
