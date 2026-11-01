using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.General.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SystemLog.Models;
using qHRM.Areas.WorkShift.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpGenInfo_temp
    {
        public HrEmpGenInfo_temp()
        {
            HrEmpAttendance = new HashSet<HrEmpAttendance>();
            HrEmpBankAcct = new HashSet<HrEmpBankAcct>();
            HrEmpDependent = new HashSet<HrEmpDependent>();
            HrEmpDocument = new HashSet<HrEmpDocument>();
            HrEmpEduQualif = new HashSet<HrEmpEduQualif>();
            HrEmpJobAssign = new HashSet<HrEmpJobAssign>();
            HrEmpPhoto = new HashSet<HrEmpPhoto>();
            HrEmpPreExp = new HashSet<HrEmpPreExp>();
            HrEmpProbation = new HashSet<HrEmpProbation>();
            HrEmpResidInfo = new HashSet<HrEmpResidInfo>();
            HrEmpSponsor = new HashSet<HrEmpSponsor>();
            HrEmpWkRoster = new HashSet<HrEmpWkRoster>();
            HrLvAnlSchedule = new HashSet<HrLvAnlSchedule>();
            HrLvRegister = new HashSet<HrLvRegister>();
            HrPrAdHocTrns = new HashSet<HrPrAdHocTrns>();
            HrPrEmpLoan = new HashSet<HrPrEmpLoan>();
            HrPrEmpSlryStruc = new HashSet<HrPrEmpSlryStruc>();
            LogHremp = new HashSet<LogHremp>();
            LogHrlv = new HashSet<LogHrlv>();
            LogHrpr = new HashSet<LogHrpr>();
            LogOrg = new HashSet<LogOrg>();
        }

        public int EmpSysId { get; set; }

        [NotMapped]
        public int SNo { get; set; }

        [Display(Name = "Employee ID")]
        [Remote(action: "IsExistEmpID", controller: "HrEmpGenInfo", ErrorMessage = "Employee ID is already existing", AdditionalFields = "ex_EmpId")]
        [Required(ErrorMessage = "Please specify Employee ID")]
        [StringLength(10)]
        public string EmpId { get; set; }

        [NotMapped]
        public string ex_EmpId { get; set; }

        public string AdId { get; set; }
        public string Password { get; set; }

        [Display(Name = "Joined Date")]
        [Required(ErrorMessage = "Please specify Join Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateJoinedDate", controller: "HrEmpGenInfo", ErrorMessage = "Invalid Joined Date")]
        public DateTime JoinedDate { get; set; }

        [Display(Name = "Employeement Type")]
        [Required(ErrorMessage = "Please specify Employeement Type")]
        public string EmpType { get; set; }

        [Display(Name = "Office Location")]
        [Required(ErrorMessage = "Please specify office Location")]
        public int LocId { get; set; }

        [Display(Name = "Grade")]
        [Required(ErrorMessage = "Please specify Grade")]
        public int GrdId { get; set; }

        [Display(Name = "Group")]
        [Required(ErrorMessage = "Please specify Group")]
        public int? GrpId { get; set; }

        [Display(Name = "Air Ticket Eligibility")]
        [Required(ErrorMessage = "Please specify Air Ticket Eligibility")]
        public string TcktTyp { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "Please specify First Name")]
        [StringLength(30)]
        public string FirstName { get; set; }
        
        [Display(Name = "Middle Name")]
        [StringLength(30)]
        public string MdlName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Please specify Last Name")]
        [StringLength(30)]
        public string LastName { get; set; }

        [NotMapped]
        public string FullName { get { return String.Concat(FirstName, " ", LastName); } }

        [Display(Name = "Name as in Passport")]
        [Required(ErrorMessage = "Please specify Name as in Passport")]
        [StringLength(150)]
        public string PstName { get; set; }

        [Display(Name = "Name in Arabic")]
        [StringLength(150)]
        public string NameArb { get; set; }

        [Display(Name = "Title")]
        [Required(ErrorMessage = "Please specify Title")]
        public string Title { get; set; }

        [Display(Name = "Date of Birth")]
        [Required(ErrorMessage = "Please specify Date of Birth")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateDOB", controller: "HrEmpGenInfo", ErrorMessage = "Invalid Birth Date")]
        public DateTime Dob { get; set; }

        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Please specify Gender")]
        public string Gender { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Please specify Country")]
        public int CntryId { get; set; }

        [Display(Name = "Nationality")]        
        [Required(ErrorMessage = "Please specify Nationality")]
        public int NltyId { get; set; }

        [Display(Name = "Permanent Address")]
        [StringLength(200)]
        public string PmtAddr { get; set; }

        [Display(Name = "Building No")]
        [StringLength(10)]
        public string BldNo { get; set; }

        [Display(Name = "Street No & Name State")]
        [StringLength(50)]
        public string SrtName { get; set; }

        [Display(Name = "City")]
        [StringLength(10)]
        public string City { get; set; }

        [Display(Name = "Address Country")]
        public int? AddrCntryId { get; set; }

        [Display(Name = "Region")]
        [StringLength(10)]
        public string Regoin { get; set; }

        [Display(Name = "Postal Code")]
        [StringLength(10)]
        public string PstlCode { get; set; }

        [Display(Name = "Marital Status")]
        [Required(ErrorMessage = "Please specify Marital Status")]
        public string MrtlStat { get; set; }

        [Display(Name = "Home Phone")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [StringLength(20)]
        public string HmPhone { get; set; }

        [Display(Name = "Mobile Phone")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [StringLength(20)]
        public string MblPhone { get; set; }

        [Display(Name = "Personal Email")]
        [DataType(DataType.EmailAddress)]
        [StringLength(50)]
        [EmailAddress]
        public string PsnEmail { get; set; }

        [Display(Name = "Blood Group")]
        public string BldGrp { get; set; }

        [Display(Name = "Permit Type")]
        [Required(ErrorMessage = "Please specify Permit Type")]
        public string PmtTyp { get; set; }

        [Display(Name = "QID / Visa No")]
        [Required(ErrorMessage = "Please specify QID / Visa No")]
        [Remote(action: "IsExistQID", controller: "HrEmpGenInfo", ErrorMessage = "QID / Visa No is already existing", AdditionalFields = "ex_PmtId")]
        [StringLength(20)]
        public string PmtId { get; set; }

        [NotMapped]
        public string ex_PmtId { get; set; }

        [Required(ErrorMessage = "Please specify QID / Visa Expiry Date")]
        [Display(Name = "QID/Visa Expiry Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidatePmtExpDate", controller: "HrEmpGenInfo", ErrorMessage = "Invalid QID / Visa Expiry Date")]
        public DateTime PmtExpDate { get; set; }

        [Display(Name = "Passport Number")]
        [StringLength(15)]
        public string PstNo { get; set; }

        [Display(Name = "Passport Expiry")]
        [DataType(DataType.Date)]
        public DateTime? PstExpDate { get; set; }

        [Display(Name = "Passport Issued Country")]
        public int? PstCntryId { get; set; }

        [Display(Name = "Driving License Number")]
        public string DrvLcnNo { get; set; }

        [Display(Name = "Driving License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? DrvLcnExpDate { get; set; }

        [Display(Name = "Health Card Number")]
        [StringLength(15)]
        public string HlthCrdNo { get; set; }

        [Display(Name = "Health Card Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? HlthCrdExpDate { get; set; }

        [Display(Name = "Emergency Contact Number 1")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [Required(ErrorMessage = "Please specify Emergency Contact Number (1)")]
        [StringLength(15)]
        public string EmgCnt1 { get; set; }

        [Display(Name = "Emergency Contact Number 2")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [Required(ErrorMessage = "Please specify Emergency Contact Number (2)")]
        [StringLength(15)]
        public string EmgCnt2 { get; set; }

        [Display(Name = "Emergency Contact Number 3")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please specify valid Contact Number")]
        [StringLength(15)]
        public string EmgCnt3 { get; set; }

        [Display(Name = "Emergency Contact Email")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [StringLength(30)]
        public string EmgCntEmail { get; set; }

        [Display(Name = "Emergency Contact Relationship")]
        [Required(ErrorMessage = "Please specify Emergency Contact Relationship")]
        [StringLength(30)]
        public string EmgCntRelation { get; set; }

        [Display(Name = "Note")]
        [StringLength(300)]
        public string Note { get; set; }

        [Display(Name = "Current Status")]
        public short CurrStat { get; set; }

        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public virtual OrgCountry AddrCntry { get; set; }
        [NotMapped]
        public virtual OrgCountry Cntry { get; set; }
        [NotMapped]
        public virtual HrEmpGrade Grd { get; set; }
        [NotMapped]
        public virtual HrEmpGroup Grp { get; set; }
        [NotMapped]
        public virtual OrgLocation Loc { get; set; }
        [NotMapped]
        public virtual HrLvGrpPolicy LvPlcy { get; set; }
        [NotMapped]
        public virtual OrgNationality Nlty { get; set; }
        [NotMapped]
        public virtual OrgCountry PstCntry { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpAttendance> HrEmpAttendance { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpBankAcct> HrEmpBankAcct { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpDependent> HrEmpDependent { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpDocument> HrEmpDocument { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpEduQualif> HrEmpEduQualif { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpJobAssign> HrEmpJobAssign { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpPhoto> HrEmpPhoto { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpPreExp> HrEmpPreExp { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpProbation> HrEmpProbation { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpResidInfo> HrEmpResidInfo { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpSponsor> HrEmpSponsor { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrEmpWkRoster> HrEmpWkRoster { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvAnlSchedule> HrLvAnlSchedule { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrLvRegister> HrLvRegister { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrAdHocTrns> HrPrAdHocTrns { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrEmpLoan> HrPrEmpLoan { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrPrEmpSlryStruc> HrPrEmpSlryStruc { get; set; }
        [JsonIgnore]
        public virtual ICollection<LogHremp> LogHremp { get; set; }
        [JsonIgnore]
        public virtual ICollection<LogHrlv> LogHrlv { get; set; }
        [JsonIgnore]
        public virtual ICollection<LogHrpr> LogHrpr { get; set; }
        [JsonIgnore]
        public virtual ICollection<LogOrg> LogOrg { get; set; }
    }
}
