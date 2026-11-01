using System;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysSelectListElement
    {
        public int LstElmntId { get; set; }
        public int LstTypId { get; set; }
        public short Indx { get; set; }
        public string ElmntTxt { get; set; }
        public string Desc { get; set; }
        public bool? ResTyp { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDatetime { get; set; }

        public virtual SysSelectListType LstTyp { get; set; }
    }
}
