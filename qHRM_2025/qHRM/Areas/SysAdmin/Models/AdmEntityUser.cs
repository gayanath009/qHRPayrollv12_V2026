using qHRM.Areas.General.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.SysAdmin.Models
{
    
    public partial class AdmEntityUser
    {
       [Key]
        public int AutoId { get; set; }
        public int EntId { get; set; }
        public int UsrId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual AdmUser Usr { get; set; }
    }
}
