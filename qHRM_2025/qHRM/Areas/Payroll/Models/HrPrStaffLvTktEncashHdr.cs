using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    [Table("HR_PrStaffLvTktEncashHdr")]
    public partial class HrPrStaffLvTktEncashHdr
    {
        [Key]
        [Column("EncashHdr_Id")]
        public int EncashHdrId { get; set; }

        [Column("Prc_Year")]
        public int PrcYear { get; set; }

        [Column("Prc_Month")]
        public int PrcMonth { get; set; }

        [Column("Staff_Category")]
        [StringLength(20)]
        public string StaffCategory { get; set; } = "ALL";

        [Column("Prc_Stat")]
        public short PrcStat { get; set; } = 1; // 1 = Draft / Calculated, 2 = Verified, 3 = Approved / Confirmed

        [Column("Wf_Stat")]
        [StringLength(30)]
        public string WfStat { get; set; } = "Draft"; // Draft, Verified, PendingLevel1, PendingFinal, Approved, Rejected

        [Column("Wf_Inst_Id")]
        public Guid? WfInstId { get; set; }

        [Column("Total_Internal_Leave", TypeName = "decimal(18, 2)")]
        public decimal TotalInternalLeave { get; set; }

        [Column("Total_Internal_Ticket", TypeName = "decimal(18, 2)")]
        public decimal TotalInternalTicket { get; set; }

        [Column("Total_Internal_Settlement", TypeName = "decimal(18, 2)")]
        public decimal TotalInternalSettlement { get; set; }

        [Column("Total_Outsource_Leave", TypeName = "decimal(18, 2)")]
        public decimal TotalOutsourceLeave { get; set; }

        [Column("Total_Outsource_Ticket", TypeName = "decimal(18, 2)")]
        public decimal TotalOutsourceTicket { get; set; }

        [Column("Total_Outsource_Settlement", TypeName = "decimal(18, 2)")]
        public decimal TotalOutsourceSettlement { get; set; }

        [Column("Grand_Total_Settlement", TypeName = "decimal(18, 2)")]
        public decimal GrandTotalSettlement { get; set; }

        [Column("Init_By")]
        public int InitBy { get; set; } = 1;

        [Column("Init_DateTime")]
        public DateTime InitDateTime { get; set; } = DateTime.Now;

        [Column("Vrfd_By")]
        public int? VrfdBy { get; set; }

        [Column("Vrfd_DateTime")]
        public DateTime? VrfdDateTime { get; set; }

        [Column("Wf_Submitted_By")]
        public int? WfSubmittedBy { get; set; }

        [Column("Wf_Submitted_DateTime")]
        public DateTime? WfSubmittedDateTime { get; set; }

        [Column("Wf_Approved_By")]
        public int? WfApprovedBy { get; set; }

        [Column("Wf_Approved_DateTime")]
        public DateTime? WfApprovedDateTime { get; set; }

        [Column("Wf_Comments")]
        public string WfComments { get; set; }

        [Column("Rec_Stat")]
        public short RecStat { get; set; } = 1;
    }
}
