using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.General.Models
{
    public partial class OrgCountry
    {
        public OrgCountry()
        {
            HrEmpDependent = new HashSet<HrEmpDependent>();
            HrEmpGenInfoAddrCntry = new HashSet<HrEmpGenInfo>();
            HrEmpGenInfoCntry = new HashSet<HrEmpGenInfo>();
            HrEmpGenInfoPstCntry = new HashSet<HrEmpGenInfo>();
            HrEmpResidInfo = new HashSet<HrEmpResidInfo>();
            OrgNationality = new HashSet<OrgNationality>();
        }

        [Key]
        public int CntryId { get; set; }

        [Display(Name = "Country Name")]
        [Required(ErrorMessage = "Please Enter Country Name")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgCountry", ErrorMessage = "Country Name is already existing", AdditionalFields = "ex_CntryNameEng")]
        public string CntryNameEng { get; set; }

        [NotMapped]
        public string ex_CntryNameEng { get; set; }

        public string CntryNameArb { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpDependent> HrEmpDependent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfoAddrCntry { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfoCntry { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfoPstCntry { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpResidInfo> HrEmpResidInfo { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrgNationality> OrgNationality { get; set; }
    }
}
