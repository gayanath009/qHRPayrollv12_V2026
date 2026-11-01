using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Integrations.Data;
using qHRM.Areas.Leave.Data;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using static qHRM.Areas.Attendance.Data.AttnContext;
using static qHRM.Areas.SysAdmin.Data.SysSelectListContext;

using qHRM.Models;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    [Authorize]
    public class HrLvRegisterController : Controller
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly LeaveContext _leaveContext;
        private readonly LvIntgrtnContext _lvIntgrtnContext;
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _attdContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrLvRegister> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly LeaveWorkflowService _lvWfService;
        private readonly LeaveBalanceService _balanceService;

        public HrLvRegisterController(qHRMasterContext hrmContext,
                                      LeaveContext leaveContext,
                                      LvIntgrtnContext lvIntgrtnContext,
                                      EmployeeContext empContext,
                                      AttnContext attdContext,
                                      SysSelectListContext sysSelectListContext,
                                      DbCommitContext<HrLvRegister> dbCommitContext,
                                      AdmUserGrpPrvContext prvContext,
                                      LeaveWorkflowService lvWfService,
                                      LeaveBalanceService balanceService = null)
        {
            _hrmContext = hrmContext;
            _leaveContext = leaveContext;
            _lvIntgrtnContext = lvIntgrtnContext;
            _dbCommitContext = dbCommitContext;
            _empContext = empContext;
            _attdContext = attdContext;
            _sysSelectListContext = sysSelectListContext;
            _prvContext = prvContext;
            _lvWfService = lvWfService;
            _balanceService = balanceService;
        }

        public async Task<IActionResult> Index(string tdyFltr)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("LV_REG") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _lvWfService.SyncActiveWorkflowStatusesAsync(cts.Token);
                }
                catch { }

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                int entId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(empSysId, grpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(empSysId, grpId);
                bool showAllEmp = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(empSysId, grpId);

                var allowedSubordinates = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(empSysId).ToHashSet();
                if (empSysId > 0 && !isSelfOnlyUser) allowedSubordinates.Add(empSysId);
                bool canManageSubordinates = !isSelfOnlyUser && allowedSubordinates.Count > 1;
                bool canSelectEmployee = !isSelfOnlyUser && (showAllEmp || isOutsourceAdmin || canManageSubordinates);

                ViewBag.IsRestrictedUser = isSelfOnlyUser;
                ViewBag.ShowAllEmployees = showAllEmp;
                ViewBag.IsOutsourceAdmin = isOutsourceAdmin;
                ViewBag.CanSelectEmployee = canSelectEmployee;

                if (tdyFltr != null)
                    ViewBag.DataSource = _leaveContext.GetLeaveRegister(!canSelectEmployee, empSysId, grpId).Where(x => x.StDate <= DateTime.Today && x.EndDate >= DateTime.Today).OrderByDescending(x => x.CrtDateTime);
                else 
                    ViewBag.DataSource = _leaveContext.GetLeaveRegister(!canSelectEmployee, empSysId, grpId);

                ViewBag.lstEmp = _empContext.GetActvEmpNameLst(entId > 0 ? entId : 1, empSysId, grpId);
                ViewBag.CurrentEmpSysId = empSysId;

                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "LV_REG").FirstOrDefault();
                ViewBag.RecAdd    = objprv?.RecAdd    ?? false;
                ViewBag.RecEdit   = objprv?.RecEdit   ?? false;
                ViewBag.RecDelete = objprv?.RecDelete ?? false;
                //End | Usr Prv//


                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: HrLvRegister/Create
        [HttpGet]
        public IActionResult CreateNew(HrLvRegister hrLvRstr)
        {
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                int entId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(empSysId, grpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(empSysId, grpId);
                bool showAllEmp = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(empSysId, grpId);

                var allowedSubordinates = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(empSysId).ToHashSet();
                if (empSysId > 0 && !isSelfOnlyUser) allowedSubordinates.Add(empSysId);
                bool canManageSubordinates = !isSelfOnlyUser && allowedSubordinates.Count > 1;
                bool canSelectEmployee = !isSelfOnlyUser && (showAllEmp || isOutsourceAdmin || canManageSubordinates);

                ViewBag.IsRestrictedUser = isSelfOnlyUser;
                ViewBag.ShowAllEmployees = showAllEmp;
                ViewBag.IsOutsourceAdmin = isOutsourceAdmin;
                ViewBag.CanSelectEmployee = canSelectEmployee;

                ViewBag.lstEmp = _empContext.GetActvEmpNameLst(entId > 0 ? entId : 1, empSysId, grpId);
                ViewBag.lstLvTyp = _leaveContext.GetAllLeaveTypeViewsByEntId(entId > 0 ? entId : 1);
                @ViewBag.Today = DateTime.Today;

                if (!canSelectEmployee && empSysId > 0)
                {
                    hrLvRstr.EmpSysId = empSysId;
                }
                else if (hrLvRstr.EmpSysId == null || hrLvRstr.EmpSysId <= 0)
                {
                    hrLvRstr.EmpSysId = empSysId;
                }

                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "LV_REG").FirstOrDefault();
                ViewBag.RecAdd = objprv?.RecAdd ?? false;
                ViewBag.RecEdit = objprv?.RecEdit ?? false;
                ViewBag.RecDelete = objprv?.RecDelete ?? false;
                //End | Usr Prv//

                return PartialView("_HrLvRegister_Create", hrLvRstr);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", ex.Message);
            }
        }

        // POST: HrLvRegister/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HrLvRegister hrLvRstr, IFormFile medCertFile = null, IFormFile outsourceApprovalFile = null)
        {
            try
            {
                if (hrLvRstr.EmpSysId <= 0)
                {
                    ModelState.AddModelError("EmpSysId", "Please choose a valid employee");
                }
                if (hrLvRstr.LvTypId <= 0)
                {
                    ModelState.AddModelError("LvTypId", "Please choose a valid leave type");
                }

                if (ModelState.IsValid)
                {
                    try
                    {        
                        int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                        int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                        bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
                        bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(userEmpSysId, grpId);
                        bool showAll = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(userEmpSysId, grpId);
                        var allowedIds = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(userEmpSysId).ToHashSet();
                        if (userEmpSysId > 0 && !isSelfOnlyUser) allowedIds.Add(userEmpSysId);

                        if (isSelfOnlyUser && userEmpSysId > 0)
                        {
                            hrLvRstr.EmpSysId = userEmpSysId;
                        }
                        else if (!showAll && !isOutsourceAdmin && userEmpSysId > 0)
                        {
                            if (allowedIds.Count > 1 && hrLvRstr.EmpSysId.HasValue && allowedIds.Contains(hrLvRstr.EmpSysId.Value))
                            {
                                // Allowed to apply for subordinate
                            }
                            else
                            {
                                hrLvRstr.EmpSysId = userEmpSysId;
                            }
                        }
                        else if (isOutsourceAdmin)
                        {
                            var osEmpIds = _empContext.GetOutsourceEmployeeSysIds(userEmpSysId);
                            if (!hrLvRstr.EmpSysId.HasValue || !osEmpIds.Contains(hrLvRstr.EmpSysId.Value))
                            {
                                if (userEmpSysId > 0) hrLvRstr.EmpSysId = userEmpSysId;
                            }
                        }
                        else if (userEmpSysId <= 0)
                        {
                            userEmpSysId = hrLvRstr.EmpSysId.GetValueOrDefault();
                        }

                        // Fetch Leave Type info for validations
                        var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == hrLvRstr.LvTypId);
                        string lvCode = lvType?.LvTypCode ?? "";
                        string lvName = lvType?.NameEng ?? "";

                        byte[] medCertBytes = null;
                        string medCertDocName = null;
                        string medCertDocMime = null;

                        byte[] outsourceDocBytes = null;
                        string outsourceDocName = null;
                        string outsourceDocMime = null;

                        // Handle Medical Certificate Upload if provided
                        if (medCertFile != null && medCertFile.Length > 0)
                        {
                            var uploadDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "attach", "medCert");
                            if (!System.IO.Directory.Exists(uploadDir)) System.IO.Directory.CreateDirectory(uploadDir);
                            var safeName = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(medCertFile.FileName)}";
                            var filePath = System.IO.Path.Combine(uploadDir, safeName);
                            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                            {
                                await medCertFile.CopyToAsync(stream);
                            }
                            using (var ms = new System.IO.MemoryStream())
                            {
                                await medCertFile.CopyToAsync(ms);
                                medCertBytes = ms.ToArray();
                            }
                            medCertDocName = System.IO.Path.GetFileName(medCertFile.FileName);
                            medCertDocMime = medCertFile.ContentType ?? (safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "image/jpeg");

                            hrLvRstr.DocId = 1;
                            hrLvRstr.Note = string.IsNullOrEmpty(hrLvRstr.Note) ? $"[MedCert: {safeName}]" : $"{hrLvRstr.Note} [MedCert: {safeName}]";
                        }

                        // Handle Outsourcing Employer Approval Document Upload if provided
                        if (outsourceApprovalFile != null && outsourceApprovalFile.Length > 0)
                        {
                            var uploadDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "attach", "medCert");
                            if (!System.IO.Directory.Exists(uploadDir)) System.IO.Directory.CreateDirectory(uploadDir);
                            var safeName = $"os_approval_{Guid.NewGuid()}_{System.IO.Path.GetFileName(outsourceApprovalFile.FileName)}";
                            var filePath = System.IO.Path.Combine(uploadDir, safeName);
                            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                            {
                                await outsourceApprovalFile.CopyToAsync(stream);
                            }
                            using (var ms = new System.IO.MemoryStream())
                            {
                                await outsourceApprovalFile.CopyToAsync(ms);
                                outsourceDocBytes = ms.ToArray();
                            }
                            outsourceDocName = System.IO.Path.GetFileName(outsourceApprovalFile.FileName);
                            outsourceDocMime = outsourceApprovalFile.ContentType ?? (safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "image/jpeg");

                            hrLvRstr.DocId = 1;
                            hrLvRstr.Note = string.IsNullOrEmpty(hrLvRstr.Note) ? $"[Attachment: {safeName}]" : $"{hrLvRstr.Note} [Attachment: {safeName}]";
                        }

                        // 0. ANNUAL LEAVE VALIDATION: Must complete 1 year of service & apply at least 15 days in advance
                        // For outsourced staff: Supporting approval from their outsourcing employer is mandatory
                        if (lvCode == "LV_ANL" || lvName.Contains("Annual", StringComparison.OrdinalIgnoreCase))
                        {
                            var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == hrLvRstr.EmpSysId);
                            if (emp != null)
                            {
                                bool isOutsourceEmp = emp.WrktypId == 2 || emp.GrpId == 7 || _empContext.GetOutsourceEmployeeSysIds().Contains(emp.EmpSysId);
                                if (isOutsourceEmp && (outsourceApprovalFile == null || outsourceApprovalFile.Length == 0))
                                {
                                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Outsourced employees must attach an approval document from their outsourcing employer when applying for Annual Leave.");
                                }

                                var oneYearAnniversary = emp.JoinedDate.Date.AddYears(1);
                                if (DateTime.Today < oneYearAnniversary || hrLvRstr.StDate.Date < oneYearAnniversary)
                                {
                                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Annual Leave Policy Restriction: Employees who have not completed 1 year within the company cannot apply for Annual Leave. Employee joined on {emp.JoinedDate:dd/MM/yyyy} and will be eligible from {oneYearAnniversary:dd/MM/yyyy}.");
                                }
                            }

                            var minAllowedDate = DateTime.Today.AddDays(15);
                            if (hrLvRstr.StDate.Date < minAllowedDate)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Annual Leave must be applied at least 15 days in advance. Earliest eligible start date is {minAllowedDate:dd/MM/yyyy}.");
                            }
                        }

                        // 1. SICK LEAVE VALIDATION: Medical certificate mandatory + Notice
                        if (lvCode == "LV_SICK" || lvName.Contains("Sick", StringComparison.OrdinalIgnoreCase))
                        {
                            if (hrLvRstr.DocId == null || hrLvRstr.DocId == 0)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Medical certificate attachment is mandatory for Sick Leave. Notice: First 14 days are fully paid, followed by 28 days at half pay.");
                            }
                        }

                        // 2. CASUAL LEAVE VALIDATION: Max 3 paid days per year
                        if (lvCode == "LV_CSL" || lvName.Contains("Casual", StringComparison.OrdinalIgnoreCase))
                        {
                            int maxCasualDays = lvType?.ElgDays > 0 ? lvType.ElgDays : 3;
                            int lvYear = hrLvRstr.LvYear > 0 ? hrLvRstr.LvYear : DateTime.Now.Year;
                            var usedDays = await _hrmContext.HrLvRegister
                                .Where(l => l.EmpSysId == hrLvRstr.EmpSysId
                                         && l.LvTypId == hrLvRstr.LvTypId
                                         && l.LvYear == lvYear
                                         && l.LvId != hrLvRstr.LvId
                                         && l.RecStat != 0 && l.RecStat != 3 && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                                .SumAsync(l => (int?)l.Days) ?? 0;

                            if (usedDays + hrLvRstr.Days > maxCasualDays)
                            {
                                int rem = Math.Max(0, maxCasualDays - usedDays);
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Casual Leave is restricted to a maximum of {maxCasualDays} paid days per year. Already applied/used: {usedDays} day(s). Remaining available: {rem} day(s).");
                            }
                        }

                        // 3. EMERGENCY LEAVE VALIDATION: Max 30 days per application
                        if (lvCode == "LV_EMG" || lvName.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
                        {
                            if (hrLvRstr.Days > 30)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Emergency Leave is restricted to a maximum of 30 days per application. Requested: {hrLvRstr.Days} days.");
                            }
                        }

                        // 4. HAJJ LEAVE VALIDATION: Max 14 days, once in lifetime
                        if (lvCode == "LV_HAJJ" || lvName.Contains("Hajj", StringComparison.OrdinalIgnoreCase))
                        {
                            if (hrLvRstr.Days > 14)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Hajj Leave is restricted to a maximum of 14 days. Requested: {hrLvRstr.Days} days.");
                            }

                            var priorHajj = await _hrmContext.HrLvRegister
                                .FirstOrDefaultAsync(l => l.EmpSysId == hrLvRstr.EmpSysId
                                                       && l.LvTypId == hrLvRstr.LvTypId
                                                       && l.LvId != hrLvRstr.LvId
                                                       && (l.RecStat == 2 || l.WfStat == "Approved"));
                            if (priorHajj != null)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Hajj Leave can only be taken once during the employee's entire lifetime / employment period.");
                            }
                        }

                        // 5. MATERNITY LEAVE VALIDATION: Female employees only
                        if (lvCode.Contains("MAT", StringComparison.OrdinalIgnoreCase) || lvName.Contains("Maternity", StringComparison.OrdinalIgnoreCase))
                        {
                            var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == hrLvRstr.EmpSysId);
                            string gender = emp?.Gender?.Trim() ?? "";
                            if (!string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) && !string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase))
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Maternity Leave is restricted to female employees only.");
                            }
                        }

                        // Overlapping Leave Guard: Prevent overlapping with other leave types for the same employee
                        var overlappingLeave = await _hrmContext.HrLvRegister
                            .Where(l => l.EmpSysId == hrLvRstr.EmpSysId
                                     && l.LvId != hrLvRstr.LvId
                                     && l.RecStat != 0 && l.RecStat != 3
                                     && l.WfStat != "Rejected" && l.WfStat != "Cancelled"
                                     && l.StDate.Date <= hrLvRstr.EndDate.Date
                                     && l.EndDate.Date >= hrLvRstr.StDate.Date)
                            .FirstOrDefaultAsync();

                        if (overlappingLeave != null)
                        {
                            var existingLvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == overlappingLeave.LvTypId);
                            string existingTypeName = existingLvType?.NameEng ?? "Leave";
                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"You already have a leave ({existingTypeName}: {overlappingLeave.StDate:dd/MM/yyyy} to {overlappingLeave.EndDate:dd/MM/yyyy}) within the selected date range.");
                        }

                        // 5b. SANDWICH LEAVE POLICY VALIDATION
                        if (hrLvRstr.EmpSysId.HasValue && hrLvRstr.LvTypId.HasValue)
                        {
                            var sandwichCheck = await _leaveContext.CheckSandwichLeavePolicyAsync(
                                hrLvRstr.EmpSysId.Value,
                                hrLvRstr.LvTypId.Value,
                                hrLvRstr.StDate,
                                hrLvRstr.EndDate,
                                hrLvRstr.LvId);
                            if (sandwichCheck.isRestricted)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", sandwichCheck.message);
                            }
                        }

                        // 6. ALL LEAVE TYPES: Comprehensive Available Balance Validation (Entitled - Used - InProgress)
                        // NOTE: Pass StDate as asOfDate so accrual is evaluated as of the leave start date,
                        //       matching what the UI displayed to the user (same accrual/cycle window).
                        if (_balanceService != null && hrLvRstr.EmpSysId.HasValue && hrLvRstr.LvTypId.HasValue)
                        {
                            int yr = hrLvRstr.StDate.Year > 0 ? hrLvRstr.StDate.Year : DateTime.Now.Year;
                            DateTime evalAsOf = hrLvRstr.StDate != default ? hrLvRstr.StDate : DateTime.Today;
                            var empBalances = await _balanceService.GetEmployeeLeaveBalancesAsync(hrLvRstr.EmpSysId.Value, yr, evalAsOf);
                            var lvBalItem = empBalances.FirstOrDefault(b => b.LvTypId == hrLvRstr.LvTypId.Value);

                            if (lvBalItem != null)
                            {
                                if (lvBalItem.IsAccrualBased && lvBalItem.CurrentCycleResetDate.HasValue)
                                {
                                    DateTime resetDate = lvBalItem.CurrentCycleResetDate.Value.Date;
                                    if (hrLvRstr.StDate.Date < resetDate && hrLvRstr.EndDate.Date >= resetDate)
                                    {
                                        int daysBeforeReset = (resetDate - hrLvRstr.StDate.Date).Days;
                                        int daysAfterReset = (hrLvRstr.EndDate.Date - resetDate).Days + 1;

                                        if (daysBeforeReset > lvBalItem.CurrentCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested leave requires {daysBeforeReset} day(s) before reset date {resetDate:dd/MM/yyyy}, but only {lvBalItem.CurrentCycleRemaining:0} day(s) remain in your current cycle.");
                                        }
                                        if (daysAfterReset > lvBalItem.NextCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested leave requires {daysAfterReset} day(s) after reset date {resetDate:dd/MM/yyyy}, but only {lvBalItem.NextCycleRemaining:0} day(s) are available in your next cycle.");
                                        }
                                    }
                                    else if (hrLvRstr.StDate.Date < resetDate)
                                    {
                                        if (hrLvRstr.Days > lvBalItem.CurrentCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds remaining balance ({lvBalItem.CurrentCycleRemaining:0} day(s)) until reset date {resetDate:dd/MM/yyyy}.");
                                        }
                                    }
                                    else
                                    {
                                        if (hrLvRstr.Days > lvBalItem.NextCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds available balance ({lvBalItem.NextCycleRemaining:0} day(s)) for the new cycle.");
                                        }
                                    }
                                }
                                else if (hrLvRstr.Days > lvBalItem.Remaining)
                                {
                                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds your available balance ({lvBalItem.Remaining:0} day(s) remaining after active/in-progress applications).");
                                }
                            }
                        }

                        // Get the Running Leave Incident for the Employee
                        int lvIncidnt;
                        var lvRgtrVw = _leaveContext.GetLeaveRegister(false, userEmpSysId).Where(x => x.EmpSysId == hrLvRstr.EmpSysId).OrderByDescending(o => o.Incident).FirstOrDefault();
                        if (lvRgtrVw != null)
                            lvIncidnt = lvRgtrVw.Incident + 1;
                        else
                            lvIncidnt = 1;

                        hrLvRstr.LvSdlId = (hrLvRstr.LvSdlId == null || hrLvRstr.LvSdlId == 0) ? null : hrLvRstr.LvSdlId;
                        hrLvRstr.Incident = lvIncidnt;

                        // Set current available days from balance service if present
                        // NOTE: Evaluate as of StDate to stay consistent with the balance check above.
                        if (_balanceService != null && hrLvRstr.EmpSysId.HasValue && hrLvRstr.LvTypId.HasValue)
                        {
                            int yr = hrLvRstr.StDate.Year > 0 ? hrLvRstr.StDate.Year : DateTime.Now.Year;
                            DateTime evalAsOf = hrLvRstr.StDate != default ? hrLvRstr.StDate : DateTime.Today;
                            decimal curAvbl = await _balanceService.GetAvailableBalanceAsync(hrLvRstr.EmpSysId.Value, hrLvRstr.LvTypId.Value, yr, evalAsOf);
                            hrLvRstr.CurrAvblDays = (int)curAvbl;
                        }
                        else
                        {
                            hrLvRstr.CurrAvblDays = 0;
                        }

                        hrLvRstr.LvYear = DateTime.Now.Year;
                        hrLvRstr.CrtId = userEmpSysId;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrLvRstr, userEmpSysId);

                        // Persist uploaded documents to HrLvRegDocs database table
                        if (hrLvRstr.LvId > 0)
                        {
                            try
                            {
                                if (outsourceDocBytes != null && outsourceDocBytes.Length > 0)
                                {
                                    var regDoc = new HrLvRegDocs
                                    {
                                        LvId = hrLvRstr.LvId,
                                        Desc = "Outsourcing Employer Approval Document",
                                        DocName = outsourceDocName ?? "outsource_approval.pdf",
                                        DocMime = outsourceDocMime ?? "application/pdf",
                                        ScnCopy = outsourceDocBytes,
                                        CrtId = userEmpSysId,
                                        CrtDateTime = DateTime.Now
                                    };
                                    _hrmContext.HrLvRegDocs.Add(regDoc);
                                    await _hrmContext.SaveChangesAsync();
                                }
                                else if (medCertBytes != null && medCertBytes.Length > 0)
                                {
                                    var regDoc = new HrLvRegDocs
                                    {
                                        LvId = hrLvRstr.LvId,
                                        Desc = "Doctor Medical Certificate",
                                        DocName = medCertDocName ?? "medical_certificate.pdf",
                                        DocMime = medCertDocMime ?? "application/pdf",
                                        ScnCopy = medCertBytes,
                                        CrtId = userEmpSysId,
                                        CrtDateTime = DateTime.Now
                                    };
                                    _hrmContext.HrLvRegDocs.Add(regDoc);
                                    await _hrmContext.SaveChangesAsync();
                                }
                            }
                            catch { }
                        }

                        // Updating the Attendance Process
                        _attdContext.LoadHrEmpAtndc("S", hrLvRstr.EmpSysId.GetValueOrDefault(), hrLvRstr.StDate, hrLvRstr.EndDate, AtndcLdPrcTyp.LV, "LV-Id" + hrLvRstr.LvId.ToString(), false, hrLvRstr.CrtId);

                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Register", "Record Added Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Adding Record got failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Please select a valid Employee and Leave Type");
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrLvRegister/Rejoin
        [HttpGet]
        public ActionResult RejoinGet(int lvId)
        {
            try
            {
                var lvRgtr = _leaveContext.GetHrLvRegisterByLvId(lvId) ?? _hrmContext.HrLvRegister.FirstOrDefault(x => x.LvId == lvId);
                if (lvRgtr == null)
                {
                    return Content("<div class='alert alert-danger m-3'>Leave record not found.</div>");
                }

                if (lvRgtr.RjnDate is null)
                    lvRgtr.RjnDate = DateTime.Today;

                var lvRgtrVw = _leaveContext.GetLeaveRegister(false, Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"))).FirstOrDefault(x => x.LvId == lvId);
                ViewBag.EmpName = lvRgtrVw?.Full_Name ?? "";
                ViewBag.LvTyp = lvRgtrVw?.LvTyp ?? "";

                ViewBag.lstRjnStat = _sysSelectListContext.GetElements(SelectListTypeEnum.LV_RJN_STS);

                return PartialView("_HrLvRegister_Rejoin", lvRgtr);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger m-3'>Error loading rejoin form: " + ex.Message + "</div>");
            }
        }

        // POST: HrLvRegister/Rejoin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rejoin(HrLvRegister hrLvRstr)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var extHrLvRstr = _leaveContext.GetHrLvRegisterByLvId(hrLvRstr.LvId);
                        extHrLvRstr.RjnDate = hrLvRstr.RjnDate;
                        extHrLvRstr.RjnStat = hrLvRstr.RjnStat;
                        hrLvRstr.CrtId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, extHrLvRstr, (int)HttpContext.Session.GetInt32("userEmpSysId"));

                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Register", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrLvRegister/Edit
        [HttpGet]
        public ActionResult EditGet(int id)
        {
            try
            {
                var lvRgtr = _leaveContext.GetHrLvRegisterByLvId(id) ?? _hrmContext.HrLvRegister.FirstOrDefault(x => x.LvId == id);
                if (lvRgtr == null)
                {
                    return Content("<div class='alert alert-danger m-3'>Leave record not found.</div>");
                }

                bool isSubmittedToWorkflow = (!string.IsNullOrEmpty(lvRgtr.WfStat) && !string.Equals(lvRgtr.WfStat, "Draft", StringComparison.OrdinalIgnoreCase))
                                              || lvRgtr.WfInstId.HasValue
                                              || _hrmContext.HrLvWfInstance.Any(w => w.LvId == id);

                if (isSubmittedToWorkflow)
                {
                    ViewBag.IsSubmittedToWorkflow = true;
                    var emp = _hrmContext.HrEmpGenInfo.FirstOrDefault(e => e.EmpSysId == lvRgtr.EmpSysId);
                    var typ = _hrmContext.HrLvType.FirstOrDefault(t => t.LvTypId == lvRgtr.LvTypId);
                    ViewBag.EmpName = emp != null ? $"{emp.EmpId} - {emp.FullName}" : "";
                    ViewBag.LvTypeName = typ?.NameEng ?? "";
                    return PartialView("_HrLvRegister_Edit", lvRgtr);
                }

                lvRgtr.ex_StDate = lvRgtr.StDate;
                lvRgtr.ex_EndDate = lvRgtr.EndDate;

                ViewBag.lstEmp = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"))).Where(x => x.ESysId == lvRgtr.EmpSysId);
                ViewBag.lstLvTyp = _leaveContext.GetAllLeaveTypeViewsByEntId((int)HttpContext.Session.GetInt32("userEntId"));

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "LV_REG").FirstOrDefault();            
                ViewBag.RecEdit = objprv != null ? objprv.RecEdit : true;            
                //End | Usr Prv//

                return PartialView("_HrLvRegister_Edit", lvRgtr);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger m-3'>Error loading leave record: " + ex.Message + "</div>");
            }
        }

        // POST: HrLvRegister/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HrLvRegister hrLvRstr)
        {
            try
            {
                var existingLeave = _leaveContext.GetHrLvRegisterByLvId(hrLvRstr.LvId) ?? _hrmContext.HrLvRegister.FirstOrDefault(x => x.LvId == hrLvRstr.LvId);
                if (existingLeave != null)
                {
                    bool isSubmitted = (!string.IsNullOrEmpty(existingLeave.WfStat) && !string.Equals(existingLeave.WfStat, "Draft", StringComparison.OrdinalIgnoreCase))
                                       || existingLeave.WfInstId.HasValue
                                       || _hrmContext.HrLvWfInstance.Any(w => w.LvId == hrLvRstr.LvId);
                    if (isSubmitted)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "You can not edit the leave once you submitted the leave to the workflow");
                    }
                }

                if (hrLvRstr.EmpSysId <= 0)
                {
                    ModelState.AddModelError("EmpSysId", "Please choose a valid employee");
                }
                if (hrLvRstr.LvTypId <= 0)
                {
                    ModelState.AddModelError("LvTypId", "Please choose a valid leave type");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        DateTime stD = new DateTime();
                        DateTime endD = new DateTime();

                        if (hrLvRstr.ex_StDate < hrLvRstr.StDate)
                            stD = hrLvRstr.ex_StDate;
                        else
                            stD = hrLvRstr.StDate;

                        if (hrLvRstr.EndDate > hrLvRstr.ex_EndDate)
                            endD = hrLvRstr.EndDate;
                        else
                            endD = hrLvRstr.ex_EndDate;

                        int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                        if (userEmpSysId <= 0)
                        {
                            userEmpSysId = hrLvRstr.EmpSysId.GetValueOrDefault();
                        }

                        // Fetch Leave Type info for validations
                        var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == hrLvRstr.LvTypId);
                        string lvCode = lvType?.LvTypCode ?? "";
                        string lvName = lvType?.NameEng ?? "";

                        // ANNUAL LEAVE VALIDATION: Must complete 1 year of service & apply at least 15 days in advance
                        if (lvCode == "LV_ANL" || lvName.Contains("Annual", StringComparison.OrdinalIgnoreCase))
                        {
                            var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == hrLvRstr.EmpSysId);
                            if (emp != null)
                            {
                                var oneYearAnniversary = emp.JoinedDate.Date.AddYears(1);
                                if (DateTime.Today < oneYearAnniversary || hrLvRstr.StDate.Date < oneYearAnniversary)
                                {
                                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Annual Leave Policy Restriction: Employees who have not completed 1 year within the company cannot apply for Annual Leave. Employee joined on {emp.JoinedDate:dd/MM/yyyy} and will be eligible from {oneYearAnniversary:dd/MM/yyyy}.");
                                }
                            }

                            var minAllowedDate = DateTime.Today.AddDays(15);
                            if (hrLvRstr.StDate.Date < minAllowedDate)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Annual Leave must be applied at least 15 days in advance. Earliest eligible start date is {minAllowedDate:dd/MM/yyyy}.");
                            }
                        }

                        // Overlapping Leave Guard: Prevent overlapping with other leave types for the same employee
                        var overlappingLeave = await _hrmContext.HrLvRegister
                            .Where(l => l.EmpSysId == hrLvRstr.EmpSysId
                                     && l.LvId != hrLvRstr.LvId
                                     && l.RecStat != 0 && l.RecStat != 3
                                     && l.WfStat != "Rejected" && l.WfStat != "Cancelled"
                                     && l.StDate.Date <= hrLvRstr.EndDate.Date
                                     && l.EndDate.Date >= hrLvRstr.StDate.Date)
                            .FirstOrDefaultAsync();

                        if (overlappingLeave != null)
                        {
                            var existingLvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == overlappingLeave.LvTypId);
                            string existingTypeName = existingLvType?.NameEng ?? "Leave";
                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"You already have a leave ({existingTypeName}: {overlappingLeave.StDate:dd/MM/yyyy} to {overlappingLeave.EndDate:dd/MM/yyyy}) within the selected date range.");
                        }

                        // Sandwich Leave Policy Validation for Edit
                        if (hrLvRstr.EmpSysId.HasValue && hrLvRstr.LvTypId.HasValue)
                        {
                            var sandwichCheck = await _leaveContext.CheckSandwichLeavePolicyAsync(
                                hrLvRstr.EmpSysId.Value,
                                hrLvRstr.LvTypId.Value,
                                hrLvRstr.StDate,
                                hrLvRstr.EndDate,
                                hrLvRstr.LvId);
                            if (sandwichCheck.isRestricted)
                            {
                                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", sandwichCheck.message);
                            }
                        }

                        // Available Balance Validation for Edit
                        if (_balanceService != null && hrLvRstr.EmpSysId.HasValue && hrLvRstr.LvTypId.HasValue)
                        {
                            int yr = hrLvRstr.StDate.Year > 0 ? hrLvRstr.StDate.Year : DateTime.Now.Year;
                            DateTime evalAsOf = hrLvRstr.StDate != default ? hrLvRstr.StDate : DateTime.Today;
                            var empBalances = await _balanceService.GetEmployeeLeaveBalancesAsync(hrLvRstr.EmpSysId.Value, yr, evalAsOf);
                            var lvBalItem = empBalances.FirstOrDefault(b => b.LvTypId == hrLvRstr.LvTypId.Value);

                            if (lvBalItem != null)
                            {
                                if (lvBalItem.IsAccrualBased && lvBalItem.CurrentCycleResetDate.HasValue)
                                {
                                    DateTime resetDate = lvBalItem.CurrentCycleResetDate.Value.Date;
                                    if (hrLvRstr.StDate.Date < resetDate && hrLvRstr.EndDate.Date >= resetDate)
                                    {
                                        int daysBeforeReset = (resetDate - hrLvRstr.StDate.Date).Days;
                                        int daysAfterReset = (hrLvRstr.EndDate.Date - resetDate).Days + 1;

                                        if (daysBeforeReset > lvBalItem.CurrentCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested leave requires {daysBeforeReset} day(s) before reset date {resetDate:dd/MM/yyyy}, but only {lvBalItem.CurrentCycleRemaining:0} day(s) remain in your current cycle.");
                                        }
                                        if (daysAfterReset > lvBalItem.NextCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested leave requires {daysAfterReset} day(s) after reset date {resetDate:dd/MM/yyyy}, but only {lvBalItem.NextCycleRemaining:0} day(s) are available in your next cycle.");
                                        }
                                    }
                                    else if (hrLvRstr.StDate.Date < resetDate)
                                    {
                                        if (hrLvRstr.Days > lvBalItem.CurrentCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds remaining balance ({lvBalItem.CurrentCycleRemaining:0} day(s)) until reset date {resetDate:dd/MM/yyyy}.");
                                        }
                                    }
                                    else
                                    {
                                        if (hrLvRstr.Days > lvBalItem.NextCycleRemaining)
                                        {
                                            return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds available balance ({lvBalItem.NextCycleRemaining:0} day(s)) for the new cycle.");
                                        }
                                    }
                                }
                                else if (hrLvRstr.Days > lvBalItem.Remaining)
                                {
                                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", $"Requested {lvName} ({hrLvRstr.Days} day(s)) exceeds your available balance ({lvBalItem.Remaining:0} day(s) remaining after active/in-progress applications).");
                                }
                            }
                        }

                        hrLvRstr.LvSdlId = (hrLvRstr.LvSdlId == null || hrLvRstr.LvSdlId == 0) ? null : hrLvRstr.LvSdlId;
                        hrLvRstr.CurrAvblDays = 0;
                        hrLvRstr.RecStat = 1;
                        hrLvRstr.CrtId = userEmpSysId;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrLvRstr, userEmpSysId);                       


                        //---------- UPdating the Attendance Process ----------//
                        //var obj = _attdContext.GetAttdProcDates(stD, endD);
                        //_attdContext.RunEmpAttendance("S", hrLvRstr.EmpSysId, obj.Start, obj.End);
                        _attdContext.LoadHrEmpAtndc("S", hrLvRstr.EmpSysId.GetValueOrDefault(), hrLvRstr.StDate, hrLvRstr.EndDate, AtndcLdPrcTyp.LV, "LV-Id" + hrLvRstr.LvId.ToString(), false, hrLvRstr.CrtId);

                        //----------------------------------------------------//

                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Register", "Record Updated Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Modifying Record got failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Leave Register", "Please select a valid Employee and Leave Type");
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrLvRegister/Delete
        [HttpGet]
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var lvRgtr = _leaveContext.GetHrLvRegisterByLvId(id) ?? _hrmContext.HrLvRegister.FirstOrDefault(x => x.LvId == id);
                if (lvRgtr == null)
                {
                    return Content("<div class='alert alert-danger m-3'>Leave record not found.</div>");
                }

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "LV_REG").FirstOrDefault();                
                ViewBag.RecDelete = objprv != null ? objprv.RecDelete : true;
                //End | Usr Prv//

                return PartialView("_HrLvRegister_Delete", lvRgtr);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger m-3'>Error loading leave record: " + ex.Message + "</div>");
            }
        }

        // POST: HrLvRegister/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(HrLvRegister hrLvRstr)
        {
            try
            {
                // 1. Clean up attached documents for this leave
                var regDocs = _hrmContext.HrLvRegDocs.Where(d => d.LvId == hrLvRstr.LvId).ToList();
                if (regDocs.Any())
                {
                    _hrmContext.HrLvRegDocs.RemoveRange(regDocs);
                    _hrmContext.SaveChanges();
                }

                // 2. Clean up any related workflow instances & tracking records for this leave
                var wfInstances = _hrmContext.HrLvWfInstance.Where(w => w.LvId == hrLvRstr.LvId).ToList();
                if (wfInstances.Any())
                {
                    _hrmContext.HrLvWfInstance.RemoveRange(wfInstances);
                    _hrmContext.SaveChanges();
                }

                // 3. Fetch the existing entity to ensure clean detachment and deletion
                var existingLeave = _leaveContext.GetHrLvRegisterByLvId(hrLvRstr.LvId);
                var leaveToDelete = existingLeave ?? hrLvRstr;

                int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int empSysId = leaveToDelete.EmpSysId ?? userEmpSysId;

                // 4. If this leave was approved, restore running balance
                if (_balanceService != null && (existingLeave?.WfStat == "Approved" || existingLeave?.RecStat == 2))
                {
                    await _balanceService.RestoreLeaveBalanceAsync(leaveToDelete.LvId, userEmpSysId, "Leave record deleted/cancelled");
                }

                _dbCommitContext.commitChanges(LogEventTypes.DEL, leaveToDelete, userEmpSysId);

                // 5. Updating the Attendance Process
                _attdContext.LoadHrEmpAtndc("S", empSysId, leaveToDelete.StDate, leaveToDelete.EndDate, AtndcLdPrcTyp.LV, "LV-Id" + leaveToDelete.LvId.ToString(), false, leaveToDelete.CrtId);

                return RedirectToAction(nameof(Index)).WithSuccess("Leave Register", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                var fullError = $"{ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : string.Empty)}";
                string userFriendlyMsg;

                if (fullError.Contains("FK_HR_LvRegDocs_HR_LvRegister", StringComparison.OrdinalIgnoreCase) ||
                    fullError.Contains("HR_LvRegDocs", StringComparison.OrdinalIgnoreCase))
                {
                    userFriendlyMsg = "Cannot delete this leave record because it has attached documents or medical certificates associated with it.";
                }
                else if (fullError.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) ||
                         fullError.Contains("conflicted with the REFERENCE constraint", StringComparison.OrdinalIgnoreCase))
                {
                    userFriendlyMsg = "Cannot delete this leave record because it is referenced by other system records or processes.";
                }
                else
                {
                    userFriendlyMsg = "An error occurred while deleting the leave record. Please try again or contact your administrator.";
                }

                return RedirectToAction(nameof(Index)).WithWarning("Leave Register", userFriendlyMsg);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "LvTypCode")] string value, [Bind(Prefix = "ex_LvTypCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _leaveContext.GetLeaveTypeByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _leaveContext.GetLeaveTypeByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }

        public async Task<IActionResult> ValidateDateRange(
            [Bind(Prefix = "StDate")] DateTime? stDate, 
            [Bind(Prefix = "EndDate")] DateTime? endDate, 
            [Bind(Prefix = "EmpSysId")] int? EmpSysId, 
            [Bind(Prefix = "LvId")] int? LvId,
            [Bind(Prefix = "LvTypId")] int? LvTypId)
        {
            if (!stDate.HasValue || !endDate.HasValue)
            {
                return Json(true);
            }

            if (endDate.Value.Date < stDate.Value.Date)
            {
                return Json("End Date cannot be earlier than Start Date.");
            }

            int targetEmpSysId = EmpSysId.GetValueOrDefault();
            if (targetEmpSysId <= 0)
            {
                targetEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
            }

            if (targetEmpSysId > 0)
            {
                int currentLvId = LvId.GetValueOrDefault();
                var overlapping = await _hrmContext.HrLvRegister
                    .Where(x => x.EmpSysId == targetEmpSysId
                             && x.LvId != currentLvId
                             && x.RecStat != 0 && x.RecStat != 3
                             && x.WfStat != "Rejected" && x.WfStat != "Cancelled"
                             && x.StDate.Date <= endDate.Value.Date
                             && x.EndDate.Date >= stDate.Value.Date)
                    .FirstOrDefaultAsync();

                if (overlapping != null)
                {
                    var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == overlapping.LvTypId);
                    string typeName = lvType?.NameEng ?? "Leave";
                    return Json($"You already have a leave ({typeName}: {overlapping.StDate:dd/MM/yyyy} to {overlapping.EndDate:dd/MM/yyyy}) within the selected date range.");
                }

                if (LvTypId.HasValue && LvTypId.Value > 0)
                {
                    var sandwichCheck = await _leaveContext.CheckSandwichLeavePolicyAsync(targetEmpSysId, LvTypId.Value, stDate.Value, endDate.Value, LvId);
                    if (sandwichCheck.isRestricted)
                    {
                        return Json(sandwichCheck.message);
                    }
                }
            }

            return Json(true);
        }

        // GET: HrLvRegister/CheckLeaveOverlap
        [HttpGet]
        public async Task<IActionResult> CheckLeaveOverlap(int empSysId, DateTime stDate, DateTime endDate, int? lvId = null, int? lvTypId = null)
        {
            try
            {
                if (empSysId <= 0 || stDate == default || endDate == default)
                    return Json(new { hasOverlap = false });

                int currentLvId = lvId.GetValueOrDefault();
                var overlapping = await _hrmContext.HrLvRegister
                    .Where(x => x.EmpSysId == empSysId
                             && x.LvId != currentLvId
                             && x.RecStat != 0 && x.RecStat != 3
                             && x.WfStat != "Rejected" && x.WfStat != "Cancelled"
                             && x.StDate.Date <= endDate.Date
                             && x.EndDate.Date >= stDate.Date)
                    .FirstOrDefaultAsync();

                if (overlapping != null)
                {
                    var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == overlapping.LvTypId);
                    string typeName = lvType?.NameEng ?? "Leave";
                    return Json(new
                    {
                        hasOverlap = true,
                        existingType = typeName,
                        startDate = overlapping.StDate.ToString("dd/MM/yyyy"),
                        endDate = overlapping.EndDate.ToString("dd/MM/yyyy"),
                        message = $"You already have a leave ({typeName}: {overlapping.StDate:dd/MM/yyyy} to {overlapping.EndDate:dd/MM/yyyy}) within the selected date range."
                    });
                }

                // Sandwich Leave Policy Check
                if (lvTypId.HasValue && lvTypId.Value > 0)
                {
                    var sandwichCheck = await _leaveContext.CheckSandwichLeavePolicyAsync(empSysId, lvTypId.Value, stDate, endDate, lvId);
                    if (sandwichCheck.isRestricted)
                    {
                        return Json(new
                        {
                            hasOverlap = true,
                            isSandwich = true,
                            message = sandwichCheck.message
                        });
                    }
                }

                return Json(new { hasOverlap = false });
            }
            catch (Exception ex)
            {
                return Json(new { hasOverlap = false, error = ex.Message });
            }
        }

        // =====================================================================
        // POST: HrLvRegister/SubmitToWorkflow
        // Called via AJAX from the Leave Register grid.
        // Validates the leave record, starts the workflow engine process,
        // and returns JSON result to update the UI without a full page reload.
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitToWorkflow(int lvId)
        {
            try
            {
                // Privilege check
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { success = false, message = "Session expired. Please log in again." });

                // Load the leave record
                var lvRec = _leaveContext.GetHrLvRegisterByLvId(lvId);
                if (lvRec == null)
                    return Json(new { success = false, message = "Leave record not found." });

                // Prevent re-submission
                if (!string.IsNullOrEmpty(lvRec.WfStat) && !string.Equals(lvRec.WfStat, "Draft", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = $"This leave is already in the workflow with status: {lvRec.WfStat}" });

                int submitterEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                if (submitterEmpSysId <= 0 && lvRec.EmpSysId.HasValue)
                {
                    submitterEmpSysId = lvRec.EmpSysId.Value;
                }

                // Start the workflow
                var wfInstance = await _lvWfService.StartLeaveWorkflowAsync(lvRec, submitterEmpSysId);

                return Json(new
                {
                    success      = true,
                    instanceNo   = wfInstance.WfInstanceNo,
                    message      = $"Leave application submitted to workflow. Reference: {wfInstance.WfInstanceNo}"
                });
            }
            catch (Exception ex)
            {
                // Return full exception (including inner exceptions) to aid debugging in local/dev environment.
                // NOTE: Remove or reduce verbosity before deploying to production.
                return Json(new { success = false, message = ex.ToString() });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/Review?openLvId=6057&instanceId={guid}
        // Dedicated review page for a single leave incident
        // =====================================================================
        public async Task<IActionResult> Review(int? openLvId, Guid? instanceId, int? lvId)
        {
            if (HttpContext.Session.GetString("LV_REG") == null)
            {
                var returnUrl = Request.Path + Request.QueryString;
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId)
                                    .FirstOrDefault(x => x.PrvCode == "LV_REG");
            if (objprv != null)
            {
                ViewBag.RecAdd    = objprv.RecAdd;
                ViewBag.RecEdit   = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
            }

            int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
            int targetLvId = openLvId ?? lvId ?? 0;

            if (targetLvId <= 0 && instanceId.HasValue && instanceId.Value != Guid.Empty)
            {
                var inst = await _hrmContext.HrLvWfInstance.FirstOrDefaultAsync(i => i.WfInstanceId == instanceId.Value);
                if (inst != null && inst.LvId > 0)
                {
                    targetLvId = inst.LvId;
                }
            }

            var incident = await _lvWfService.GetLeaveIncidentForReviewAsync(targetLvId, empSysId);
            return View(incident);
        }

        // =====================================================================
        // GET: HrLvRegister/GetReviewIncident?lvId=6057
        // JSON endpoint returning single incident details for review
        // =====================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetReviewIncident(int lvId)
        {
            try
            {
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { success = false, message = "Session expired." });

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var incident = await _lvWfService.GetLeaveIncidentForReviewAsync(lvId, empSysId);
                return Json(incident);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/Pending
        // Approver inbox: shows all leave applications awaiting the current
        // user's approval action (fetched from the Workflow Engine).
        // =====================================================================
        public IActionResult Pending()
        {
            if (HttpContext.Session.GetString("LV_REG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId)
                                    .FirstOrDefault(x => x.PrvCode == "LV_REG");
            if (objprv != null)
            {
                ViewBag.RecAdd    = objprv.RecAdd;
                ViewBag.RecEdit   = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
            }

            return View();
        }

        // =====================================================================
        // GET: HrLvRegister/GetMyPendingApprovals
        // Returns JSON list of leave applications awaiting the current user's
        // approval, resolved by mapping EmpSysId → Workflow Engine User GUID.
        // =====================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetMyPendingApprovals()
        {
            try
            {
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { error = "Session expired." });

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var pending  = await _lvWfService.GetPendingApprovalsForUserAsync(empSysId);
                return Json(pending);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetPendingInboxSummary
        // Lightweight endpoint that returns the two metric-card counts for the
        // Pending Approvals inbox page header (total system pending + my queue).
        // =====================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetPendingInboxSummary()
        {
            try
            {
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { totalPending = 0, awaitingMyAction = 0 });

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var pendingItems = await _lvWfService.GetPendingApprovalsForUserAsync(empSysId);
                int total    = pendingItems.Count;
                int myAction = pendingItems.Count;
                return Json(new { totalPending = total, awaitingMyAction = myAction });
            }
            catch (Exception ex)
            {
                return Json(new { totalPending = 0, awaitingMyAction = 0, error = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetWfHistory?wfInstanceId={guid}
        // Returns the workflow approval history for a given instance.
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetWfHistory(Guid wfInstanceId)
        {
            try
            {
                var history = await _lvWfService.GetWorkflowHistoryAsync(wfInstanceId);
                return Json(history);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/TimelineGet?lvId={int}
        // Returns the HTML Partial View for the Leave Approval Timeline modal.
        // =====================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> TimelineGet(int lvId)
        {
            try
            {
                var timeline = await _lvWfService.GetLeaveApprovalTimelineAsync(lvId);
                return PartialView("_HrLvRegister_Timeline", timeline);
            }
            catch (Exception ex)
            {
                var errModel = new LeaveApprovalTimelineViewModel
                {
                    Success = false,
                    Message = "Error loading timeline: " + ex.Message
                };
                return PartialView("_HrLvRegister_Timeline", errModel);
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetWfHistoryByLvId?lvId={int}
        // Returns the workflow instance status, steps and history for a leave application as JSON.
        // =====================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetWfHistoryByLvId(int lvId)
        {
            try
            {
                var timeline = await _lvWfService.GetLeaveApprovalTimelineAsync(lvId);
                return Json(timeline);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // POST: HrLvRegister/ApproveStep
        // Approver approves a specific workflow step.
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStep([FromBody] ApproverActionRequest req)
        {
            try
            {
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { success = false, message = "Session expired." });

                await _lvWfService.ApproveStepAsync(req.ApprovalId, req.Comment);
                return Json(new { success = true, message = "Approval recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // POST: HrLvRegister/RejectStep
        // Approver rejects a specific workflow step.
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStep([FromBody] ApproverActionRequest req)
        {
            try
            {
                if (HttpContext.Session.GetString("LV_REG") == null)
                    return Json(new { success = false, message = "Session expired." });

                await _lvWfService.RejectStepAsync(req.ApprovalId, req.Comment);
                return Json(new { success = true, message = "Rejection recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetAvailableBalance
        // Evaluates balance dynamically as of requested start date / today
        // =====================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableBalance(int empSysId, int lvTypId, int? year, DateTime? stDate, DateTime? asOfDate)
        {
            try
            {
                DateTime evalDate = asOfDate ?? (stDate ?? DateTime.Today);
                int yr = year.HasValue && year.Value > 0 ? year.Value : evalDate.Year;
                decimal avbl = 0;
                decimal ent = 0;
                decimal used = 0;
                decimal inProgress = 0;
                decimal accruedDays = 0;
                decimal annualQuota = 0;
                decimal openingDays = 0;
                decimal accrualRatePerMonth = 0;
                bool isAccrualBased = false;
                DateTime? servicePeriodStart = null;
                DateTime? servicePeriodEnd = null;
                DateTime? nextResetDate = null;
                DateTime? currentCycleResetDate = null;
                decimal currentCycleRemaining = 0;
                decimal nextCycleRemaining = 0;
                string formulaText = "";
                string ruleNotice = "";
                bool hasCompletedOneYear = true;
                DateTime? oneYearCompletionDate = null;
                bool isServiceRestricted = false;
                string restrictionReason = "";

                if (_balanceService != null)
                {
                    var balances = await _balanceService.GetEmployeeLeaveBalancesAsync(empSysId, yr, evalDate);
                    var item = balances.FirstOrDefault(x => x.LvTypId == lvTypId);
                    if (item != null)
                    {
                        ent = item.Entitlement;
                        used = item.Used;
                        inProgress = item.InProgress;
                        avbl = item.Remaining;
                        accruedDays = item.AccruedDays;
                        annualQuota = item.AnnualQuota;
                        openingDays = item.OpeningDays;
                        isAccrualBased = item.IsAccrualBased;
                        accrualRatePerMonth = item.AccrualRatePerMonth;
                        servicePeriodStart = item.ServicePeriodStart;
                        servicePeriodEnd = item.ServicePeriodEnd;
                        nextResetDate = item.NextResetDate;
                        currentCycleResetDate = item.CurrentCycleResetDate;
                        currentCycleRemaining = item.CurrentCycleRemaining;
                        nextCycleRemaining = item.NextCycleRemaining;
                        formulaText = item.AccrualFormulaText;
                        ruleNotice = item.RuleNotice;
                        hasCompletedOneYear = item.HasCompletedOneYear;
                        oneYearCompletionDate = item.OneYearCompletionDate;
                        isServiceRestricted = item.IsServiceRestricted;
                        restrictionReason = item.RestrictionReason;
                    }
                }

                var lvType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == lvTypId);
                string code = lvType?.LvTypCode ?? "";
                string name = lvType?.NameEng ?? "";

                bool isSick = code == "LV_SICK" || name.Contains("Sick", StringComparison.OrdinalIgnoreCase);
                bool isCasual = code == "LV_CSL" || name.Contains("Casual", StringComparison.OrdinalIgnoreCase);
                bool isEmergency = code == "LV_EMG" || name.Contains("Emergency", StringComparison.OrdinalIgnoreCase);
                bool isHajj = code == "LV_HAJJ" || code == "LV_HAJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase);
                bool isMaternity = code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase);
                bool isAnnual = code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase);

                var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
                bool isOutsource = emp != null && (emp.WrktypId == 2 || emp.GrpId == 7 || _empContext.GetOutsourceEmployeeSysIds().Contains(emp.EmpSysId));

                return Json(new
                {
                    success = true,
                    availableBalance = avbl,
                    entitlement = ent,
                    used = used,
                    inProgress = inProgress,
                    remaining = avbl,
                    accruedDays = accruedDays,
                    annualQuota = annualQuota,
                    openingDays = openingDays,
                    isAccrualBased = isAccrualBased,
                    accrualRatePerMonth = accrualRatePerMonth,
                    servicePeriodStart = servicePeriodStart?.ToString("dd/MM/yyyy"),
                    servicePeriodEnd = servicePeriodEnd?.ToString("dd/MM/yyyy"),
                    nextResetDate = nextResetDate?.ToString("dd/MM/yyyy"),
                    nextResetDateIso = nextResetDate?.ToString("yyyy-MM-dd"),
                    currentCycleResetDate = currentCycleResetDate?.ToString("dd/MM/yyyy"),
                    currentCycleResetDateIso = currentCycleResetDate?.ToString("yyyy-MM-dd"),
                    currentCycleRemaining = currentCycleRemaining,
                    nextCycleRemaining = nextCycleRemaining,
                    accrualFormulaText = formulaText,
                    ruleNotice = ruleNotice,
                    hasCompletedOneYear,
                    oneYearCompletionDate = oneYearCompletionDate?.ToString("dd/MM/yyyy"),
                    isServiceRestricted,
                    restrictionReason,
                    isSick,
                    isCasual,
                    isEmergency,
                    isHajj,
                    isMaternity,
                    isAnnual,
                    isOutsource,
                    sickNotice = isSick ? "Notice: First 14 days are fully paid, followed by 28 days at half pay." : ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetEmployeeBalances
        // =====================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEmployeeBalances(int empSysId, int? year, DateTime? asOfDate)
        {
            try
            {
                DateTime evalDate = asOfDate ?? DateTime.Today;
                int yr = year.HasValue && year.Value > 0 ? year.Value : evalDate.Year;
                int sessionEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                int sessionGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(sessionEmpSysId, sessionGrpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(sessionEmpSysId, sessionGrpId);
                bool showAll = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(sessionEmpSysId, sessionGrpId);
                var allowedIds = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(sessionEmpSysId).ToHashSet();
                if (sessionEmpSysId > 0 && !isSelfOnlyUser) allowedIds.Add(sessionEmpSysId);

                if (isSelfOnlyUser)
                {
                    empSysId = sessionEmpSysId;
                }
                else if (!showAll && !isOutsourceAdmin && sessionEmpSysId > 0)
                {
                    if (allowedIds.Count > 1 && empSysId > 0 && allowedIds.Contains(empSysId))
                    {
                        // Subordinate balance access allowed
                    }
                    else
                    {
                        empSysId = sessionEmpSysId;
                    }
                }
                else if (isOutsourceAdmin && empSysId > 0)
                {
                    var osEmpIds = _empContext.GetOutsourceEmployeeSysIds(sessionEmpSysId);
                    if (!osEmpIds.Contains(empSysId))
                    {
                        empSysId = sessionEmpSysId;
                    }
                }
                else if (empSysId <= 0)
                {
                    empSysId = sessionEmpSysId;
                }

                if (_balanceService != null)
                {
                    var summary = await _balanceService.GetEmployeeBalanceSummaryAsync(empSysId, yr, evalDate);
                    return Json(new { success = true, data = summary });
                }
                return Json(new { success = false, message = "Balance service not available." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetBalanceAudit
        // =====================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBalanceAudit(int empSysId, int? lvTypId, int? year)
        {
            try
            {
                int yr = year.HasValue && year.Value > 0 ? year.Value : DateTime.Now.Year;
                int sessionEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                int sessionGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(sessionEmpSysId, sessionGrpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(sessionEmpSysId, sessionGrpId);
                bool showAll = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(sessionEmpSysId, sessionGrpId);
                var allowedIds = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(sessionEmpSysId).ToHashSet();
                if (sessionEmpSysId > 0 && !isSelfOnlyUser) allowedIds.Add(sessionEmpSysId);

                if (isSelfOnlyUser)
                {
                    empSysId = sessionEmpSysId;
                }
                else if (!showAll && !isOutsourceAdmin && sessionEmpSysId > 0)
                {
                    if (allowedIds.Count > 1 && empSysId > 0 && allowedIds.Contains(empSysId))
                    {
                        // Subordinate audit history access allowed
                    }
                    else
                    {
                        empSysId = sessionEmpSysId;
                    }
                }
                else if (isOutsourceAdmin && empSysId > 0)
                {
                    var osEmpIds = _empContext.GetOutsourceEmployeeSysIds(sessionEmpSysId);
                    if (!osEmpIds.Contains(empSysId))
                    {
                        empSysId = sessionEmpSysId;
                    }
                }
                else if (empSysId <= 0)
                {
                    empSysId = sessionEmpSysId;
                }

                if (_balanceService != null)
                {
                    var history = await _balanceService.GetAuditHistoryAsync(empSysId, lvTypId, yr);
                    return Json(new { success = true, data = history });
                }
                return Json(new { success = false, message = "Balance service not available." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // POST: HrLvRegister/SyncRunningBalances
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> SyncRunningBalances(int? year)
        {
            try
            {
                int yr = year.HasValue && year.Value > 0 ? year.Value : DateTime.Now.Year;
                if (_balanceService != null)
                {
                    int count = await _balanceService.SyncAllRunningBalancesAsync(yr);
                    return Json(new { success = true, message = $"Running balances synchronized successfully ({count} records updated/verified)." });
                }
                return Json(new { success = false, message = "Balance service not available." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetActiveEmployees
        // =====================================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetActiveEmployees()
        {
            try
            {
                int entId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));
                int sessionEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                int sessionGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                var emps = _empContext.GetActvEmpNameLst(entId > 0 ? entId : 1, sessionEmpSysId, sessionGrpId);
                return Json(new { success = true, data = emps });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetEmployeeEligibleLeaveTypes?empSysId={empSysId}
        // Returns leave types eligible for the employee based on Gender & Religion
        // =====================================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEmployeeEligibleLeaveTypes(int empSysId)
        {
            try
            {
                int entId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));
                if (entId <= 0) entId = 1;

                var allLeaveTypes = _leaveContext.GetAllLeaveTypeViewsByEntId(entId).ToList();

                var emp = await _hrmContext.HrEmpGenInfo
                    .Include(e => e.Rel)
                    .FirstOrDefaultAsync(e => e.EmpSysId == empSysId);

                bool isFemale = false;
                bool isMuslim = false;

                if (emp != null)
                {
                    isFemale = string.Equals(emp.Gender, "Female", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(emp.Gender, "F", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(emp.Title, "Ms.", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(emp.Title, "Mrs.", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(emp.Title, "Miss", StringComparison.OrdinalIgnoreCase);

                    if (emp.RelId.HasValue)
                    {
                        var rel = emp.Rel ?? await _hrmContext.OrgReligion.FirstOrDefaultAsync(r => r.RelId == emp.RelId.Value);
                        if (rel != null)
                        {
                            isMuslim = rel.RelNameEng.Equals("Islam", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                bool hasCompletedOneYear = true;
                DateTime? oneYearCompletionDate = null;
                if (emp != null)
                {
                    oneYearCompletionDate = emp.JoinedDate.Date.AddYears(1);
                    hasCompletedOneYear = DateTime.Today >= oneYearCompletionDate.Value;
                }

                var filtered = allLeaveTypes.Where(lt =>
                {
                    string code = lt.LvTypCode ?? "";
                    string name = lt.NameEng ?? "";

                    bool isMaternity = code == "LV_MTN" || code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase);
                    if (isMaternity && !isFemale) return false;

                    bool isHajji = code == "LV_HAJ" || code == "LV_HAJJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase) || name.Contains("HAJJI", StringComparison.OrdinalIgnoreCase);
                    if (isHajji && !isMuslim) return false;

                    return true;
                }).Select(lt =>
                {
                    string code = lt.LvTypCode ?? "";
                    string name = lt.NameEng ?? "";
                    bool isAnnual = code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase);
                    bool isRestricted = isAnnual && !hasCompletedOneYear;

                    return new
                    {
                        lt.LvTypId,
                        lt.LvTypCode,
                        lt.NameEng,
                        lt.NameArb,
                        lt.Desc,
                        lt.ElgDays,
                        lt.MaxCntDays,
                        isAnnual,
                        isRestricted,
                        restrictionReason = isRestricted ? $"Requires 1 year of service (Eligible: {oneYearCompletionDate:dd/MM/yyyy})" : ""
                    };
                }).ToList();

                return Json(new { 
                    success = true, 
                    data = filtered, 
                    isFemale = isFemale, 
                    isMuslim = isMuslim,
                    hasCompletedOneYear = hasCompletedOneYear,
                    oneYearCompletionDate = oneYearCompletionDate?.ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: HrLvRegister/GetDocument?id={docId}&lvId={lvId}
        // Streams the attachment binary data from HrLvRegDocs or disk
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetDocument(int? id, int? lvId)
        {
            try
            {
                HrLvRegDocs doc = null;
                if (id.HasValue && id.Value > 0)
                {
                    doc = await _hrmContext.HrLvRegDocs.FirstOrDefaultAsync(d => d.DocId == id.Value);
                }
                else if (lvId.HasValue && lvId.Value > 0)
                {
                    doc = await _hrmContext.HrLvRegDocs.OrderByDescending(d => d.DocId).FirstOrDefaultAsync(d => d.LvId == lvId.Value);
                }

                if (doc != null && doc.ScnCopy != null && doc.ScnCopy.Length > 0)
                {
                    string mime = !string.IsNullOrEmpty(doc.DocMime) ? doc.DocMime : "application/pdf";
                    string fileName = !string.IsNullOrEmpty(doc.DocName) ? doc.DocName : $"leave_document_{doc.LvId}.pdf";
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(doc.ScnCopy, mime);
                }

                // If not in database, check physical attach directories
                if (lvId.HasValue && lvId.Value > 0)
                {
                    var lvRec = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == lvId.Value);
                    if (lvRec != null && !string.IsNullOrEmpty(lvRec.Note))
                    {
                        var parsed = LeaveWorkflowService.ParseNoteAndAttachment(lvRec.Note);
                        if (parsed.hasAttachment && !string.IsNullOrEmpty(parsed.attachmentFileName))
                        {
                            var fPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "attach", "medCert", parsed.attachmentFileName);
                            if (System.IO.File.Exists(fPath))
                            {
                                var bytes = await System.IO.File.ReadAllBytesAsync(fPath);
                                var ext = System.IO.Path.GetExtension(parsed.attachmentFileName).ToLowerInvariant();
                                var mime = ext == ".pdf" ? "application/pdf" : (new[] { ".png", ".jpg", ".jpeg" }.Contains(ext) ? "image/jpeg" : "application/octet-stream");
                                Response.Headers["Content-Disposition"] = $"inline; filename=\"{parsed.attachmentFileName}\"";
                                return File(bytes, mime);
                            }
                        }
                    }

                    var medCertDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "attach", "medCert");
                    if (System.IO.Directory.Exists(medCertDir))
                    {
                        var files = System.IO.Directory.GetFiles(medCertDir);
                        if (files.Length > 0)
                        {
                            var matched = files.OrderByDescending(System.IO.File.GetCreationTime).FirstOrDefault();
                            if (matched != null)
                            {
                                var bytes = await System.IO.File.ReadAllBytesAsync(matched);
                                var fName = System.IO.Path.GetFileName(matched);
                                var ext = System.IO.Path.GetExtension(fName).ToLowerInvariant();
                                var mime = ext == ".pdf" ? "application/pdf" : (ext == ".png" ? "image/png" : "image/jpeg");
                                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fName}\"";
                                return File(bytes, mime);
                            }
                        }
                    }
                }

                return NotFound("Document attachment not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Error retrieving document: " + ex.Message);
            }
        }
    }

    /// <summary>Request body for approve/reject actions from the Pending view.</summary>
    public class ApproverActionRequest
    {
        public Guid   ApprovalId { get; set; }
        public string Comment    { get; set; }
    }
}
