using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.General.Models
{
    public partial class OrgNationality
    {
        public OrgNationality()
        {
            HrEmpDependent = new HashSet<HrEmpDependent>();
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
            OrgNltyEmpRatio = new HashSet<OrgNltyEmpRatio>();
        }

        public int NltyId { get; set; }

        [Display(Name = "Nationality Group")]
        public int NltyTypId { get; set; }

        [Display(Name = "Nationality")]
        [Required(ErrorMessage = "Please Enter Nationality Name")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgNationality", ErrorMessage = "Nationality Name is already existing", AdditionalFields = "ex_NltyNameEng")]
        public string NltyNameEng { get; set; }

        [NotMapped]
        public string ex_NltyNameEng { get; set; }

        public string NltyNameArb { get; set; }

        [Required(ErrorMessage = "Please Select Country")]
        public int CntryId { get; set; }

        [Display(Name = "Country")]
        [NotMapped]
        public string ex_CntryNameEng { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual OrgCountry Cntry { get; set; }
        public virtual OrgNltyType NltyTyp { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpDependent> HrEmpDependent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
        public virtual ICollection<OrgNltyEmpRatio> OrgNltyEmpRatio { get; set; }
    }
}
