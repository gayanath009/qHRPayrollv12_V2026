using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Integrations.Models
{
    public partial class LogLvLdPrc
    {
        public int LogId { get; set; }
        public string PrcSrc { get; set; }
        public string PrcSrcDtl { get; set; }
        public string PrcTyp { get; set; }
        public int FrmIncdnt { get; set; }
        public int ToIncdnt { get; set; }
        public DateTime PrcSt { get; set; }
        public DateTime? PrcEnd { get; set; }
        public string PrcStat { get; set; }
        public bool RstAll { get; set; }
        public int PrcBy { get; set; }
        public DateTime PrcDateTime { get; set; }
    }
}
