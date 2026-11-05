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
    public class HrPrSlryPrcApprovalController : Controller
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly PayrollContext _payrollContext;
        private readonly PayrollWorkflowService _workflowService;

        public HrPrSlryPrcApprovalController(
            qHRMasterContext hrmContext,
            PayrollContext payrollContext,
            PayrollWorkflowService workflowService)
        {
            _hrmContext = hrmContext;
            _payrollContext = payrollContext;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(int? prcId, int? year, int? month)
        {
            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;

            HrPrSlryPrcHdr hdr = null;
            if (prcId.HasValue && prcId.Value > 0)
            {
                hdr = await _hrmContext.HrPrSlryPrcHdr.FirstOrDefaultAsync(h => h.PrcId == prcId.Value);
            }
            else if (year.HasValue && month.HasValue)
            {
                hdr = await _hrmContext.HrPrSlryPrcHdr
                    .FirstOrDefaultAsync(h => h.EntId == entId && h.EftYear == year.Value && h.EftMonth == month.Value);
            }

            if (hdr == null)
            {
                hdr = _payrollContext.GetLstSlryPrcHdr(entId);
            }

            if (hdr == null)
            {
                ViewBag.ErrorMessage = "No salary process record found.";
                return View();
            }

            var approvers = await _workflowService.GetWorkflowApproversAsync("Salary Process");

            // Authorization check: Only Ashhar (452), Hasan (11), Kaushalya (6), or Admin (userGrpId == 1)
            bool isApprover = (userEmpSysId == approvers.Level1Id || userEmpSysId == approvers.FinalId || userEmpSysId == approvers.InitiatorId || userGrpId == 1);
            if (!isApprover)
            {
                ViewBag.ErrorMessage = "You are not authorized to access the Salary Process Approval screen.";
                return View();
            }

            // Retrieve salary items for the grid
            var salaryList = _payrollContext.GetSlryDtl(entId, "_paysheet", 0, hdr.EftMonth, hdr.EftYear)?.ToList() ?? new List<PrSlryShtVw>();

            decimal inHouseTotal = salaryList.Where(x => x.InHouse).Sum(x => x.NtSlry);
            decimal outsourceTotal = salaryList.Where(x => !x.InHouse).Sum(x => x.NtSlry);
            decimal grossTotal = salaryList.Sum(x => x.GrsSlry);
            decimal deductionsTotal = salaryList.Sum(x => x.TotDctAmt);
            decimal grandTotal = salaryList.Sum(x => x.NtSlry);

            // Workflow history
            var history = await _hrmContext.HrPrWfHistory
                .Where(h => h.WfType == "Salary Process" && h.RefId == hdr.PrcId)
                .OrderByDescending(h => h.ActionDateTime)
                .ToListAsync();

            // Determine specific action permissions
            bool canApproveL1 = (userEmpSysId == approvers.Level1Id || userGrpId == 1) && hdr.WfStat == "PendingLevel1";
            bool canApproveFinal = (userEmpSysId == approvers.FinalId || userGrpId == 1) && hdr.WfStat == "PendingFinal";
            bool canApprove = canApproveL1 || canApproveFinal;
            bool canReject = (canApproveL1 || canApproveFinal);

            ViewBag.Header = hdr;
            ViewBag.SalaryList = salaryList;
            ViewBag.History = history;
            ViewBag.InHouseTotal = inHouseTotal;
            ViewBag.OutsourceTotal = outsourceTotal;
            ViewBag.GrossTotal = grossTotal;
            ViewBag.DeductionsTotal = deductionsTotal;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CanApprove = canApprove;
            ViewBag.CanApproveL1 = canApproveL1;
            ViewBag.CanApproveFinal = canApproveFinal;
            ViewBag.CanReject = canReject;
            ViewBag.ApproverL1Id = approvers.Level1Id;
            ViewBag.ApproverFinalId = approvers.FinalId;

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
        public async Task<IActionResult> Approve(int prcId, string comments)
        {
            try
            {
                int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                var res = await _workflowService.ApproveSalaryProcessAsync(prcId, userEmpSysId, comments);
                return Json(new { ok = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int prcId, string comments)
        {
            try
            {
                int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                var res = await _workflowService.RejectSalaryProcessAsync(prcId, userEmpSysId, comments);
                return Json(new { ok = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }
    }
}
