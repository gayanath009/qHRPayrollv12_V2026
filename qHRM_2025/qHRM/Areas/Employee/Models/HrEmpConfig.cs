using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpConfig
    {
        public HrEmpConfig()
        {
            HrEmpCnfNtfLvl = new HashSet<HrEmpCnfNtfLvl>();
        }

        public int EmpCnfId { get; set; }

        [Display(Name = "Default Notice Period for Probation Staff")]
        public int DftPrbNtf { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrEmpCnfNtfLvl> HrEmpCnfNtfLvl { get; set; }
    }
}
