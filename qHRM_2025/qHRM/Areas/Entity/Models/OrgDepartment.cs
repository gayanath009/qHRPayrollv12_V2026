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
    public partial class OrgDepartment
    {
        public OrgDepartment()
        {
            HrEmpJobAssign = new HashSet<HrEmpJobAssign>();
            HrEmpProbation = new HashSet<HrEmpProbation>();
        }

        public int DeptId { get; set; }

        [Display(Name = "Department Code")]
        [Required(ErrorMessage = "Please enter Department Code")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgDepartment", ErrorMessage = "Depaerment Code is already existing", AdditionalFields = "ex_DeptCode")]
        public string DeptCode { get; set; }

        [NotMapped]
        public string ex_DeptCode { get; set; }

        [Display(Name = "Short Name")]
        public string ShtName { get; set; }

        [Display(Name = "Department Name")]
        [Required(ErrorMessage = "Please enter department Name")]
        public string NameEng { get; set; }
        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string DescEng { get; set; }
        public string DescArb { get; set; }

        [Display(Name = "Parent Deparment")]
        [Required(ErrorMessage = "Please specify parent department")]
        public int? ParentId { get; set; }

        public int EntId { get; set; }
        public short RecStat { get; set; }
        public int SrtOrd { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgEntity Ent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpJobAssign> HrEmpJobAssign { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpProbation> HrEmpProbation { get; set; }
    }
}
