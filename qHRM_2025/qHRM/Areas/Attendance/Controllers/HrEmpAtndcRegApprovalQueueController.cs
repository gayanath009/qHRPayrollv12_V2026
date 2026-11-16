using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using qHRM.Models;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.Payroll.Models;
using static qHRM.Areas.Attendance.Data.AttnContext;

namespace qHRM.Areas.Attendance.Controllers
{
    [Area("Attendance")]
    public class HrEmpAtndcRegApprovalQueueController : Controller
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _attdContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly AttendanceRegularizationService _regService;
        private readonly IConfiguration _config;
        private readonly ILogger<HrEmpAtndcRegApprovalQueueController> _logger;

        public HrEmpAtndcRegApprovalQueueController(
            qHRMasterContext hrmContext,
            EmployeeContext empContext,
            AttnContext attdContext,
            AdmUserGrpPrvContext prvContext,
            IConfiguration config,
            ILogger<HrEmpAtndcRegApprovalQueueController> logger,
            AttendanceRegularizationService regService = null)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
            _attdContext = attdContext;
            _prvContext = prvContext;
            _config = config;
            _logger = logger;
            _regService = regService;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string filter = "all", string q = null)
        {
            int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

            if (HttpContext.Session.GetString("ATN_REG_Q") == null && grpId != 1)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
            bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(userEmpSysId, grpId);
            bool showAllEmp = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(userEmpSysId, grpId);

            var subordinates = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(userEmpSysId).ToHashSet();
            if (userEmpSysId > 0 && !isSelfOnlyUser) subordinates.Add(userEmpSysId);
            bool canManageSubordinates = !isSelfOnlyUser && subordinates.Count > 1;

            bool canApprove = (grpId == 1 || showAllEmp || canManageSubordinates || isOutsourceAdmin);

            // Default cycle: 21st of previous month to 20th of current month
            DateTime today = DateTime.Today;
            DateTime defFrom, defTo;
            if (today.Day <= 20)
            {
                defFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-1).AddDays(20);
                defTo = new DateTime(today.Year, today.Month, 20);
            }
            else
            {
                defFrom = new DateTime(today.Year, today.Month, 21);
                defTo = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(19);
            }

            DateTime cycleStart = fromDate ?? defFrom;
            DateTime cycleEnd = toDate ?? defTo;

            // Query regularizations in range
            var query = from exp in _hrmContext.HrEmpAttendExp
                        where exp.StDate <= cycleEnd.Date && exp.StDate >= cycleStart.Date
                        join eg in _hrmContext.HrEmpGenInfo on exp.EmpSysId equals eg.EmpSysId into egJoin
                        from emp in egJoin.DefaultIfEmpty()
                        join ja in _hrmContext.HrEmpJobAssign on emp.EmpSysId equals ja.EmpSysId into jaJoin
                        from job in jaJoin.Where(j => j.WkStat == 1).DefaultIfEmpty()
                        join dept in _hrmContext.OrgDepartment on job.DeptId equals dept.DeptId into deptJoin
                        from dpt in deptJoin.DefaultIfEmpty()
                        join desg in _hrmContext.OrgDesignation on job.DesigId equals desg.DesigId into desgJoin
                        from dsg in desgJoin.DefaultIfEmpty()
                        select new
                        {
                            exp.AtnExpId,
                            exp.EmpSysId,
                            EmpId = emp != null ? emp.EmpId : "",
                            FirstName = emp != null ? emp.FirstName : "",
                            MdlName = emp != null ? emp.MdlName : "",
                            LastName = emp != null ? emp.LastName : "",
                            Department = dpt != null ? dpt.NameEng : "",
                            Designation = dsg != null ? dsg.NameEng : "",
                            AttendanceDate = exp.StDate,
                            exp.ReqNo,
                            exp.ReqInTime,
                            exp.ReqOutTime,
                            exp.ActualInTime,
                            exp.ActualOutTime,
                            exp.TotalHours,
                            exp.ReasonCategory,
                            exp.ReasonDescription,
                            exp.WfStatus,
                            exp.AprvdStatId,
                            exp.RecStat,
                            exp.AprvdId,
                            exp.AprvdDateTime,
                            exp.AprvdRmks,
                            exp.CrtDateTime,
                            RptMngId = job != null ? job.RptMngId : null
                        };

            // Restrict employees based on user role
            if (isSelfOnlyUser)
            {
                query = query.Where(x => x.EmpSysId == userEmpSysId);
            }
            else if (!showAllEmp && !isOutsourceAdmin && canManageSubordinates)
            {
                query = query.Where(x => subordinates.Contains(x.EmpSysId));
            }
            else if (isOutsourceAdmin)
            {
                var outsourceEmpSysIds = _empContext.GetOutsourceEmployeeSysIds(userEmpSysId);
                if (userEmpSysId > 0) outsourceEmpSysIds.Add(userEmpSysId);
                query = query.Where(x => outsourceEmpSysIds.Contains(x.EmpSysId));
            }

            var rawItems = await query.OrderByDescending(x => x.AttendanceDate).ThenByDescending(x => x.CrtDateTime).ToListAsync();

            // Preload manager names for quick lookup
            var managerIds = rawItems.Where(x => x.RptMngId.HasValue).Select(x => x.RptMngId.Value).Distinct().ToList();
            var approverIds = rawItems.Where(x => x.AprvdId.HasValue).Select(x => x.AprvdId.Value).Distinct().ToList();
            var allEmpIdsToLoad = managerIds.Union(approverIds).Distinct().ToList();

            var empNames = await _hrmContext.HrEmpGenInfo
                .Where(m => allEmpIdsToLoad.Contains(m.EmpSysId))
                .ToDictionaryAsync(
                    m => m.EmpSysId,
                    m => ((m.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(m.LastName) ? "" : " " + m.LastName.Trim())).Trim()
                );

            // Map items
            var allItems = new List<RegularizationQueueItemViewModel>();
            foreach (var r in rawItems)
            {
                string fullName = ((r.FirstName ?? "").Trim() +
                                  (string.IsNullOrWhiteSpace(r.MdlName) ? "" : " " + r.MdlName.Trim()) +
                                  (string.IsNullOrWhiteSpace(r.LastName) ? "" : " " + r.LastName.Trim())).Trim();

                string normWf = r.WfStatus?.Trim() ?? "";
                int aprvStat = r.AprvdStatId ?? 0;
                short recStat = r.RecStat;

                string status;
                string pendingWith = "";
                string approverName = "";

                if (normWf.Equals("Approved", StringComparison.OrdinalIgnoreCase) || aprvStat == 1)
                {
                    status = "Approved";
                    if (r.AprvdId.HasValue && empNames.TryGetValue(r.AprvdId.Value, out var aName))
                    {
                        approverName = aName;
                    }
                    else if (!string.IsNullOrWhiteSpace(r.AprvdRmks))
                    {
                        approverName = r.AprvdRmks.Replace("Approver:", "").Trim();
                    }
                    else
                    {
                        approverName = "HR Manager / Administrator";
                    }
                }
                else if (normWf.Equals("Rejected", StringComparison.OrdinalIgnoreCase) || aprvStat == 2)
                {
                    status = "Rejected";
                    if (r.AprvdId.HasValue && empNames.TryGetValue(r.AprvdId.Value, out var aName))
                    {
                        approverName = aName;
                    }
                }
                else if (normWf.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || aprvStat == 3)
                {
                    status = "Cancelled";
                }
                else
                {
                    status = "Pending";
                    if (r.RptMngId.HasValue && empNames.TryGetValue(r.RptMngId.Value, out var mName) && !string.IsNullOrWhiteSpace(mName))
                    {
                        pendingWith = mName + " (Reporting Manager)";
                    }
                    else
                    {
                        pendingWith = "HR / Direct Approver";
                    }
                }

                // Check action permissions:
                bool canActionItem = canApprove;
                if (!showAllEmp && !isOutsourceAdmin && canManageSubordinates)
                {
                    canActionItem = subordinates.Contains(r.EmpSysId);
                }

                decimal totHours = r.TotalHours ?? 0m;
                if (totHours == 0 && r.ReqInTime.HasValue && r.ReqOutTime.HasValue)
                {
                    double diff = (r.ReqOutTime.Value - r.ReqInTime.Value).TotalHours;
                    if (diff < 0) diff += 24;
                    totHours = Math.Round((decimal)diff, 2);
                }

                allItems.Add(new RegularizationQueueItemViewModel
                {
                    AtnExpId = r.AtnExpId,
                    EmpSysId = r.EmpSysId,
                    EmpId = r.EmpId,
                    EmpName = fullName,
                    Department = r.Department,
                    Designation = r.Designation,
                    AttendanceDate = r.AttendanceDate,
                    ReqNo = string.IsNullOrWhiteSpace(r.ReqNo) ? $"REG-{r.AtnExpId}" : r.ReqNo,
                    ReqInTime = r.ReqInTime,
                    ReqOutTime = r.ReqOutTime,
                    ActualInTime = r.ActualInTime,
                    ActualOutTime = r.ActualOutTime,
                    TotalHours = totHours,
                    ReasonCategory = r.ReasonCategory ?? "Other",
                    ReasonDescription = r.ReasonDescription ?? "",
                    Status = status,
                    PendingWith = pendingWith,
                    ApproverName = approverName,
                    ApprovalDateTime = r.AprvdDateTime,
                    ApprovalRemarks = r.AprvdRmks ?? "",
                    CreatedDateTime = r.CrtDateTime,
                    CanAction = canActionItem
                });
            }

            // Calculate Counts
            int totalCount = allItems.Count;
            int pendingCount = allItems.Count(x => x.Status == "Pending");
            int approvedCount = allItems.Count(x => x.Status == "Approved");
            int rejectedCount = allItems.Count(x => x.Status == "Rejected");
            int cancelledCount = allItems.Count(x => x.Status == "Cancelled");

            // Filter items
            IEnumerable<RegularizationQueueItemViewModel> filtered = allItems;
            string activeFilter = (filter ?? "all").Trim().ToLowerInvariant();

            filtered = activeFilter switch
            {
                "pending" => filtered.Where(x => x.Status == "Pending"),
                "approved" => filtered.Where(x => x.Status == "Approved"),
                "rejected" => filtered.Where(x => x.Status == "Rejected"),
                "cancelled" => filtered.Where(x => x.Status == "Cancelled"),
                _ => filtered
            };

            // Search query filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                string searchLower = q.Trim().ToLowerInvariant();
                filtered = filtered.Where(x =>
                    (x.EmpId != null && x.EmpId.ToLowerInvariant().Contains(searchLower)) ||
                    (x.EmpName != null && x.EmpName.ToLowerInvariant().Contains(searchLower)) ||
                    (x.Department != null && x.Department.ToLowerInvariant().Contains(searchLower)) ||
                    (x.Designation != null && x.Designation.ToLowerInvariant().Contains(searchLower)) ||
                    (x.ReqNo != null && x.ReqNo.ToLowerInvariant().Contains(searchLower)) ||
                    (x.ReasonCategory != null && x.ReasonCategory.ToLowerInvariant().Contains(searchLower)) ||
                    (x.ReasonDescription != null && x.ReasonDescription.ToLowerInvariant().Contains(searchLower)) ||
                    (x.ApproverName != null && x.ApproverName.ToLowerInvariant().Contains(searchLower)) ||
                    (x.PendingWith != null && x.PendingWith.ToLowerInvariant().Contains(searchLower))
                );
            }

            var vm = new RegularizationApprovalQueueViewModel
            {
                CycleStart = cycleStart,
                CycleEnd = cycleEnd,
                ActiveFilter = activeFilter,
                SearchQuery = q ?? "",
                TotalCount = totalCount,
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                CancelledCount = cancelledCount,
                CanApprove = canApprove,
                UserEmpSysId = userEmpSysId,
                UserGrpId = grpId,
                Items = filtered.ToList()
            };

            return View(vm);
        }

        // =====================================================================
        // POST: /Attendance/HrEmpAtndcRegApprovalQueue/ApproveRegularization
        // Approves regularization request and synchronizes attendance shift
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRegularization([FromBody] RegQueueActionRequest req)
        {
            try
            {
                int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
                if (isSelfOnlyUser && grpId != 1)
                    return Json(new { success = false, message = "You do not have permission to approve regularization requests." });

                var exp = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == req.AtnExpId);
                if (exp == null)
                    return Json(new { success = false, message = $"Regularization record #{req.AtnExpId} not found." });

                var actorName = await GetEmployeeDetailsAsync(userEmpSysId);

                // 1. Update HR_EmpAttendExp
                exp.WfStatus = "Approved";
                exp.AprvdStatId = 1;
                exp.RecStat = 1;
                exp.AprvdId = userEmpSysId;
                exp.AprvdDateTime = DateTime.Now;
                exp.AprvdRmks = string.IsNullOrWhiteSpace(req.Comment) ? "Approved via Regularization Approval Queue" : req.Comment.Trim();
                exp.HrAction = "Approved";
                exp.HrActionDateTime = DateTime.Now;
                exp.HrComments = exp.AprvdRmks;
                exp.MdfId = userEmpSysId;
                exp.MdfDateTime = DateTime.Now;

                // 2. Synchronize HR_EmpAtndcSft record: regularize attendance and prevent LOP
                var sft = await _hrmContext.HrEmpAtndcSft
                    .FirstOrDefaultAsync(s => s.EmpSysId == exp.EmpSysId && s.AtndcDate == exp.StDate && s.RecStat == 1);

                if (sft != null)
                {
                    sft.InTime = exp.ReqInTime;
                    sft.OutTime = exp.ReqOutTime;
                    sft.InSource = "REGULARIZED";
                    sft.OutSource = "REGULARIZED";

                    if (exp.ReqInTime.HasValue && exp.ReqOutTime.HasValue)
                    {
                        int workedMins = (int)(exp.ReqOutTime.Value - exp.ReqInTime.Value).TotalMinutes;
                        if (workedMins < 0) workedMins += 1440;
                        sft.WrkMnt = workedMins;
                    }

                    sft.AtndcStatId = 1; // Present / On Time
                    sft.SlryDctFct = 0.00m; // Waive deduction factor - NO LOP!
                    sft.LateInMnt = 0;
                    sft.ErlyLvMnt = 0;
                    sft.MnlMdfd = true;
                    sft.Note = $"Regularized ({exp.ReqNo}): {exp.ReasonCategory}";
                }
                else
                {
                    // Create shift record if missing
                    int wrkMnt = 0;
                    if (exp.ReqInTime.HasValue && exp.ReqOutTime.HasValue)
                    {
                        int diff = (int)(exp.ReqOutTime.Value - exp.ReqInTime.Value).TotalMinutes;
                        if (diff < 0) diff += 1440;
                        wrkMnt = diff;
                    }

                    _hrmContext.HrEmpAtndcSft.Add(new HrEmpAtndcSft
                    {
                        EmpSysId = exp.EmpSysId,
                        AtndcDate = exp.StDate,
                        SftId = 1,
                        InTime = exp.ReqInTime,
                        OutTime = exp.ReqOutTime,
                        InSource = "REGULARIZED",
                        OutSource = "REGULARIZED",
                        WrkMnt = wrkMnt,
                        AtndcStatId = 1,
                        SlryDctFct = 0.00m,
                        LateInMnt = 0,
                        ErlyLvMnt = 0,
                        MnlMdfd = true,
                        Note = $"Regularized ({exp.ReqNo}): {exp.ReasonCategory}",
                        RecStat = 1,
                        CrtId = userEmpSysId,
                        CrtDateTime = DateTime.Now
                    });
                }

                // 3. Add Workflow History
                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Regularization",
                    RefId = exp.AtnExpId,
                    PrcYear = exp.StDate.Year,
                    PrcMonth = exp.StDate.Month,
                    StepNumber = 3,
                    StepName = "Regularization Approval Queue",
                    Action = "Approved",
                    ActorEmpSysId = userEmpSysId,
                    ActorName = actorName,
                    Comments = string.IsNullOrWhiteSpace(req.Comment) ? "Approved via Regularization Approval Queue. Attendance marked Present (Slry_Dct_Fct = 0)." : req.Comment.Trim(),
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Dispatch approval email notification to employee
                if (_regService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _regService.SendApprovalAlertToEmployeeAsync(exp.AtnExpId); }
                        catch (Exception ex) { _logger?.LogError(ex, "Failed to send queue approval email for AtnExpId: {AtnExpId}", exp.AtnExpId); }
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Regularization request {exp.ReqNo} has been successfully approved and finalized. Attendance updated to Present with 0 deduction."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving regularization #{AtnExpId}", req.AtnExpId);
                return Json(new { success = false, message = "Error approving regularization: " + ex.Message });
            }
        }

        // =====================================================================
        // POST: /Attendance/HrEmpAtndcRegApprovalQueue/RejectRegularization
        // Rejects regularization request and reverts attendance
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRegularization([FromBody] RegQueueActionRequest req)
        {
            try
            {
                int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
                if (isSelfOnlyUser && grpId != 1)
                    return Json(new { success = false, message = "You do not have permission to reject regularization requests." });

                var exp = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == req.AtnExpId);
                if (exp == null)
                    return Json(new { success = false, message = $"Regularization record #{req.AtnExpId} not found." });

                var actorName = await GetEmployeeDetailsAsync(userEmpSysId);

                exp.WfStatus = "Rejected";
                exp.AprvdStatId = 2;
                exp.RecStat = 0;
                exp.AprvdId = userEmpSysId;
                exp.AprvdDateTime = DateTime.Now;
                exp.AprvdRmks = string.IsNullOrWhiteSpace(req.Comment) ? "Rejected via Regularization Approval Queue" : req.Comment.Trim();
                exp.HrAction = "Rejected";
                exp.HrActionDateTime = DateTime.Now;
                exp.HrComments = exp.AprvdRmks;
                exp.MdfId = userEmpSysId;
                exp.MdfDateTime = DateTime.Now;

                // Revert HR_EmpAtndcSft if regularized
                var sft = await _hrmContext.HrEmpAtndcSft
                    .FirstOrDefaultAsync(s => s.EmpSysId == exp.EmpSysId && s.AtndcDate == exp.StDate && s.RecStat == 1);

                if (sft != null && (sft.InSource == "REGULARIZED" || sft.OutSource == "REGULARIZED"))
                {
                    if (exp.ActualInTime.HasValue || exp.ActualOutTime.HasValue)
                    {
                        sft.InTime = exp.ActualInTime;
                        sft.OutTime = exp.ActualOutTime;
                        sft.InSource = "BIOMETRIC";
                        sft.OutSource = "BIOMETRIC";
                        sft.AtndcStatId = 1;
                        sft.SlryDctFct = 0.00m;
                        sft.Note = $"Regularization Rejected (Restored biometric punch)";
                    }
                    else
                    {
                        sft.InTime = null;
                        sft.OutTime = null;
                        sft.InSource = null;
                        sft.OutSource = null;
                        sft.WrkMnt = 0;
                        sft.AtndcStatId = 2; // Absent
                        sft.SlryDctFct = 1.00m; // Full LOP
                        sft.Note = $"Regularization Rejected: {req.Comment}";
                    }
                }

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Regularization",
                    RefId = exp.AtnExpId,
                    PrcYear = exp.StDate.Year,
                    PrcMonth = exp.StDate.Month,
                    StepNumber = 3,
                    StepName = "Regularization Approval Queue",
                    Action = "Rejected",
                    ActorEmpSysId = userEmpSysId,
                    ActorName = actorName,
                    Comments = string.IsNullOrWhiteSpace(req.Comment) ? "Rejected via Regularization Approval Queue" : req.Comment.Trim(),
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Dispatch rejection email notification to employee
                if (_regService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _regService.SendRejectionAlertToEmployeeAsync(exp.AtnExpId, req.Comment, false); }
                        catch (Exception ex) { _logger?.LogError(ex, "Failed to send queue rejection email for AtnExpId: {AtnExpId}", exp.AtnExpId); }
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Regularization request {exp.ReqNo} has been rejected."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting regularization #{AtnExpId}", req.AtnExpId);
                return Json(new { success = false, message = "Error rejecting regularization: " + ex.Message });
            }
        }

        // =====================================================================
        // GET: /Attendance/HrEmpAtndcRegApprovalQueue/GetDetails
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var exp = await (from r in _hrmContext.HrEmpAttendExp
                                 where r.AtnExpId == id
                                 join eg in _hrmContext.HrEmpGenInfo on r.EmpSysId equals eg.EmpSysId into egJoin
                                 from emp in egJoin.DefaultIfEmpty()
                                 join ja in _hrmContext.HrEmpJobAssign on emp.EmpSysId equals ja.EmpSysId into jaJoin
                                 from job in jaJoin.Where(j => j.WkStat == 1).DefaultIfEmpty()
                                 join dept in _hrmContext.OrgDepartment on job.DeptId equals dept.DeptId into deptJoin
                                 from dpt in deptJoin.DefaultIfEmpty()
                                 join desg in _hrmContext.OrgDesignation on job.DesigId equals desg.DesigId into desgJoin
                                 from dsg in desgJoin.DefaultIfEmpty()
                                 select new
                                 {
                                     r.AtnExpId,
                                     r.ReqNo,
                                     r.EmpSysId,
                                     EmpId = emp != null ? emp.EmpId : "",
                                     EmpName = emp != null ? (emp.FirstName + " " + emp.LastName).Trim() : "",
                                     Department = dpt != null ? dpt.NameEng : "",
                                     Designation = dsg != null ? dsg.NameEng : "",
                                     Date = r.StDate.ToString("dd-MMM-yyyy"),
                                     DayOfWeek = r.StDate.DayOfWeek.ToString(),
                                     ReqIn = r.ReqInTime.HasValue ? DateTime.Today.Add(r.ReqInTime.Value).ToString("hh:mm tt") : "-",
                                     ReqOut = r.ReqOutTime.HasValue ? DateTime.Today.Add(r.ReqOutTime.Value).ToString("hh:mm tt") : "-",
                                     ActualIn = r.ActualInTime.HasValue ? DateTime.Today.Add(r.ActualInTime.Value).ToString("hh:mm tt") : "-",
                                     ActualOut = r.ActualOutTime.HasValue ? DateTime.Today.Add(r.ActualOutTime.Value).ToString("hh:mm tt") : "-",
                                     TotalHours = r.TotalHours,
                                     ReasonCategory = r.ReasonCategory,
                                     ReasonDescription = r.ReasonDescription,
                                     WfStatus = r.WfStatus,
                                     AprvdStatId = r.AprvdStatId,
                                     ApproverRemarks = r.AprvdRmks,
                                     ApprovalDate = r.AprvdDateTime.HasValue ? r.AprvdDateTime.Value.ToString("dd-MMM-yyyy hh:mm tt") : "-",
                                     CreatedDate = r.CrtDateTime.ToString("dd-MMM-yyyy hh:mm tt")
                                 }).FirstOrDefaultAsync();

                if (exp == null) return Json(new { success = false, message = "Record not found." });

                // Load workflow history
                var history = await _hrmContext.HrPrWfHistory
                    .Where(h => h.WfType == "Regularization" && h.RefId == id)
                    .OrderBy(h => h.ActionDateTime)
                    .Select(h => new
                    {
                        h.StepName,
                        h.Action,
                        h.ActorName,
                        h.Comments,
                        ActionDate = h.ActionDateTime.ToString("dd-MMM-yyyy hh:mm tt")
                    })
                    .ToListAsync();

                return Json(new { success = true, data = exp, history });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GetEmployeeDetailsAsync(int empSysId)
        {
            if (empSysId <= 0) return "System / Admin";
            var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
            if (emp == null) return "User #" + empSysId;
            string name = $"{emp.FirstName} {emp.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "User #" + empSysId : name;
        }
    }
}
