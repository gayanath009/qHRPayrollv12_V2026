using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using System;
using System.ComponentModel.DataAnnotations;


namespace qHRM.Areas.WorkShift.Models
{
    public partial class HrWkRosterShift
    {
        public int SchId { get; set; }
        [Display(Name = "Roster")]
        public int RstId { get; set; }

        [Display(Name = "Day")]
        [Required(ErrorMessage = "Day cannot be blank.")]
        //[Remote(action: "TestValidation", controller: "HrWkRosterShift", ErrorMessage = "Test Validation on day")]
        public string Day { get; set; }

        [Display(Name = "Shift")]
        [Required(ErrorMessage = "Roster Code cannot be blank.")]
        //[Remote(action: "ValidateShift", controller: "HrWkRosterShift", ErrorMessage = "Shift cannot be overlap with other shifts in same day.", AdditionalFields = "RstId,SchId,Day")] //SftId
        public int SftId { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description is Required.")]
        public string DescEng { get; set; }
        public string DescArb { get; set; }
        public decimal PrfIndx { get; set; }
        public int EntId { get; set; }

        [Display(Name = "Status")]
        public short RecStat { get; set; }
        public string ex_RecStatTxt { get { return (RecStat == 1) ? "Active" : "Deactive"; } }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual HrWkRoster Rst { get; set; }
        public virtual HrWkShift Sft { get; set; }
    }
}
