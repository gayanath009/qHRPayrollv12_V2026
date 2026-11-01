using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace qHRM.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the qHRMUser class
    public class qHRMUser : IdentityUser
    {
        [PersonalData]
        public int Usr_Id { get; set; }

        [PersonalData]
        public int Emp_Sys_Id { get; set; }
    }
}
