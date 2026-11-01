using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Entity.Models
{
    public partial class OrgDesignation
    {
        public OrgDesignation()
        {
            HrEmpJobAssign = new HashSet<HrEmpJobAssign>();
            HrEmpProbation = new HashSet<HrEmpProbation>();
        }

        public int DesigId { get; set; }

        [Display(Name = "Designation Code")]
        [Required(ErrorMessage = "Please enter Designation Code")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgDesignation", ErrorMessage = "Designation Code is already existing", AdditionalFields = "ex_DesigCode")]
        public string DesigCode { get; set; }

        [NotMapped]
        public string ex_DesigCode { get; set; }

        [Display(Name = "Designation Name")]
        [Required(ErrorMessage = "Please enter designation Name")]
        public string NameEng { get; set; }
        public string NameArb { get; set; }


        [Display(Name = "Job Description")]
        public string JobDescEng { get; set; }
        public string JobDescArb { get; set; }

        [Display(Name = "Designation Level")]
        public int DesigLevel { get; set; }

        [Display(Name = "Minimum Employee Grade")]
        public int MinEmpGrd { get; set; }

        [Display(Name = "Maximum Employee Grade")]
        public int MaxEmpGrd { get; set; }

        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgEntity Ent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpJobAssign> HrEmpJobAssign { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpProbation> HrEmpProbation { get; set; }
    }
}
