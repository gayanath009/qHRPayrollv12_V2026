using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Attendance.Models
{
    public partial class LogAtndcLdPrc
    {
        public int LogId { get; set; }
        public string PrcSrc { get; set; }
        public string PrcSrcDtl { get; set; }
        public string PrcTyp { get; set; }
        public int RefId { get; set; }
        public DateTime FrmDate { get; set; }
        public DateTime ToDate { get; set; }
        public int PrcBy { get; set; }
        public DateTime PrcDateTime { get; set; }
    }
}
