using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrCnfGrtvtyLvl
    {
        public int GrtLvlId { get; set; }

        [Display(Name = "Note")]
        public string Desc { get; set; }

        [Display(Name = "Min No of Years (>)")]
        public int LwrBndryYrs { get; set; }

        [Display(Name = "Max No of Years (<=)")]
        [Remote(action: "ValidateUprBndryYrs", controller: "HrPrConfig", ErrorMessage = "Should be grater than minimum years.", AdditionalFields = "LwrBndryYrs")]
        public int UprBndryYrs { get; set; }

        [Display(Name = "Eligible Days")]
        [Range(1, 42, ErrorMessage = "Out of range (1 -42)")]
        public int EftDays { get; set; }
        public int PrCnfId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual HrPrConfig PrCnf { get; set; }
    }
}
