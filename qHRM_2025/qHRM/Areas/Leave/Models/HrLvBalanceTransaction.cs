using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Leave.Models
{
    [Table("HR_LvBalanceTransaction")]
    public partial class HrLvBalanceTransaction
    {
        [Key]
        [Column("TrnId")]
        public int TrnId { get; set; }

        [Column("EmpSysId")]
        public int EmpSysId { get; set; }

        [Column("LvTypId")]
        public int LvTypId { get; set; }

        [Column("LvYear")]
        public int LvYear { get; set; }

        [Column("TrnType")]
        [Required]
        [StringLength(50)]
        public string TrnType { get; set; } // 'Opening Balance', 'Leave Approved', 'Leave Cancelled', 'Leave Reversed', 'Manual Adjustment'

        [Column("LvId")]
        public int? LvId { get; set; }

        [Column("Days", TypeName = "decimal(5, 2)")]
        public decimal Days { get; set; } // (+) for credit, (-) for debit

        [Column("BalanceBefore", TypeName = "decimal(5, 2)")]
        public decimal BalanceBefore { get; set; }

        [Column("BalanceAfter", TypeName = "decimal(5, 2)")]
        public decimal BalanceAfter { get; set; }

        [Column("Remarks")]
        [StringLength(250)]
        public string Remarks { get; set; }

        [Column("CrtId")]
        public int CrtId { get; set; }

        [Column("CrtDateTime")]
        public DateTime CrtDateTime { get; set; }

        [ForeignKey("EmpSysId")]
        public virtual HrEmpGenInfo EmpGenInfo { get; set; }

        [ForeignKey("LvTypId")]
        public virtual HrLvType LvType { get; set; }

        [ForeignKey("LvId")]
        public virtual HrLvRegister LvRegister { get; set; }
    }
}
