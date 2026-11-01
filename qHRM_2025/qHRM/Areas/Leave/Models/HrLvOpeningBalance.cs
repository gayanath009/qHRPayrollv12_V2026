using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Leave.Models
{
    [Table("HR_LvOpeningBalance")]
    public partial class HrLvOpeningBalance
    {
        [Key]
        [Column("OpnBalId")]
        public int OpnBalId { get; set; }

        [Column("EmpSysId")]
        public int EmpSysId { get; set; }

        [Column("LvTypId")]
        public int LvTypId { get; set; }

        [Column("LvYear")]
        public int LvYear { get; set; }

        [Column("OpeningDays", TypeName = "decimal(5, 2)")]
        public decimal OpeningDays { get; set; }

        [Column("BookedDays", TypeName = "decimal(5, 2)")]
        public decimal BookedDays { get; set; }

        [Column("SnapshotDate")]
        public DateTime? SnapshotDate { get; set; }

        [Column("SrcRef")]
        [StringLength(100)]
        public string SrcRef { get; set; }

        [Column("Remarks")]
        [StringLength(250)]
        public string Remarks { get; set; }

        [Column("CrtId")]
        public int CrtId { get; set; }

        [Column("CrtDateTime")]
        public DateTime CrtDateTime { get; set; }

        [Column("MdfId")]
        public int? MdfId { get; set; }

        [Column("MdfDateTime")]
        public DateTime? MdfDateTime { get; set; }

        [ForeignKey("EmpSysId")]
        public virtual HrEmpGenInfo EmpGenInfo { get; set; }

        [ForeignKey("LvTypId")]
        public virtual HrLvType LvType { get; set; }
    }
}
