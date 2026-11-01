using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public class mvGroupPrivilege
    {
        
        [Key]
        public int PrvId { get; set; }
        public string PrvCode { get; set; }
        public int GrpId { get; set; }        

        
        [Display(Name = "Group Name")]
        public string NameEng { get; set; }

        [Display(Name = "Group Name")]
        public string NameArb { get; set; }        
        public int ParentId { get; set; }

        public Boolean RecView { get; set; }
        public Boolean RecAdd { get; set; }
        public Boolean RecEdit { get; set; }
        public Boolean RecDelete { get; set; }
        
        //public Int32 RecStat { get; set; }
    }
}
