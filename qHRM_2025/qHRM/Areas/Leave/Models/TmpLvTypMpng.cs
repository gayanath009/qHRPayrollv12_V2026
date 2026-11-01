using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Leave.Models
{
    public class TmpLvTypMpng
    {
        public int AutoId { get; set; }
        public string Lvtype { get; set; }
        public string LvtypeSub { get; set; }
        public string LvtypeSubPay { get; set; }
        public int LvTypId { get; set; }
        [NotMapped]
        public string LvTypName { get; set; }
    }
}
