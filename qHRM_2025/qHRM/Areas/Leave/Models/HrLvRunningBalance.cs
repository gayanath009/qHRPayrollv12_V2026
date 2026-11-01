using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Leave.Models
{
    [Table("HR_LvRunningBalance")]
    public partial class HrLvRunningBalance
    {
        [Key]
        [Column("RunBalId")]
        public int RunBalId { get; set; }

        [Column("EmpSysId")]
        public int EmpSysId { get; set; }

        [Column("LvTypId")]
        public int LvTypId { get; set; }

        [Column("LvYear")]
        public int LvYear { get; set; }

        [Column("EntitledDays", TypeName = "decimal(5, 2)")]
        public decimal EntitledDays { get; set; }

        [Column("UsedDays", TypeName = "decimal(5, 2)")]
        public decimal UsedDays { get; set; }

        [Column("BalanceDays", TypeName = "decimal(5, 2)")]
        public decimal BalanceDays { get; set; }

        [Column("LastUpdated")]
        public DateTime LastUpdated { get; set; }

        [ForeignKey("EmpSysId")]
        public virtual HrEmpGenInfo EmpGenInfo { get; set; }

        [ForeignKey("LvTypId")]
        public virtual HrLvType LvType { get; set; }
    }
}
