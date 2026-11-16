
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAttdView
    {
        [Key]
        public int RecId { get; set; }

        [Display(Name = "Effective Date")]
        [DisplayFormat(DataFormatString = "{0:dd MM yyyy}")]
        public DateTime EfctvDate { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Full Name")]
        [NotMapped]
        public string FullName { get; set; }

        [Display(Name = "Shift")]
        public int SftId { get; set; }

        [Display(Name = "In Time")]
        [DataType(DataType.Time)]
        public string InTime { get; set; }
        public DateTime? EfctvInTime { get; set; }
        public string InSource { get; set; }

        [Display(Name = "Out Time")]
        [DataType(DataType.Time)]
        public string OutTime { get; set; }
        public DateTime? EfctvOutTime { get; set; }
       
        public bool OvrNghtStat { get; set; }
        public int? CrtId { get; set; }
        public string AtdRmks { get; set; }

        public decimal Ot_Amt { get; set; }

        public decimal No_Pay_Amt { get; set; }

        public int AbsntStat { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Display(Name = "To Date")]        
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Display(Name = "Remarks")]
        [StringLength(300)]
        public string Note { get; set; }


    }
}

