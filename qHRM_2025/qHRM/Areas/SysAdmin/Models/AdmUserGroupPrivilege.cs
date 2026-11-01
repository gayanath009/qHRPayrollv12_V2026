using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class AdmUserGroupPrivilege
    {
        public int AutoId { get; set; }
        public int GrpId { get; set; }
        public int PrvId { get; set; }
        public bool RecView { get; set; }
        public bool RecAdd { get; set; }
        public bool RecEdit { get; set; }
        public bool RecDelete { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }
        public int ModId { get; set; }       

        [JsonIgnore]
        public virtual AdmUserGroup Grp { get; set; }

        [JsonIgnore]
        public virtual SysSystemPrivilege Prv { get; set; }
    }
}
