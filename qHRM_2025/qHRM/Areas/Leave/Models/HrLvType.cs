using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Areas.Leave.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Leave.Models
{
    public partial class HrLvType
    {
        public HrLvType()
        {
            HrLvAnlSchedule = new HashSet<HrLvAnlSchedule>();
            HrLvRegister = new HashSet<HrLvRegister>();
            HrLvPolicy = new HashSet<HrLvPolicy>();
        }

        [Key]
        public int LvTypId { get; set; }

        [Display(Name = "Leave Type Code")]
        [Required(ErrorMessage = "Please enter Unique Leave Type Code")]
        [Remote(action: "IsAlreadyExisting", controller: "HrLvType", ErrorMessage = "Leave Type Code is already existing", AdditionalFields = "ex_LvTypCode")]
        [StringLength(10)]
        public string LvTypCode { get; set; }

        [NotMapped]
        public string ex_LvTypCode { get; set; }

        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Please enter Leave Type Text")]
        [StringLength(10)]
        public string NameEng { get; set; }

        [StringLength(10)]
        public string NameArb { get; set; }        
        
        [Display(Name = "Description")]
        [StringLength(200)]
        public string Desc { get; set; }
        [Display(Name = "Maximum Eligible Days")]
        public int ElgDays { get; set; }
        [Display(Name = "Maximum at a Time")]
        public int MaxCntDays { get; set; }
        [Display(Name = "Prior Approval Required")]
        public bool AprlRqd { get; set; }
        [Display(Name = "Prior Schedule Required")]
        public bool PreShld { get; set; }

        [Display(Name = "No Pay Factor")]
        [Range(0.00, 1.00, ErrorMessage = "Out of range (0.00 - 1.00)")]
        public decimal SlryDctFct { get; set; }

        [Display(Name = "Rejoining Form Required")]
        public bool RjnFmRqd { get; set; }

        [Display(Name = "Applicable for Encashment")]
        public bool Encsh { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgEntity Ent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvAnlSchedule> HrLvAnlSchedule { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvRegister> HrLvRegister { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvPolicy> HrLvPolicy { get; set; }
    }
}
