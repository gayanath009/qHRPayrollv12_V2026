using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpGrade
    {
        public HrEmpGrade()
        {
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
        }

        public int GrdId { get; set; }

        [Display(Name = "Grade Code")]
        [Required(ErrorMessage = "Please enter Grade Code")]
        [Remote(action: "IsAlreadyExisting", controller: "HrEmpGrade", ErrorMessage = "Grade Code is already existing", AdditionalFields = "ex_GrdCode")]
        public string GrdCode { get; set; }

        [NotMapped]
        public string ex_GrdCode { get; set; }

        [Display(Name = "Grade Name")]
        [Required(ErrorMessage = "Please enter Grade Name")]
        public string TitleEng { get; set; }

        [Display(Name = "Grade Name Arb")]
        public string TitleArb { get; set; }
        [Display(Name = "Description")]
        public string DescEng { get; set; }
        [Display(Name = "Description Arb")]
        public string DescArb { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore] 
        public virtual OrgEntity Ent { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
    }
}
