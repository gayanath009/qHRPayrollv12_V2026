using System;
using System.Collections.Generic;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysSystemPrivilege
    {
        public SysSystemPrivilege()
        {
            AdmUserGroupPrivilege = new HashSet<AdmUserGroupPrivilege>();
            InverseParent = new HashSet<SysSystemPrivilege>();
        }

        public int PrvId { get; set; }
        public string PrvCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public int? ParentId { get; set; }
        public short RecStat { get; set; }
        public string Target { get; set; }
        public string Icon { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual SysSystemPrivilege Parent { get; set; }
        public virtual ICollection<AdmUserGroupPrivilege> AdmUserGroupPrivilege { get; set; }
        public virtual ICollection<SysSystemPrivilege> InverseParent { get; set; }
    }
}
