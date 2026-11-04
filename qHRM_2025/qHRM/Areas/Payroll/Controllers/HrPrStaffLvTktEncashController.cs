using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.Payroll.Reports;
using qHRM.Models;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrStaffLvTktEncashController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly qHRMasterContext _hrmContext;

        public HrPrStaffLvTktEncashController(PayrollContext payrollContext, qHRMasterContext hrmContext)
        {
            _payrollContext = payrollContext;
            _hrmContext = hrmContext;
        }

        public IActionResult Index(int? year, int? month, string staffCat = "OUTSOURCE", bool load = false)
        {
            if (HttpContext.Session.GetString("STF_LV_TKT") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            int pYear = year ?? 2026;
            int pMonth = month ?? 10;
            string category = string.IsNullOrWhiteSpace(staffCat) ? "OUTSOURCE" : staffCat.ToUpper();

            ViewBag.Year = pYear;
            ViewBag.Month = pMonth;
            ViewBag.StaffCat = category;
            ViewBag.IsLoaded = load;

            List<HrPrStaffLvTktEncashVw> records;
            if (!load)
            {
                records = new List<HrPrStaffLvTktEncashVw>();
                ViewBag.InternalLeaveTotal = 0m;
                ViewBag.InternalTicketTotal = 0m;
                ViewBag.InternalSettlementTotal = 0m;
                ViewBag.OutsourceLeaveTotal = 0m;
                ViewBag.OutsourceTicketTotal = 0m;
                ViewBag.OutsourceSettlementTotal = 0m;
                ViewBag.GrandTotal = 0m;
            }
            else
            {
                records = _payrollContext.CalculateStaffLvTktEncash(pYear, pMonth, category);

                ViewBag.InternalLeaveTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalLeaveSalary);
                ViewBag.InternalTicketTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.AirTicketValue);
                ViewBag.InternalSettlementTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalSettlement);

                ViewBag.OutsourceLeaveTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalLeaveSalary);
                ViewBag.OutsourceTicketTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.AirTicketValue);
                ViewBag.OutsourceSettlementTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalSettlement);

                ViewBag.GrandTotal = records.Sum(x => x.TotalSettlement);
            }

            return View(records);
        }

        public class StaffEncashSaveItemDto
        {
            public int EncashId { get; set; }
            public int PrcYear { get; set; }
            public int PrcMonth { get; set; }
            public int EmpSysId { get; set; }
            public string StaffCategory { get; set; }
            public DateTime JoinedDate { get; set; }
            public DateTime? VacationReturnDate { get; set; }
            public DateTime NextResetDate { get; set; }
            public int ApplicableDays { get; set; }
            public decimal EligibleLeaveDays { get; set; }
            public decimal PaidLeaveDays { get; set; }
            public int NetLeaveDays { get; set; }
            public decimal MonthlySalary { get; set; }
            public decimal DailySalary { get; set; }
            public decimal TotalLeaveSalary { get; set; }
            public int TktCount { get; set; }
            public decimal TktPrice { get; set; }
            public decimal AirTicketValue { get; set; }
            public decimal TotalSettlement { get; set; }
            public string Remarks { get; set; }
        }

        [HttpPost]
        public JsonResult SaveConfirm([FromBody] List<StaffEncashSaveItemDto> items)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return Json(new { ok = false, message = "No records provided to save." });
                }

                int userId = HttpContext.Session.GetInt32("userEmpSysId") ?? HttpContext.Session.GetInt32("userID") ?? 1;

                var entities = items.Select(x => new HrPrStaffLvTktEncash
                {
                    EncashId = x.EncashId,
                    PrcYear = x.PrcYear,
                    PrcMonth = x.PrcMonth,
                    EmpSysId = x.EmpSysId,
                    StaffCategory = x.StaffCategory,
                    JoinedDate = x.JoinedDate,
                    VacationReturnDate = x.VacationReturnDate,
                    NextResetDate = x.NextResetDate,
                    ApplicableDays = x.ApplicableDays,
                    EligibleLeaveDays = x.EligibleLeaveDays,
                    PaidLeaveDays = x.PaidLeaveDays,
                    NetLeaveDays = x.NetLeaveDays,
                    MonthlySalary = x.MonthlySalary,
                    DailySalary = x.DailySalary,
                    TotalLeaveSalary = x.TotalLeaveSalary,
                    TktCount = x.TktCount,
                    TktPrice = x.TktPrice,
                    AirTicketValue = x.AirTicketValue,
                    TotalSettlement = x.TotalSettlement,
                    Remarks = x.Remarks,
                    PrcStat = 2
                }).ToList();

                _payrollContext.SaveStaffLvTktEncashList(entities, userId);

                return Json(new { ok = true, message = "Staff Leave Salary & Air Ticket Encashment confirmed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        public IActionResult ExportExcel(int year, int month, string staffCat = "OUTSOURCE")
        {
            try
            {
                var records = _payrollContext.CalculateStaffLvTktEncash(year, month, staffCat);
                byte[] bytes = StaffLeaveTicketEncashExcelReport.GenerateReport(year, month, records);

                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string filename = $"Staff_Leave_AirTicket_Encashment_{monthName}_{year}.xlsx";

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
            }
            catch (Exception ex)
            {
                return Content("Error generating report: " + ex.Message);
            }
        }

        public IActionResult PrintReport(int year, int month, string staffCat = "OUTSOURCE")
        {
            var records = _payrollContext.CalculateStaffLvTktEncash(year, month, staffCat);
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.MonthName = new DateTime(year, month, 1).ToString("MMMM");
            ViewBag.StaffCat = staffCat;

            ViewBag.InternalLeaveTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalLeaveSalary);
            ViewBag.InternalTicketTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.AirTicketValue);
            ViewBag.InternalSettlementTotal = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalSettlement);

            ViewBag.OutsourceLeaveTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalLeaveSalary);
            ViewBag.OutsourceTicketTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.AirTicketValue);
            ViewBag.OutsourceSettlementTotal = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalSettlement);

            ViewBag.GrandTotal = records.Sum(x => x.TotalSettlement);

            return View(records);
        }
    }
}

