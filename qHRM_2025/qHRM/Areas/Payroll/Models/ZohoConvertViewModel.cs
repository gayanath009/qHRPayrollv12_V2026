using System;
using System.Collections.Generic;

namespace qHRM.Areas.Payroll.Models
{
    public class ZohoConvertViewModel
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public int Month { get; set; } = DateTime.Today.Month;
        public string FileName { get; set; }
        public int TotalCount { get; set; }
        public int InHouseCount { get; set; }
        public int OutsourcedCount { get; set; }
        public decimal TotalBasic { get; set; }
        public decimal TotalFixedSalary { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal InternalStaffNet { get; set; }
        public decimal OutsourcedStaffNet { get; set; }
        public decimal TotalNetSalary { get; set; }
        public List<PrSlryShtVw> Employees { get; set; } = new List<PrSlryShtVw>();
    }
}
