using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private readonly PayrollWorkflowService _workflowService;

        public HrPrStaffLvTktEncashController(PayrollContext payrollContext, qHRMasterContext hrmContext, PayrollWorkflowService workflowService)
        {
            _payrollContext = payrollContext;
            _hrmContext = hrmContext;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(int? year, int? month, string staffCat = "OUTSOURCE", bool load = false)
        {
            if (HttpContext.Session.GetString("STF_LV_TKT") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            int pYear;
            int pMonth;

            if (!year.HasValue || !month.HasValue)
            {
                var latestHdr = await _hrmContext.HrPrStaffLvTktEncashHdr
                    .OrderByDescending(h => h.PrcYear)
                    .ThenByDescending(h => h.PrcMonth)
                    .FirstOrDefaultAsync();

                if (latestHdr != null)
                {
                    if (latestHdr.PrcStat == 3) // Completed -> advance to next month
                    {
                        var next = new DateTime(latestHdr.PrcYear, latestHdr.PrcMonth, 1).AddMonths(1);
                        pYear = next.Year;
                        pMonth = next.Month;
                    }
                    else
                    {
                        pYear = latestHdr.PrcYear;
                        pMonth = latestHdr.PrcMonth;
                    }
                }
                else
                {
                    pYear = DateTime.Now.Year;
                    pMonth = DateTime.Now.Month;
                }
            }
            else
            {
                pYear = year.Value;
                pMonth = month.Value;
            }

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

            var encashHdr = await _hrmContext.HrPrStaffLvTktEncashHdr.FirstOrDefaultAsync(h => h.PrcYear == pYear && h.PrcMonth == pMonth);
            string wfStat = encashHdr?.WfStat ?? "Draft";
            int prcStat = encashHdr?.PrcStat ?? 1;

            int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
            var encashApprovers = await _workflowService.GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");
            bool isInitiator = (userEmpSysId == encashApprovers.InitiatorId || userGrpId == 1 || userEmpSysId == 1 || userEmpSysId == 0 || encashApprovers.InitiatorId <= 0);

            bool isApproved = (wfStat == "Approved");
            bool isCompleted = (prcStat == 3);
            bool isSaved = (prcStat == 2);

            bool canSubmit = isSaved && (wfStat == "Draft" || wfStat == "Verified" || wfStat == "Rejected");
            bool canCompleteAndWps = (isInitiator || userGrpId == 1) && isApproved && prcStat == 2;
            bool canGenerateWps = isApproved;

            ViewBag.WfStat = wfStat;
            ViewBag.PrcStat = prcStat;
            ViewBag.IsInitiator = isInitiator;
            ViewBag.IsApproved = isApproved;
            ViewBag.IsCompleted = isCompleted;
            ViewBag.IsSaved = isSaved;
            ViewBag.CanSubmit = canSubmit;
            ViewBag.CanCompleteAndWps = canCompleteAndWps;
            ViewBag.CanGenerateWps = canGenerateWps;

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

                int pYear = items.First().PrcYear;
                int pMonth = items.First().PrcMonth;

                // Query all saved records for this period to compute complete header totals
                var allMonthRecords = _hrmContext.HrPrStaffLvTktEncash
                    .Where(x => x.PrcYear == pYear && x.PrcMonth == pMonth && x.RecStat == 1)
                    .ToList();

                decimal inLv = allMonthRecords.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalLeaveSalary);
                decimal inTkt = allMonthRecords.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.AirTicketValue);
                decimal outLv = allMonthRecords.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalLeaveSalary);
                decimal outTkt = allMonthRecords.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.AirTicketValue);

                // Ensure header exists and has PrcStat = 2 (Verified/Saved)
                var hdr = _hrmContext.HrPrStaffLvTktEncashHdr.FirstOrDefault(h => h.PrcYear == pYear && h.PrcMonth == pMonth);
                if (hdr == null)
                {
                    hdr = new HrPrStaffLvTktEncashHdr
                    {
                        PrcYear = pYear,
                        PrcMonth = pMonth,
                        StaffCategory = "ALL",
                        PrcStat = 2,
                        WfStat = "Verified",
                        TotalInternalLeave = inLv,
                        TotalInternalTicket = inTkt,
                        TotalInternalSettlement = inLv + inTkt,
                        TotalOutsourceLeave = outLv,
                        TotalOutsourceTicket = outTkt,
                        TotalOutsourceSettlement = outLv + outTkt,
                        GrandTotalSettlement = inLv + inTkt + outLv + outTkt,
                        InitBy = userId,
                        InitDateTime = DateTime.Now,
                        VrfdBy = userId,
                        VrfdDateTime = DateTime.Now,
                        RecStat = 1
                    };
                    _hrmContext.HrPrStaffLvTktEncashHdr.Add(hdr);
                }
                else
                {
                    hdr.PrcStat = 2;
                    hdr.VrfdBy = userId;
                    hdr.VrfdDateTime = DateTime.Now;
                    hdr.TotalInternalLeave = inLv;
                    hdr.TotalInternalTicket = inTkt;
                    hdr.TotalInternalSettlement = inLv + inTkt;
                    hdr.TotalOutsourceLeave = outLv;
                    hdr.TotalOutsourceTicket = outTkt;
                    hdr.TotalOutsourceSettlement = outLv + outTkt;
                    hdr.GrandTotalSettlement = inLv + inTkt + outLv + outTkt;
                    if (string.IsNullOrEmpty(hdr.WfStat) || hdr.WfStat == "Draft")
                    {
                        hdr.WfStat = "Verified";
                    }
                }
                _hrmContext.SaveChanges();

                return Json(new { ok = true, message = "Staff Leave Salary & Air Ticket Encashment confirmed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> SubmitForApproval(int year, int month)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("userEmpSysId") ?? HttpContext.Session.GetInt32("userID") ?? 1;
                if (userId <= 0) userId = 1;
                var allRecords = _payrollContext.CalculateStaffLvTktEncash(year, month, "ALL");
                decimal inLv = allRecords.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalLeaveSalary);
                decimal inTkt = allRecords.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.AirTicketValue);
                decimal outLv = allRecords.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalLeaveSalary);
                decimal outTkt = allRecords.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.AirTicketValue);

                var res = await _workflowService.SubmitEncashmentAsync(year, month, userId, inLv, inTkt, outLv, outTkt);
                return Json(new { ok = res.Success, message = res.Message });
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

        [HttpPost]
        public async Task<IActionResult> CompleteProcess(int year, int month)
        {
            try
            {
                int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                var encashApprovers = await _workflowService.GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");
                bool isInitiator = (userEmpSysId == encashApprovers.InitiatorId || userGrpId == 1 || userEmpSysId == 1);

                if (!isInitiator)
                {
                    return Json(new { ok = false, message = "Only the initiator or administrator can complete the leave salary & air ticket encashment process." });
                }

                var hdr = await _hrmContext.HrPrStaffLvTktEncashHdr.FirstOrDefaultAsync(h => h.PrcYear == year && h.PrcMonth == month);
                if (hdr == null)
                {
                    return Json(new { ok = false, message = "Encashment process record not found for this period." });
                }

                if (hdr.WfStat != "Approved" && userGrpId != 1)
                {
                    return Json(new { ok = false, message = "Leave salary & air ticket encashment must be approved through the workflow before it can be completed." });
                }

                // Set PrcStat = 3 (Completed)
                hdr.PrcStat = 3;
                _hrmContext.Update(hdr);

                // Update detail records to PrcStat = 3
                var details = await _hrmContext.HrPrStaffLvTktEncash.Where(d => d.PrcYear == year && d.PrcMonth == month).ToListAsync();
                foreach (var d in details)
                {
                    d.PrcStat = 3;
                }
                _hrmContext.UpdateRange(details);

                await _hrmContext.SaveChangesAsync();

                return Json(new { ok = true, message = $"Leave salary & air ticket encashment for {new DateTime(year, month, 1):MMMM yyyy} completed successfully. The system is now ready for next month." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateWps(int year, int month, string staffCat = "OUTSOURCE")
        {
            try
            {
                string category = string.IsNullOrWhiteSpace(staffCat) ? "OUTSOURCE" : staffCat.ToUpper();
                var records = _payrollContext.CalculateStaffLvTktEncash(year, month, category);
                if (!records.Any())
                {
                    records = _payrollContext.CalculateStaffLvTktEncash(year, month, "ALL");
                }

                int recSeq = 1;
                int wrknDays = DateTime.DaysInMonth(year, month);
                decimal totNet = records.Sum(x => x.TotalSettlement);
                int rcrCount = records.Count();

                var sb = new StringBuilder();
                sb.AppendLine("Employer ID,File Creation Date,File Creation Time,Payer EID,Payer QID,Payer Bank Short Name,Payer IBAN,Salary Year And Month,Total Salaries,Total Records");
                sb.AppendLine($"10169600,{DateTime.Today:yyyyMMdd},{DateTime.Now:HHmm},10169600,,CBQ,QA87CBQA000000006010162429001,{year}{month.ToString().PadLeft(2, '0')},{totNet:F2},{rcrCount}");
                sb.AppendLine("Record Sequence,Employee QID,Employee Visa ID,Employee Name,Employee Bank Short Name,Employee Account,Salary Frequency,Number Of Working Days,Net Salary,Basic Salary,Extra hours,Extra Income,Deductions,Payment Type,Notes/Comments");

                foreach (var obj in records)
                {
                    var genInfo = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(g => g.EmpSysId == obj.EmpSysId);
                    var bnk = await _hrmContext.HrEmpBankAcct.FirstOrDefaultAsync(b => b.EmpSysId == obj.EmpSysId);

                    string qid = genInfo?.PmtId ?? "";
                    string visa = "";
                    string bankShort = !string.IsNullOrEmpty(bnk?.ShtBankName) ? bnk.ShtBankName : "CBQ";
                    string bankAcct = bnk?.AcctNo ?? "";
                    string empName = !string.IsNullOrEmpty(bnk?.AcctName) ? bnk.AcctName : obj.FullName;
                    decimal net = obj.TotalSettlement;
                    decimal basic = obj.TotalLeaveSalary;
                    decimal extraInc = obj.AirTicketValue;

                    sb.AppendLine($"{recSeq},{qid},{visa},{empName},{bankShort},{bankAcct},M,{wrknDays},{net:F2},{basic:F2},0,{extraInc:F2},0,,Leave Encashment");
                    recSeq++;
                }

                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                string filename = $"WPS_LeaveEncash_{year}_{month.ToString().PadLeft(2, '0')}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                return File(bytes, "text/csv", filename);
            }
            catch (Exception ex)
            {
                return Content("Error generating WPS file: " + ex.Message);
            }
        }
    }
}

