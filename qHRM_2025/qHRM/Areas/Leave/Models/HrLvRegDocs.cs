using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Leave.Models
{
    public class HrLvRegDocs
    {
        public int DocId { get; set; }
        public int LvId { get; set; }
        public string Desc { get; set; }
        public byte[] ScnCopy { get; set; }
        public string DocName { get; set; }
        public string DocMime { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrLvRegister Lv { get; set; }
    }
}
