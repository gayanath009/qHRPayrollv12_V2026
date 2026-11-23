using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Reports.Models;
using qHRM.Models;

namespace qHRM.Areas.Reports.Controllers
{
    [Area("Reports")]
    [Authorize]
    public class HrOperationsController : Controller
    {
        private readonly qHRMasterContext _context;

        public HrOperationsController(qHRMasterContext context)
        {
            _context = context;
        }

        private async Task<Dictionary<int, (string DeptName, string StaffCat)>> GetActiveJobInfoAsync()
        {
            var today = DateTime.Today;
            var jobs = await (from j in _context.HrEmpJobAssign
                              join d in _context.OrgDepartment on j.DeptId equals d.DeptId into dGrp
                              from d in dGrp.DefaultIfEmpty()
                              where j.WkStat == 1 && (j.EndDate == null || j.EndDate >= today)
                              orderby j.StDate descending
                              select new
                              {
                                  j.EmpSysId,
                                  DeptName = d != null ? d.NameEng : "General"
                              }).ToListAsync();

            var map = new Dictionary<int, (string DeptName, string StaffCat)>();
            foreach (var j in jobs)
            {
                if (!map.ContainsKey(j.EmpSysId))
                {
                    bool isOut = j.DeptName.IndexOf("Outsource", StringComparison.OrdinalIgnoreCase) >= 0;
                    map[j.EmpSysId] = (j.DeptName, isOut ? "OUTSOURCE" : "INTERNAL");
                }
            }
            return map;
        }

        private static string ResolveStaffCategory(int? wrktypId, string deptName)
        {
            bool isOut = wrktypId == 2 || (!string.IsNullOrEmpty(deptName) && deptName.IndexOf("Outsource", StringComparison.OrdinalIgnoreCase) >= 0);
            return isOut ? "OUTSOURCE" : "INTERNAL";
        }

        private bool HasPriv(string code) => !string.IsNullOrEmpty(HttpContext.Session.GetString(code));

        // 1. Document & Visa Expiry Tracker
        public async Task<IActionResult> DocumentExpiry(string staffCat = "ALL", int days = 90)
        {
            if (!HasPriv("RPT_DOCEXP")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            var today = DateTime.Today;
            var cutoff = today.AddDays(days);

            var emps = await _context.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId,
                    e.PmtExpDate,
                    e.PstExpDate,
                    e.DrvLcnExpDate,
                    e.HlthCrdExpDate
                }).ToListAsync();

            var jobMap = await GetActiveJobInfoAsync();
            var items = new List<DocumentExpiryItemVm>();

            void TryAddDoc(string empId, string fullName, string dept, string sCat, string docType, DateTime? expDate)
            {
                if (!expDate.HasValue || expDate.Value.Year <= 1900) return;
                if (expDate.Value.Date > cutoff) return;
                int rem = (int)(expDate.Value.Date - today).TotalDays;
                string urgency = rem < 0 ? "Expired" : rem <= 30 ? "0-30 Days" : rem <= 60 ? "31-60 Days" : $"61-{days} Days";
                items.Add(new DocumentExpiryItemVm
                {
                    EmpId = empId,
                    FullName = fullName,
                    Department = dept,
                    StaffCategory = sCat,
                    DocType = docType,
                    ExpiryDate = expDate.Value.Date,
                    DaysRemaining = rem,
                    Urgency = urgency
                });
            }

            foreach (var e in emps)
            {
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                TryAddDoc(e.EmpId, e.FullName, dept, sCat, "QID / Visa", e.PmtExpDate);
                TryAddDoc(e.EmpId, e.FullName, dept, sCat, "Passport", e.PstExpDate);
                TryAddDoc(e.EmpId, e.FullName, dept, sCat, "Driving License", e.DrvLcnExpDate);
                TryAddDoc(e.EmpId, e.FullName, dept, sCat, "Health Card", e.HlthCrdExpDate);
            }

            items = items.OrderBy(x => x.DaysRemaining).ToList();

            var docTypeColors = new Dictionary<string, string>
            {
                { "QID / Visa", "#F3C6DC" },
                { "Passport", "#C7D8FA" },
                { "Driving License", "#BDECD6" },
                { "Health Card", "#FDE6A8" }
            };
            var chartData = docTypeColors.Select(kv => new DocumentExpiryChartVm
            {
                DocType = kv.Key,
                Color = kv.Value,
                TotalCount = items.Count(x => x.DocType == kv.Key),
                Expired = items.Count(x => x.DocType == kv.Key && x.Urgency == "Expired"),
                Days0To30 = items.Count(x => x.DocType == kv.Key && x.Urgency == "0-30 Days"),
                Days31To60 = items.Count(x => x.DocType == kv.Key && x.Urgency == "31-60 Days"),
                Days61To90 = items.Count(x => x.DocType == kv.Key && x.DaysRemaining > 60)
            }).ToList();

            var vm = new DocumentExpiryReportVm
            {
                StaffCat = cat,
                HorizonDays = days,
                ExpiredCount = items.Count(x => x.Urgency == "Expired"),
                Expiring30Count = items.Count(x => x.Urgency == "0-30 Days"),
                Expiring60Count = items.Count(x => x.Urgency == "31-60 Days"),
                Expiring90Count = items.Count(x => x.DaysRemaining > 60),
                ChartData = chartData,
                Items = items
            };

            return View(vm);
        }

        // 2. Attendance & Regularization Audit
        public async Task<IActionResult> AttendanceAudit(DateTime? fromDate, DateTime? toDate, string staffCat = "ALL")
        {
            if (!HasPriv("RPT_ATNAUD")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            var today = DateTime.Today;
            DateTime defaultTo = today.Day >= 21 ? new DateTime(today.Year, today.Month, 20).AddMonths(1) : new DateTime(today.Year, today.Month, 20);
            DateTime defaultFrom = defaultTo.AddMonths(-1).AddDays(1);

            DateTime start = (fromDate ?? defaultFrom).Date;
            DateTime end = (toDate ?? defaultTo).Date;

            var regs = await (from r in _context.HrEmpAttendExp
                              join e in _context.HrEmpGenInfo on r.EmpSysId equals e.EmpSysId
                              where r.StDate >= start && r.StDate <= end
                              orderby r.StDate descending, r.CrtDateTime descending
                              select new
                              {
                                  r.ReqNo,
                                  e.EmpSysId,
                                  e.EmpId,
                                  FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                                  e.WrktypId,
                                  r.StDate,
                                  r.ActualInTime,
                                  r.ActualOutTime,
                                  r.ReqInTime,
                                  r.ReqOutTime,
                                  r.TotalHours,
                                  r.ReasonCategory,
                                  r.ReasonDescription,
                                  r.WfStatus,
                                  r.CrtDateTime
                              }).ToListAsync();

            var jobMap = await GetActiveJobInfoAsync();
            var items = new List<AttendanceAuditItemVm>();

            string FmtTime(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm") : "-";

            foreach (var r in regs)
            {
                string dept = jobMap.TryGetValue(r.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(r.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                int totalMins = 0;
                if (r.ReqInTime.HasValue && r.ReqOutTime.HasValue)
                {
                    var diff = r.ReqOutTime.Value - r.ReqInTime.Value;
                    if (diff.TotalMinutes < 0) diff = diff.Add(TimeSpan.FromHours(24));
                    totalMins = (int)Math.Round(diff.TotalMinutes, MidpointRounding.AwayFromZero);
                }
                else if (r.TotalHours.HasValue && r.TotalHours.Value > 0)
                {
                    totalMins = (int)Math.Round(r.TotalHours.Value * 60m, MidpointRounding.AwayFromZero);
                }
                string totalHoursStr = $"{totalMins / 60:D2}h {totalMins % 60:D2}m";

                items.Add(new AttendanceAuditItemVm
                {
                    ReqNo = r.ReqNo ?? "-",
                    EmpId = r.EmpId,
                    FullName = r.FullName,
                    Department = dept,
                    StaffCategory = sCat,
                    AttendanceDate = r.StDate,
                    ActualInOut = $"{FmtTime(r.ActualInTime)} / {FmtTime(r.ActualOutTime)}",
                    RequestedInOut = $"{FmtTime(r.ReqInTime)} / {FmtTime(r.ReqOutTime)}",
                    TotalHours = r.TotalHours ?? 0m,
                    TotalHoursStr = totalHoursStr,
                    ReasonCategory = string.IsNullOrWhiteSpace(r.ReasonCategory) ? "Other" : r.ReasonCategory,
                    ReasonDescription = r.ReasonDescription ?? "-",
                    WfStatus = string.IsNullOrWhiteSpace(r.WfStatus) ? "Approved" : r.WfStatus,
                    CreatedDate = r.CrtDateTime
                });
            }

            var chartData = items
                .GroupBy(x => x.ReasonCategory)
                .Select(g => new ReasonSummaryVm
                {
                    ReasonCategory = g.Key,
                    Approved = g.Count(x => x.WfStatus == "Approved"),
                    Pending = g.Count(x => x.WfStatus == "Pending" || x.WfStatus == "PendingManager" || x.WfStatus == "PendingHR"),
                    Rejected = g.Count(x => x.WfStatus == "Rejected" || x.WfStatus == "Cancelled")
                })
                .OrderByDescending(x => x.Approved + x.Pending + x.Rejected)
                .ToList();

            var vm = new AttendanceAuditReportVm
            {
                FromDate = start,
                ToDate = end,
                StaffCat = cat,
                TotalRequests = items.Count,
                ApprovedCount = items.Count(x => x.WfStatus == "Approved"),
                PendingCount = items.Count(x => x.WfStatus == "Pending" || x.WfStatus == "PendingManager" || x.WfStatus == "PendingHR"),
                RejectedCancelledCount = items.Count(x => x.WfStatus == "Rejected" || x.WfStatus == "Cancelled"),
                TotalRegularizedHours = Math.Round(items.Where(x => x.WfStatus == "Approved").Sum(x => x.TotalHours), 2),
                ReasonChartData = chartData,
                Items = items
            };

            return View(vm);
        }

        // 3. Leave Utilization & Liability Report
        public async Task<IActionResult> LeaveLiability(int? year, string staffCat = "ALL")
        {
            if (!HasPriv("RPT_LVLIAB")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            int targetYear = year ?? DateTime.Today.Year;
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();

            var emps = await _context.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId
                }).ToListAsync();

            var balances = await _context.HrLvOpeningBalance
                .Where(b => b.LvYear == targetYear && b.LvTypId == 1)
                .ToDictionaryAsync(b => b.EmpSysId, b => b);

            var approvedTaken = await _context.HrLvRegister
                .Where(l => l.LvYear == targetYear && l.LvTypId == 1 && l.RecStat == 1)
                .GroupBy(l => l.EmpSysId)
                .Select(g => new { EmpSysId = g.Key ?? 0, Days = g.Sum(x => x.Days) })
                .ToDictionaryAsync(x => x.EmpSysId, x => x.Days);

            var salaries = (await _context.HrPrEmpSlryStruc
                .Where(s => s.RecStat == 1)
                .OrderByDescending(s => s.SlStrcId)
                .ToListAsync())
                .GroupBy(s => s.EmpSysId)
                .ToDictionary(g => g.Key, g => g.First());

            var jobMap = await GetActiveJobInfoAsync();
            var items = new List<LeaveLiabilityItemVm>();

            foreach (var e in emps)
            {
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                balances.TryGetValue(e.EmpSysId, out var bal);
                decimal opening = bal?.OpeningDays ?? 0m;
                decimal booked = bal?.BookedDays ?? 0m;
                decimal remaining = Math.Max(0m, opening - booked);

                approvedTaken.TryGetValue(e.EmpSysId, out int takenInYr);

                decimal monthly = 0m;
                if (salaries.TryGetValue(e.EmpSysId, out var s))
                {
                    monthly = s.Basic + s.HrAlwnc + s.TrnsAlwnc + s.Tra + s.PstnAlwnc + s.ProjAlwnc
                            + s.MedAlwnc + s.MobAlwnc + s.SpcAlwnc + s.PrfmAlwnc + s.OthrAlwnc;
                }
                decimal daily = Math.Round(monthly / 30.0m, 2);
                decimal liability = Math.Round(remaining * (monthly / 30.0m), 2);

                items.Add(new LeaveLiabilityItemVm
                {
                    EmpId = e.EmpId,
                    FullName = e.FullName,
                    Department = dept,
                    StaffCategory = sCat,
                    OpeningDays = opening,
                    BookedDays = booked,
                    RemainingDays = remaining,
                    ApprovedTakenInYear = takenInYr,
                    MonthlySalary = monthly,
                    DailyRate = daily,
                    EstimatedLiability = liability
                });
            }

            items = items.OrderByDescending(x => x.EstimatedLiability).ToList();

            var palette = new[]
            {
                "#F3C6DC", "#C7D8FA", "#BDECD6", "#BAE6FD", "#FDE6A8",
                "#F8C4C0", "#DDD6FE", "#FED7AA", "#CCFBF1", "#E2E8F0"
            };
            var deptChart = items
                .GroupBy(x => x.Department)
                .Select(g => new DeptLeaveSummaryVm
                {
                    Department = g.Key,
                    BookedDays = g.Sum(x => x.BookedDays),
                    RemainingDays = g.Sum(x => x.RemainingDays),
                    LiabilityAmount = Math.Round(g.Sum(x => x.EstimatedLiability), 0)
                })
                .OrderByDescending(x => x.LiabilityAmount)
                .Take(10)
                .Select((d, idx) =>
                {
                    d.Color = palette[idx % palette.Length];
                    return d;
                })
                .ToList();

            var vm = new LeaveLiabilityReportVm
            {
                Year = targetYear,
                StaffCat = cat,
                TotalOpeningDays = items.Sum(x => x.OpeningDays),
                TotalBookedDays = items.Sum(x => x.BookedDays),
                TotalRemainingDays = items.Sum(x => x.RemainingDays),
                TotalLiabilityAmount = Math.Round(items.Sum(x => x.EstimatedLiability), 0),
                DeptChartData = deptChart,
                Items = items
            };

            return View(vm);
        }

        // 4. Probation & Anniversary Tracker
        public async Task<IActionResult> AnniversaryProbation(string staffCat = "ALL", int days = 60)
        {
            if (!HasPriv("RPT_PRBANN")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            var today = DateTime.Today;
            var horizonEnd = today.AddDays(days);

            var emps = await _context.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId,
                    e.JoinedDate
                }).ToListAsync();

            var probs = await _context.HrEmpProbation
                .Where(p => p.WkStat == 1)
                .OrderBy(p => p.EndDate)
                .ToListAsync();

            var jobMap = await GetActiveJobInfoAsync();
            var empLookup = emps.ToDictionary(x => x.EmpSysId, x => x);

            var annivList = new List<AnniversaryItemVm>();
            foreach (var e in emps)
            {
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                DateTime nextAnniv;
                try
                {
                    nextAnniv = new DateTime(today.Year, e.JoinedDate.Month, e.JoinedDate.Day);
                }
                catch
                {
                    nextAnniv = new DateTime(today.Year, e.JoinedDate.Month, 28);
                }
                if (nextAnniv < today) nextAnniv = nextAnniv.AddYears(1);

                int completedYears = nextAnniv.Year - e.JoinedDate.Year;
                if (completedYears < 1) continue;

                int daysUntil = (int)(nextAnniv - today).TotalDays;
                if (daysUntil <= days)
                {
                    annivList.Add(new AnniversaryItemVm
                    {
                        EmpId = e.EmpId,
                        FullName = e.FullName,
                        Department = dept,
                        StaffCategory = sCat,
                        JoinedDate = e.JoinedDate,
                        NextAnniversaryDate = nextAnniv,
                        CompletedYears = completedYears,
                        DaysUntil = daysUntil,
                        EncashmentEntitlement = completedYears == 1 ? "Air Ticket Only (1st Year)" : "Leave Salary + Air Ticket"
                    });
                }
            }

            annivList = annivList.OrderBy(x => x.DaysUntil).ToList();

            var probList = new List<ProbationItemVm>();
            foreach (var p in probs)
            {
                if (!empLookup.TryGetValue(p.EmpSysId, out var e)) continue;
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                probList.Add(new ProbationItemVm
                {
                    EmpId = e.EmpId,
                    FullName = e.FullName,
                    Department = dept,
                    StaffCategory = sCat,
                    StDate = p.StDate,
                    EndDate = p.EndDate,
                    DaysRemaining = (int)(p.EndDate.Date - today).TotalDays,
                    IsExtended = p.IsExtended,
                    Note = p.Note ?? "-"
                });
            }

            var chartData = annivList
                .GroupBy(x => new { x.NextAnniversaryDate.Year, x.NextAnniversaryDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyAnnivSummaryVm
                {
                    MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    FirstYearTicketOnly = g.Count(x => x.CompletedYears == 1),
                    MultiYearLeaveAndTicket = g.Count(x => x.CompletedYears > 1)
                }).ToList();

            var vm = new AnniversaryProbationReportVm
            {
                StaffCat = cat,
                HorizonDays = days,
                ActiveProbationCount = probList.Count,
                ProbationEndingSoonCount = probList.Count(x => x.DaysRemaining <= 30),
                UpcomingFirstYearCount = annivList.Count(x => x.CompletedYears == 1),
                UpcomingMultiYearCount = annivList.Count(x => x.CompletedYears > 1),
                MonthlyChartData = chartData,
                Anniversaries = annivList,
                Probations = probList
            };

            return View(vm);
        }

        // 5. Workflow Bottleneck & SLA Monitor
        public async Task<IActionResult> WorkflowSla(string module = "ALL")
        {
            if (!HasPriv("RPT_WFSLA")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string mod = string.IsNullOrWhiteSpace(module) ? "ALL" : module;
            var now = DateTime.Now;
            var items = new List<WorkflowSlaItemVm>();
            var jobMap = await GetActiveJobInfoAsync();

            if (mod == "ALL" || mod == "Leave")
            {
                var leaves = await (from l in _context.HrLvRegister
                                    join e in _context.HrEmpGenInfo on l.EmpSysId equals e.EmpSysId
                                    join t in _context.HrLvType on l.LvTypId equals t.LvTypId
                                    where l.LvYear >= DateTime.Today.Year - 1
                                    orderby l.CrtDateTime descending
                                    select new
                                    {
                                        l.LvId,
                                        e.EmpSysId,
                                        e.EmpId,
                                        FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                                        LeaveType = t.NameEng,
                                        l.StDate,
                                        l.Days,
                                        l.WfStat,
                                        l.CrtDateTime
                                    }).Take(500).ToListAsync();

                foreach (var l in leaves)
                {
                    string status = string.IsNullOrWhiteSpace(l.WfStat) ? "Pending" : l.WfStat;
                    bool isPending = status.IndexOf("Pending", StringComparison.OrdinalIgnoreCase) >= 0 || status.Equals("Submitted", StringComparison.OrdinalIgnoreCase);
                    double hrs = Math.Round((now - l.CrtDateTime).TotalHours, 1);
                    string dept = jobMap.TryGetValue(l.EmpSysId, out var jm) ? jm.DeptName : "General";

                    items.Add(new WorkflowSlaItemVm
                    {
                        Module = "Leave",
                        Reference = $"LV-{l.LvId}",
                        EmpId = l.EmpId,
                        FullName = l.FullName,
                        Department = dept,
                        RequestType = $"{l.LeaveType} ({l.Days}d from {l.StDate:dd-MMM})",
                        SubmittedAt = l.CrtDateTime,
                        CompletedAt = null,
                        AgingOrTurnaroundHours = isPending ? Math.Max(0, hrs) : 0,
                        WfStatus = status,
                        SlaState = isPending ? (hrs > 48 ? "SLA Breach (>48h)" : "Within SLA") : "Completed"
                    });
                }
            }

            if (mod == "ALL" || mod == "Regularization")
            {
                var regs = await (from r in _context.HrEmpAttendExp
                                  join e in _context.HrEmpGenInfo on r.EmpSysId equals e.EmpSysId
                                  where r.StDate >= DateTime.Today.AddMonths(-2)
                                  orderby r.CrtDateTime descending
                                  select new
                                  {
                                      r.AtnExpId,
                                      r.ReqNo,
                                      e.EmpSysId,
                                      e.EmpId,
                                      FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                                      r.ReasonCategory,
                                      r.StDate,
                                      r.WfStatus,
                                      r.CrtDateTime,
                                      r.ManagerActionDateTime,
                                      r.HrActionDateTime
                                  }).Take(500).ToListAsync();

                foreach (var r in regs)
                {
                    string status = string.IsNullOrWhiteSpace(r.WfStatus) ? "Approved" : r.WfStatus;
                    bool isPending = status.IndexOf("Pending", StringComparison.OrdinalIgnoreCase) >= 0;
                    DateTime? doneAt = r.HrActionDateTime ?? r.ManagerActionDateTime;
                    double hrs = isPending
                        ? Math.Round((now - r.CrtDateTime).TotalHours, 1)
                        : (doneAt.HasValue ? Math.Round((doneAt.Value - r.CrtDateTime).TotalHours, 1) : 0);
                    string dept = jobMap.TryGetValue(r.EmpSysId, out var jm) ? jm.DeptName : "General";

                    items.Add(new WorkflowSlaItemVm
                    {
                        Module = "Regularization",
                        Reference = r.ReqNo ?? $"REG-{r.AtnExpId}",
                        EmpId = r.EmpId,
                        FullName = r.FullName,
                        Department = dept,
                        RequestType = $"{r.ReasonCategory ?? "Other"} ({r.StDate:dd-MMM})",
                        SubmittedAt = r.CrtDateTime,
                        CompletedAt = doneAt,
                        AgingOrTurnaroundHours = Math.Max(0, hrs),
                        WfStatus = status,
                        SlaState = isPending ? (hrs > 48 ? "SLA Breach (>48h)" : "Within SLA") : "Completed"
                    });
                }
            }

            var pendingItems = items.Where(x => x.SlaState != "Completed").ToList();
            var completedWithTime = items.Where(x => x.SlaState == "Completed" && x.AgingOrTurnaroundHours > 0).ToList();

            string GetBucket(double h) => h <= 24 ? "0-24 Hours" : h <= 48 ? "24-48 Hours" : h <= 168 ? "2-7 Days" : ">7 Days";
            var buckets = new[] { "0-24 Hours", "24-48 Hours", "2-7 Days", ">7 Days" };
            var chartData = buckets.Select(b => new WorkflowAgingBucketVm
            {
                Bucket = b,
                LeaveCount = pendingItems.Count(x => x.Module == "Leave" && GetBucket(x.AgingOrTurnaroundHours) == b),
                RegularizationCount = pendingItems.Count(x => x.Module == "Regularization" && GetBucket(x.AgingOrTurnaroundHours) == b)
            }).ToList();

            var vm = new WorkflowSlaReportVm
            {
                Module = mod,
                TotalPending = pendingItems.Count,
                PendingOver48h = pendingItems.Count(x => x.AgingOrTurnaroundHours > 48),
                AvgTurnaroundHours = completedWithTime.Any() ? Math.Round(completedWithTime.Average(x => x.AgingOrTurnaroundHours), 1) : 0,
                TotalProcessed = items.Count(x => x.SlaState == "Completed"),
                AgingChartData = chartData,
                Items = items.OrderByDescending(x => x.SlaState == "SLA Breach (>48h)")
                             .ThenByDescending(x => x.SubmittedAt)
                             .ToList()
            };

            return View(vm);
        }

        // 6. Leave Balance Summary (Leave type-wise individual balances)
        public async Task<IActionResult> LeaveBalanceSummary(int? year, string staffCat = "ALL", string status = "ALL")
        {
            if (!HasPriv("RPT_LVBALS")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            int targetYear = year ?? DateTime.Today.Year;
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            string stat = string.IsNullOrWhiteSpace(status) ? "ALL" : status.ToUpper();

            var emps = await _context.HrEmpGenInfo
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId,
                    e.CurrStat,
                    e.SrvEndDate
                }).ToListAsync();

            var photos = (await _context.HrEmpPhoto
                .Where(p => p.RecStat == 1)
                .OrderByDescending(p => p.PhtId)
                .Select(p => new { p.EmpSysId, p.PhotoName })
                .ToListAsync())
                .GroupBy(p => p.EmpSysId)
                .ToDictionary(g => g.Key, g => g.First().PhotoName);

            var lvTypes = await _context.HrLvType
                .Select(t => new { t.LvTypId, t.LvTypCode })
                .ToListAsync();
            var typeMap = lvTypes.ToDictionary(t => t.LvTypId, t => (t.LvTypCode ?? "").ToUpper());

            var allBalances = await _context.HrLvOpeningBalance
                .Where(b => b.LvYear == targetYear)
                .ToListAsync();

            var allRegTaken = await _context.HrLvRegister
                .Where(l => l.LvYear == targetYear && l.RecStat == 1 && l.EmpSysId.HasValue && l.LvTypId.HasValue)
                .GroupBy(l => new { EmpSysId = l.EmpSysId.Value, LvTypId = l.LvTypId.Value })
                .Select(g => new { g.Key.EmpSysId, g.Key.LvTypId, Days = g.Sum(x => x.Days) })
                .ToListAsync();

            string MapGroup(string code)
            {
                if (code.Contains("ANL")) return "Annual";
                if (code.Contains("CSL")) return "Casual";
                if (code.Contains("SICK")) return "Sick";
                if (code.Contains("EMG")) return "Emergency";
                if (code.Contains("MTN")) return "Maternity";
                if (code.Contains("HAJ")) return "Hajj";
                return null;
            }

            var balLookup = new Dictionary<(int EmpSysId, string Group), (decimal Open, decimal Booked)>();
            foreach (var b in allBalances)
            {
                if (!typeMap.TryGetValue(b.LvTypId, out var code)) continue;
                var grp = MapGroup(code);
                if (grp == null) continue;
                var key = (b.EmpSysId, grp);
                balLookup.TryGetValue(key, out var cur);
                balLookup[key] = (cur.Open + b.OpeningDays, cur.Booked + b.BookedDays);
            }

            var regLookup = new Dictionary<(int EmpSysId, string Group), decimal>();
            foreach (var r in allRegTaken)
            {
                if (!typeMap.TryGetValue(r.LvTypId, out var code)) continue;
                var grp = MapGroup(code);
                if (grp == null) continue;
                var key = (r.EmpSysId, grp);
                regLookup.TryGetValue(key, out var cur);
                regLookup[key] = cur + r.Days;
            }

            var jobMap = await GetActiveJobInfoAsync();
            var items = new List<LeaveBalanceSummaryItemVm>();

            (string BaText, decimal Booked, decimal Balance, decimal Alloc) CalcGroup(int empSysId, string grp)
            {
                balLookup.TryGetValue((empSysId, grp), out var b);
                regLookup.TryGetValue((empSysId, grp), out var reg);
                decimal alloc = Math.Round(b.Open, 0);
                decimal booked = Math.Round(Math.Max(b.Booked, reg), 0);
                decimal bal = Math.Max(0m, alloc - booked);
                string text = (alloc == 0m && booked == 0m) ? "0" : $"{booked:0}/{alloc:0}";
                return (text, booked, bal, alloc);
            }

            decimal totAnlOpen = 0m, totAnlBooked = 0m, totSickBooked = 0m, totCslBooked = 0m;
            var chartTotals = new Dictionary<string, (decimal Booked, decimal Bal)>
            {
                { "Annual", (0m, 0m) },
                { "Casual", (0m, 0m) },
                { "Sick", (0m, 0m) },
                { "Emergency", (0m, 0m) },
                { "Maternity", (0m, 0m) },
                { "Hajj", (0m, 0m) }
            };

            foreach (var e in emps)
            {
                bool isExited = e.CurrStat != 0 || (e.SrvEndDate.HasValue && e.SrvEndDate.Value.Date <= DateTime.Today);
                if (stat == "ACTIVE" && isExited) continue;
                if (stat == "EXITED" && !isExited) continue;

                bool hasAnyBal = balLookup.Keys.Any(k => k.EmpSysId == e.EmpSysId);
                bool hasAnyReg = regLookup.Keys.Any(k => k.EmpSysId == e.EmpSysId);
                if (isExited && !hasAnyBal && !hasAnyReg && (!e.SrvEndDate.HasValue || e.SrvEndDate.Value.Year < targetYear))
                    continue;

                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                var anl = CalcGroup(e.EmpSysId, "Annual");
                var csl = CalcGroup(e.EmpSysId, "Casual");
                var sck = CalcGroup(e.EmpSysId, "Sick");
                var emg = CalcGroup(e.EmpSysId, "Emergency");
                var mtn = CalcGroup(e.EmpSysId, "Maternity");
                var haj = CalcGroup(e.EmpSysId, "Hajj");

                totAnlOpen += anl.Alloc;
                totAnlBooked += anl.Booked;
                totSickBooked += sck.Booked;
                totCslBooked += csl.Booked;

                void Acc(string k, (string BaText, decimal Booked, decimal Balance, decimal Alloc) g)
                {
                    var cur = chartTotals[k];
                    chartTotals[k] = (cur.Booked + g.Booked, cur.Bal + g.Balance);
                }
                Acc("Annual", anl);
                Acc("Casual", csl);
                Acc("Sick", sck);
                Acc("Emergency", emg);
                Acc("Maternity", mtn);
                Acc("Hajj", haj);

                photos.TryGetValue(e.EmpSysId, out string photoName);

                items.Add(new LeaveBalanceSummaryItemVm
                {
                    EmpId = e.EmpId,
                    FullName = e.FullName,
                    PhotoName = string.IsNullOrWhiteSpace(photoName) ? "user.png" : photoName,
                    ExitDateText = e.SrvEndDate.HasValue ? $"( Exit Date - {e.SrvEndDate.Value:dd-MMM-yyyy} )" : "",
                    Department = dept,
                    StaffCategory = sCat,
                    OpeningBalance = anl.Alloc,
                    BA_Annual = anl.BaText,
                    BA_Casual = csl.BaText,
                    BA_Sick = sck.BaText,
                    BA_Emergency = emg.BaText,
                    BA_Maternity = mtn.BaText,
                    BA_Hajj = haj.BaText,
                    Bal_Annual = anl.Balance,
                    Bal_Casual = csl.Balance,
                    Bal_Sick = sck.Balance,
                    Bal_Emergency = emg.Balance,
                    Bal_Maternity = mtn.Balance,
                    Bal_Hajj = haj.Balance
                });
            }

            items = items.OrderBy(x => x.EmpId).ToList();

            var typeColors = new Dictionary<string, string>
            {
                { "Annual", "#C7D8FA" },
                { "Casual", "#BDECD6" },
                { "Sick", "#FDE6A8" },
                { "Emergency", "#F8C4C0" },
                { "Maternity", "#F3C6DC" },
                { "Hajj", "#DDD6FE" }
            };

            var typeChartData = typeColors.Select(kv => new LeaveBalanceTypeChartVm
            {
                LeaveType = kv.Key,
                Color = kv.Value,
                Booked = chartTotals[kv.Key].Booked,
                Balance = chartTotals[kv.Key].Bal
            }).ToList();

            var vm = new LeaveBalanceSummaryReportVm
            {
                Year = targetYear,
                StaffCat = cat,
                StatusFilter = stat,
                TotalAnnualOpening = totAnlOpen,
                TotalAnnualBooked = totAnlBooked,
                TotalSickBooked = totSickBooked,
                TotalCasualBooked = totCslBooked,
                TypeChartData = typeChartData,
                Items = items
            };

            return View(vm);
        }
    }
}
