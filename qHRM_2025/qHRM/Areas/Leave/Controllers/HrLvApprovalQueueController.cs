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
using qHRM.Areas.Leave.Models;
using qHRM.Areas.Leave.Data;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.SysAdmin.Data;
using static qHRM.Areas.Attendance.Data.AttnContext;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    public class HrLvApprovalQueueController : Controller
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly LeaveContext _leaveContext;
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _attdContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly LeaveWorkflowService _lvWfService;
        private readonly LeaveBalanceService _balanceService;
        private readonly IConfiguration _config;
        private readonly ILogger<HrLvApprovalQueueController> _logger;

        public HrLvApprovalQueueController(
            qHRMasterContext hrmContext,
            LeaveContext leaveContext,
            EmployeeContext empContext,
            AttnContext attdContext,
            AdmUserGrpPrvContext prvContext,
            LeaveWorkflowService lvWfService,
            LeaveBalanceService balanceService,
            IConfiguration config,
            ILogger<HrLvApprovalQueueController> logger)
        {
            _hrmContext = hrmContext;
            _leaveContext = leaveContext;
            _empContext = empContext;
            _attdContext = attdContext;
            _prvContext = prvContext;
            _lvWfService = lvWfService;
            _balanceService = balanceService;
            _config = config;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string filter = "all", string q = null)
        {
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            if (HttpContext.Session.GetString("LV_APP_Q") == null || !(grpId == 1 || grpId == 2 || grpId == 7))
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));

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

            // Pre-sync workflow statuses if possible
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _lvWfService.SyncActiveWorkflowStatusesAsync(cts.Token);
            }
            catch { }

            // Query leaves overlapping the payroll cycle
            var leavesQuery = from lv in _hrmContext.HrLvRegister
                              where lv.StDate <= cycleEnd.Date && lv.EndDate >= cycleStart.Date
                              join lt in _hrmContext.HrLvType on lv.LvTypId equals lt.LvTypId into ltJoin
                              from lvtyp in ltJoin.DefaultIfEmpty()
                              join eg in _hrmContext.HrEmpGenInfo on lv.EmpSysId equals eg.EmpSysId into egJoin
                              from emp in egJoin.DefaultIfEmpty()
                              join ja in _hrmContext.HrEmpJobAssign on emp.EmpSysId equals ja.EmpSysId into jaJoin
                              from job in jaJoin.Where(j => j.WkStat == 1).DefaultIfEmpty()
                              join dept in _hrmContext.OrgDepartment on job.DeptId equals dept.DeptId into deptJoin
                              from dpt in deptJoin.DefaultIfEmpty()
                              join desg in _hrmContext.OrgDesignation on job.DesigId equals desg.DesigId into desgJoin
                              from dsg in desgJoin.DefaultIfEmpty()
                              select new
                              {
                                  lv.LvId,
                                  lv.EmpSysId,
                                  EmpId = emp != null ? emp.EmpId : "",
                                  FirstName = emp != null ? emp.FirstName : "",
                                  MdlName = emp != null ? emp.MdlName : "",
                                  LastName = emp != null ? emp.LastName : "",
                                  Department = dpt != null ? dpt.NameEng : "",
                                  Designation = dsg != null ? dsg.NameEng : "",
                                  lv.LvTypId,
                                  LeaveType = lvtyp != null ? lvtyp.NameEng : "",
                                  lv.StDate,
                                  lv.EndDate,
                                  lv.Days,
                                  lv.CrtDateTime,
                                  lv.Note,
                                  lv.WfStat,
                                  lv.RecStat,
                                  lv.WfInstId,
                                  RptMngId = job != null ? job.RptMngId : null
                              };

            // Restrict employees based on user role
            if (isSelfOnlyUser)
            {
                leavesQuery = leavesQuery.Where(x => x.EmpSysId == userEmpSysId);
            }
            else if (!showAllEmp && !isOutsourceAdmin && canManageSubordinates)
            {
                leavesQuery = leavesQuery.Where(x => x.EmpSysId.HasValue && subordinates.Contains(x.EmpSysId.Value));
            }
            else if (isOutsourceAdmin)
            {
                var outsourceEmpSysIds = _empContext.GetOutsourceEmployeeSysIds(userEmpSysId);
                if (userEmpSysId > 0) outsourceEmpSysIds.Add(userEmpSysId);
                leavesQuery = leavesQuery.Where(x => x.EmpSysId.HasValue && outsourceEmpSysIds.Contains(x.EmpSysId.Value));
            }

            var rawLeaves = await leavesQuery.OrderByDescending(x => x.CrtDateTime).ToListAsync();

            // Preload manager names for quick lookup
            var managerIds = rawLeaves.Where(x => x.RptMngId.HasValue).Select(x => x.RptMngId.Value).Distinct().ToList();
            var managerNames = await _hrmContext.HrEmpGenInfo
                .Where(m => managerIds.Contains(m.EmpSysId))
                .ToDictionaryAsync(
                    m => m.EmpSysId,
                    m => ((m.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(m.LastName) ? "" : " " + m.LastName.Trim())).Trim()
                );

            // Preload active workflow instances for these leaves
            var lvIds = rawLeaves.Select(x => x.LvId).ToList();
            var wfInstances = await _hrmContext.HrLvWfInstance
                .Where(w => lvIds.Contains(w.LvId))
                .ToDictionaryAsync(w => w.LvId, w => w);

            // Map to ViewModel
            var allItems = new List<LeaveApprovalQueueItemViewModel>();
            foreach (var r in rawLeaves)
            {
                string fullName = ((r.FirstName ?? "").Trim() + 
                                  (string.IsNullOrWhiteSpace(r.MdlName) ? "" : " " + r.MdlName.Trim()) + 
                                  (string.IsNullOrWhiteSpace(r.LastName) ? "" : " " + r.LastName.Trim())).Trim();

                string normWf = r.WfStat?.Trim() ?? "";
                if (string.IsNullOrEmpty(normWf))
                {
                    normWf = r.RecStat switch
                    {
                        1 => "Approved",
                        2 => "Approved",
                        3 => "Rejected",
                        0 => "Cancelled",
                        _ => "Pending"
                    };
                }

                string displayStatus;
                string pendingWith;
                bool isApproved = normWf.Equals("Approved", StringComparison.OrdinalIgnoreCase) || r.RecStat == 1 || r.RecStat == 2;
                bool isRejected = normWf.Equals("Rejected", StringComparison.OrdinalIgnoreCase) || r.RecStat == 3;
                bool isCancelled = normWf.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || (r.RecStat == 0 && !normWf.Equals("Pending", StringComparison.OrdinalIgnoreCase));

                if (isApproved)
                {
                    displayStatus = "Approved";
                    pendingWith = "Approved";
                }
                else if (isRejected)
                {
                    displayStatus = "Rejected";
                    pendingWith = "Rejected";
                }
                else if (isCancelled)
                {
                    displayStatus = "Cancelled";
                    pendingWith = "Cancelled";
                }
                else
                {
                    displayStatus = "Pending";
                    // Resolve with whom it is pending
                    if (r.RptMngId.HasValue && managerNames.TryGetValue(r.RptMngId.Value, out var mName) && !string.IsNullOrWhiteSpace(mName))
                    {
                        pendingWith = mName + " (Reporting Manager)";
                    }
                    else
                    {
                        pendingWith = "HR / Direct Approver";
                    }
                }

                allItems.Add(new LeaveApprovalQueueItemViewModel
                {
                    LvId = r.LvId,
                    EmpSysId = r.EmpSysId.GetValueOrDefault(),
                    EmpId = r.EmpId,
                    FullName = fullName,
                    Department = r.Department,
                    Designation = r.Designation,
                    LvTypId = r.LvTypId.GetValueOrDefault(),
                    LeaveType = r.LeaveType,
                    StDate = r.StDate,
                    EndDate = r.EndDate,
                    Days = r.Days,
                    AppliedDate = r.CrtDateTime,
                    Reason = r.Note ?? "",
                    Status = displayStatus,
                    PendingWith = pendingWith,
                    CanDirectApprove = canApprove && displayStatus == "Pending",
                    CanDirectReject = canApprove && displayStatus == "Pending",
                    RecStat = r.RecStat
                });
            }

            // Calculate Counts for quick filter tabs
            int countAll = allItems.Count;
            int countPending = allItems.Count(x => x.Status == "Pending");
            int countApproved = allItems.Count(x => x.Status == "Approved");
            int countRejected = allItems.Count(x => x.Status == "Rejected");
            int countCancelled = allItems.Count(x => x.Status == "Cancelled");

            // Filter items based on active filter
            string activeFilter = (filter ?? "all").ToLowerInvariant();
            var filteredItems = activeFilter switch
            {
                "pending" => allItems.Where(x => x.Status == "Pending").ToList(),
                "approved" => allItems.Where(x => x.Status == "Approved").ToList(),
                "rejected" => allItems.Where(x => x.Status == "Rejected").ToList(),
                "cancelled" => allItems.Where(x => x.Status == "Cancelled").ToList(),
                _ => allItems
            };

            // Optional search query filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToLowerInvariant();
                filteredItems = filteredItems.Where(x =>
                    x.EmpId.ToLowerInvariant().Contains(term) ||
                    x.FullName.ToLowerInvariant().Contains(term) ||
                    x.Department.ToLowerInvariant().Contains(term) ||
                    x.LeaveType.ToLowerInvariant().Contains(term) ||
                    x.Reason.ToLowerInvariant().Contains(term) ||
                    x.PendingWith.ToLowerInvariant().Contains(term)
                ).ToList();
            }

            var model = new LeaveApprovalQueueViewModel
            {
                FromDate = cycleStart,
                ToDate = cycleEnd,
                Filter = activeFilter,
                SearchQuery = q ?? "",
                TotalAll = countAll,
                TotalPending = countPending,
                TotalApproved = countApproved,
                TotalRejected = countRejected,
                TotalCancelled = countCancelled,
                UserCanApprove = canApprove,
                Items = filteredItems
            };

            return View(model);
        }

        // =====================================================================
        // POST: /Leave/HrLvApprovalQueue/ApproveLeave
        // Directly finalizes leave approval and updates attendance to prevent LOP
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave([FromBody] LeaveQueueActionRequest req)
        {
            try
            {
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (HttpContext.Session.GetString("LV_APP_Q") == null || !(grpId == 1 || grpId == 2 || grpId == 7))
                    return Json(new { success = false, message = "Access denied. You do not have permission to approve leave requests." });

                int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _empContext.IsOutsourceAdmin(userEmpSysId, grpId);
                bool showAllEmp = !isSelfOnlyUser && !isOutsourceAdmin && _empContext.GetUserShowAllEmployees(userEmpSysId, grpId);
                var subordinates = isSelfOnlyUser ? new HashSet<int>() : _empContext.GetEmpSbrdnts(userEmpSysId).ToHashSet();
                if (userEmpSysId > 0 && !isSelfOnlyUser) subordinates.Add(userEmpSysId);
                bool canManageSubordinates = !isSelfOnlyUser && subordinates.Count > 1;

                if (isSelfOnlyUser && grpId != 1)
                    return Json(new { success = false, message = "You do not have permission to approve leave requests." });

                var lv = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == req.LvId);
                if (lv == null)
                    return Json(new { success = false, message = $"Leave record #{req.LvId} not found." });

                // 1. Mark leave as Approved and Active in HR_LvRegister
                lv.WfStat = "Approved";
                lv.RecStat = 1;
                if (!string.IsNullOrWhiteSpace(req.Comment))
                {
                    string stamp = $"[Approved by User #{userEmpSysId}: {req.Comment.Trim()}]";
                    lv.Note = string.IsNullOrWhiteSpace(lv.Note) ? stamp : $"{lv.Note} | {stamp}";
                }
                await _hrmContext.SaveChangesAsync();

                // 2. Mark any related HrLvWfInstance as Approved
                var wfInst = await _hrmContext.HrLvWfInstance.FirstOrDefaultAsync(w => w.LvId == req.LvId);
                if (wfInst != null)
                {
                    wfInst.WfStatus = "Approved";
                    wfInst.MdfDateTime = DateTime.Now;
                    await _hrmContext.SaveChangesAsync();

                    // Finalize WorkflowEngineDb instance if connected
                    try
                    {
                        var wfConnStr = _config.GetConnectionString("workflowconstring")
                                     ?? _config.GetSection("connectionstrings").GetSection("workflowconstring")?.Value;
                        if (!string.IsNullOrEmpty(wfConnStr) && wfInst.WfInstanceId != Guid.Empty)
                        {
                            using var wfConn = new SqlConnection(wfConnStr);
                            await wfConn.OpenAsync();
                            using var cmd = wfConn.CreateCommand();
                            cmd.CommandText = @"
                                UPDATE WorkflowInstances 
                                SET Status = 2, EndDate = GETDATE(), ModifiedDate = GETDATE(), ModifiedBy = @user
                                WHERE Id = @instId;

                                UPDATE WorkflowApprovals 
                                SET Status = 2, ActionDate = GETDATE(), Comment = @cmt, ModifiedDate = GETDATE()
                                WHERE WorkflowInstanceStepId IN (
                                    SELECT Id FROM WorkflowInstanceSteps WHERE WorkflowInstanceId = @instId
                                ) AND Status = 1;";
                            cmd.Parameters.AddWithValue("@instId", wfInst.WfInstanceId);
                            cmd.Parameters.AddWithValue("@user", $"Queue Approval (#{userEmpSysId})");
                            cmd.Parameters.AddWithValue("@cmt", string.IsNullOrWhiteSpace(req.Comment) ? "Directly approved via Leave Approval Queue" : req.Comment.Trim());
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not update WorkflowEngineDb status for leave #{LvId}", req.LvId);
                    }
                }

                // 3. CRITICAL: Clear LOP / Absent records in HR_EmpAtndcSft so the days are NEVER considered as LOP
                if (lv.EmpSysId.HasValue)
                {
                    int empSysId = lv.EmpSysId.Value;
                    DateTime stDate = lv.StDate.Date;
                    DateTime endDate = lv.EndDate.Date;

                    // Direct update of HR_EmpAtndcSft to ensure Atndc_Stat_Id = 2 (Leave) and Slry_Dct_Fct = 0
                    await _hrmContext.Database.ExecuteSqlRawAsync(@"
                        UPDATE HR_EmpAtndcSft
                        SET Atndc_Stat_Id = 2, -- Leave
                            Slry_Dct_Fct = 0.00 -- No salary deduction
                        WHERE Emp_Sys_Id = {0}
                          AND Atndc_Date >= {1} AND Atndc_Date <= {2}
                          AND (Atndc_Stat_Id = 0 OR Slry_Dct_Fct > 0)",
                        empSysId, stDate, endDate);

                    // Sync attendance loader procedure
                    try
                    {
                        _attdContext.LoadHrEmpAtndc("S", empSysId, stDate, endDate, AtndcLdPrcTyp.LV, "LV-Id" + lv.LvId, false, userEmpSysId > 0 ? userEmpSysId : 1);
                    }
                    catch { }

                    // Re-verify that Slry_Dct_Fct is 0.00 for the period
                    await _hrmContext.Database.ExecuteSqlRawAsync(@"
                        UPDATE HR_EmpAtndcSft
                        SET Atndc_Stat_Id = 2,
                            Slry_Dct_Fct = 0.00
                        WHERE Emp_Sys_Id = {0}
                          AND Atndc_Date >= {1} AND Atndc_Date <= {2}
                          AND Atndc_Stat_Id IN (0, 2)",
                        empSysId, stDate, endDate);
                }

                // 4. Deduct Leave Balance
                try
                {
                    await _balanceService.DeductLeaveBalanceAsync(lv.LvId, userEmpSysId);
                }
                catch { }

                return Json(new
                {
                    success = true,
                    message = $"Leave request #{req.LvId} has been successfully approved and finalized. Attendance updated to prevent LOP."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving leave #{LvId} in approval queue", req.LvId);
                return Json(new { success = false, message = "Error approving leave: " + ex.Message });
            }
        }

        // =====================================================================
        // POST: /Leave/HrLvApprovalQueue/RejectLeave
        // Rejects leave request and resets any attendance / balance
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave([FromBody] LeaveQueueActionRequest req)
        {
            try
            {
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (HttpContext.Session.GetString("LV_APP_Q") == null || !(grpId == 1 || grpId == 2 || grpId == 7))
                    return Json(new { success = false, message = "Access denied. You do not have permission to approve leave requests." });

                int userEmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));

                bool isSelfOnlyUser = _empContext.IsRestrictedSelfOnlyUser(userEmpSysId, grpId);
                if (isSelfOnlyUser && grpId != 1)
                    return Json(new { success = false, message = "You do not have permission to reject leave requests." });

                var lv = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == req.LvId);
                if (lv == null)
                    return Json(new { success = false, message = $"Leave record #{req.LvId} not found." });

                lv.WfStat = "Rejected";
                lv.RecStat = 0; // Inactive / Rejected
                if (!string.IsNullOrWhiteSpace(req.Comment))
                {
                    string stamp = $"[Rejected by User #{userEmpSysId}: {req.Comment.Trim()}]";
                    lv.Note = string.IsNullOrWhiteSpace(lv.Note) ? stamp : $"{lv.Note} | {stamp}";
                }
                await _hrmContext.SaveChangesAsync();

                // Update workflow instance
                var wfInst = await _hrmContext.HrLvWfInstance.FirstOrDefaultAsync(w => w.LvId == req.LvId);
                if (wfInst != null)
                {
                    wfInst.WfStatus = "Rejected";
                    wfInst.MdfDateTime = DateTime.Now;
                    await _hrmContext.SaveChangesAsync();

                    try
                    {
                        var wfConnStr = _config.GetConnectionString("workflowconstring")
                                     ?? _config.GetSection("connectionstrings").GetSection("workflowconstring")?.Value;
                        if (!string.IsNullOrEmpty(wfConnStr) && wfInst.WfInstanceId != Guid.Empty)
                        {
                            using var wfConn = new SqlConnection(wfConnStr);
                            await wfConn.OpenAsync();
                            using var cmd = wfConn.CreateCommand();
                            cmd.CommandText = @"
                                UPDATE WorkflowInstances 
                                SET Status = 3, EndDate = GETDATE(), ModifiedDate = GETDATE(), ModifiedBy = @user
                                WHERE Id = @instId;

                                UPDATE WorkflowApprovals 
                                SET Status = 3, ActionDate = GETDATE(), Comment = @cmt, ModifiedDate = GETDATE()
                                WHERE WorkflowInstanceStepId IN (
                                    SELECT Id FROM WorkflowInstanceSteps WHERE WorkflowInstanceId = @instId
                                ) AND Status = 1;";
                            cmd.Parameters.AddWithValue("@instId", wfInst.WfInstanceId);
                            cmd.Parameters.AddWithValue("@user", $"Queue Rejection (#{userEmpSysId})");
                            cmd.Parameters.AddWithValue("@cmt", string.IsNullOrWhiteSpace(req.Comment) ? "Rejected via Leave Approval Queue" : req.Comment.Trim());
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch { }
                }

                // Restore leave balance
                try
                {
                    await _balanceService.RestoreLeaveBalanceAsync(lv.LvId, userEmpSysId);
                }
                catch { }

                // Refresh attendance
                if (lv.EmpSysId.HasValue)
                {
                    try
                    {
                        _attdContext.LoadHrEmpAtndc("S", lv.EmpSysId.Value, lv.StDate, lv.EndDate, AtndcLdPrcTyp.LV, "LV-Id" + lv.LvId, false, userEmpSysId > 0 ? userEmpSysId : 1);
                    }
                    catch { }
                }

                return Json(new { success = true, message = $"Leave request #{req.LvId} has been rejected." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting leave #{LvId} in approval queue", req.LvId);
                return Json(new { success = false, message = "Error rejecting leave: " + ex.Message });
            }
        }

        // =====================================================================
        // GET: /Leave/HrLvApprovalQueue/Timeline?lvId={int}
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Timeline(int lvId)
        {
            try
            {
                var timeline = await _lvWfService.GetLeaveApprovalTimelineAsync(lvId);
                return PartialView("~/Areas/Leave/Views/HrLvRegister/_HrLvRegister_Timeline.cshtml", timeline);
            }
            catch (Exception ex)
            {
                var errModel = new LeaveApprovalTimelineViewModel
                {
                    Success = false,
                    Message = "Error loading timeline: " + ex.Message
                };
                return PartialView("~/Areas/Leave/Views/HrLvRegister/_HrLvRegister_Timeline.cshtml", errModel);
            }
        }
    }
}
