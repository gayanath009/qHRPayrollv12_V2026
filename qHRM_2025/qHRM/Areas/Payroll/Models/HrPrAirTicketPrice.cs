using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.General.Models;

namespace qHRM.Areas.Payroll.Models
{
    [Table("HR_PrAirTicketPrice")]
    public partial class HrPrAirTicketPrice
    {
        [Key]
        [Column("Tkt_Price_Id")]
        public int TktPriceId { get; set; }

        [Column("Cntry_Id")]
        [Display(Name = "Country")]
        [Required(ErrorMessage = "Please select a country")]
        public int CntryId { get; set; }

        [Column("Price", TypeName = "decimal(12, 2)")]
        [Display(Name = "Ticket Price")]
        [Range(0.00, 999999.00, ErrorMessage = "Price must be a valid positive amount")]
        [Required(ErrorMessage = "Ticket Price is required")]
        public decimal Price { get; set; }

        [Column("Effective_Year")]
        [Display(Name = "Effective Year")]
        public int? EffectiveYear { get; set; }

        [Column("Note")]
        [StringLength(250)]
        public string Note { get; set; }

        [Column("Rec_Stat")]
        public short RecStat { get; set; } = 1;

        [Column("Crt_By")]
        public int CrtBy { get; set; } = 1;

        [Column("Crt_DateTime")]
        public DateTime CrtDateTime { get; set; } = DateTime.Now;

        [Column("Mod_By")]
        public int? ModBy { get; set; }

        [Column("Mod_DateTime")]
        public DateTime? ModDateTime { get; set; }

        [ForeignKey("CntryId")]
        public virtual OrgCountry Cntry { get; set; }
    }
}

