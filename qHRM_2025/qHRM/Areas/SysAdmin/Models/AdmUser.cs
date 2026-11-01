    using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
//kiran test checkin

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class AdmUser
    {
        public AdmUser()
        {
            AdmEntityUser = new HashSet<AdmEntityUser>();
        }

        public int UsrId { get; set; }

        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Please enter User Name")]
        [Remote(action: "IsExists", controller: "AdmUser", ErrorMessage = "User Name is already existing", AdditionalFields = "exUsrName")]
        public string UsrName { get; set; }
        
        [NotMapped]
        public string exUsrName { get; set; }

        
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Please enter Password")]        
        [StringLength(255, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "Confirm the password")]
        [StringLength(255, ErrorMessage = "Must be between 6 and 255 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [NotMapped]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Group Name")]
        public int GrpId { get; set; }
        [Required(ErrorMessage = "Please choose an Employee")]
        public int EmpSysId { get; set; }

        
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }
        public short RecStat { get; set; }
        public DateTime? CrtDateTime { get; set; }

        [NotMapped]
        [Display(Name = "Group Name")]
        public string GrpName { get; set; }
        
        
        [NotMapped]
        [Display(Name = "Employee")]
        public string FullName { get; set; }

        [NotMapped]
        public string email { get; set; }

        [NotMapped]
        public List<AdmUserGroup> lstUG { get; set; }
        [NotMapped]
        public List<HrEmpGenInfo> lstEmp { get; set; }

        [JsonIgnore]
        public virtual AdmUserGroup Grp { get; set; }
        public virtual AdmUserLogs AdmUserLogs { get; set; }
        public virtual ICollection<AdmEntityUser> AdmEntityUser { get; set; }
        
    }
}
