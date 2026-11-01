using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using qHRM.Areas.General.Models;

namespace qHRM.Areas.Leave.Models
{
    /// <summary>
    /// Represents the company Leave Entitlement and Compensation Policy associated with HR_LvType.
    /// Defines eligibility thresholds (applicable after days from date of joining), total entitlement days,
    /// full paid, half paid, and unpaid / no-pay days.
    /// </summary>
    public partial class HrLvPolicy
    {
        [Key]
        [Display(Name = "Policy ID")]
        public int PlcyId { get; set; }

        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Please select Leave Type")]
        public int LvTypId { get; set; }

        [Display(Name = "Description")]
        [StringLength(200)]
        public string Desc { get; set; }

        [Display(Name = "Applicable After (Days from Joining)")]
        [Range(0, 3650, ErrorMessage = "Days must be between 0 and 3650")]
        public int AppAfterDays { get; set; }

        [Display(Name = "Eligibility Days")]
        [Range(0, 365, ErrorMessage = "Eligibility days must be between 0 and 365")]
        public decimal ElgDays { get; set; }

        [Display(Name = "Full Paid Days")]
        [Range(0, 365, ErrorMessage = "Full paid days must be between 0 and 365")]
        public decimal FullPaidDays { get; set; }

        [Display(Name = "Half Paid Days")]
        [Range(0, 365, ErrorMessage = "Half paid days must be between 0 and 365")]
        public decimal HalfPaidDays { get; set; }

        [Display(Name = "No Pay Days")]
        [Range(0, 365, ErrorMessage = "No pay days must be between 0 and 365")]
        public decimal NoPayDays { get; set; }

        [Display(Name = "Is No Pay Only")]
        public bool IsNoPay { get; set; }

        [Display(Name = "Entitlement Period Unit")]
        [StringLength(20)]
        public string PrdUnit { get; set; } = "YEAR"; // "YEAR", "SERVICE_LIFETIME", etc.

        public int EntId { get; set; } = 1;
        public short RecStat { get; set; } = 1;
        public DateTime CrtDateTime { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string CrtBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModDateTime { get; set; }

        [Display(Name = "Modified By")]
        [StringLength(50)]
        public string ModBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(LvTypId))]
        [InverseProperty(nameof(HrLvType.HrLvPolicy))]
        public virtual HrLvType LvTyp { get; set; }

        [NotMapped]
        public virtual OrgEntity Ent { get; set; }
    }
}
