using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Areas.Travel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpDependent
    {
        public HrEmpDependent()
        {
            HrEmpDpndDoc = new HashSet<HrEmpDpndDoc>();
            HrTrvlTktDtl = new HashSet<HrTrvlTktDtl>();
        }

        public int DpndId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Relationship")]
        [Required(ErrorMessage = "Please specify the Relationship")]
        public string RltnTyp { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "Please specify First Name")]
        [StringLength(30)]        
        public string FirstName { get; set; }

        [Display(Name = "Middle Name")]
        public string MdlName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Please specify Last Name")]
        [StringLength(30)]
        public string LastName { get; set; }

    
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
        [Remote(action: "ValidateDOB", controller: "HrEmpDependent", ErrorMessage = "Invalid Date of Birth")]
        public DateTime Dob { get; set; }

        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Please specify Gender")]
        public string Gender { get; set; }

        [Display(Name = "Nationality")]
        [Required(ErrorMessage = "Please specify Nationality")]
        public int NltyId { get; set; }

        [Display(Name = "Photo")]
        public byte[] Photo { get; set; }
        public string PhotoName { get; set; }

        [NotMapped]
        public string exPhotoName { get; set; }
        
        public string PhotoMime { get; set; }

        [NotMapped]
        public string exPhotoMime { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Please specify Country")]
        public int? CntryId { get; set; }
        
        [Display(Name = "Mobile")]
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

        [Display(Name = "Resident Permit Type")]
        [Required(ErrorMessage = "Please specify Resident Permit Type")]
        public string PmtTyp { get; set; }

        
        public string PmtId { get; set; }
        [Display(Name = "QID/Visa Numbers")]
        [Remote(action: "IsExistPmtId", controller: "HrEmpDependent", ErrorMessage = "QID is already existing", AdditionalFields = "ex_PmtId")]
        [Required(ErrorMessage = "Please specify QID/Visa")]

        [NotMapped]
        public string exPmtId { get; set; }

        [Display(Name = "QID/Visa Expiry Date")]
        [Required(ErrorMessage = "Please specify QID/Visa Expiry Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidatePmtExpDate", controller: "HrEmpDependent", ErrorMessage = "Invalid QID/Visa Expiry Date")]
        public DateTime PmtExpDate { get; set; }

        [Display(Name = "Passport Number")]
        [Remote(action: "IsExistPstNo", controller: "HrEmpDependent", ErrorMessage = "Passport Number is already existing", AdditionalFields = "ex_PstNo")]
        [StringLength(15)]
        public string PstNo { get; set; }

        [NotMapped]
        public string exPstNo { get; set; }

        [Display(Name = "Passport Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? PstExpDate { get; set; }

        [Display(Name = "Passport Country")]
        public int? PstCntryId { get; set; }

        [Display(Name = "Occupation")]
        [Required(ErrorMessage = "Please specify Occupation")]
        public string Occupation { get; set; }

        [Display(Name = "College/Company Details")]
        [StringLength(300)]
        public string Institute { get; set; }

        [Display(Name = "Note")]
        [StringLength(500)]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        
        public virtual OrgCountry Cntry { get; set; }
        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual OrgNationality Nlty { get; set; }

        [JsonIgnore]
        public virtual ICollection<HrEmpDpndDoc> HrEmpDpndDoc { get; set; }
        public virtual ICollection<HrTrvlTktDtl> HrTrvlTktDtl { get; set; }
    }
}
