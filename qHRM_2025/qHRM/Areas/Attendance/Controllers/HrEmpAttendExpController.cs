using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAttendExpController : Controller
    {
        private readonly AttnContext _attnContext;
        private readonly DbCommitContext<HrEmpAttendExp> _dbCommitContext;
        private readonly EmployeeContext _empContext;
        private readonly EntityContext _entityContext;
        private readonly qHRMasterContext _hrmContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly AttendanceRegularizationService _regService;
        private readonly IWebHostEnvironment _env;

        public HrEmpAttendExpController(
            AttnContext attnContext,
            DbCommitContext<HrEmpAttendExp> dbCommitContext,
            EmployeeContext empContext,
            EntityContext entityContext,
            qHRMasterContext hrmContext,
            AdmUserGrpPrvContext prvContext,
            AttendanceRegularizationService regService,
            IWebHostEnvironment env)
        {
            _attnContext = attnContext;
            _dbCommitContext = dbCommitContext;
            _empContext = empContext;
            _entityContext = entityContext;
            _hrmContext = hrmContext;
            _prvContext = prvContext;
            _regService = regService;
            _env = env;
        }

        // =====================================================================
        // GET: Attendance/HrEmpAttendExp (Attendance Regularization Hub)
        // =====================================================================
        public async Task<IActionResult> Index(int? empSysId, string fromDate, string toDate, string viewTab, string tab = null, string tdyFltr = null)
        {
            try
            {
                if (HttpContext.Session.GetString("ATN_EXP") == null)
                    return RedirectToPage("/Account/Login", new { area = "Identity" });

                string effectiveTab = !string.IsNullOrWhiteSpace(tab) ? tab : (!string.IsNullOrWhiteSpace(viewTab) ? viewTab : "summary");
                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                int entId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));

                // Access Control
                var (canViewAll, canViewSubordinates, allowedIds) = await _regService.GetAccessRightsAsync(currEmpSysId, userGrpId);

                int targetEmpSysId = currEmpSysId;
                if (empSysId.HasValue && empSysId.Value > 0)
                {
                    if (canViewAll || allowedIds.Contains(empSysId.Value))
                    {
                        targetEmpSysId = empSysId.Value;
                    }
                }

                // Date Range resolution (Default: Current week Sunday - Saturday)
                DateTime startDt;
                DateTime endDt;
                DateTime today = DateTime.Today;

                string[] dateFormats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MMM-yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fffZ" };

                if (tdyFltr == "1")
                {
                    startDt = today;
                    endDt = today;
                }
                else if (!string.IsNullOrWhiteSpace(fromDate) && (DateTime.TryParseExact(fromDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom) || DateTime.TryParse(fromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedFrom) || DateTime.TryParse(fromDate, out parsedFrom)))
                {
                    startDt = parsedFrom.Date;
                }
                else
                {
                    // Sunday as week start
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    startDt = today.AddDays(-diff).Date;
                }

                if (!string.IsNullOrWhiteSpace(toDate) && (DateTime.TryParseExact(toDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo) || DateTime.TryParse(toDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedTo) || DateTime.TryParse(toDate, out parsedTo)))
                {
                    endDt = parsedTo.Date;
                }
                else
                {
                    endDt = startDt.AddDays(6).Date; // Saturday
                }

                // Fetch Data
                var timeline = await _regService.GetAttendanceTimelineAsync(targetEmpSysId, startDt, endDt);
                var allRequests = await _regService.GetRegularizationRequestsAsync(currEmpSysId, userGrpId, canViewAll ? empSysId : targetEmpSysId);
                var pendingApprovals = await _regService.GetPendingApprovalsAsync(currEmpSysId, userGrpId);
                var config = await _regService.GetConfigAsync();

                // Get target employee metadata
                var targetEmp = await (from e in _hrmContext.HrEmpGenInfo
                                       join j in _hrmContext.HrEmpJobAssign on e.EmpSysId equals j.EmpSysId into jg
                                       from j in jg.Where(x => x.WkStat == 1).DefaultIfEmpty()
                                       join d in _hrmContext.OrgDepartment on j.DeptId equals d.DeptId into dg
                                       from d in dg.DefaultIfEmpty()
                                       join ds in _hrmContext.OrgDesignation on j.DesigId equals ds.DesigId into dsg
                                       from ds in dsg.DefaultIfEmpty()
                                       where e.EmpSysId == targetEmpSysId
                                       select new
                                       {
                                           e.EmpSysId,
                                           e.EmpId,
                                           FullName = ((e.FirstName ?? "").Trim() + " " + (e.LastName ?? "").Trim()).Trim(),
                                           DeptName = d != null ? d.NameEng : "-",
                                           DesigName = ds != null ? ds.NameEng : "-"
                                       }).FirstOrDefaultAsync();

                // Employee dropdown list based on role
                var allActiveEmps = _empContext.GetActvEmpNameLst(entId).ToList();
                if (!canViewAll)
                {
                    if (canViewSubordinates)
                    {
                        allActiveEmps = allActiveEmps.Where(e => allowedIds.Contains(e.ESysId)).ToList();
                    }
                    else
                    {
                        allActiveEmps = allActiveEmps.Where(e => e.ESysId == currEmpSysId).ToList();
                    }
                }

                // Privileges
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).FirstOrDefault(x => x.PrvCode == "ATN_EXP");
                ViewBag.RecAdd = objprv?.RecAdd ?? true;
                ViewBag.RecEdit = objprv?.RecEdit ?? true;
                ViewBag.RecDelete = objprv?.RecDelete ?? true;

                ViewBag.Timeline = timeline;
                ViewBag.RegularizationRequests = allRequests;
                ViewBag.PendingApprovals = pendingApprovals;
                ViewBag.Config = config;
                ViewBag.EmployeeList = allActiveEmps;
                ViewBag.CurrentEmpSysId = currEmpSysId;
                ViewBag.UserGrpId = userGrpId;
                ViewBag.CanViewAll = canViewAll;
                ViewBag.CanViewSubordinates = canViewSubordinates;
                ViewBag.TargetEmpSysId = targetEmpSysId;
                ViewBag.TargetEmpName = targetEmp != null ? $"{targetEmp.EmpId} - {targetEmp.FullName}" : "Selected Employee";
                ViewBag.TargetEmpCode = targetEmp?.EmpId ?? "";
                ViewBag.TargetFullName = targetEmp?.FullName ?? "";
                ViewBag.TargetDept = targetEmp?.DeptName ?? "-";
                ViewBag.TargetDesig = targetEmp?.DesigName ?? "-";
                ViewBag.FromDate = startDt.ToString("dd-MMM-yyyy");
                ViewBag.ToDate = endDt.ToString("dd-MMM-yyyy");
                ViewBag.ViewTab = effectiveTab;

                // Backward-compatibility
                ViewBag.DataSource = allRequests;

                return View();
            }
            catch (Exception ex)
            {
                return View().WithWarning("Attendance Regularization", ex.Message);
            }
        }

        // =====================================================================
        // AJAX JSON ENDPOINTS
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> GetTimelineData(int empSysId, string fromDate, string toDate)
        {
            try
            {
                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                var (canViewAll, canViewSubordinates, allowedIds) = await _regService.GetAccessRightsAsync(currEmpSysId, userGrpId);
                int targetId = empSysId;
                if (!canViewAll && !allowedIds.Contains(targetId))
                {
                    targetId = currEmpSysId;
                }

                if (!DateTime.TryParse(fromDate, out var startDt))
                {
                    int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    startDt = DateTime.Today.AddDays(-diff).Date;
                }
                if (!DateTime.TryParse(toDate, out var endDt))
                {
                    endDt = startDt.AddDays(6).Date;
                }

                var timeline = await _regService.GetAttendanceTimelineAsync(targetId, startDt, endDt);
                return Json(new
                {
                    success = true,
                    data = timeline,
                    fromDateStr = startDt.ToString("dd-MMM-yyyy"),
                    toDateStr = endDt.ToString("dd-MMM-yyyy"),
                    headerRange = $"{startDt:dd-MMM-yyyy} - {endDt:dd-MMM-yyyy}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicableDates(int empSysId, string periodType, string targetDate, string customEndDate)
        {
            try
            {
                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                var (canViewAll, canViewSubordinates, allowedIds) = await _regService.GetAccessRightsAsync(currEmpSysId, userGrpId);
                int targetId = empSysId > 0 ? empSysId : currEmpSysId;
                if (!canViewAll && !allowedIds.Contains(targetId))
                {
                    targetId = currEmpSysId;
                }

                DateTime tDate = DateTime.Today;
                string[] dateFormats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MMM-yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fffZ" };
                if (!string.IsNullOrWhiteSpace(targetDate))
                {
                    if (!DateTime.TryParseExact(targetDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out tDate)
                        && !DateTime.TryParse(targetDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out tDate)
                        && !DateTime.TryParse(targetDate, out tDate))
                    {
                        tDate = DateTime.Today;
                    }
                }

                DateTime? cEndDate = null;
                if (!string.IsNullOrWhiteSpace(customEndDate))
                {
                    if (DateTime.TryParseExact(customEndDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pEndDate)
                        || DateTime.TryParse(customEndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out pEndDate)
                        || DateTime.TryParse(customEndDate, out pEndDate))
                    {
                        cEndDate = pEndDate;
                    }
                }

                var config = await _regService.GetConfigAsync();
                DateTime today = DateTime.Today;
                DateTime yesterday = today.AddDays(-1);

                if (periodType == "Day" || string.IsNullOrEmpty(periodType))
                {
                    if (tDate >= today)
                    {
                        return Json(new { success = false, message = "Today's attendance cannot be regularized until the shift is completed. You can regularize attendance up to yesterday.", dates = new List<object>() });
                    }
                    if (tDate.DayOfWeek == DayOfWeek.Friday || tDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        return Json(new { success = false, message = $"Regularization is not applicable for weekends (Friday and Saturday). {tDate:dd-MMM-yyyy} is a weekend.", dates = new List<object>() });
                    }
                }

                if (periodType == "Custom")
                {
                    DateTime start = tDate.Date;
                    DateTime end = (cEndDate ?? tDate.AddDays(4)).Date;
                    if (end > yesterday)
                    {
                        end = yesterday;
                        cEndDate = yesterday;
                    }
                    if (start > yesterday)
                    {
                        start = yesterday;
                        tDate = yesterday;
                    }

                    int workingDaysCount = AttendanceRegularizationService.CountWorkingDays(start, end);
                    if (workingDaysCount > config.MaxDaysAllowed)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"You cannot regularize more than {config.MaxDaysAllowed} working days entries. Selected range has {workingDaysCount} working days.",
                            dates = new List<object>()
                        });
                    }
                }

                var dates = await _regService.GetApplicableDatesForRegularizationAsync(targetId, periodType ?? "Day", tDate, cEndDate);
                return Json(new { success = true, dates = dates });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SubmitRegularizationRequestDto
        {
            public int EmpSysId { get; set; }
            public string PeriodType { get; set; }
            public string ItemsJson { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRegularization([FromForm] SubmitRegularizationRequestDto dto, IFormFile attachment)
        {
            try
            {
                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                var (canViewAll, canViewSubordinates, allowedIds) = await _regService.GetAccessRightsAsync(currEmpSysId, userGrpId);
                int targetEmpSysId = dto.EmpSysId > 0 ? dto.EmpSysId : currEmpSysId;
                if (!canViewAll && !allowedIds.Contains(targetEmpSysId))
                {
                    targetEmpSysId = currEmpSysId;
                }

                if (string.IsNullOrWhiteSpace(dto.ItemsJson))
                {
                    return Json(new { success = false, message = "Please select at least one working day to regularize." });
                }

                var items = JsonSerializer.Deserialize<List<AttendanceRegularizationService.RegularizationSubmitItemDto>>(dto.ItemsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (items == null || !items.Any())
                {
                    return Json(new { success = false, message = "No valid dates selected." });
                }

                var config = await _regService.GetConfigAsync();
                DateTime today = DateTime.Today;

                if (items.Any(i => i.Date.Date >= today))
                {
                    return Json(new { success = false, message = "Today's attendance cannot be regularized until the shift is completed." });
                }

                if (items.Count > config.MaxDaysAllowed)
                {
                    return Json(new { success = false, message = $"You cannot regularize more than {config.MaxDaysAllowed} working days entries." });
                }

                string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var result = await _regService.SubmitRegularizationAsync(targetEmpSysId, dto.PeriodType, items, attachment, webRoot, currEmpSysId);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    reqNos = result.GeneratedReqNos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Submission failed: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int regId, string comments)
        {
            try
            {
                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId && r.RecStat == 1);
                if (reg == null)
                {
                    return Json(new { success = false, message = "Regularization request not found." });
                }

                bool isAdmin = (userGrpId == 1 || userGrpId == 2 || userGrpId == 7);

                if (reg.WfStatus == "PendingManager")
                {
                    if (!isAdmin && reg.ManagerEmpId != currEmpSysId)
                    {
                        return Json(new { success = false, message = "You are not authorized to approve this request at the Reporting Manager level." });
                    }
                    var res = await _regService.ApproveByReportingManagerAsync(regId, currEmpSysId, comments);
                    return Json(new { success = res.Success, message = res.Message });
                }
                else if (reg.WfStatus == "PendingHR")
                {
                    if (!isAdmin && reg.HrEmpId != currEmpSysId && currEmpSysId != 6)
                    {
                        return Json(new { success = false, message = "You are not authorized to grant Final HR Approval." });
                    }
                    var res = await _regService.ApproveByHrAsync(regId, currEmpSysId, comments);
                    return Json(new { success = res.Success, message = res.Message });
                }
                else
                {
                    return Json(new { success = false, message = $"Request is in status '{reg.WfStatus}' and cannot be approved." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Approval failed: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int regId, string comments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comments))
                {
                    return Json(new { success = false, message = "Please provide a reason/comment for rejection." });
                }

                int currEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId && r.RecStat == 1);
                if (reg == null)
                {
                    return Json(new { success = false, message = "Regularization request not found." });
                }

                bool isManagerStage = (reg.WfStatus == "PendingManager");
                var res = await _regService.RejectRegularizationAsync(regId, currEmpSysId, comments, isManagerStage);
                return Json(new { success = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Rejection failed: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkflowHistory(int regId)
        {
            try
            {
                var history = await _regService.GetWorkflowHistoryAsync(regId);
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId);
                return Json(new
                {
                    success = true,
                    reqNo = reg?.ReqNo ?? $"REG-{regId}",
                    currentStatus = reg?.WfStatus ?? "Pending",
                    isLop = reg?.IsLopApplied == true,
                    history = history.Select(h => new
                    {
                        stepNumber = h.StepNumber,
                        stepName = h.StepName,
                        action = h.Action,
                        actorName = h.ActorName,
                        actionDateTime = h.ActionDateTime.ToString("dd-MMM-yyyy hh:mm tt"),
                        comments = h.Comments
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(int maxDays, bool allowFuture, string workingDays)
        {
            try
            {
                int userGrpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (userGrpId != 1 && userGrpId != 2 && userGrpId != 7)
                {
                    return Json(new { success = false, message = "Only administrators can configure Attendance Regularization policies." });
                }

                if (maxDays <= 0 || maxDays > 90)
                {
                    return Json(new { success = false, message = "Permitted window must be between 1 and 90 days." });
                }

                var res = await _regService.SaveConfigAsync(maxDays, allowFuture, workingDays);
                return Json(new { success = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // LEGACY COMPATIBILITY METHODS
        // =====================================================================

        [HttpGet]
        public IActionResult CreateNew(HrEmpAttendExp attdExcp)
        {
            try
            {
                ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.Today = DateTime.Today;
                attdExcp.empTyp = "A";

                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).FirstOrDefault(x => x.PrvCode == "ATN_EXP");
                ViewBag.RecAdd = objprv?.RecAdd ?? true;

                return PartialView("_HrEmpAttendExp_Create", attdExcp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        attdExcp.RqstId = attdExcp.EmpSysId;
                        attdExcp.AprvdStatId = 1;
                        attdExcp.AprvdId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));
                        attdExcp.AprvdDateTime = DateTime.Now;
                        int _result = _attnContext.SaveHrEmpAttendExp(attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"), Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                        if (_result == 1)
                            return RedirectToAction(nameof(Index)).WithSuccess("Attendance Regularization", "Record(s) Added Successfully");
                        else
                            return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "Adding Record got failed");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "Adding Record got failed");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public ActionResult EditGet(int id)
        {
            var empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
            ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
            ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
            var attdExp = _attnContext.GetHrEmpAttendExeption(false, empSysId).Where(x => x.AtnExpId == id);

            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).FirstOrDefault(x => x.PrvCode == "ATN_EXP");
            ViewBag.RecEdit = objprv?.RecEdit ?? true;
            return PartialView("_HrEmpAttendExp_Edit", attdExp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var varChanges = _dbCommitContext.commitChanges(LogEventTypes.UPD, attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        var prcSrcDtl = "Edit Regularization" + attdExcp.AtnExpId;
                        _attnContext.LoadHrEmpAtndc("S", attdExcp.EmpSysId, attdExcp.StDate, attdExcp.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Attendance Regularization", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "Updating Record got failed");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public IActionResult DeleteGet(int id)
        {
            try
            {
                var empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                var attdExp = _attnContext.GetHrEmpAttendExeption(false, empSysId).FirstOrDefault(x => x.AtnExpId == id);
                HrEmpAttendExp attdExcpNew = new HrEmpAttendExp();
                if (attdExp != null)
                {
                    attdExcpNew.EmpSysId = attdExp.EmpSysId;
                    attdExcpNew.AtnExpId = attdExp.AtnExpId;
                    attdExcpNew.StDate = attdExp.StDate;
                    attdExcpNew.EndDate = attdExp.EndDate;
                    attdExcpNew.Note = attdExp.Note;
                    ViewBag.EmployeeName = _empContext.GetAllEmpGenInfo().FirstOrDefault(x => x.EmpSysId == attdExp.EmpSysId)?.FullName;
                }
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).FirstOrDefault(x => x.PrvCode == "ATN_EXP");
                ViewBag.RecDelete = objprv?.RecDelete ?? true;
                return PartialView("_HrEmpAttendExp_Delete", attdExcpNew);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(HrEmpAttendExp attdExcp)
        {
            try
            {
                var hrEmpAttendExp = _attnContext.GetHrEmpAttendExpByAtnExpId(attdExcp.AtnExpId);
                if (hrEmpAttendExp != null)
                {
                    hrEmpAttendExp.RecStat = 0;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpAttendExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    var prcSrcDtl = "Delete Regularization" + attdExcp.AtnExpId;
                    _attnContext.LoadHrEmpAtndc("S", attdExcp.EmpSysId, hrEmpAttendExp.StDate, hrEmpAttendExp.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    return RedirectToAction(nameof(Index)).WithSuccess("Attendance Regularization", "Record Deleted Successfully.");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "Record Deletion got failed.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", ex.Message);
            }
        }

        public IActionResult DeleteAllGet()
        {
            try
            {
                ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.Today = DateTime.Today;

                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).FirstOrDefault(x => x.PrvCode == "ATN_EXP");
                ViewBag.RecDelete = objprv?.RecDelete ?? true;
                return PartialView("_HrEmpAttendExp_Delete_All");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAll(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var _delReclist = new List<HrEmpAttendExp>();
                    string prcSrcDtl = "";
                    var currUsrId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                    if (attdExcp.empTyp == "A")
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("A", 0, attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Regularization (A)";
                    }
                    else if (attdExcp.empTyp == "D")
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("D", Convert.ToInt32(attdExcp.dptId), attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Regularization (D)";
                    }
                    else if (attdExcp.empTyp == "S")
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("S", attdExcp.empSysId ?? 0, attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Regularization (S)";
                    }

                    if (_delReclist.Count() > 0)
                    {
                        foreach (var rec in _delReclist)
                        {
                            rec.RecStat = 0;
                            _dbCommitContext.commitChanges(LogEventTypes.UPD, rec, currUsrId);
                            _attnContext.LoadHrEmpAtndc(attdExcp.empTyp, 0, rec.StDate, rec.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, currUsrId);
                        }
                        return RedirectToAction(nameof(Index)).WithSuccess("Attendance Regularization", "Record(s) Deleted Successfully");
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "No Matching Records Found");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Regularization", "Deleting Record got failed");
            }
        }

        public JsonResult Validate([Bind(Prefix = "EmpSysId")] int empSysId, [Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            IEnumerable<AttnContext.HrEmpAttendExpView> _obj;
            var eSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");

            if (empSysId == 0)
                _obj = _attnContext.GetHrEmpAttendExeption(false, eSysId).Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate));
            else
                _obj = _attnContext.GetHrEmpAttendExeption(false, eSysId).Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate) && x.EmpSysId == empSysId);

            if (_obj.Any())
            {
                return Json("There is a record already existing for the selected date range");
            }
            if (endDate < stDate)
            {
                return Json("Invalid date range");
            }
            return Json(true);
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "Del_StDate")] DateTime? stDate, [Bind(Prefix = "Del_EndDate")] DateTime? endDate)
        {
            if (endDate >= stDate)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}
