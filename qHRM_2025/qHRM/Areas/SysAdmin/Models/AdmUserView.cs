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
    public partial class AdmUserView
    {
        [Key]
        public int Usr_Id { get; set; }

        [Required(ErrorMessage = "Please choose an Employee")]
        public int Emp_Sys_Id { get; set; }
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Please enter User Name")]
        [Remote(action: "IsExists", controller: "AdmUser", ErrorMessage = "User Name is already existing", AdditionalFields = "exUsr_Name")]
        public string Usr_Name { get; set; }
        public string exUsr_Name { get; set; }


        [Display(Name = "Old Password")]
        [Required(ErrorMessage = "Please enter Old Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [RegularExpression(@"^((?=.*[a-z])(?=.*[A-Z])(?=.*\d)).+$")]
        [DataType(DataType.Password)]
        public string Old_Password { get; set; }


        [Display(Name = "Password")]
        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [RegularExpression(@"^((?=.*[a-z])(?=.*[A-Z])(?=.*\d)).+$")]
        [DataType(DataType.Password)]
        public string Password { get; set; }



        [Required(ErrorMessage = "Confirm the password")]
        [StringLength(255, ErrorMessage = "Must be between 6 and 255 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [NotMapped]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Group Name")]
        public int Grp_Id { get; set; }

        [Display(Name = "Group Name")]
        public string Grp_Name { get; set; }

        [Display(Name = "Employee")]
        public string Full_Name { get; set; }
        public string Remarks { get; set; }
        public string AspUser_Id { get; set; }
        public string AspUser_Name { get; set; }

        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Please specify the email")]
        public string email { get; set; }

        [Display(Name = "Entity")]
        [Required(ErrorMessage = "Please choose the Entity")]
        public int EntityId { get; set; }


    }
}
