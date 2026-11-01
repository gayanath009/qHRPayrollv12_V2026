using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrOvrtmNoPayView
    {
        [Key]
        public int RecId { get; set; }                       
        public string EfctvDate { get; set; }
        public int EmpSysId { get; set; }        
        public string FullName { get; set; }       
        public decimal Ot_Amt { get; set; }
        public string OT_Hrs { get; set; }
        public string Ext_OT_Hrs { get; set; }
        public decimal No_Pay_Amt { get; set; }
    }
}
