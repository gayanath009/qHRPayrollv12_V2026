using qHRM.Areas.SysAdmin.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{  
     
    public class mvUserGroupAssign
    {
               
        public IEnumerable<AdmUserGroup> lstUsrGroup { get; set; }
        public IEnumerable<mvGroupPrivilege> lstParent_Priv { get; set; }
        public IEnumerable<mvGroupPrivilege> lstChild_Priv_1 { get; set; }
        public IEnumerable<mvGroupPrivilege> lstChild_Priv_2 { get; set; }
    }
}
