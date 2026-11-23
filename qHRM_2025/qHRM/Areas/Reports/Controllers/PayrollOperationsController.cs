using System;
using System.Collections.Generic;
using System.Data;
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
    public class PayrollOperationsController : Controller
    {
        private readonly qHRMasterContext _context;

        public PayrollOperationsController(qHRMasterContext context)
        {
            _context = context;
        }

        private async Task<Dictionary<int, (string DeptName, string StaffCat)>> GetJobInfoMapAsync()
        {
            var jobs = await (from j in _context.HrEmpJobAssign
                              join d in _context.OrgDepartment on j.DeptId equals d.DeptId into dGrp
                              from d in dGrp.DefaultIfEmpty()
                              orderby j.WkStat descending, j.StDate descending
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

        private static decimal CalcMomPct(decimal curr, decimal prev)
        {
            if (prev == 0m) return 0m;
            return Math.Round(((curr - prev) / prev) * 100m, 1);
        }

        private bool HasPriv(string code) => !string.IsNullOrEmpty(HttpContext.Session.GetString(code));

        // 1. Monthly Payroll Cost Trend & Variance (12 Months)
        public async Task<IActionResult> MonthlyCostTrend()
        {
            if (!HasPriv("RPT_PRCST")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            var hdrs = await _context.HrPrSlryPrcHdr
                .OrderByDescending(h => h.EftYear)
                .ThenByDescending(h => h.EftMonth)
                .Take(12)
                .ToListAsync();

            hdrs = hdrs.OrderBy(h => h.EftYear).ThenBy(h => h.EftMonth).ToList();
            var prcIds = hdrs.Select(h => h.PrcId).ToList();

            var actvRows = new List<(int PrcId, int EmpSysId, decimal Basic, decimal Allowances, decimal OtAmt, decimal Deductions)>();
            if (prcIds.Any())
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        SELECT Prc_Id, Emp_Sys_Id,
                               ISNULL(Basic, 0) AS BasicAmt,
                               (ISNULL(HR_Alwnc,0)+ISNULL(Trns_Alwnc,0)+ISNULL(TRA,0)+ISNULL(Pstn_Alwnc,0)+ISNULL(Med_Alwnc,0)+ISNULL(Mob_Alwnc,0)
                                +ISNULL(Proj_Alwnc,0)+ISNULL(Spc_Alwnc,0)+ISNULL(Incntv_Amt,0)+ISNULL(Prfm_Alwnc,0)+ISNULL(Othr_Alwnc,0)
                                +ISNULL(Othr_Pmt,0)+ISNULL(Atn_Pmt_Amt,0)+ISNULL(Bonus_Amt,0)+ISNULL(AirTckt_Fee,0)+ISNULL(Anl_Lv_Enchmnt,0)) AS Allowances,
                               ISNULL(Ot_Amt, 0) AS OtAmt,
                               (ISNULL(Othr_Rcvry,0)+ISNULL(Atn_Rcvry_Amt,0)+ISNULL(Slry_Adv,0)+ISNULL(Stf_Ln_Pmt,0)+ISNULL(Np_Amt,0)) AS Deductions
                        FROM dbo.HR_PrSlryPrcDtl_Actv
                        WHERE Rec_Stat = 1 AND Prc_Id IN ({string.Join(",", prcIds)})";
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            actvRows.Add((
                                rdr.GetInt32(0),
                                rdr.GetInt32(1),
                                rdr.GetDecimal(2),
                                rdr.GetDecimal(3),
                                rdr.GetDecimal(4),
                                rdr.GetDecimal(5)
                            ));
                        }
                    }
                }
            }

            var psvRows = await _context.HrPrSlryPrcDtlPsv
                .Where(d => prcIds.Contains(d.PrcId) && d.RecStat == 1)
                .Select(d => new
                {
                    d.PrcId,
                    d.EmpSysId,
                    d.Basic,
                    Allowances = d.HrAlwnc + d.TrnsAlwnc + d.Tra + d.PstnAlwnc + d.MedAlwnc + d.MobAlwnc
                               + d.ProjAlwnc + d.SpcAlwnc + d.IncntvAmt + d.PrfmAlwnc + d.OthrAlwnc
                               + d.OthrPmt + d.AtnPmtAmt + d.BonusAmt + d.AirTckt_Fee + d.Anl_Lv_Enchmnt,
                    d.OtAmt,
                    Deductions = d.OthrRcvry + d.AtnRcvryAmt + d.SlryAdv + d.StfLnPmt + d.NpAmt
                }).ToListAsync();

            var months = new List<MonthlyCostTrendItemVm>();
            decimal prevNet = 0m;

            foreach (var h in hdrs)
            {
                var a = actvRows.Where(x => x.PrcId == h.PrcId).ToList();
                var p = psvRows.Where(x => x.PrcId == h.PrcId).ToList();

                int hc = a.Any() ? a.Select(x => x.EmpSysId).Distinct().Count() : p.Select(x => x.EmpSysId).Distinct().Count();
                decimal basic = a.Any() ? a.Sum(x => x.Basic) : p.Sum(x => x.Basic);
                decimal allow = a.Any() ? a.Sum(x => x.Allowances) : p.Sum(x => x.Allowances);
                decimal ot = a.Any() ? a.Sum(x => x.OtAmt) : p.Sum(x => x.OtAmt);
                decimal gross = basic + allow + ot;
                decimal ded = a.Any() ? a.Sum(x => x.Deductions) : p.Sum(x => x.Deductions);
                decimal net = gross - ded;

                decimal mom = prevNet > 0m ? CalcMomPct(net, prevNet) : 0m;
                prevNet = net;

                months.Add(new MonthlyCostTrendItemVm
                {
                    Year = h.EftYear,
                    Month = h.EftMonth,
                    MonthLabel = new DateTime(h.EftYear, h.EftMonth, 1).ToString("MMM yyyy"),
                    Headcount = hc,
                    Basic = Math.Round(basic, 0),
                    Allowances = Math.Round(allow, 0),
                    Overtime = Math.Round(ot, 0),
                    Gross = Math.Round(gross, 0),
                    Deductions = Math.Round(ded, 0),
                    NetPayout = Math.Round(net, 0),
                    MomVariancePct = mom
                });
            }

            var latest = months.LastOrDefault() ?? new MonthlyCostTrendItemVm();
            var prev = months.Count >= 2 ? months[months.Count - 2] : new MonthlyCostTrendItemVm();

            var vm = new MonthlyCostTrendReportVm
            {
                LatestNetPayout = latest.NetPayout,
                LatestGross = latest.Gross,
                LatestOvertime = latest.Overtime,
                LatestDeductions = latest.Deductions,
                NetMomPct = CalcMomPct(latest.NetPayout, prev.NetPayout),
                GrossMomPct = CalcMomPct(latest.Gross, prev.Gross),
                OtMomPct = CalcMomPct(latest.Overtime, prev.Overtime),
                DedMomPct = CalcMomPct(latest.Deductions, prev.Deductions),
                Months = months
            };

            return View(vm);
        }

        // 2. Department & Cost-Center Cost Breakdown
        public async Task<IActionResult> DepartmentCostBreakdown(int? year, int? month, string staffCat = "ALL")
        {
            if (!HasPriv("RPT_DPTCST")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();

            var targetHdr = (year.HasValue && month.HasValue)
                ? await _context.HrPrSlryPrcHdr.FirstOrDefaultAsync(h => h.EftYear == year.Value && h.EftMonth == month.Value)
                : await _context.HrPrSlryPrcHdr.OrderByDescending(h => h.EftYear).ThenByDescending(h => h.EftMonth).FirstOrDefaultAsync();

            int pYear = year ?? targetHdr?.EftYear ?? DateTime.Today.Year;
            int pMonth = month ?? targetHdr?.EftMonth ?? DateTime.Today.Month;

            var emps = await _context.HrEmpGenInfo
                .Select(e => new { e.EmpSysId, e.EmpId, FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(), e.WrktypId })
                .ToDictionaryAsync(e => e.EmpSysId, e => e);

            var jobMap = await GetJobInfoMapAsync();

            var payrollByEmp = new Dictionary<int, (decimal Gross, decimal Net)>();
            if (targetHdr != null)
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Emp_Sys_Id,
                               (ISNULL(Basic,0)+ISNULL(HR_Alwnc,0)+ISNULL(Trns_Alwnc,0)+ISNULL(TRA,0)+ISNULL(Pstn_Alwnc,0)+ISNULL(Med_Alwnc,0)+ISNULL(Mob_Alwnc,0)
                                +ISNULL(Proj_Alwnc,0)+ISNULL(Spc_Alwnc,0)+ISNULL(Incntv_Amt,0)+ISNULL(Prfm_Alwnc,0)+ISNULL(Othr_Alwnc,0)
                                +ISNULL(Othr_Pmt,0)+ISNULL(Atn_Pmt_Amt,0)+ISNULL(Bonus_Amt,0)+ISNULL(Ot_Amt,0)+ISNULL(AirTckt_Fee,0)+ISNULL(Anl_Lv_Enchmnt,0)) AS GrossAmt,
                               (ISNULL(Othr_Rcvry,0)+ISNULL(Atn_Rcvry_Amt,0)+ISNULL(Slry_Adv,0)+ISNULL(Stf_Ln_Pmt,0)+ISNULL(Np_Amt,0)) AS DedAmt
                        FROM dbo.HR_PrSlryPrcDtl_Actv
                        WHERE Rec_Stat = 1 AND Prc_Id = @PrcId";
                    var prm = cmd.CreateParameter();
                    prm.ParameterName = "@PrcId";
                    prm.Value = targetHdr.PrcId;
                    cmd.Parameters.Add(prm);
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            int eid = rdr.GetInt32(0);
                            decimal g = rdr.GetDecimal(1);
                            decimal d = rdr.GetDecimal(2);
                            payrollByEmp[eid] = (g, g - d);
                        }
                    }
                }
                if (!payrollByEmp.Any())
                {
                    var psv = await _context.HrPrSlryPrcDtlPsv
                        .Where(d => d.PrcId == targetHdr.PrcId && d.RecStat == 1)
                        .ToListAsync();
                    foreach (var p in psv)
                    {
                        decimal gross = p.Basic + p.HrAlwnc + p.TrnsAlwnc + p.Tra + p.PstnAlwnc + p.MedAlwnc + p.MobAlwnc
                                      + p.ProjAlwnc + p.SpcAlwnc + p.IncntvAmt + p.PrfmAlwnc + p.OthrAlwnc
                                      + p.OthrPmt + p.AtnPmtAmt + p.BonusAmt + p.OtAmt + p.AirTckt_Fee + p.Anl_Lv_Enchmnt;
                        decimal ded = p.OthrRcvry + p.AtnRcvryAmt + p.SlryAdv + p.StfLnPmt + p.NpAmt;
                        payrollByEmp[p.EmpSysId] = (gross, gross - ded);
                    }
                }
            }

            var encashByEmp = await _context.HrPrStaffLvTktEncash
                .Where(x => x.PrcYear == pYear && x.PrcMonth == pMonth && x.RecStat == 1)
                .GroupBy(x => x.EmpSysId)
                .Select(g => new { EmpSysId = g.Key, Amt = g.Sum(x => x.TotalSettlement) })
                .ToDictionaryAsync(x => x.EmpSysId, x => x.Amt);

            int ym = pYear * 100 + pMonth;
            var projByEmp = await _context.HrPrProjAlwnc
                .Where(p => p.RecStat == 1 && (p.EftYear * 100 + p.EftMonth) <= ym && (p.EndYear * 100 + p.EndMonth) >= ym)
                .GroupBy(p => p.EmpSysId)
                .Select(g => new { EmpSysId = g.Key, Amt = g.Sum(x => (decimal)x.ProjAlwncAmt) })
                .ToDictionaryAsync(x => x.EmpSysId, x => x.Amt);

            var allEmpIds = payrollByEmp.Keys.Union(encashByEmp.Keys).Union(projByEmp.Keys).Distinct().ToList();
            var empItems = new List<DepartmentEmpCostItemVm>();

            foreach (var eid in allEmpIds)
            {
                emps.TryGetValue(eid, out var emp);
                string dept = jobMap.TryGetValue(eid, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(emp?.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                payrollByEmp.TryGetValue(eid, out var pr);
                encashByEmp.TryGetValue(eid, out decimal enc);
                projByEmp.TryGetValue(eid, out decimal proj);

                empItems.Add(new DepartmentEmpCostItemVm
                {
                    EmpId = emp?.EmpId ?? eid.ToString(),
                    FullName = emp?.FullName ?? $"Employee #{eid}",
                    Department = dept,
                    StaffCategory = sCat,
                    PayrollNet = Math.Round(pr.Net, 0),
                    EncashmentCost = Math.Round(enc, 0),
                    ProjectAllowance = Math.Round(proj, 0),
                    TotalCost = Math.Round(pr.Net + enc + proj, 0)
                });
            }

            var deptItems = empItems
                .GroupBy(x => new { x.Department, x.StaffCategory })
                .Select(g => new DepartmentCostItemVm
                {
                    Department = g.Key.Department,
                    StaffCategory = g.Key.StaffCategory,
                    EmployeeCount = g.Count(),
                    PayrollGross = g.Sum(x => x.PayrollNet),
                    EncashmentCost = g.Sum(x => x.EncashmentCost),
                    ProjectAllowance = g.Sum(x => x.ProjectAllowance),
                    TotalCost = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(x => x.TotalCost)
                .ToList();

            var palette = new[] { "#F3C6DC", "#C7D8FA", "#BDECD6", "#BAE6FD", "#FDE6A8", "#F8C4C0", "#DDD6FE", "#FED7AA", "#CCFBF1", "#E2E8F0" };
            var donutData = empItems
                .GroupBy(x => x.Department)
                .Select(g => new { Dept = g.Key, Total = g.Sum(x => x.TotalCost) })
                .OrderByDescending(x => x.Total)
                .Select((x, idx) => new DepartmentCostDonutVm
                {
                    Department = x.Dept,
                    TotalCost = x.Total,
                    Color = palette[idx % palette.Length]
                }).ToList();

            var vm = new DepartmentCostBreakdownReportVm
            {
                Year = pYear,
                Month = pMonth,
                StaffCat = cat,
                TotalNetPayroll = empItems.Sum(x => x.PayrollNet),
                TotalEncashment = empItems.Sum(x => x.EncashmentCost),
                TotalProjectAllowance = empItems.Sum(x => x.ProjectAllowance),
                CombinedCost = empItems.Sum(x => x.TotalCost),
                DonutData = donutData,
                DeptItems = deptItems,
                EmpItems = empItems.OrderByDescending(x => x.TotalCost).ToList()
            };

            return View(vm);
        }

        // 3. Annual Leave & Air Ticket Encashment Forecast (Next 12 Months)
        public async Task<IActionResult> EncashmentForecast(string staffCat = "ALL")
        {
            if (!HasPriv("RPT_ENCFC")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            var today = DateTime.Today;
            var horizonEnd = today.AddMonths(12);

            var emps = await _context.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId,
                    e.JoinedDate,
                    e.CntryId
                }).ToListAsync();

            var salaries = (await _context.HrPrEmpSlryStruc
                .Where(s => s.RecStat == 1)
                .OrderByDescending(s => s.SlStrcId)
                .ToListAsync())
                .GroupBy(s => s.EmpSysId)
                .ToDictionary(g => g.Key, g => g.First());

            var ticketPrices = await _context.HrPrAirTicketPrice
                .Where(x => x.RecStat == 1)
                .ToDictionaryAsync(x => x.CntryId, x => x.Price);

            var depCounts = await _context.HrEmpDependent
                .Where(x => x.RecStat == 1)
                .GroupBy(x => x.EmpSysId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var jobMap = await GetJobInfoMapAsync();
            var items = new List<EncashmentForecastItemVm>();

            foreach (var e in emps)
            {
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                DateTime nextAnniv;
                try { nextAnniv = new DateTime(today.Year, e.JoinedDate.Month, e.JoinedDate.Day); }
                catch { nextAnniv = new DateTime(today.Year, e.JoinedDate.Month, 28); }
                if (nextAnniv < today) nextAnniv = nextAnniv.AddYears(1);

                if (nextAnniv > horizonEnd) continue;
                int completedYears = nextAnniv.Year - e.JoinedDate.Year;
                if (completedYears < 1) continue;

                bool isFirstYear = completedYears == 1;
                decimal monthly = 0m;
                if (salaries.TryGetValue(e.EmpSysId, out var sl))
                {
                    monthly = sl.Basic + sl.HrAlwnc + sl.TrnsAlwnc + sl.Tra + sl.PstnAlwnc + sl.ProjAlwnc
                            + sl.MedAlwnc + sl.MobAlwnc + sl.SpcAlwnc + sl.PrfmAlwnc + sl.OthrAlwnc;
                }

                decimal estLeave = isFirstYear ? 0m : Math.Round(monthly, 0);
                int tktCnt = 1 + Math.Min(3, depCounts.TryGetValue(e.EmpSysId, out int dCnt) ? dCnt : 0);
                ticketPrices.TryGetValue(e.CntryId, out decimal tktPrice);
                decimal estTkt = Math.Round(tktCnt * tktPrice, 0);

                items.Add(new EncashmentForecastItemVm
                {
                    EmpId = e.EmpId,
                    FullName = e.FullName,
                    Department = dept,
                    StaffCategory = sCat,
                    AnniversaryMonth = nextAnniv.ToString("MMM yyyy"),
                    AnniversaryDate = nextAnniv,
                    CompletedYears = completedYears,
                    EntitlementRule = isFirstYear ? "1st Year (Ticket Only)" : "2+ Years (Leave + Ticket)",
                    EstLeaveSalary = estLeave,
                    EstAirTicket = estTkt,
                    TotalForecast = estLeave + estTkt
                });
            }

            items = items.OrderBy(x => x.AnniversaryDate).ToList();

            var monthlyChart = new List<EncashmentForecastMonthVm>();
            for (int m = 0; m < 12; m++)
            {
                var dt = new DateTime(today.Year, today.Month, 1).AddMonths(m);
                string lbl = dt.ToString("MMM yyyy");
                var mRows = items.Where(x => x.AnniversaryMonth == lbl).ToList();
                monthlyChart.Add(new EncashmentForecastMonthVm
                {
                    MonthLabel = lbl,
                    FirstYearTicketAmount = mRows.Where(x => x.CompletedYears == 1).Sum(x => x.TotalForecast),
                    MultiYearAmount = mRows.Where(x => x.CompletedYears > 1).Sum(x => x.TotalForecast)
                });
            }

            var vm = new EncashmentForecastReportVm
            {
                StaffCat = cat,
                TotalForecastAmount = items.Sum(x => x.TotalForecast),
                FirstYearTicketAmount = items.Where(x => x.CompletedYears == 1).Sum(x => x.TotalForecast),
                MultiYearLeaveTicketAmount = items.Where(x => x.CompletedYears > 1).Sum(x => x.TotalForecast),
                TotalEligibleEmployees = items.Count,
                MonthlyChart = monthlyChart,
                Items = items
            };

            return View(vm);
        }

        // 4. Department-wise Employee Turnover (Last 3, 6, 12 Months)
        public async Task<IActionResult> DepartmentTurnover(int months = 12, string staffCat = "ALL")
        {
            if (!HasPriv("RPT_DPTTUR")) return RedirectToPage("/Account/Login", new { area = "Identity" });
            int period = (months == 3 || months == 6 || months == 12) ? months : 12;
            string cat = string.IsNullOrWhiteSpace(staffCat) ? "ALL" : staffCat.ToUpper();
            var today = DateTime.Today;
            var cut3 = today.AddMonths(-3);
            var cut6 = today.AddMonths(-6);
            var cut12 = today.AddMonths(-12);
            var cutSelected = today.AddMonths(-period);

            var emps = await _context.HrEmpGenInfo
                .Select(e => new
                {
                    e.EmpSysId,
                    e.EmpId,
                    FullName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                    e.WrktypId,
                    e.CurrStat,
                    e.JoinedDate,
                    e.SrvEndDate
                }).ToListAsync();

            var jobMap = await GetJobInfoMapAsync();
            var deptList = new List<(string Dept, bool IsActive, DateTime Joined, DateTime? ExitDate)>();
            var exitDetails = new List<EmployeeExitItemVm>();

            foreach (var e in emps)
            {
                string dept = jobMap.TryGetValue(e.EmpSysId, out var jm) ? jm.DeptName : "General";
                string sCat = ResolveStaffCategory(e.WrktypId, dept);
                if (cat != "ALL" && sCat != cat) continue;

                bool isExited = e.CurrStat != 0 || (e.SrvEndDate.HasValue && e.SrvEndDate.Value.Date <= today);
                DateTime? exitDt = isExited ? e.SrvEndDate : null;

                deptList.Add((dept, !isExited, e.JoinedDate, exitDt));

                if (exitDt.HasValue && exitDt.Value.Date >= cutSelected && exitDt.Value.Date <= today)
                {
                    decimal tenure = Math.Round((decimal)(exitDt.Value.Date - e.JoinedDate.Date).TotalDays / 365.25m, 1);
                    exitDetails.Add(new EmployeeExitItemVm
                    {
                        EmpId = e.EmpId,
                        FullName = e.FullName,
                        Department = dept,
                        StaffCategory = sCat,
                        JoinedDate = e.JoinedDate,
                        ExitDate = exitDt.Value,
                        TenureYears = Math.Max(0m, tenure)
                    });
                }
            }

            var deptSummaries = deptList
                .GroupBy(x => x.Dept)
                .Select(g =>
                {
                    int active = g.Count(x => x.IsActive);
                    int joiners = g.Count(x => x.Joined.Date >= cutSelected && x.Joined.Date <= today);
                    int ex3 = g.Count(x => x.ExitDate.HasValue && x.ExitDate.Value.Date >= cut3 && x.ExitDate.Value.Date <= today);
                    int ex6 = g.Count(x => x.ExitDate.HasValue && x.ExitDate.Value.Date >= cut6 && x.ExitDate.Value.Date <= today);
                    int ex12 = g.Count(x => x.ExitDate.HasValue && x.ExitDate.Value.Date >= cut12 && x.ExitDate.Value.Date <= today);
                    int exSel = period == 3 ? ex3 : period == 6 ? ex6 : ex12;
                    decimal avgHead = active + (exSel / 2.0m);
                    decimal rate = avgHead > 0 ? Math.Round((exSel / avgHead) * 100m, 1) : 0m;
                    var tenures = g.Where(x => x.ExitDate.HasValue && x.ExitDate.Value.Date >= cutSelected)
                                   .Select(x => Math.Max(0m, (decimal)(x.ExitDate.Value.Date - x.Joined.Date).TotalDays / 365.25m))
                                   .ToList();

                    return new DepartmentTurnoverSummaryVm
                    {
                        Department = g.Key,
                        ActiveCount = active,
                        JoinersInPeriod = joiners,
                        Exits3M = ex3,
                        Exits6M = ex6,
                        Exits12M = ex12,
                        ExitsSelected = exSel,
                        TurnoverRatePct = rate,
                        AvgTenureYears = tenures.Any() ? Math.Round(tenures.Average(), 1) : 0m
                    };
                })
                .Where(x => x.ActiveCount > 0 || x.Exits12M > 0)
                .OrderByDescending(x => x.ExitsSelected)
                .ThenByDescending(x => x.ActiveCount)
                .ToList();

            int totActive = deptSummaries.Sum(x => x.ActiveCount);
            int totExits = deptSummaries.Sum(x => x.ExitsSelected);
            decimal avgTotHead = totActive + (totExits / 2.0m);

            var vm = new DepartmentTurnoverReportVm
            {
                PeriodMonths = period,
                StaffCat = cat,
                ActiveHeadcount = totActive,
                ExitsInPeriod = totExits,
                JoinersInPeriod = deptSummaries.Sum(x => x.JoinersInPeriod),
                TurnoverRatePct = avgTotHead > 0 ? Math.Round((totExits / avgTotHead) * 100m, 1) : 0m,
                DeptSummaries = deptSummaries,
                ExitDetails = exitDetails.OrderByDescending(x => x.ExitDate).ToList()
            };

            return View(vm);
        }
    }
}
