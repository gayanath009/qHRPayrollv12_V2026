using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Models;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrStaffLvTktEncashApprovalController : Controller
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly PayrollContext _payrollContext;
        private readonly PayrollWorkflowService _workflowService;

        public HrPrStaffLvTktEncashApprovalController(
            qHRMasterContext hrmContext,
            PayrollContext payrollContext,
            PayrollWorkflowService workflowService)
        {
            _hrmContext = hrmContext;
            _payrollContext = payrollContext;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            int pYear = year ?? 2026;
            int pMonth = month ?? 10;
            int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;

            var approvers = await _workflowService.GetWorkflowApproversAsync("Leave Salary & Air Ticket Encashment");

            // Authorization: Ashhar (452), Hasan (11), Kaushalya (6), or Admin (userGrpId == 1)
            bool isAuthorized = (userEmpSysId == approvers.Level1Id || userEmpSysId == approvers.FinalId || userEmpSysId == approvers.InitiatorId || userGrpId == 1);
            if (!isAuthorized)
            {
                ViewBag.ErrorMessage = "You are not authorized to view the Encashment Approval screen.";
                return View();
            }

            var hdr = await _hrmContext.HrPrStaffLvTktEncashHdr
                .FirstOrDefaultAsync(h => h.PrcYear == pYear && h.PrcMonth == pMonth);

            if (hdr == null)
            {
                hdr = await _workflowService.GetOrCreateEncashmentHdrAsync(pYear, pMonth);
            }

            var records = _payrollContext.CalculateStaffLvTktEncash(pYear, pMonth, "ALL");

            decimal inLv = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.TotalLeaveSalary);
            decimal inTkt = records.Where(x => x.StaffCategory == "INTERNAL").Sum(x => x.AirTicketValue);
            decimal outLv = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.TotalLeaveSalary);
            decimal outTkt = records.Where(x => x.StaffCategory == "OUTSOURCE").Sum(x => x.AirTicketValue);

            hdr.TotalInternalLeave = inLv;
            hdr.TotalInternalTicket = inTkt;
            hdr.TotalInternalSettlement = inLv + inTkt;
            hdr.TotalOutsourceLeave = outLv;
            hdr.TotalOutsourceTicket = outTkt;
            hdr.TotalOutsourceSettlement = outLv + outTkt;
            hdr.GrandTotalSettlement = hdr.TotalInternalSettlement + hdr.TotalOutsourceSettlement;

            var history = await _hrmContext.HrPrWfHistory
                .Where(h => h.WfType == "Leave Salary & Air Ticket Encashment" && h.PrcYear == pYear && h.PrcMonth == pMonth)
                .OrderByDescending(h => h.ActionDateTime)
                .ToListAsync();

            bool canApproveL1 = (userEmpSysId == approvers.Level1Id || userGrpId == 1) && hdr.WfStat == "PendingLevel1";
            bool canApproveFinal = (userEmpSysId == approvers.FinalId || userGrpId == 1) && hdr.WfStat == "PendingFinal";
            bool canApprove = canApproveL1 || canApproveFinal;
            bool canReject = canApproveL1 || canApproveFinal;

            ViewBag.Header = hdr;
            ViewBag.Records = records;
            ViewBag.History = history;
            ViewBag.CanApprove = canApprove;
            ViewBag.CanApproveL1 = canApproveL1;
            ViewBag.CanApproveFinal = canApproveFinal;
            ViewBag.CanReject = canReject;
            ViewBag.Year = pYear;
            ViewBag.Month = pMonth;
            ViewBag.MonthName = new DateTime(pYear, pMonth, 1).ToString("MMMM");

            var (initName, _) = await _workflowService.GetEmployeeDetailsAsync(approvers.InitiatorId);
            var (l1Name, _) = await _workflowService.GetEmployeeDetailsAsync(approvers.Level1Id);
            var (finalName, _) = await _workflowService.GetEmployeeDetailsAsync(approvers.FinalId);
            ViewBag.InitiatorName = initName;
            ViewBag.L1ApproverName = l1Name;
            ViewBag.FinalApproverName = finalName;

            return View(hdr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int year, int month, string comments)
        {
            try
            {
                int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                var res = await _workflowService.ApproveEncashmentAsync(year, month, userEmpSysId, comments);
                return Json(new { ok = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int year, int month, string comments)
        {
            try
            {
                int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                var res = await _workflowService.RejectEncashmentAsync(year, month, userEmpSysId, comments);
                return Json(new { ok = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }
    }
}

