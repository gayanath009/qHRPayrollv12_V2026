using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.WorkShift.Models
{
    public partial class HrWkRoster
    {
        public HrWkRoster()
        {
            HrEmpWkRoster = new HashSet<HrEmpWkRoster>();
            HrWkRosterShift = new HashSet<HrWkRosterShift>();
        }

        public int RstId { get; set; }

        [Display(Name = "Roster Code")]
        [Required(ErrorMessage = "Roster Code cannot be blank.")]
        [Remote(action: "IsAlreadyExist", controller: "HrWkRoster", ErrorMessage = "Roster Code is already existing", AdditionalFields = "ex_RstCode")]
        public string RstCode { get; set; }

        [NotMapped]
        public string ex_RstCode { get; set; }

        [Display(Name = "Roster Name")]
        [Required(ErrorMessage = "Roster name cannot be blank.")]
        public string NameEng { get; set; }
        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string DescEng { get; set; }
        public string DescArb { get; set; }

        [Display(Name = "No of Shifts per Day")]
        public int SftNos { get; set; }
        public int EntId { get; set; }

        [Display(Name = "Status")]
        public short RecStat { get; set; }
        public string RecStatTxt { get { return (RecStat == 1) ? "Active" : "Deactive"; } }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }

        public virtual ICollection<HrEmpWkRoster> HrEmpWkRoster { get; set; }
        public virtual ICollection<HrWkRosterShift> HrWkRosterShift { get; set; }
    }
}
