using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpCnfNtfLvl
    {
        public int NtfLvlId { get; set; }

        [Display(Name = "Note")]
        public string Desc { get; set; }

        [Display(Name = "Min No of Years")]
        [Required(ErrorMessage = "Min no of years cannot be blank")]
        public int LwrBndryYrs { get; set; }

        [Display(Name = "Max No of Years")]
        [Required(ErrorMessage = "Max no of years cannot be blank")]
        public int UprBndryYrs { get; set; }

        [Display(Name = "Minimum Notice Period (in Days)")]
        [Required(ErrorMessage = "Max no of years cannot be blank")]
        public int EftDays { get; set; }
        public int EmpCnfId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpConfig EmpCnf { get; set; }
    }
}
