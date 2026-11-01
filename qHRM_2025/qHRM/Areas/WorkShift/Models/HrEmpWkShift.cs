using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace qHRM.Areas.WorkShift.Models
{
    public partial class HrEmpWkShift
    {
        [Key]
        public int RecId { get; set; }
        public int EmpSysId { get; set; }
        public int SftId { get; set; }
        public DateTime EftDate { get; set; }
        [NotMapped]
        public DateTime Eft_EDate { get; set; }
        public int CndrAtndc { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }       

        [NotMapped]
        public string FullName { get; set; }

        [NotMapped]
        public string Day { get; set; }

        [NotMapped]
        public string ShiftName { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
        [JsonIgnore]
        public virtual HrWkShift Sft { get; set; }
    }
}
