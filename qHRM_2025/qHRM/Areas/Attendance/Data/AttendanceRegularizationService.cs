using System.Globalization;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using qHRM.Area.Config;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.Notify.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.WorkShift.Models;
using qHRM.Models;

namespace qHRM.Areas.Attendance.Data
{
    public class AttendanceRegularizationService
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;
        private readonly SysConfigContext _sysConfigContext;
        private readonly WorkflowNotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceRegularizationService> _logger;

        public AttendanceRegularizationService(
            qHRMasterContext hrmContext,
            EmployeeContext empContext,
            SysConfigContext sysConfigContext = null,
            WorkflowNotificationService notificationService = null,
            IConfiguration configuration = null,
            ILogger<AttendanceRegularizationService> logger = null)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
            _sysConfigContext = sysConfigContext;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        // =====================================================================
        // CONFIGURATION
        // =====================================================================
        public async Task<HrAtndcRegConfig> GetConfigAsync()
        {
            try
            {
                var cfg = await _hrmContext.HrAtndcRegConfig.FirstOrDefaultAsync(c => c.IsActive);
                if (cfg != null) return cfg;

                // Fallback to HrLvWfConfig for "Regularization"
                var wfCfg = await _hrmContext.HrLvWfConfig.FirstOrDefaultAsync(w => w.WfType == "Regularization" && w.RecStat == 1);
                int maxDays = 14;
                bool allowFuture = false;
                if (wfCfg != null && !string.IsNullOrWhiteSpace(wfCfg.PolicyRules))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(wfCfg.PolicyRules);
                        if (doc.RootElement.TryGetProperty("MaxDaysAllowed", out var m) && m.TryGetInt32(out int md))
                            maxDays = md;
                        if (doc.RootElement.TryGetProperty("AllowFutureDates", out var f))
                            allowFuture = f.GetBoolean();
                    }
                    catch { }
                }

                return new HrAtndcRegConfig
                {
                    ConfigId = 0,
                    MaxDaysAllowed = maxDays,
                    AllowFutureDates = allowFuture,
                    WorkingDays = "Sunday,Monday,Tuesday,Wednesday,Thursday",
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not load regularization configuration, using defaults.");
                return new HrAtndcRegConfig { MaxDaysAllowed = 14, AllowFutureDates = false };
            }
        }

        public async Task<(bool Success, string Message)> SaveConfigAsync(int maxDays, bool allowFuture, string workingDays = null)
        {
            try
            {
                var cfg = await _hrmContext.HrAtndcRegConfig.FirstOrDefaultAsync(c => c.IsActive);
                if (cfg == null)
                {
                    cfg = new HrAtndcRegConfig
                    {
                        MaxDaysAllowed = maxDays,
                        AllowFutureDates = allowFuture,
                        WorkingDays = string.IsNullOrWhiteSpace(workingDays) ? "Sunday,Monday,Tuesday,Wednesday,Thursday" : workingDays,
                        IsActive = true,
                        CrtDateTime = DateTime.Now
                    };
                    _hrmContext.HrAtndcRegConfig.Add(cfg);
                }
                else
                {
                    cfg.MaxDaysAllowed = maxDays;
                    cfg.AllowFutureDates = allowFuture;
                    if (!string.IsNullOrWhiteSpace(workingDays)) cfg.WorkingDays = workingDays;
                    cfg.MdfDateTime = DateTime.Now;
                }

                // Also sync to HrLvWfConfig for "Regularization"
                var wfCfg = await _hrmContext.HrLvWfConfig.FirstOrDefaultAsync(w => w.WfType == "Regularization" && w.RecStat == 1);
                if (wfCfg != null)
                {
                    var rulesObj = new { MaxDaysAllowed = maxDays, AllowFutureDates = allowFuture, WorkingDays = cfg.WorkingDays };
                    wfCfg.PolicyRules = JsonSerializer.Serialize(rulesObj);
                }

                await _hrmContext.SaveChangesAsync();
                return (true, "Attendance Regularization configuration saved successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Error saving configuration: " + ex.Message);
            }
        }

        // =====================================================================
        // PERMISSIONS & EMPLOYEE ACCESS CONTROL
        // =====================================================================
        public async Task<(bool CanViewAll, bool CanViewSubordinates, List<int> AllowedEmpSysIds)> GetAccessRightsAsync(int currentEmpSysId, int userGrpId)
        {
            // Administrator / Top Management (GrpId: 1, 2, 7, etc.) or groups configured with Show All Employees (ShwAlEmp)
            var userGroup = await _hrmContext.AdmUserGroup.FirstOrDefaultAsync(g => g.GrpId == userGrpId && g.RecStat == 1);
            if (userGrpId == 1 || userGrpId == 2 || userGrpId == 7 || (userGroup != null && userGroup.ShwAlEmp))
            {
                return (true, true, new List<int>()); // Empty list indicates all
            }

            // BU Heads (GrpId: 11) or users with subordinates
            var subordinates = _empContext.GetEmpSbrdnts(currentEmpSysId).ToList();
            bool isBuHead = (userGrpId == 11) || subordinates.Any();

            if (isBuHead)
            {
                var allowed = new List<int> { currentEmpSysId };
                allowed.AddRange(subordinates);
                return (false, true, allowed.Distinct().ToList());
            }

            // Normal employee: only self
            return (false, false, new List<int> { currentEmpSysId });
        }

        // =====================================================================
        // TIMELINE SUMMARY (Image 3)
        // =====================================================================
        public class DayAttendanceTimelineVm
        {
            public DateTime Date { get; set; }
            public string DayName { get; set; }            // "Sun", "Mon", etc.
            public string DayNumber { get; set; }          // "25", "26", etc.
            public string FullDateStr { get; set; }        // "25-Oct-2026"
            public bool IsWeekend { get; set; }            // Friday / Saturday
            public bool IsWorkingDay { get; set; }         // Sunday - Thursday
            public bool IsToday { get; set; }
            public bool IsFuture { get; set; }

            // Office Punches
            public TimeSpan? InTime { get; set; }
            public TimeSpan? OutTime { get; set; }
            public string InTimeStr { get; set; }
            public string OutTimeStr { get; set; }
            public int LateMinutes { get; set; }
            public string LateByStr { get; set; }
            public int EarlyMinutes { get; set; }
            public string EarlyByStr { get; set; }
            public int WorkedMinutes { get; set; }
            public string WorkedHoursStr { get; set; }

            // Shift timings
            public TimeSpan ShiftStart { get; set; }
            public TimeSpan ShiftEnd { get; set; }
            public string ShiftName { get; set; }

            // Status Badges & Timeline styling
            public string Status { get; set; }             // "Present", "Late IN", "Early Leave", "Missing In", "Missing Out", "Absent", "Weekend", "Holiday", "Regularized", "Pending Regularization"
            public string StatusColor { get; set; }        // "green", "red", "yellow", "purple", "cyan"
            public string BarType { get; set; }            // "worked", "absent", "weekend", "regularized", "pending"

            // Proportional timeline calculations (Standard Shift 07:30 to 15:30)
            public double InPosPercent { get; set; }
            public double OutPosPercent { get; set; }
            public double WorkedWidthPercent { get; set; }
            public double LateWidthPercent { get; set; }
            public double EarlyWidthPercent { get; set; }
            public bool HasLatePeriod { get; set; }
            public bool HasEarlyPeriod { get; set; }
            public string ShiftStartStr { get; set; } = "07:30 AM";
            public string ShiftEndStr { get; set; } = "03:30 PM";

            // Regularization Details (if any request exists)
            public int? RegId { get; set; }
            public string RegReqNo { get; set; }
            public string RegWfStatus { get; set; }
            public string RegReasonCategory { get; set; }
            public string RegDescription { get; set; }
            public TimeSpan? RegInTime { get; set; }
            public TimeSpan? RegOutTime { get; set; }

            // Action Eligibility
            public bool CanRequestRegularization { get; set; }
        }

        public async Task<DateTime?> GetTodayFirstPunchAsync(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId)) return null;
            try
            {
                var conn = _hrmContext.Database.GetDbConnection();
                bool wasClosed = conn.State != System.Data.ConnectionState.Open;
                if (wasClosed)
                    await conn.OpenAsync();

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandTimeout = 4;
                        cmd.CommandText = @"
                            DECLARE @todayLocal DATETIME = CAST(CAST(GETDATE() AS DATE) AS DATETIME);
                            DECLARE @startUtc DATETIME = DATEADD(HOUR, -3, @todayLocal);
                            DECLARE @endUtc DATETIME = DATEADD(HOUR, 24, @startUtc);
                            SELECT MIN(DATEADD(HOUR, 3, eventTime)) AS FirstPunch
                            FROM dbo.vw_zoho_atndclogs
                            WHERE (employeeID = @empId OR TRY_CAST(employeeID AS INT) = TRY_CAST(@empId AS INT))
                              AND eventTime >= @startUtc AND eventTime < @endUtc;";

                        var p = cmd.CreateParameter();
                        p.ParameterName = "@empId";
                        p.Value = empId.Trim();
                        cmd.Parameters.Add(p);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value && DateTime.TryParse(result.ToString(), out var dt))
                        {
                            return dt;
                        }
                    }
                }
                finally
                {
                    if (wasClosed && conn.State == System.Data.ConnectionState.Open)
                        await conn.CloseAsync();
                }
            }
            catch (Exception)
            {
                // Fallback gracefully if view is unreachable or table not synced
            }
            return null;
        }

        public async Task<List<DayAttendanceTimelineVm>> GetAttendanceTimelineAsync(int empSysId, DateTime fromDate, DateTime toDate)
        {
            var config = await GetConfigAsync();
            DateTime today = DateTime.Today;
            DateTime minAllowedDate = today.AddDays(-config.MaxDaysAllowed);

            // Check if today falls in range and fetch today's first punch from Zoho logs
            DateTime? todayPunch = null;
            if (fromDate.Date <= today && toDate.Date >= today)
            {
                var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
                if (emp != null && !string.IsNullOrWhiteSpace(emp.EmpId))
                {
                    todayPunch = await GetTodayFirstPunchAsync(emp.EmpId);
                }
            }

            // Fetch shift records
            var shiftRecords = await _hrmContext.HrEmpAtndcSft
                .Include(s => s.Sft)
                .Where(s => s.EmpSysId == empSysId && s.AtndcDate >= fromDate && s.AtndcDate <= toDate && s.RecStat == 1)
                .ToDictionaryAsync(s => s.AtndcDate.Date, s => s);

            // Fetch regularization requests for this period
            var regRequests = await _hrmContext.HrEmpAttendExp
                .Where(r => r.EmpSysId == empSysId && r.StDate >= fromDate && r.StDate <= toDate && r.RecStat == 1)
                .OrderByDescending(r => r.AtnExpId)
                .ToListAsync();

            var regByDate = regRequests
                .GroupBy(r => r.StDate.Date)
                .ToDictionary(g => g.Key, g => g.First());

            // Default fallback shift (07:30 to 15:30)
            var defaultStart = new TimeSpan(7, 30, 0);
            var defaultEnd = new TimeSpan(15, 30, 0);

            var list = new List<DayAttendanceTimelineVm>();

            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                var vm = new DayAttendanceTimelineVm
                {
                    Date = d,
                    DayName = d.ToString("ddd"),
                    DayNumber = d.ToString("dd"),
                    FullDateStr = d.ToString("dd-MMM-yyyy"),
                    IsWeekend = (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday),
                    IsWorkingDay = (d.DayOfWeek >= DayOfWeek.Sunday && d.DayOfWeek <= DayOfWeek.Thursday),
                    IsToday = (d == today),
                    IsFuture = (d > today),
                    ShiftStart = defaultStart,
                    ShiftEnd = defaultEnd,
                    ShiftName = "General Shift (07:30 - 15:30)"
                };

                // Check Shift Record
                shiftRecords.TryGetValue(d, out var sft);
                if (vm.IsToday)
                {
                    if (sft?.Sft != null)
                    {
                        vm.ShiftStart = sft.Sft.StTime.TimeOfDay;
                        vm.ShiftEnd = sft.Sft.EndTime.TimeOfDay;
                        vm.ShiftName = sft.Sft.NameEng ?? vm.ShiftName;
                    }

                    if (todayPunch.HasValue)
                    {
                        vm.InTime = todayPunch.Value.TimeOfDay;
                        vm.InTimeStr = todayPunch.Value.ToString("hh:mm tt");
                        vm.OutTime = null;
                        vm.OutTimeStr = null;

                        TimeSpan shiftStartForCalc = vm.ShiftStart > TimeSpan.Zero ? vm.ShiftStart : defaultStart;
                        if (vm.InTime.Value > shiftStartForCalc)
                        {
                            int lateMinutes = (int)(vm.InTime.Value - shiftStartForCalc).TotalMinutes;
                            if (lateMinutes > 0)
                            {
                                vm.LateMinutes = lateMinutes;
                                int lh = lateMinutes / 60;
                                int lm = lateMinutes % 60;
                                vm.LateByStr = $"Late by {lh:D2}:{lm:D2}";
                            }
                        }
                        vm.WorkedHoursStr = "In Progress";
                    }
                    else if (sft != null && sft.InTime.HasValue)
                    {
                        vm.InTime = sft.InTime;
                        vm.InTimeStr = DateTime.Today.Add(sft.InTime.Value).ToString("hh:mm tt");
                        vm.OutTime = sft.OutTime;
                        vm.OutTimeStr = sft.OutTime.HasValue ? DateTime.Today.Add(sft.OutTime.Value).ToString("hh:mm tt") : null;
                        vm.LateMinutes = sft.LateInMnt;
                        vm.EarlyMinutes = sft.ErlyLvMnt;
                        vm.WorkedMinutes = sft.WrkMnt;

                        if (sft.LateInMnt > 0)
                        {
                            int h = sft.LateInMnt / 60;
                            int m = sft.LateInMnt % 60;
                            vm.LateByStr = $"Late by {h:D2}:{m:D2}";
                        }
                        vm.WorkedHoursStr = sft.WrkMnt > 0 ? $"{sft.WrkMnt / 60:D2}:{sft.WrkMnt % 60:D2} Hrs worked" : "In Progress";
                    }
                    else
                    {
                        vm.WorkedHoursStr = "--";
                    }
                }
                else if (sft != null)
                {
                    if (sft.Sft != null)
                    {
                        vm.ShiftStart = sft.Sft.StTime.TimeOfDay;
                        vm.ShiftEnd = sft.Sft.EndTime.TimeOfDay;
                        vm.ShiftName = sft.Sft.NameEng ?? vm.ShiftName;
                    }

                    vm.InTime = sft.InTime;
                    vm.OutTime = sft.OutTime;
                    vm.InTimeStr = sft.InTime.HasValue ? DateTime.Today.Add(sft.InTime.Value).ToString("hh:mm tt") : null;
                    vm.OutTimeStr = sft.OutTime.HasValue ? DateTime.Today.Add(sft.OutTime.Value).ToString("hh:mm tt") : null;
                    vm.LateMinutes = sft.LateInMnt;
                    vm.EarlyMinutes = sft.ErlyLvMnt;
                    vm.WorkedMinutes = sft.WrkMnt;

                    if (sft.LateInMnt > 0)
                    {
                        int h = sft.LateInMnt / 60;
                        int m = sft.LateInMnt % 60;
                        vm.LateByStr = $"Late by {h:D2}:{m:D2}";
                    }
                    if (sft.ErlyLvMnt > 0)
                    {
                        int h = sft.ErlyLvMnt / 60;
                        int m = sft.ErlyLvMnt % 60;
                        vm.EarlyByStr = $"Early by {h:D2}:{m:D2}";
                    }

                    int wh = sft.WrkMnt / 60;
                    int wm = sft.WrkMnt % 60;
                    vm.WorkedHoursStr = $"{wh:D2}:{wm:D2} Hrs worked";
                }
                else
                {
                    vm.WorkedHoursStr = "00:00 Hrs worked";
                }

                // Status classification
                if (vm.IsWeekend || d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday)
                {
                    vm.IsWeekend = true;
                    vm.Status = "Weekend";
                    vm.StatusColor = "yellow";
                    vm.BarType = "weekend";
                    vm.CanRequestRegularization = false;
                }
                else if (vm.IsFuture)
                {
                    vm.Status = "Upcoming";
                    vm.StatusColor = "muted";
                    vm.BarType = "future";
                    vm.CanRequestRegularization = false;
                }
                else if (vm.IsToday)
                {
                    vm.CanRequestRegularization = false;
                    if (todayPunch.HasValue || (sft != null && sft.InTime.HasValue))
                    {
                        vm.Status = "Checked In";
                        vm.StatusColor = "green";
                        vm.BarType = "checkedin";
                    }
                    else
                    {
                        vm.Status = "Not Checked In";
                        vm.StatusColor = "amber";
                        vm.BarType = "future";
                    }
                }
                else
                {
                    // Regularization overlay
                    regByDate.TryGetValue(d, out var reg);
                    if (reg != null)
                    {
                        vm.RegId = reg.AtnExpId;
                        vm.RegReqNo = reg.ReqNo ?? $"REG-{reg.AtnExpId}";
                        vm.RegWfStatus = reg.WfStatus;
                        vm.RegReasonCategory = reg.ReasonCategory;
                        vm.RegDescription = reg.ReasonDescription;
                        vm.RegInTime = reg.ReqInTime;
                        vm.RegOutTime = reg.ReqOutTime;

                        if (reg.WfStatus == "Approved")
                        {
                            vm.Status = "Regularized";
                            vm.StatusColor = "cyan";
                            vm.BarType = "regularized";
                        }
                        else if (reg.WfStatus == "Rejected")
                        {
                            vm.Status = "Regularization Rejected";
                            vm.StatusColor = "red";
                            vm.BarType = "absent";
                        }
                        else
                        {
                            vm.Status = "Pending Regularization";
                            vm.StatusColor = "purple";
                            vm.BarType = "pending";
                        }
                    }
                    else if (vm.InTime.HasValue && vm.OutTime.HasValue)
                    {
                        if (vm.LateMinutes > 0 || vm.EarlyMinutes > 0)
                        {
                            vm.Status = vm.LateMinutes > 0 ? "Late IN" : "Early Leave";
                            vm.StatusColor = "amber";
                            vm.BarType = "worked";
                        }
                        else
                        {
                            vm.Status = "Present";
                            vm.StatusColor = "green";
                            vm.BarType = "worked";
                        }
                    }
                    else if (vm.InTime.HasValue && !vm.OutTime.HasValue)
                    {
                        vm.Status = "Missing Out";
                        vm.StatusColor = "amber";
                        vm.BarType = "worked";
                    }
                    else if (!vm.InTime.HasValue && vm.OutTime.HasValue)
                    {
                        vm.Status = "Missing In";
                        vm.StatusColor = "amber";
                        vm.BarType = "worked";
                    }
                    else
                    {
                        vm.Status = "Absent";
                        vm.StatusColor = "red";
                        vm.BarType = "absent";
                    }

                    // Can request regularization if:
                    // 1. Working day (Sun - Thu) - NOT weekend
                    // 2. Not future date and not today (Date < Today)
                    // 3. Within max days allowed window
                    // 4. Either absent, late, early, missing punch, or not already approved
                    bool isWeekendDay = (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday || vm.IsWeekend);
                    bool withinWindow = d >= minAllowedDate && d < today;
                    bool notAlreadyApproved = (reg == null || reg.WfStatus == "Rejected");
                    vm.CanRequestRegularization = !isWeekendDay && withinWindow && notAlreadyApproved;
                }

                // Proportional timeline calculation against standard working shift (07:30 AM to 03:30 PM)
                TimeSpan shiftStart = vm.ShiftStart > TimeSpan.Zero ? vm.ShiftStart : defaultStart;
                TimeSpan shiftEnd = vm.ShiftEnd > TimeSpan.Zero ? vm.ShiftEnd : defaultEnd;
                double totalShiftMinutes = (shiftEnd - shiftStart).TotalMinutes;
                if (totalShiftMinutes <= 0)
                {
                    shiftStart = defaultStart;
                    shiftEnd = defaultEnd;
                    totalShiftMinutes = 480;
                }

                vm.ShiftStart = shiftStart;
                vm.ShiftEnd = shiftEnd;
                vm.ShiftStartStr = DateTime.Today.Add(shiftStart).ToString("hh:mm tt");
                vm.ShiftEndStr = DateTime.Today.Add(shiftEnd).ToString("hh:mm tt");

                if (!vm.IsWeekend && !vm.IsFuture && vm.Status != "Absent" && vm.Status != "Not Checked In" && (vm.InTime.HasValue || vm.OutTime.HasValue))
                {
                    double shiftStartMinutes = shiftStart.TotalMinutes;

                    // In-time offset
                    double inOffsetMinutes = 0;
                    if (vm.InTime.HasValue)
                    {
                        double actualInMinutes = vm.InTime.Value.TotalMinutes;
                        inOffsetMinutes = actualInMinutes - shiftStartMinutes;
                        if (inOffsetMinutes < 0) inOffsetMinutes = 0; // In before or at shift start
                        if (inOffsetMinutes > totalShiftMinutes) inOffsetMinutes = totalShiftMinutes;
                    }

                    // Out-time offset
                    double outOffsetMinutes = totalShiftMinutes;
                    if (vm.OutTime.HasValue)
                    {
                        double actualOutMinutes = vm.OutTime.Value.TotalMinutes;
                        outOffsetMinutes = actualOutMinutes - shiftStartMinutes;
                        if (outOffsetMinutes > totalShiftMinutes) outOffsetMinutes = totalShiftMinutes; // Worked past shift end
                        if (outOffsetMinutes < inOffsetMinutes) outOffsetMinutes = inOffsetMinutes;
                        if (outOffsetMinutes < 0) outOffsetMinutes = 0;
                    }
                    else if (vm.IsToday)
                    {
                        double nowMinutes = DateTime.Now.TimeOfDay.TotalMinutes - shiftStartMinutes;
                        if (nowMinutes < inOffsetMinutes) nowMinutes = inOffsetMinutes + 15;
                        if (nowMinutes > totalShiftMinutes) nowMinutes = totalShiftMinutes;
                        outOffsetMinutes = nowMinutes;
                    }

                    vm.InPosPercent = Math.Round((inOffsetMinutes / totalShiftMinutes) * 100.0, 2);
                    vm.OutPosPercent = Math.Round((outOffsetMinutes / totalShiftMinutes) * 100.0, 2);

                    if (vm.OutPosPercent < vm.InPosPercent)
                    {
                        vm.OutPosPercent = vm.InPosPercent;
                    }

                    vm.WorkedWidthPercent = Math.Round(Math.Max(1.0, vm.OutPosPercent - vm.InPosPercent), 2);

                    // Unworked late arrival period: 0% to InPos%
                    if (vm.InPosPercent > 0.5)
                    {
                        vm.HasLatePeriod = true;
                        vm.LateWidthPercent = vm.InPosPercent;
                    }

                    // Unworked early departure period: OutPos% to 100%
                    if (!vm.IsToday && vm.OutPosPercent < 99.5)
                    {
                        vm.HasEarlyPeriod = true;
                        vm.EarlyWidthPercent = Math.Round(100.0 - vm.OutPosPercent, 2);
                    }
                }
                else
                {
                    vm.InPosPercent = 0;
                    vm.OutPosPercent = 100;
                    vm.WorkedWidthPercent = (vm.IsWeekend || vm.IsFuture || vm.Status == "Absent" || vm.Status == "Not Checked In") ? 0 : 100;
                    vm.LateWidthPercent = 0;
                    vm.EarlyWidthPercent = 0;
                }

                list.Add(vm);
            }

            return list;
        }

        // =====================================================================
        // REQUEST REGULARIZATION DATES & AUTO MISSING HOURS (Image 1 & 2)
        // =====================================================================
        public class RegularizationDateItemDto
        {
            public DateTime Date { get; set; }
            public string WorkedDayText { get; set; }       // "Nov 08 Sun"
            public string DayOfWeekName { get; set; }
            public bool IsWeekend { get; set; }
            public bool IsEligible { get; set; }
            public string IneligibilityReason { get; set; }

            // Existing punches
            public TimeSpan? ActualInTime { get; set; }
            public TimeSpan? ActualOutTime { get; set; }
            public string ActualInTimeText { get; set; }
            public string ActualOutTimeText { get; set; }
            public string ActualWorkedHoursText { get; set; }
            public bool IsAbsent { get; set; }

            // Suggested / Pre-filled Regularization
            public DateTime CheckInDateTime { get; set; }
            public DateTime CheckOutDateTime { get; set; }
            public string CheckInDateTimeStr { get; set; }  // "dd-MMM-yyyy hh:mm AM/PM"
            public string CheckOutDateTimeStr { get; set; }
            public string CheckInTimeOnly { get; set; }     // "07:30"
            public string CheckOutTimeOnly { get; set; }    // "15:30"
            public string DateStr { get; set; }             // "2026-11-08"
            public string TotalHoursText { get; set; }      // "08:00"
            public decimal TotalHours { get; set; }

            // Suggested category and note
            public string SuggestedReason { get; set; }    // "Direct Customer Visit", "Forgot to check-in", etc.
            public string Description { get; set; }
            public string MissingPeriodNote { get; set; }
        }

        public async Task<List<RegularizationDateItemDto>> GetApplicableDatesForRegularizationAsync(
            int empSysId, string periodType, DateTime targetDate, DateTime? customEndDate = null)
        {
            var config = await GetConfigAsync();
            DateTime today = DateTime.Today;
            DateTime minAllowedDate = today.AddDays(-config.MaxDaysAllowed);

            DateTime fromDate;
            DateTime toDate;

            switch ((periodType ?? "Day").ToUpper())
            {
                case "DAY":
                    fromDate = targetDate.Date;
                    toDate = targetDate.Date;
                    break;

                case "WEEK":
                    // Sunday as week start in Qatar
                    int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    fromDate = targetDate.AddDays(-diff).Date;
                    toDate = fromDate.AddDays(4).Date; // Sunday to Thursday (5 working days)
                    break;

                case "MONTH":
                    fromDate = new DateTime(targetDate.Year, targetDate.Month, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    break;

                case "CUSTOM":
                default:
                    fromDate = targetDate.Date;
                    toDate = (customEndDate ?? targetDate.AddDays(4)).Date;
                    break;
            }

            // Cap to allowed window
            if (!config.AllowFutureDates && toDate > today)
            {
                toDate = today;
            }

            DateTime qFrom = fromDate.Date;
            DateTime qTo = toDate.Date;

            var shiftList = await _hrmContext.HrEmpAtndcSft
                .Include(s => s.Sft)
                .Where(s => s.EmpSysId == empSysId && s.AtndcDate >= qFrom && s.AtndcDate <= qTo && s.RecStat == 1)
                .ToListAsync();

            var shiftRecords = shiftList
                .GroupBy(s => s.AtndcDate.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var defaultStart = new TimeSpan(7, 30, 0);
            var defaultEnd = new TimeSpan(15, 30, 0);

            var result = new List<RegularizationDateItemDto>();

            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                bool isWeekend = (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday);
                // Prompt: "Only working days (Sunday to Thursday) should be available; weekends should be disabled."
                if (isWeekend)
                {
                    continue; // Skip weekends
                }

                bool isFuture = (d > today && !config.AllowFutureDates);
                bool isOlderThanWindow = (d < minAllowedDate);

                bool isEligible = !isFuture && !isOlderThanWindow;
                string reason = null;
                if (isFuture) reason = "Future dates are not eligible for regularization.";
                else if (isOlderThanWindow) reason = $"Date exceeds permitted {config.MaxDaysAllowed}-day window.";

                shiftRecords.TryGetValue(d, out var sft);

                TimeSpan shiftStart = sft?.Sft != null ? sft.Sft.StTime.TimeOfDay : defaultStart;
                TimeSpan shiftEnd = sft?.Sft != null ? sft.Sft.EndTime.TimeOfDay : defaultEnd;

                TimeSpan? actualIn = sft?.InTime;
                TimeSpan? actualOut = sft?.OutTime;
                bool hasPunches = actualIn.HasValue || actualOut.HasValue;
                bool isAbsent = !hasPunches;

                // Auto-detect missing period:
                TimeSpan reqIn;
                TimeSpan reqOut;
                string suggestedCategory;
                string missingNote;

                if (isAbsent)
                {
                    reqIn = shiftStart;
                    reqOut = shiftEnd;
                    suggestedCategory = "Forgot to check-in";
                    missingNote = "No attendance recorded. Please enter check-in and check-out times.";
                }
                else if (actualIn.HasValue && !actualOut.HasValue)
                {
                    // Missing Out Punch
                    reqIn = actualIn.Value;
                    reqOut = shiftEnd;
                    suggestedCategory = "Forgot to check-out";
                    missingNote = $"Missing check-out punch (Shift ends at {DateTime.Today.Add(shiftEnd):hh:mm tt})";
                }
                else if (!actualIn.HasValue && actualOut.HasValue)
                {
                    // Missing In Punch
                    reqIn = shiftStart;
                    reqOut = actualOut.Value;
                    suggestedCategory = "Forgot to check-in";
                    missingNote = $"Missing check-in punch (Shift starts at {DateTime.Today.Add(shiftStart):hh:mm tt})";
                }
                else
                {
                    // Both punches exist - prioritize largest missing interval
                    int earlyMnt = sft?.ErlyLvMnt ?? (int)Math.Max(0, (shiftEnd - actualOut.Value).TotalMinutes);
                    int lateMnt = sft?.LateInMnt ?? (int)Math.Max(0, (actualIn.Value - shiftStart).TotalMinutes);

                    if (earlyMnt > 15 && earlyMnt >= lateMnt)
                    {
                        // Significant early departure (e.g. left at 01:32 PM, shift ends at 03:30 PM)
                        reqIn = actualOut.Value;
                        reqOut = shiftEnd;
                        suggestedCategory = "Direct Customer Visit";
                        missingNote = $"Missing afternoon hours ({DateTime.Today.Add(actualOut.Value):hh:mm tt} - {DateTime.Today.Add(shiftEnd):hh:mm tt})";
                    }
                    else if (lateMnt > 15)
                    {
                        // Significant late arrival (e.g. arrived at 10:35 AM, shift starts at 07:30 AM)
                        reqIn = shiftStart;
                        reqOut = actualIn.Value;
                        suggestedCategory = "Direct Customer Visit";
                        missingNote = $"Missing morning hours ({DateTime.Today.Add(shiftStart):hh:mm tt} - {DateTime.Today.Add(actualIn.Value):hh:mm tt})";
                    }
                    else if (earlyMnt > 0)
                    {
                        reqIn = actualOut.Value;
                        reqOut = shiftEnd;
                        suggestedCategory = "Direct Customer Visit";
                        missingNote = $"Early departure ({DateTime.Today.Add(actualOut.Value):hh:mm tt} - {DateTime.Today.Add(shiftEnd):hh:mm tt})";
                    }
                    else if (lateMnt > 0)
                    {
                        reqIn = shiftStart;
                        reqOut = actualIn.Value;
                        suggestedCategory = "Direct Customer Visit";
                        missingNote = $"Late arrival ({DateTime.Today.Add(shiftStart):hh:mm tt} - {DateTime.Today.Add(actualIn.Value):hh:mm tt})";
                    }
                    else
                    {
                        reqIn = shiftStart;
                        reqOut = shiftEnd;
                        suggestedCategory = "Other";
                        missingNote = "Standard full day shift.";
                    }
                }

                DateTime inDt = d.Add(reqIn);
                DateTime outDt = d.Add(reqOut);
                if (outDt < inDt) outDt = inDt;

                decimal totalHours = Math.Round((decimal)(outDt - inDt).TotalHours, 2);
                int th = (int)totalHours;
                int tm = (int)Math.Round((totalHours - th) * 60);

                string actualWorkedText = "00:00 Hrs";
                if (sft != null && sft.WrkMnt > 0)
                {
                    int awh = sft.WrkMnt / 60;
                    int awm = sft.WrkMnt % 60;
                    actualWorkedText = $"{awh:D2}:{awm:D2} Hrs";
                }
                else if (actualIn.HasValue && actualOut.HasValue)
                {
                    int diffM = (int)(actualOut.Value - actualIn.Value).TotalMinutes;
                    if (diffM > 0)
                    {
                        actualWorkedText = $"{diffM / 60:D2}:{diffM % 60:D2} Hrs";
                    }
                }

                result.Add(new RegularizationDateItemDto
                {
                    Date = d,
                    WorkedDayText = $"{d:MMM dd} {d:ddd}",
                    DayOfWeekName = d.ToString("dddd"),
                    IsWeekend = false,
                    IsEligible = isEligible,
                    IneligibilityReason = reason,
                    ActualInTime = actualIn,
                    ActualOutTime = actualOut,
                    ActualInTimeText = actualIn.HasValue ? DateTime.Today.Add(actualIn.Value).ToString("hh:mm tt") : "-",
                    ActualOutTimeText = actualOut.HasValue ? DateTime.Today.Add(actualOut.Value).ToString("hh:mm tt") : "-",
                    ActualWorkedHoursText = actualWorkedText,
                    IsAbsent = isAbsent,
                    CheckInDateTime = inDt,
                    CheckOutDateTime = outDt,
                    CheckInDateTimeStr = inDt.ToString("dd-MMM-yyyy hh:mm tt"),
                    CheckOutDateTimeStr = outDt.ToString("dd-MMM-yyyy hh:mm tt"),
                    CheckInTimeOnly = inDt.ToString("HH:mm"),
                    CheckOutTimeOnly = outDt.ToString("HH:mm"),
                    DateStr = d.ToString("yyyy-MM-dd"),
                    TotalHours = totalHours,
                    TotalHoursText = $"{th:D2}:{tm:D2}",
                    SuggestedReason = suggestedCategory,
                    Description = (suggestedCategory == "Other") ? "" : suggestedCategory,
                    MissingPeriodNote = missingNote
                });
            }

            return result;
        }

        // =====================================================================
        // SUBMIT REGULARIZATION REQUEST
        // =====================================================================
        public class FlexibleDateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string str = reader.GetString();
                    if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return dt;
                    if (DateTime.TryParse(str, out dt))
                        return dt;
                }
                return reader.GetDateTime();
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
            }
        }

        public class RegularizationSubmitItemDto
        {
            [JsonConverter(typeof(FlexibleDateTimeConverter))]
            public DateTime Date { get; set; }

            [JsonConverter(typeof(FlexibleDateTimeConverter))]
            public DateTime CheckIn { get; set; }

            [JsonConverter(typeof(FlexibleDateTimeConverter))]
            public DateTime CheckOut { get; set; }

            public string ReasonCategory { get; set; }
            public string Description { get; set; }
        }

        public async Task<(bool Success, string Message, List<string> GeneratedReqNos)> SubmitRegularizationAsync(
            int empSysId,
            string periodType,
            List<RegularizationSubmitItemDto> items,
            IFormFile fileAttachment,
            string webRootPath,
            int currentUserId)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return (false, "No working days selected for attendance regularization.", new List<string>());
                }

                var config = await GetConfigAsync();
                DateTime today = DateTime.Today;
                DateTime minAllowedDate = today.AddDays(-config.MaxDaysAllowed);

                // Handle file upload
                string attachmentPath = null;
                string attachmentFileName = null;

                if (fileAttachment != null && fileAttachment.Length > 0)
                {
                    if (fileAttachment.Length > 5 * 1024 * 1024)
                    {
                        return (false, "File size exceeds the 5MB limit.", new List<string>());
                    }

                    string ext = Path.GetExtension(fileAttachment.FileName).ToLowerInvariant();
                    string[] allowedExts = { ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
                    if (!allowedExts.Contains(ext))
                    {
                        return (false, "File format not supported. Supported: PDF, XLS, DOC, Images.", new List<string>());
                    }

                    string uploadsFolder = Path.Combine(webRootPath, "uploads", "regularization");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = $"reg_{empSysId}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                    string fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await fileAttachment.CopyToAsync(stream);
                    }

                    attachmentPath = $"/uploads/regularization/{uniqueFileName}";
                    attachmentFileName = fileAttachment.FileName;
                }

                // Resolve Reporting Manager based on managerial hierarchy
                int reportingManagerId = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId && j.WkStat == 1 && j.RptMngId != null)
                    .Select(j => j.RptMngId.Value)
                    .FirstOrDefaultAsync();

                // Fallback to HR/Admin if no direct manager configured
                if (reportingManagerId <= 0)
                {
                    reportingManagerId = 6; // Kaushalya Fonseka
                }

                // Resolve Final HR Approver from HrLvWfConfig
                int finalHrApproverId = 6;
                var wfCfg = await _hrmContext.HrLvWfConfig
                    .FirstOrDefaultAsync(w => w.WfType == "Regularization" && w.RecStat == 1);
                if (wfCfg != null && !string.IsNullOrWhiteSpace(wfCfg.ApproverConfig))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(wfCfg.ApproverConfig);
                        if (doc.RootElement.TryGetProperty("FinalApproverEmpId", out var f) && f.TryGetInt32(out int fid) && fid > 0)
                            finalHrApproverId = fid;
                    }
                    catch { }
                }

                var (submitterName, _) = await GetEmployeeDetailsAsync(empSysId);
                var createdReqNos = new List<string>();
                var createdRegIds = new List<int>();

                foreach (var item in items)
                {
                    DateTime regDate = item.Date.Date;

                    // Validations
                    if (!config.AllowFutureDates && regDate > today)
                    {
                        return (false, $"Attendance Regularization cannot be submitted for future date ({regDate:dd-MMM-yyyy}).", new List<string>());
                    }

                    if (regDate < minAllowedDate)
                    {
                        return (false, $"Attendance Regularization cannot be submitted for {regDate:dd-MMM-yyyy} as it exceeds the permitted {config.MaxDaysAllowed}-day window.", new List<string>());
                    }

                    if (regDate.DayOfWeek == DayOfWeek.Friday || regDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        return (false, $"Attendance Regularization is only allowed on working days (Sunday to Thursday). {regDate:dd-MMM-yyyy} is a weekend.", new List<string>());
                    }

                    // Check for existing pending/approved request
                    var existingPending = await _hrmContext.HrEmpAttendExp
                        .FirstOrDefaultAsync(r => r.EmpSysId == empSysId && r.StDate == regDate && r.RecStat == 1 && (r.WfStatus == "PendingManager" || r.WfStatus == "PendingHR" || r.WfStatus == "Approved"));

                    if (existingPending != null)
                    {
                        return (false, $"An active regularization request ({existingPending.ReqNo}) already exists for {regDate:dd-MMM-yyyy}.", new List<string>());
                    }

                    // Look up actual punches
                    var sft = await _hrmContext.HrEmpAtndcSft
                        .FirstOrDefaultAsync(s => s.EmpSysId == empSysId && s.AtndcDate == regDate && s.RecStat == 1);

                    TimeSpan reqIn = item.CheckIn.TimeOfDay;
                    TimeSpan reqOut = item.CheckOut.TimeOfDay;
                    decimal totalHours = Math.Round((decimal)(item.CheckOut - item.CheckIn).TotalHours, 2);
                    if (totalHours < 0) totalHours = 0;

                    // Generate unique Request Number
                    string reqNo = $"REG-{DateTime.Now:yyyyMM}-{new Random().Next(1000, 9999)}";

                    var regRecord = new HrEmpAttendExp
                    {
                        EmpSysId = empSysId,
                        StDate = regDate,
                        EndDate = regDate,
                        Del_StDate = regDate,
                        Del_EndDate = regDate,
                        Note = item.Description ?? item.ReasonCategory,
                        RqstId = empSysId,
                        ReqNo = reqNo,
                        ReqInTime = reqIn,
                        ReqOutTime = reqOut,
                        ActualInTime = sft?.InTime,
                        ActualOutTime = sft?.OutTime,
                        TotalHours = totalHours,
                        ReasonCategory = item.ReasonCategory ?? "Other",
                        ReasonDescription = item.Description,
                        AttachmentPath = attachmentPath,
                        AttachmentFileName = attachmentFileName,
                        PeriodType = periodType ?? "Day",
                        WfStatus = "PendingManager",
                        WfStep = 1,
                        ManagerEmpId = reportingManagerId,
                        HrEmpId = finalHrApproverId,
                        IsLopApplied = false,
                        RecStat = 1,
                        CrtId = currentUserId > 0 ? currentUserId : empSysId,
                        CrtDateTime = DateTime.Now
                    };

                    _hrmContext.HrEmpAttendExp.Add(regRecord);
                    await _hrmContext.SaveChangesAsync();

                    createdReqNos.Add(reqNo);
                    createdRegIds.Add(regRecord.AtnExpId);

                    // Log Workflow History
                    _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                    {
                        WfType = "Regularization",
                        RefId = regRecord.AtnExpId,
                        PrcYear = regDate.Year,
                        PrcMonth = regDate.Month,
                        StepNumber = 1,
                        StepName = "Regularization Request Submission",
                        Action = "Submitted",
                        ActorEmpSysId = currentUserId > 0 ? currentUserId : empSysId,
                        ActorName = submitterName,
                        Comments = $"Submitted for {regDate:dd-MMM-yyyy}. Reason: {item.ReasonCategory} - {item.Description}",
                        ActionDateTime = DateTime.Now
                    });

                    await _hrmContext.SaveChangesAsync();
                }

                // Dispatch email alerts to Reporting Manager for approval
                foreach (var newRegId in createdRegIds)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await SendSubmissionAlertToManagerAsync(newRegId); }
                        catch (Exception ex) { _logger?.LogError(ex, "Failed to send submission email for RegId: {RegId}", newRegId); }
                    });
                }

                return (true, $"Regularization request successfully submitted ({createdReqNos.Count} day(s)) and forwarded to Reporting Manager for approval.", createdReqNos);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error submitting attendance regularization for EmpSysId: {EmpSysId}", empSysId);
                return (false, "Error submitting regularization: " + ex.Message, new List<string>());
            }
        }

        // =====================================================================
        // WORKFLOW APPROVALS: REPORTING MANAGER & HR
        // =====================================================================
        public async Task<(bool Success, string Message)> ApproveByReportingManagerAsync(int regId, int actorEmpSysId, string comments)
        {
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId && r.RecStat == 1);
                if (reg == null) return (false, "Regularization request not found.");

                if (reg.WfStatus != "PendingManager")
                {
                    return (false, $"Request is not in Pending Manager status (Current: {reg.WfStatus}).");
                }

                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);

                reg.ManagerAction = "Approved";
                reg.ManagerActionDateTime = DateTime.Now;
                reg.ManagerComments = comments;
                reg.WfStatus = "PendingHR";
                reg.WfStep = 2;
                reg.MdfId = actorEmpSysId;
                reg.MdfDateTime = DateTime.Now;

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Regularization",
                    RefId = reg.AtnExpId,
                    PrcYear = reg.StDate.Year,
                    PrcMonth = reg.StDate.Month,
                    StepNumber = 2,
                    StepName = "Reporting Manager Approval",
                    Action = "Approved",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = actorName,
                    Comments = string.IsNullOrWhiteSpace(comments) ? "Approved at Step 1 (Reporting Manager)." : comments,
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Dispatch email alert to Final HR Approver
                _ = Task.Run(async () =>
                {
                    try { await SendManagerApprovalAlertToHrAsync(reg.AtnExpId); }
                    catch (Exception ex) { _logger?.LogError(ex, "Failed to send manager approval email for RegId: {RegId}", reg.AtnExpId); }
                });

                return (true, $"Request {reg.ReqNo} approved and forwarded to HR for final approval.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in ApproveByReportingManagerAsync for RegId: {RegId}", regId);
                return (false, "Error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ApproveByHrAsync(int regId, int actorEmpSysId, string comments)
        {
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId && r.RecStat == 1);
                if (reg == null) return (false, "Regularization request not found.");

                if (reg.WfStatus != "PendingHR")
                {
                    return (false, $"Request is not in Pending HR status (Current: {reg.WfStatus}).");
                }

                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);

                reg.HrAction = "Approved";
                reg.HrActionDateTime = DateTime.Now;
                reg.HrComments = comments;
                reg.WfStatus = "Approved";
                reg.WfStep = 3;
                reg.AprvdStatId = 1;
                reg.AprvdId = actorEmpSysId;
                reg.AprvdDateTime = DateTime.Now;
                reg.AprvdRmks = comments;
                reg.MdfId = actorEmpSysId;
                reg.MdfDateTime = DateTime.Now;

                // Update HR_EmpAtndcSft record to regularize attendance!
                var sft = await _hrmContext.HrEmpAtndcSft
                    .FirstOrDefaultAsync(s => s.EmpSysId == reg.EmpSysId && s.AtndcDate == reg.StDate && s.RecStat == 1);

                if (sft != null)
                {
                    sft.InTime = reg.ReqInTime;
                    sft.OutTime = reg.ReqOutTime;
                    sft.InSource = "REGULARIZED";
                    sft.OutSource = "REGULARIZED";

                    if (reg.ReqInTime.HasValue && reg.ReqOutTime.HasValue)
                    {
                        int workedMins = (int)(reg.ReqOutTime.Value - reg.ReqInTime.Value).TotalMinutes;
                        if (workedMins < 0) workedMins = 0;
                        sft.WrkMnt = workedMins;
                    }

                    sft.AtndcStatId = 1; // On Time / Regularized
                    sft.SlryDctFct = 0m;  // Waive deduction
                    sft.LateInMnt = 0;
                    sft.ErlyLvMnt = 0;
                    sft.MnlMdfd = true;
                    sft.Note = $"Regularized ({reg.ReqNo}): {reg.ReasonCategory}";
                }

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Regularization",
                    RefId = reg.AtnExpId,
                    PrcYear = reg.StDate.Year,
                    PrcMonth = reg.StDate.Month,
                    StepNumber = 3,
                    StepName = "Final HR Approval",
                    Action = "Approved",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = actorName,
                    Comments = string.IsNullOrWhiteSpace(comments) ? "Final HR approval granted. Attendance updated." : comments,
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Dispatch approval confirmation email to Employee (and CC Manager)
                _ = Task.Run(async () =>
                {
                    try { await SendApprovalAlertToEmployeeAsync(reg.AtnExpId); }
                    catch (Exception ex) { _logger?.LogError(ex, "Failed to send HR approval email for RegId: {RegId}", reg.AtnExpId); }
                });

                return (true, $"Regularization request {reg.ReqNo} has received Final HR Approval and attendance was regularized.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in ApproveByHrAsync for RegId: {RegId}", regId);
                return (false, "Error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> RejectRegularizationAsync(
            int regId, int actorEmpSysId, string comments, bool isManagerStage)
        {
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId && r.RecStat == 1);
                if (reg == null) return (false, "Regularization request not found.");

                var (actorName, _) = await GetEmployeeDetailsAsync(actorEmpSysId);

                reg.WfStatus = "Rejected";
                reg.MdfId = actorEmpSysId;
                reg.MdfDateTime = DateTime.Now;

                string stepName;
                if (isManagerStage)
                {
                    reg.ManagerAction = "Rejected";
                    reg.ManagerActionDateTime = DateTime.Now;
                    reg.ManagerComments = comments;
                    stepName = "Reporting Manager Review";
                }
                else
                {
                    reg.HrAction = "Rejected";
                    reg.HrActionDateTime = DateTime.Now;
                    reg.HrComments = comments;
                    stepName = "Final HR Review";
                }

                // Loss of Pay (LOP) Enforcement on Rejected Full-Day Requests:
                // User Prompt: "Rejected full-day requests should be subject to Loss of Pay (LOP), where applicable."
                bool wasAbsent = (!reg.ActualInTime.HasValue && !reg.ActualOutTime.HasValue);
                if (wasAbsent)
                {
                    reg.IsLopApplied = true;
                    var sft = await _hrmContext.HrEmpAtndcSft
                        .FirstOrDefaultAsync(s => s.EmpSysId == reg.EmpSysId && s.AtndcDate == reg.StDate && s.RecStat == 1);

                    if (sft != null)
                    {
                        sft.AtndcStatId = 0; // Absent
                        sft.SlryDctFct = 1.0m; // Full-day Loss of Pay deduction
                        sft.MnlMdfd = true;
                        sft.Note = $"LOP Applied (Regularization Rejected: {comments})";
                    }
                }

                _hrmContext.HrPrWfHistory.Add(new HrPrWfHistory
                {
                    WfType = "Regularization",
                    RefId = reg.AtnExpId,
                    PrcYear = reg.StDate.Year,
                    PrcMonth = reg.StDate.Month,
                    StepNumber = isManagerStage ? 2 : 3,
                    StepName = stepName,
                    Action = "Rejected",
                    ActorEmpSysId = actorEmpSysId,
                    ActorName = actorName,
                    Comments = $"Rejected: {comments}" + (reg.IsLopApplied == true ? " (Loss of Pay applied)" : ""),
                    ActionDateTime = DateTime.Now
                });

                await _hrmContext.SaveChangesAsync();

                // Dispatch rejection email to Employee
                _ = Task.Run(async () =>
                {
                    try { await SendRejectionAlertToEmployeeAsync(reg.AtnExpId, comments, isManagerStage); }
                    catch (Exception ex) { _logger?.LogError(ex, "Failed to send rejection email for RegId: {RegId}", reg.AtnExpId); }
                });

                return (true, $"Request {reg.ReqNo} rejected." + (reg.IsLopApplied == true ? " Full-day Loss of Pay (LOP) has been applied." : ""));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in RejectRegularizationAsync for RegId: {RegId}", regId);
                return (false, "Error: " + ex.Message);
            }
        }

        // =====================================================================
        // LISTING & WORKFLOW HISTORY
        // =====================================================================
        public async Task<List<HrEmpAttendExp>> GetRegularizationRequestsAsync(int currentEmpSysId, int userGrpId, int? filterEmpSysId = null)
        {
            var (canViewAll, canViewSubordinates, allowedIds) = await GetAccessRightsAsync(currentEmpSysId, userGrpId);

            var query = _hrmContext.HrEmpAttendExp
                .Where(r => r.RecStat == 1 && r.ReqNo != null);

            if (filterEmpSysId.HasValue && filterEmpSysId.Value > 0)
            {
                if (canViewAll || allowedIds.Contains(filterEmpSysId.Value))
                {
                    query = query.Where(r => r.EmpSysId == filterEmpSysId.Value);
                }
                else
                {
                    query = query.Where(r => r.EmpSysId == currentEmpSysId);
                }
            }
            else if (!canViewAll)
            {
                query = query.Where(r => allowedIds.Contains(r.EmpSysId));
            }

            var list = await query
                .OrderByDescending(r => r.StDate)
                .ThenByDescending(r => r.AtnExpId)
                .ToListAsync();

            // Populate Employee names and departments
            var empIds = list.Select(r => r.EmpSysId).Distinct().ToList();
            var empDict = await (from e in _hrmContext.HrEmpGenInfo
                                 join j in _hrmContext.HrEmpJobAssign on e.EmpSysId equals j.EmpSysId into jg
                                 from j in jg.Where(x => x.WkStat == 1).DefaultIfEmpty()
                                 join d in _hrmContext.OrgDepartment on j.DeptId equals d.DeptId into dg
                                 from d in dg.DefaultIfEmpty()
                                 join ds in _hrmContext.OrgDesignation on j.DesigId equals ds.DesigId into dsg
                                 from ds in dsg.DefaultIfEmpty()
                                 where empIds.Contains(e.EmpSysId)
                                 select new
                                 {
                                     e.EmpSysId,
                                     e.EmpId,
                                     FullName = ((e.FirstName ?? "").Trim() + " " + (e.LastName ?? "").Trim()).Trim(),
                                     DeptName = d != null ? d.NameEng : "-",
                                     DesigName = ds != null ? ds.NameEng : "-"
                                 }).ToDictionaryAsync(e => e.EmpSysId, e => e);

            var reqEmpDates = list.Select(l => l.StDate.Date).Distinct().ToList();
            var reqEmpSysIds = list.Select(l => l.EmpSysId).Distinct().ToList();
            var reqSftList = await _hrmContext.HrEmpAtndcSft
                .Where(s => reqEmpSysIds.Contains(s.EmpSysId) && reqEmpDates.Contains(s.AtndcDate) && s.RecStat == 1)
                .ToListAsync();

            foreach (var item in list)
            {
                if (empDict.TryGetValue(item.EmpSysId, out var emp))
                {
                    item.EmpName = emp.FullName;
                    item.EmpIdStr = emp.EmpId;
                    item.DeptName = emp.DeptName;
                    item.DesigName = emp.DesigName;
                }

                var sft = reqSftList.FirstOrDefault(s => s.EmpSysId == item.EmpSysId && s.AtndcDate.Date == item.StDate.Date);
                if (!item.ActualInTime.HasValue && sft?.InTime.HasValue == true) item.ActualInTime = sft.InTime;
                if (!item.ActualOutTime.HasValue && sft?.OutTime.HasValue == true) item.ActualOutTime = sft.OutTime;

                string actualWorked = "00:00 Hrs";
                if (sft != null && sft.WrkMnt > 0)
                {
                    actualWorked = $"{(sft.WrkMnt / 60):D2}:{(sft.WrkMnt % 60):D2} Hrs";
                }
                else if (item.ActualInTime.HasValue && item.ActualOutTime.HasValue)
                {
                    int diffM = (int)(item.ActualOutTime.Value - item.ActualInTime.Value).TotalMinutes;
                    if (diffM > 0)
                    {
                        actualWorked = $"{(diffM / 60):D2}:{(diffM % 60):D2} Hrs";
                    }
                }
                item.ActualWorkedHoursText = actualWorked;
            }

            return list;
        }

        public async Task<int> GetPendingApprovalsCountAsync(int currentEmpSysId, int userGrpId)
        {
            var (canViewAll, _, _) = await GetAccessRightsAsync(currentEmpSysId, userGrpId);

            var query = _hrmContext.HrEmpAttendExp
                .Where(r => r.RecStat == 1 && r.ReqNo != null);

            if (canViewAll)
            {
                query = query.Where(r => r.WfStatus == "PendingManager" || r.WfStatus == "PendingHR");
            }
            else
            {
                query = query.Where(r =>
                    (r.WfStatus == "PendingManager" && r.ManagerEmpId == currentEmpSysId) ||
                    (r.WfStatus == "PendingHR" && r.HrEmpId == currentEmpSysId));
            }

            return await query.CountAsync();
        }

        public async Task<List<HrEmpAttendExp>> GetPendingApprovalsAsync(int currentEmpSysId, int userGrpId)
        {
            var (canViewAll, _, _) = await GetAccessRightsAsync(currentEmpSysId, userGrpId);

            // Reporting Manager pending: ManagerEmpId == currentEmpSysId AND WfStatus == 'PendingManager'
            // HR pending: HrEmpId == currentEmpSysId (or GrpId == 1/2) AND WfStatus == 'PendingHR'
            var query = _hrmContext.HrEmpAttendExp
                .Where(r => r.RecStat == 1 && r.ReqNo != null);

            if (canViewAll)
            {
                query = query.Where(r => r.WfStatus == "PendingManager" || r.WfStatus == "PendingHR");
            }
            else
            {
                query = query.Where(r =>
                    (r.WfStatus == "PendingManager" && r.ManagerEmpId == currentEmpSysId) ||
                    (r.WfStatus == "PendingHR" && r.HrEmpId == currentEmpSysId));
            }

            var list = await query
                .OrderByDescending(r => r.StDate)
                .ThenByDescending(r => r.AtnExpId)
                .ToListAsync();

            var empIds = list.Select(r => r.EmpSysId).Distinct().ToList();
            var empDict = await (from e in _hrmContext.HrEmpGenInfo
                                 join j in _hrmContext.HrEmpJobAssign on e.EmpSysId equals j.EmpSysId into jg
                                 from j in jg.Where(x => x.WkStat == 1).DefaultIfEmpty()
                                 join d in _hrmContext.OrgDepartment on j.DeptId equals d.DeptId into dg
                                 from d in dg.DefaultIfEmpty()
                                 where empIds.Contains(e.EmpSysId)
                                 select new
                                 {
                                     e.EmpSysId,
                                     e.EmpId,
                                     FullName = ((e.FirstName ?? "").Trim() + " " + (e.LastName ?? "").Trim()).Trim(),
                                     DeptName = d != null ? d.NameEng : "-"
                                 }).ToDictionaryAsync(e => e.EmpSysId, e => e);

            var appEmpDates = list.Select(l => l.StDate.Date).Distinct().ToList();
            var appEmpSysIds = list.Select(l => l.EmpSysId).Distinct().ToList();
            var appSftList = await _hrmContext.HrEmpAtndcSft
                .Where(s => appEmpSysIds.Contains(s.EmpSysId) && appEmpDates.Contains(s.AtndcDate) && s.RecStat == 1)
                .ToListAsync();

            foreach (var item in list)
            {
                if (empDict.TryGetValue(item.EmpSysId, out var emp))
                {
                    item.EmpName = emp.FullName;
                    item.EmpIdStr = emp.EmpId;
                    item.DeptName = emp.DeptName;
                }

                var sft = appSftList.FirstOrDefault(s => s.EmpSysId == item.EmpSysId && s.AtndcDate.Date == item.StDate.Date);
                if (!item.ActualInTime.HasValue && sft?.InTime.HasValue == true) item.ActualInTime = sft.InTime;
                if (!item.ActualOutTime.HasValue && sft?.OutTime.HasValue == true) item.ActualOutTime = sft.OutTime;

                string actualWorked = "00:00 Hrs";
                if (sft != null && sft.WrkMnt > 0)
                {
                    actualWorked = $"{(sft.WrkMnt / 60):D2}:{(sft.WrkMnt % 60):D2} Hrs";
                }
                else if (item.ActualInTime.HasValue && item.ActualOutTime.HasValue)
                {
                    int diffM = (int)(item.ActualOutTime.Value - item.ActualInTime.Value).TotalMinutes;
                    if (diffM > 0)
                    {
                        actualWorked = $"{(diffM / 60):D2}:{(diffM % 60):D2} Hrs";
                    }
                }
                item.ActualWorkedHoursText = actualWorked;
            }

            return list;
        }

        public async Task<List<HrPrWfHistory>> GetWorkflowHistoryAsync(int regId)
        {
            return await _hrmContext.HrPrWfHistory
                .Where(h => h.WfType == "Regularization" && h.RefId == regId)
                .OrderBy(h => h.HistoryId)
                .ToListAsync();
        }

        private async Task<(string Name, string Email)> GetEmployeeDetailsAsync(int empSysId)
        {
            string name = "System";
            string email = "";

            try
            {
                var emp = await (from e in _hrmContext.HrEmpGenInfo
                                 join j in _hrmContext.HrEmpJobAssign on e.EmpSysId equals j.EmpSysId into jg
                                 from j in jg.Where(x => x.WkStat == 1).DefaultIfEmpty()
                                 where e.EmpSysId == empSysId
                                 select new
                                 {
                                     e.EmpId,
                                     FullName = ((e.FirstName ?? "").Trim() + " " + (e.LastName ?? "").Trim()).Trim(),
                                     j.OfcEmail
                                 }).FirstOrDefaultAsync();

                if (emp != null)
                {
                    name = $"{emp.FullName} ({emp.EmpId})";
                    email = emp.OfcEmail ?? "";
                }
            }
            catch { }

            return (name, email);
        }

        // =====================================================================
        // EMAIL NOTIFICATIONS (WORKFLOW STAGE ALERTS & CENTERED HTML TEMPLATES)
        // =====================================================================
        public class EmployeeEmailInfo
        {
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
            public string Department { get; set; }
            public string Designation { get; set; }
            public string OfficialEmail { get; set; }
        }

        public async Task<EmployeeEmailInfo> GetEmployeeFullDetailsAsync(int empSysId)
        {
            var info = new EmployeeEmailInfo
            {
                EmpSysId = empSysId,
                EmpId = "",
                FullName = "Employee",
                Department = "—",
                Designation = "—",
                OfficialEmail = ""
            };

            if (empSysId <= 0) return info;

            try
            {
                var emp = await _hrmContext.HrEmpGenInfo.FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
                if (emp != null)
                {
                    info.EmpId = emp.EmpId ?? "";
                    info.FullName = ((emp.FirstName ?? "").Trim() + " " + (emp.LastName ?? "").Trim()).Trim();
                    if (string.IsNullOrWhiteSpace(info.FullName)) info.FullName = emp.FullName ?? $"Emp #{empSysId}";
                }

                var job = await _hrmContext.HrEmpJobAssign
                    .Where(j => j.EmpSysId == empSysId && j.WkStat == 1)
                    .OrderByDescending(j => j.StDate)
                    .FirstOrDefaultAsync();

                if (job == null)
                {
                    job = await _hrmContext.HrEmpJobAssign
                        .Where(j => j.EmpSysId == empSysId)
                        .OrderByDescending(j => j.StDate)
                        .FirstOrDefaultAsync();
                }

                if (job != null)
                {
                    if (!string.IsNullOrWhiteSpace(job.OfcEmail))
                    {
                        info.OfficialEmail = job.OfcEmail.Trim();
                    }

                    if (job.DeptId > 0)
                    {
                        var dept = await _hrmContext.OrgDepartment.FirstOrDefaultAsync(d => d.DeptId == job.DeptId);
                        if (dept != null) info.Department = dept.NameEng ?? "";
                    }

                    if (job.DesigId > 0)
                    {
                        var desg = await _hrmContext.OrgDesignation.FirstOrDefaultAsync(d => d.DesigId == job.DesigId);
                        if (desg != null) info.Designation = desg.NameEng ?? "";
                    }
                }

                if (string.IsNullOrWhiteSpace(info.OfficialEmail) && emp != null && !string.IsNullOrWhiteSpace(emp.PsnEmail))
                {
                    info.OfficialEmail = emp.PsnEmail.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to resolve employee full details for EmpSysId: {EmpSysId}", empSysId);
            }

            return info;
        }

        public string GetAppBaseUrl()
        {
            string baseUrl = _configuration?["AppSettings:AppBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');
            baseUrl = _configuration?["WorkflowEngine:CallbackBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl)) return baseUrl.TrimEnd('/');
            return "http://localhost:55465";
        }

        private string BuildCenteredEmailHtml(
            string title,
            string statusBadge,
            string badgeColor,
            string badgeBgColor,
            string badgeBorderColor,
            string employeeName,
            string empId,
            string department,
            string designation,
            string reqNo,
            DateTime atndcDate,
            string requestedTiming,
            string actualTiming,
            string durationHours,
            string reasonCategory,
            string remarks,
            string actionNotes,
            string actionBtnText,
            string actionBtnUrl)
        {
            string actionButtonHtml = "";
            if (!string.IsNullOrEmpty(actionBtnText) && !string.IsNullOrEmpty(actionBtnUrl))
            {
                actionButtonHtml = $@"
              <table role='presentation' border='0' cellspacing='0' cellpadding='0' align='center' style='margin: 25px auto 10px auto; text-align: center;'>
                <tr>
                  <td align='center' style='border-radius: 6px; background-color: #540033;'>
                    <a href='{actionBtnUrl}' target='_blank' style='display: inline-block; padding: 12px 28px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; font-size: 14px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 6px;'>
                      {actionBtnText}
                    </a>
                  </td>
                </tr>
              </table>";
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{title}</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #1e293b; text-align: center;'>
  <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' align='center' style='width: 100%; margin: 0 auto; padding: 35px 15px; background-color: #f1f5f9; text-align: center;'>
    <tr>
      <td align='center' style='text-align: center;'>
        <table role='presentation' width='600' border='0' cellspacing='0' cellpadding='0' align='center' style='max-width: 600px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05); overflow: hidden; text-align: left;'>
          <tr>
            <td align='center' style='background: linear-gradient(135deg, #540033 0%, #7d0a4e 100%); padding: 28px 30px; text-align: center;'>
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' align='center' style='text-align: center;'>
                <tr>
                  <td align='center'>
                    <span style='display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; background-color: rgba(255, 255, 255, 0.2); color: #ffffff;'>
                      Attendance Regularization
                    </span>
                    <h1 style='margin: 8px 0 0 0; color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: -0.3px; line-height: 1.3;'>
                      {title}
                    </h1>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td align='center' style='padding: 18px 30px 0 30px; text-align: center;'>
              <span style='display: inline-block; padding: 6px 16px; border-radius: 20px; font-size: 13px; font-weight: 600; background-color: {badgeBgColor}; color: {badgeColor}; border: 1px solid {badgeBorderColor};'>
                {statusBadge}
              </span>
            </td>
          </tr>
          <tr>
            <td style='padding: 20px 30px 28px 30px;'>
              <p style='margin: 0 0 18px 0; font-size: 14px; line-height: 1.6; color: #334155; text-align: center;'>
                {actionNotes}
              </p>
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='width: 100%; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; background-color: #f8fafc; margin-bottom: 20px;'>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; width: 38%; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Request No</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>{reqNo}</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Employee</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>{employeeName} ({empId})</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Department / Role</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; border-bottom: 1px solid #e2e8f0;'>{department} - {designation}</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Attendance Date</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>{atndcDate:dd-MMM-yyyy}</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Requested Timings</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>{requestedTiming} ({durationHours} hrs)</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Actual Timings</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0;'>{actualTiming}</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; border-bottom: 1px solid #e2e8f0; font-weight: 600;'>Reason Category</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a; border-bottom: 1px solid #e2e8f0;'>{reasonCategory}</td>
                </tr>
                <tr>
                  <td style='padding: 10px 14px; font-size: 13px; color: #64748b; font-weight: 600;'>Remarks / Description</td>
                  <td style='padding: 10px 14px; font-size: 13px; color: #0f172a;'>{remarks}</td>
                </tr>
              </table>
              {actionButtonHtml}
            </td>
          </tr>
          <tr>
            <td align='center' style='padding: 18px 30px; background-color: #f8fafc; border-top: 1px solid #e2e8f0; text-align: center;'>
              <p style='margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.5;'>
                This is an automated notification from <strong>qHRM Attendance &amp; Payroll Management System</strong>.<br/>
                Please do not reply directly to this email.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        public async Task SendSubmissionAlertToManagerAsync(int regId)
        {
            if (_sysConfigContext == null) return;
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId);
                if (reg == null || !reg.ManagerEmpId.HasValue || reg.ManagerEmpId.Value <= 0) return;

                var emp = await GetEmployeeFullDetailsAsync(reg.EmpSysId);
                var manager = await GetEmployeeFullDetailsAsync(reg.ManagerEmpId.Value);

                if (string.IsNullOrWhiteSpace(manager.OfficialEmail))
                {
                    _logger?.LogWarning("Cannot send regularization submission email: Manager {ManagerName} (EmpSysId {ManagerId}) has no official email.", manager.FullName, reg.ManagerEmpId.Value);
                    return;
                }

                string baseUrl = GetAppBaseUrl();
                string reqTiming = (reg.ReqInTime.HasValue ? DateTime.Today.Add(reg.ReqInTime.Value).ToString("hh:mm tt") : "—")
                                   + " - " +
                                   (reg.ReqOutTime.HasValue ? DateTime.Today.Add(reg.ReqOutTime.Value).ToString("hh:mm tt") : "—");
                string actTiming = (reg.ActualInTime.HasValue ? DateTime.Today.Add(reg.ActualInTime.Value).ToString("hh:mm tt") : "Missing")
                                   + " - " +
                                   (reg.ActualOutTime.HasValue ? DateTime.Today.Add(reg.ActualOutTime.Value).ToString("hh:mm tt") : "Missing");

                string title = "Attendance Regularization Request";
                string subject = $"Attendance Regularization Approval Required – {emp.FullName} ({reg.ReqNo})";
                string notes = $"A new attendance regularization request has been submitted by <strong>{emp.FullName}</strong> and is awaiting your review and approval.";
                string actionUrl = $"{baseUrl}/Attendance/HrEmpAtndcRegApprovalQueue";

                string html = BuildCenteredEmailHtml(
                    title: title,
                    statusBadge: "Pending Manager Review",
                    badgeColor: "#d97706",
                    badgeBgColor: "#fef3c7",
                    badgeBorderColor: "#fde68a",
                    employeeName: emp.FullName,
                    empId: emp.EmpId,
                    department: emp.Department,
                    designation: emp.Designation,
                    reqNo: reg.ReqNo,
                    atndcDate: reg.StDate,
                    requestedTiming: reqTiming,
                    actualTiming: actTiming,
                    durationHours: reg.TotalHours.HasValue ? reg.TotalHours.Value.ToString("0.##") : "0",
                    reasonCategory: reg.ReasonCategory ?? "Other",
                    remarks: reg.ReasonDescription ?? reg.Note ?? "—",
                    actionNotes: notes,
                    actionBtnText: "Review & Approve Request",
                    actionBtnUrl: actionUrl
                );

                await _sysConfigContext.SendEmailNotificationAsync(manager.OfficialEmail, "", subject, html, "");

                if (_notificationService != null)
                {
                    await _notificationService.PushInAppNotificationAsync(
                        reg.ManagerEmpId.Value,
                        subject,
                        $"{emp.FullName} requested regularization for {reg.StDate:dd-MMM-yyyy}. Review required.",
                        reg.AtnExpId,
                        actionUrl
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SendSubmissionAlertToManagerAsync for RegId: {RegId}", regId);
            }
        }

        public async Task SendManagerApprovalAlertToHrAsync(int regId)
        {
            if (_sysConfigContext == null) return;
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId);
                if (reg == null) return;

                int hrEmpId = reg.HrEmpId.HasValue && reg.HrEmpId.Value > 0 ? reg.HrEmpId.Value : 6;
                var emp = await GetEmployeeFullDetailsAsync(reg.EmpSysId);
                var manager = reg.ManagerEmpId.HasValue ? await GetEmployeeFullDetailsAsync(reg.ManagerEmpId.Value) : null;
                var hr = await GetEmployeeFullDetailsAsync(hrEmpId);

                if (string.IsNullOrWhiteSpace(hr.OfficialEmail))
                {
                    _logger?.LogWarning("Cannot send regularization manager approval email: HR Approver {HrName} (EmpSysId {HrId}) has no official email.", hr.FullName, hrEmpId);
                    return;
                }

                string baseUrl = GetAppBaseUrl();
                string reqTiming = (reg.ReqInTime.HasValue ? DateTime.Today.Add(reg.ReqInTime.Value).ToString("hh:mm tt") : "—")
                                   + " - " +
                                   (reg.ReqOutTime.HasValue ? DateTime.Today.Add(reg.ReqOutTime.Value).ToString("hh:mm tt") : "—");
                string actTiming = (reg.ActualInTime.HasValue ? DateTime.Today.Add(reg.ActualInTime.Value).ToString("hh:mm tt") : "Missing")
                                   + " - " +
                                   (reg.ActualOutTime.HasValue ? DateTime.Today.Add(reg.ActualOutTime.Value).ToString("hh:mm tt") : "Missing");

                string title = "Attendance Regularization - Final HR Approval";
                string subject = $"Attendance Regularization Final Approval Required – {emp.FullName} ({reg.ReqNo})";
                string managerNote = manager != null ? $"approved by Reporting Manager <strong>{manager.FullName}</strong>" : "approved at Step 1";
                string notes = $"The attendance regularization request for <strong>{emp.FullName}</strong> has been {managerNote} and forwarded for Final HR Approval.";
                string actionUrl = $"{baseUrl}/Attendance/HrEmpAtndcRegApprovalQueue";

                string commentsDesc = (reg.ReasonDescription ?? reg.Note ?? "—") +
                    (!string.IsNullOrWhiteSpace(reg.ManagerComments) ? $"<br/><span style='color:#0284c7;font-weight:600;'>Manager Note:</span> {reg.ManagerComments}" : "");

                string html = BuildCenteredEmailHtml(
                    title: title,
                    statusBadge: "Pending Final HR Approval",
                    badgeColor: "#2563eb",
                    badgeBgColor: "#eff6ff",
                    badgeBorderColor: "#bfdbfe",
                    employeeName: emp.FullName,
                    empId: emp.EmpId,
                    department: emp.Department,
                    designation: emp.Designation,
                    reqNo: reg.ReqNo,
                    atndcDate: reg.StDate,
                    requestedTiming: reqTiming,
                    actualTiming: actTiming,
                    durationHours: reg.TotalHours.HasValue ? reg.TotalHours.Value.ToString("0.##") : "0",
                    reasonCategory: reg.ReasonCategory ?? "Other",
                    remarks: commentsDesc,
                    actionNotes: notes,
                    actionBtnText: "Open Regularization Approval Queue",
                    actionBtnUrl: actionUrl
                );

                await _sysConfigContext.SendEmailNotificationAsync(hr.OfficialEmail, "", subject, html, "");

                if (_notificationService != null)
                {
                    await _notificationService.PushInAppNotificationAsync(
                        hrEmpId,
                        subject,
                        $"{emp.FullName}'s regularization for {reg.StDate:dd-MMM-yyyy} was approved by manager and is pending HR approval.",
                        reg.AtnExpId,
                        actionUrl
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SendManagerApprovalAlertToHrAsync for RegId: {RegId}", regId);
            }
        }

        public async Task SendApprovalAlertToEmployeeAsync(int regId)
        {
            if (_sysConfigContext == null) return;
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId);
                if (reg == null) return;

                var emp = await GetEmployeeFullDetailsAsync(reg.EmpSysId);
                if (string.IsNullOrWhiteSpace(emp.OfficialEmail))
                {
                    _logger?.LogWarning("Cannot send regularization approval confirmation: Employee {EmpName} has no official email.", emp.FullName);
                    return;
                }

                string baseUrl = GetAppBaseUrl();
                string reqTiming = (reg.ReqInTime.HasValue ? DateTime.Today.Add(reg.ReqInTime.Value).ToString("hh:mm tt") : "—")
                                   + " - " +
                                   (reg.ReqOutTime.HasValue ? DateTime.Today.Add(reg.ReqOutTime.Value).ToString("hh:mm tt") : "—");
                string actTiming = (reg.ActualInTime.HasValue ? DateTime.Today.Add(reg.ActualInTime.Value).ToString("hh:mm tt") : "Missing")
                                   + " - " +
                                   (reg.ActualOutTime.HasValue ? DateTime.Today.Add(reg.ActualOutTime.Value).ToString("hh:mm tt") : "Missing");

                string title = "Attendance Regularization Approved";
                string subject = $"Attendance Regularization Approved – {reg.ReqNo} ({reg.StDate:dd-MMM-yyyy})";
                string notes = $"Your attendance regularization request for <strong>{reg.StDate:dd-MMM-yyyy}</strong> has received final approval. Your biometric shift status has been regularized to <strong>Present</strong> with <strong>0 Loss of Pay (LOP)</strong> deduction.";
                string actionUrl = $"{baseUrl}/Attendance/HrEmpAtndcSft";

                string commentsDesc = (reg.ReasonDescription ?? reg.Note ?? "—") +
                    (!string.IsNullOrWhiteSpace(reg.HrComments ?? reg.AprvdRmks) ? $"<br/><span style='color:#16a34a;font-weight:600;'>Approver Remarks:</span> {reg.HrComments ?? reg.AprvdRmks}" : "");

                string html = BuildCenteredEmailHtml(
                    title: title,
                    statusBadge: "Approved & Finalized",
                    badgeColor: "#16a34a",
                    badgeBgColor: "#f0fdf4",
                    badgeBorderColor: "#bbf7d0",
                    employeeName: emp.FullName,
                    empId: emp.EmpId,
                    department: emp.Department,
                    designation: emp.Designation,
                    reqNo: reg.ReqNo,
                    atndcDate: reg.StDate,
                    requestedTiming: reqTiming,
                    actualTiming: actTiming,
                    durationHours: reg.TotalHours.HasValue ? reg.TotalHours.Value.ToString("0.##") : "0",
                    reasonCategory: reg.ReasonCategory ?? "Other",
                    remarks: commentsDesc,
                    actionNotes: notes,
                    actionBtnText: "View Attendance Records",
                    actionBtnUrl: actionUrl
                );

                await _sysConfigContext.SendEmailNotificationAsync(emp.OfficialEmail, "", subject, html, "");

                if (_notificationService != null)
                {
                    await _notificationService.PushInAppNotificationAsync(
                        reg.EmpSysId,
                        subject,
                        $"Your regularization for {reg.StDate:dd-MMM-yyyy} has been approved with 0 LOP deduction.",
                        reg.AtnExpId,
                        actionUrl
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SendApprovalAlertToEmployeeAsync for RegId: {RegId}", regId);
            }
        }

        public async Task SendRejectionAlertToEmployeeAsync(int regId, string comments, bool isManagerStage)
        {
            if (_sysConfigContext == null) return;
            try
            {
                var reg = await _hrmContext.HrEmpAttendExp.FirstOrDefaultAsync(r => r.AtnExpId == regId);
                if (reg == null) return;

                var emp = await GetEmployeeFullDetailsAsync(reg.EmpSysId);
                if (string.IsNullOrWhiteSpace(emp.OfficialEmail))
                {
                    _logger?.LogWarning("Cannot send regularization rejection notification: Employee {EmpName} has no official email.", emp.FullName);
                    return;
                }

                string baseUrl = GetAppBaseUrl();
                string reqTiming = (reg.ReqInTime.HasValue ? DateTime.Today.Add(reg.ReqInTime.Value).ToString("hh:mm tt") : "—")
                                   + " - " +
                                   (reg.ReqOutTime.HasValue ? DateTime.Today.Add(reg.ReqOutTime.Value).ToString("hh:mm tt") : "—");
                string actTiming = (reg.ActualInTime.HasValue ? DateTime.Today.Add(reg.ActualInTime.Value).ToString("hh:mm tt") : "Missing")
                                   + " - " +
                                   (reg.ActualOutTime.HasValue ? DateTime.Today.Add(reg.ActualOutTime.Value).ToString("hh:mm tt") : "Missing");

                string title = "Attendance Regularization Rejected";
                string subject = $"Attendance Regularization Rejected – {reg.ReqNo} ({reg.StDate:dd-MMM-yyyy})";
                string reviewer = isManagerStage ? "Reporting Manager" : "Human Resources";
                string notes = $"Your attendance regularization request for <strong>{reg.StDate:dd-MMM-yyyy}</strong> was reviewed by <strong>{reviewer}</strong> and was <strong>not approved</strong>.";
                if (reg.IsLopApplied == true)
                {
                    notes += "<br/><span style='color:#dc2626;font-weight:600;'>Notice:</span> As this full-day request was rejected, attendance remains subject to standard Loss of Pay (LOP) deduction.";
                }
                string actionUrl = $"{baseUrl}/Attendance/HrEmpAtndcSft";

                string commentsDesc = (reg.ReasonDescription ?? reg.Note ?? "—") +
                    (!string.IsNullOrWhiteSpace(comments) ? $"<br/><span style='color:#dc2626;font-weight:600;'>Rejection Reason:</span> {comments}" : "");

                string html = BuildCenteredEmailHtml(
                    title: title,
                    statusBadge: "Request Rejected",
                    badgeColor: "#dc2626",
                    badgeBgColor: "#fef2f2",
                    badgeBorderColor: "#fecaca",
                    employeeName: emp.FullName,
                    empId: emp.EmpId,
                    department: emp.Department,
                    designation: emp.Designation,
                    reqNo: reg.ReqNo,
                    atndcDate: reg.StDate,
                    requestedTiming: reqTiming,
                    actualTiming: actTiming,
                    durationHours: reg.TotalHours.HasValue ? reg.TotalHours.Value.ToString("0.##") : "0",
                    reasonCategory: reg.ReasonCategory ?? "Other",
                    remarks: commentsDesc,
                    actionNotes: notes,
                    actionBtnText: "View Attendance Records",
                    actionBtnUrl: actionUrl
                );

                await _sysConfigContext.SendEmailNotificationAsync(emp.OfficialEmail, "", subject, html, "");

                if (_notificationService != null)
                {
                    await _notificationService.PushInAppNotificationAsync(
                        reg.EmpSysId,
                        subject,
                        $"Your regularization for {reg.StDate:dd-MMM-yyyy} was rejected by {reviewer}.",
                        reg.AtnExpId,
                        actionUrl
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SendRejectionAlertToEmployeeAsync for RegId: {RegId}", regId);
            }
        }
    }
}
