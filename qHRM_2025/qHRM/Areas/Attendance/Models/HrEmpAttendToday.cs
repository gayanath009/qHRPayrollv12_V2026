using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Attendance.Models
{
    public partial class HrEmpAttendToday
    {
        //---------------------------------------//        
        [Key]
        public int RecId { get; set; }
        public string Status { get; set; }        
        public int Count { get; set; }        
        //---------------------------------------//
    }
}
