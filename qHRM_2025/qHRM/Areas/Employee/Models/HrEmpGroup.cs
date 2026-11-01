using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Areas.SysAdmin.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpGroup
    {
        public HrEmpGroup()
        {
            AdmUsrGrpEmpGrp = new HashSet<AdmUsrGrpEmpGrp>();
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
        }

        public int GrpId { get; set; }
       
        [Display(Name = "Group Code")]
        [Required(ErrorMessage = "Enter Group Code")]
        [Remote(action: "IsExisting", controller: "HrEmpGroup", ErrorMessage = "Group Code already exists", AdditionalFields = "ex_GrpCode")]
        public string GrpCode { get; set; }

        [NotMapped]
        public string ex_GrpCode { get; set; }

        [Display(Name = "Group Title")]
        [Required(ErrorMessage = "Enter Group Title")]
        public string TitleEng { get; set; }

        public string TitleArb { get; set; }

        [Display(Name = "Description")]
        public string DescEng { get; set; }
        public string DescArb { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<AdmUsrGrpEmpGrp> AdmUsrGrpEmpGrp { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
    }
}
