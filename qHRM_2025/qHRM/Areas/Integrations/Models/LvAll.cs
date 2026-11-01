using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Integrations.Models
{
    public partial class LvAll
    {
        [Key]
        public int Id { get; set; }
        public int? IncidentNo { get; set; }
        public string Qid { get; set; }
        public string EmpName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string DutyHandedOver { get; set; }
        public string ContactNoOnLeave { get; set; }
        public string Lvtype { get; set; }
        public string LvtypeSub { get; set; }
        public DateTime? LvStartDate { get; set; }
        public DateTime? LvEndDate { get; set; }
        public int? LvDays { get; set; }
        public int? LvYear { get; set; }
        public int? LvMonth { get; set; }
        public string Status { get; set; }
        public DateTime? LvInitiatedDate { get; set; }
        public string LvtypeSubPay { get; set; }
        public DateTime? LvEndDateActual { get; set; }
    }
}
