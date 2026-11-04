using System;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrStaffLvTktEncashVw
    {
        public int EncashId { get; set; }
        public int PrcYear { get; set; }
        public int PrcMonth { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Employee ID")]
        public string EmpId { get; set; }

        [Display(Name = "Courtesy Title")]
        public string CourtesyTitle { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Display(Name = "Category")]
        public string StaffCategory { get; set; } // 'INTERNAL' or 'OUTSOURCE'

        [Display(Name = "Joined Date")]
        public DateTime JoinedDate { get; set; }
        public string JoinedDateStr => JoinedDate.ToString("yyyy-MM-dd");

        [Display(Name = "Join Date after Annual Vacation")]
        public DateTime? VacationReturnDate { get; set; }
        public string VacationReturnDateStr => VacationReturnDate.HasValue ? VacationReturnDate.Value.ToString("dd/MM/yyyy") : "-";

        [Display(Name = "Nationality")]
        public string Nationality { get; set; }

        [Display(Name = "Next Leave Reset Date")]
        public DateTime NextResetDate { get; set; }
        public string NextResetDateStr => NextResetDate.ToString("yyyy-MM-dd");

        [Display(Name = "Applicable Days")]
        public int ApplicableDays { get; set; }

        [Display(Name = "Eligible Leave Days")]
        public decimal EligibleLeaveDays { get; set; }

        [Display(Name = "Paid Leave Days")]
        public decimal PaidLeaveDays { get; set; }

        [Display(Name = "Eligible Leave Days Rounded (-) Paid Leave")]
        public int NetLeaveDays { get; set; }

        [Display(Name = "Monthly Salary")]
        public decimal MonthlySalary { get; set; }

        public decimal DailySalary { get; set; }

        [Display(Name = "Total Leave Salary")]
        public decimal TotalLeaveSalary { get; set; }

        [Display(Name = "Ticket Count")]
        public int TktCount { get; set; }

        [Display(Name = "Air Ticket Price")]
        public decimal TktPrice { get; set; }

        [Display(Name = "Air Ticket Value")]
        public decimal AirTicketValue { get; set; }

        [Display(Name = "Total Settlement")]
        public decimal TotalSettlement { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        public short PrcStat { get; set; }
    }
}

