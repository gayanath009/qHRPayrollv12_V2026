using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAttnSummary
    {
        [Key]
        public int RecId { get; set; }        
        public int EmpSysId { get; set; }        
        public string FullName { get; set; }        
        public string Status { get; set; }
        public int Count { get; set; }
        public string Pcentge { get; set; }
    }
}
