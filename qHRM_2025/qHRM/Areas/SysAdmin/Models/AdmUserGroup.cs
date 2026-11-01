using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class AdmUserGroup
    {
        public AdmUserGroup()
        {
            AdmUser = new HashSet<AdmUser>();
            AdmUsrGrpEmpGrp = new HashSet<AdmUsrGrpEmpGrp>();
            AdmUserGroupPrivilege = new HashSet<AdmUserGroupPrivilege>();
        }

        [Key]
        public int GrpId { get; set; }

        [Display(Name = "User Group Name")]
        [Required(ErrorMessage = "Please enter User Group Name")]
        [Remote(action: "IsExists", controller: "AdmUserGroup", ErrorMessage = "User Group Name is already existing", AdditionalFields = "exName")]
        public string Name { get; set; }

        [NotMapped]
        public string exName { get; set; }

        [Display(Name = "Desciption")]
        [Required(ErrorMessage = "Please enter Description")]
        public string Desc { get; set; }
        public bool EmpGrpSbs { get; set; }
        public int DshbrdId { get; set; }

        [NotMapped]
        public string ex_DshbrdName { get; set; }

        [Display(Name = "Show All Employees")]
        public bool ShwAlEmp { get; set; }

        [NotMapped]
        public string ex_ShwAlEmp_Txt { get { return (ShwAlEmp == true ? "Yes" : "No"); } }
        public short RecStat { get; set; }
        public DateTime? CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual SysDshbrd Dshbrd { get; set; }
        public virtual ICollection<AdmUser> AdmUser { get; set; }
        public virtual ICollection<AdmUsrGrpEmpGrp> AdmUsrGrpEmpGrp { get; set; }

        [JsonIgnore]
        public virtual ICollection<AdmUserGroupPrivilege> AdmUserGroupPrivilege { get; set; }
    }
}
