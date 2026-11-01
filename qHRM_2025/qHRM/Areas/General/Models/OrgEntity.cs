using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.WorkShift.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.General.Models
{
    public partial class OrgEntity
    {
        public OrgEntity()
        {
            AdmEntityUser = new HashSet<AdmEntityUser>();
            HrEmpConfig = new HashSet<HrEmpConfig>();
            HrEmpGrade = new HashSet<HrEmpGrade>();
            HrEmpGroup = new HashSet<HrEmpGroup>();
            HrLvType = new HashSet<HrLvType>();
            HrPrAdHocTrnType = new HashSet<HrPrAdHocTrnType>();
            HrPrConfig = new HashSet<HrPrConfig>();
            HrPrOtrate = new HashSet<HrPrOtrate>();
            HrPrSlryPrcHdr = new HashSet<HrPrSlryPrcHdr>();
            HrPrStfLoanType = new HashSet<HrPrStfLoanType>();
            HrWkRoster = new HashSet<HrWkRoster>();
            HrWkRosterShift = new HashSet<HrWkRosterShift>();
            HrWkShift = new HashSet<HrWkShift>();
            OrgDepartment = new HashSet<OrgDepartment>();
            OrgDesignation = new HashSet<OrgDesignation>();
            OrgEmpDocType = new HashSet<OrgEmpDocType>();
            OrgLocation = new HashSet<OrgLocation>();
            OrgNltyEmpRatio = new HashSet<OrgNltyEmpRatio>();
        }

        [Key]
        public int EntId { get; set; }

        [Display(Name = "Entity Code")]
        [Required(ErrorMessage = "Please enter Entity Unique Code")]
        [Remote(action: "IsAlreadyExisting", controller: "OrgEntity", ErrorMessage = "Entity Code is already existing", AdditionalFields = "ex_EntCode")]
        public string EntCode { get; set; }
        [NotMapped]
        public string ex_EntCode { get; set; }

        [Display(Name = "Entity Name")]
        [Required(ErrorMessage = "Please enter Entity Name")]
        public string NameEng { get; set; }

        [Display(Name = "Entity Name (Arabic)")]
        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string Desc { get; set; }

        [Display(Name = "Organization")]
        public int OrgId { get; set; }

        [NotMapped]
        public string ex_OrgNameEng { get; set; }
        [NotMapped]
        public string ex_OrgNameArb { get; set; }

        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual OrgInfo Org { get; set; }

        [JsonIgnore]
        public virtual ICollection<AdmEntityUser> AdmEntityUser { get; set; }
        public virtual ICollection<HrEmpConfig> HrEmpConfig { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGrade> HrEmpGrade { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpGroup> HrEmpGroup { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvType> HrLvType { get; set; }
        public virtual ICollection<HrPrAdHocTrnType> HrPrAdHocTrnType { get; set; }
        public virtual ICollection<HrPrConfig> HrPrConfig { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrOtrate> HrPrOtrate { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrSlryPrcHdr> HrPrSlryPrcHdr { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrStfLoanType> HrPrStfLoanType { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrWkRoster> HrWkRoster { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrWkRosterShift> HrWkRosterShift { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrWkShift> HrWkShift { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrgDepartment> OrgDepartment { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrgDesignation> OrgDesignation { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrgEmpDocType> OrgEmpDocType { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrgLocation> OrgLocation { get; set; }
        public virtual ICollection<OrgNltyEmpRatio> OrgNltyEmpRatio { get; set; }
    }
}
