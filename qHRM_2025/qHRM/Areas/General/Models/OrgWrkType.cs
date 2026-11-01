using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.General.Models
{
    public partial class OrgWrkType
    {
        public OrgWrkType()
        {
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
        }

        public int WrktypId { get; set; }

        [Display(Name = "Work Type Code")]
        [Required(ErrorMessage = "Please Enter Work Type Code")]
        public string WrktypCode { get; set; }

        [Display(Name = "Work Type")]
        [Required(ErrorMessage = "Please Enter Work Type Name")]
        public string NameEng { get; set; }

        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string Desc { get; set; }

        public int EntId { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
    }
}
