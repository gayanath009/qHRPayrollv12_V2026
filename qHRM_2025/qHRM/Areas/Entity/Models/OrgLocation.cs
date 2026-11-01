using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Entity.Models
{
    public partial class OrgLocation
    {
        public OrgLocation()
        {
            HrEmpGenInfo = new HashSet<HrEmpGenInfo>();
            InverseParent = new HashSet<OrgLocation>();
        }

        [Key]
        public int LocId { get; set; }

        [Display(Name = "Location Code")]
        [Required(ErrorMessage = "Please enter an Unique Location Code")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgLocation", ErrorMessage = "Location Code is already existing", AdditionalFields = "ex_LocCode")]
        public string LocCode { get; set; }

        [NotMapped]
        public string ex_LocCode { get; set; }

        [Display(Name = "Location Name")]
        [MaxLength(30)]
        [Required(ErrorMessage = "Enter Location Name")]
        public string NameEng { get; set; }

        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string DescEng { get; set; }
        public string DescArb { get; set; }

        [Display(Name = "Parent Location")]
        public int? ParentId { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual OrgEntity Ent { get; set; }
        [JsonIgnore]
        public virtual OrgLocation Parent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGenInfo> HrEmpGenInfo { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual ICollection<OrgLocation> InverseParent { get; set; }
    }
}
