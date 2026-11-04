using System;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrAirTicketPriceVw
    {
        public int TktPriceId { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Please select a country")]
        public int CntryId { get; set; }

        [Display(Name = "Country Name")]
        public string CntryName { get; set; }

        [Display(Name = "Ticket Price")]
        [Required(ErrorMessage = "Price is required")]
        [Range(0.00, 999999.00)]
        public decimal Price { get; set; }

        [Display(Name = "Effective Year")]
        public int? EffectiveYear { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }

        public short RecStat { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }
    }
}

