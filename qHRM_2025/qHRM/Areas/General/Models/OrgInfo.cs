using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.General.Models
{
    public partial class OrgInfo
    {
        public OrgInfo()
        {
            OrgEntity = new HashSet<OrgEntity>();
        }

        public int OrgId { get; set; }
        
        [Display(Name = "Org Code")]
        [Required(ErrorMessage = "Please enter Org Code")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgInfo", ErrorMessage = "Org Code is already existing", AdditionalFields = "exOrgCode")]
        public string OrgCode { get; set; }
        
        [NotMapped]
        public string exOrgCode { get; set; }
        [Display(Name = "Org Name")]
        [Required(ErrorMessage = "Please enter Org Name")]
        
        public string NameEng { get; set; }
        
        [Display(Name = "Org Name Arb")]
        public string NameArb { get; set; }
        [Display(Name = "Description")]
        public string Desc { get; set; }
        public short RecStat { get; set; }
        public byte[] OrgLogo1 { get; set; }
        public byte[] OrgLogo2 { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual ICollection<OrgEntity> OrgEntity { get; set; }
    }
}
