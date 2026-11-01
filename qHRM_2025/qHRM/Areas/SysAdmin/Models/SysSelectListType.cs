using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysSelectListType
    {
        public SysSelectListType()
        {
            SysSelectListElement = new HashSet<SysSelectListElement>();
        }

        public int LstTypId { get; set; }
        public string LstTypKey { get; set; }
        public string LstTypName { get; set; }
        public string Section { get; set; }
        public string Desc { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<SysSelectListElement> SysSelectListElement { get; set; }
    }
}