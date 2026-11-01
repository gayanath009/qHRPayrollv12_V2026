using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using qHRM.Areas.Leave.Models;
using qHRM.Models;

namespace qHRM.Areas.Leave.Data
{
    public class LeaveBalanceService
    {
        private readonly qHRMasterContext _hrmContext;
        private readonly ILogger<LeaveBalanceService> _logger;

        public LeaveBalanceService(qHRMasterContext hrmContext, ILogger<LeaveBalanceService> logger = null)
        {
            _hrmContext = hrmContext;
            _logger = logger;
        }

        // =====================================================================
        // CORE ACCRUAL FORMULA FOR ANNUAL LEAVE
        // Service Anniversary Reset & Dynamic Pro-Rata Accrual
        // 30 days/year => 2.5 days per month => 1.25 days per 15 days (0.0833 days/day)
        // =====================================================================
        public static (DateTime cycleStart, DateTime cycleEnd, DateTime nextReset, decimal accruedDays, decimal ratePerMonth, string formulaText) CalculateAnnualLeaveAccrual(
            DateTime joinedDate, decimal annualQuota, DateTime asOfDate)
        {
            joinedDate = joinedDate.Date;
            asOfDate = asOfDate.Date;

            if (annualQuota <= 0) annualQuota = 30.00m;
            decimal ratePerMonth = Math.Round(annualQuota / 12.0m, 4); // 2.5000

            DateTime oneYearCompletionDate = joinedDate.AddYears(1);
            if (asOfDate < joinedDate)
            {
                DateTime initCycleEnd = oneYearCompletionDate.AddDays(-1);
                return (joinedDate, initCycleEnd, oneYearCompletionDate, 0m, ratePerMonth, "Service has not commenced yet.");
            }

            // Employees must complete 1 full year within the company before becoming eligible for Annual Leave
            if (asOfDate < oneYearCompletionDate)
            {
                DateTime firstYearEnd = oneYearCompletionDate.AddDays(-1);
                return (joinedDate, firstYearEnd, oneYearCompletionDate, 0m, ratePerMonth,
                    $"Service Requirement: Employees must complete 1 year of service with the company before becoming eligible for Annual Leave. Eligible from {oneYearCompletionDate:dd/MM/yyyy} (Joined: {joinedDate:dd/MM/yyyy}).");
            }

            int jMonth = joinedDate.Month;
            int jDay = joinedDate.Day;

            // Determine current anniversary in asOfDate's year
            int targetDayThisYear = Math.Min(jDay, DateTime.DaysInMonth(asOfDate.Year, jMonth));
            DateTime thisYearAnniversary = new DateTime(asOfDate.Year, jMonth, targetDayThisYear);

            DateTime cycleStart;
            DateTime nextReset;

            if (asOfDate >= thisYearAnniversary)
            {
                cycleStart = thisYearAnniversary;
                int nextTargetDay = Math.Min(jDay, DateTime.DaysInMonth(asOfDate.Year + 1, jMonth));
                nextReset = new DateTime(asOfDate.Year + 1, jMonth, nextTargetDay);
            }
            else
            {
                int prevTargetDay = Math.Min(jDay, DateTime.DaysInMonth(asOfDate.Year - 1, jMonth));
                cycleStart = new DateTime(asOfDate.Year - 1, jMonth, prevTargetDay);
                nextReset = thisYearAnniversary;
            }

            if (cycleStart < joinedDate)
            {
                cycleStart = joinedDate;
            }

            DateTime cycleEnd = nextReset.AddDays(-1);

            // Calculate months and days elapsed in current cycle
            int months = ((asOfDate.Year - cycleStart.Year) * 12) + asOfDate.Month - cycleStart.Month;
            int days = asOfDate.Day - cycleStart.Day;

            if (days < 0)
            {
                months--;
                var prevMonthDate = asOfDate.AddMonths(-1);
                int daysInPrevMonth = DateTime.DaysInMonth(prevMonthDate.Year, prevMonthDate.Month);
                days += daysInPrevMonth;
            }

            if (months < 0)
            {
                months = 0;
                days = 0;
            }

            // Accrual formula: (months * 2.5) + ((days / 30.0) * 2.5)
            // Example: 15 days => 1.25 days raw => 1 whole day accrued
            // 30 days => 2.50 days raw => 2 whole days accrued
            decimal accruedRaw = (months * ratePerMonth) + ((days / 30.0m) * ratePerMonth);
            accruedRaw = Math.Min(annualQuota, Math.Max(0m, Math.Round(accruedRaw, 2)));
            decimal accruedWholeDays = Math.Floor(accruedRaw);

            string formulaText = $"Accrued {accruedWholeDays:0} full day(s) since work anniversary {cycleStart:dd/MM/yyyy}. Next reset on {nextReset:dd/MM/yyyy}.";

            return (cycleStart, cycleEnd, nextReset, accruedWholeDays, ratePerMonth, formulaText);
        }

        // =====================================================================
        // 1. GET EMPLOYEE LEAVE BALANCES FOR DASHBOARD, INBOX & LEAVE APPLICATION
        // =====================================================================
        public async Task<List<EmpLeaveBalanceViewModel>> GetEmployeeLeaveBalancesAsync(int empSysId, int year = 0, DateTime? asOfDate = null)
        {
            var result = new List<EmpLeaveBalanceViewModel>();
            try
            {
                if (empSysId <= 0)
                {
                    var defaultEmp = await _hrmContext.HrEmpGenInfo.OrderBy(e => e.EmpSysId).FirstOrDefaultAsync();
                    if (defaultEmp != null)
                        empSysId = defaultEmp.EmpSysId;
                    else
                        return result;
                }

                DateTime evalDate = asOfDate ?? DateTime.Today;
                if (year <= 0) year = evalDate.Year;

                var emp = await _hrmContext.HrEmpGenInfo
                    .Include(e => e.Rel)
                    .FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
                if (emp == null) return result;

                bool isFemale = string.Equals(emp.Gender, "Female", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(emp.Gender, "F", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(emp.Title, "Ms.", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(emp.Title, "Mrs.", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(emp.Title, "Miss", StringComparison.OrdinalIgnoreCase);

                bool isMuslim = false;
                if (emp.RelId.HasValue)
                {
                    var rel = emp.Rel ?? await _hrmContext.OrgReligion.FirstOrDefaultAsync(r => r.RelId == emp.RelId.Value);
                    if (rel != null && rel.RelNameEng.Equals("Islam", StringComparison.OrdinalIgnoreCase))
                    {
                        isMuslim = true;
                    }
                }

                var leaveTypes = await _hrmContext.HrLvType
                    .Include(t => t.HrLvPolicy)
                    .Where(t => t.RecStat == 1)
                    .OrderBy(t => t.LvTypId)
                    .ToListAsync();

                var runningBalances = await _hrmContext.HrLvRunningBalance
                    .Where(r => r.EmpSysId == empSysId && r.LvYear == year)
                    .ToListAsync();

                var openingBalances = await _hrmContext.HrLvOpeningBalance
                    .Where(o => o.EmpSysId == empSysId && o.LvYear == year)
                    .ToListAsync();

                // Query all active leave applications for this employee to accurately filter per leave cycle
                var allEmpLeaves = await _hrmContext.HrLvRegister
                    .Where(l => l.EmpSysId == empSysId && l.RecStat != 0 && l.RecStat != 3)
                    .ToListAsync();

                // Check lifetime Hajj usage
                bool hasTakenHajjLifetime = allEmpLeaves.Any(l =>
                    l.LvTypId.HasValue &&
                    (l.RecStat == 2 || l.WfStat == "Approved") &&
                    _hrmContext.HrLvType.Any(t => t.LvTypId == l.LvTypId.Value &&
                                                 (t.LvTypCode == "LV_HAJ" || t.LvTypCode == "LV_HAJJ" || t.NameEng.Contains("Hajj") || t.NameEng.Contains("HAJJI"))));

                bool dbChanged = false;

                foreach (var lt in leaveTypes)
                {
                    string code = lt.LvTypCode ?? "";
                    string name = lt.NameEng ?? "";

                    bool isMaternity = code == "LV_MTN" || code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase);
                    if (isMaternity && !isFemale)
                    {
                        continue; // Restrict Maternity Leave to female employees only
                    }

                    bool isHajji = code == "LV_HAJ" || code == "LV_HAJJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase) || name.Contains("HAJJI", StringComparison.OrdinalIgnoreCase);
                    if (isHajji && !isMuslim)
                    {
                        continue; // Restrict HAJJI Leave to Muslim employees only
                    }

                    bool isAnnual = code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase);
                    bool isSick = code == "LV_SICK" || name.Contains("Sick", StringComparison.OrdinalIgnoreCase);
                    bool isCasual = code == "LV_CSL" || name.Contains("Casual", StringComparison.OrdinalIgnoreCase);

                    // Policy entitlement lookup
                    var policy = lt.HrLvPolicy?.FirstOrDefault(p => p.RecStat == 1);
                    decimal standardQuota = policy != null && policy.ElgDays > 0 ? policy.ElgDays : GetDefaultEntitlement(lt);

                    var opnBal = openingBalances.FirstOrDefault(o => o.LvTypId == lt.LvTypId);
                    bool hasSnapshot = opnBal != null && opnBal.SnapshotDate.HasValue;
                    decimal openingDays = opnBal?.OpeningDays ?? 0m;
                    decimal snapshotBooked = opnBal?.BookedDays ?? 0m;
                    DateTime? snapDate = opnBal?.SnapshotDate;

                    decimal entitlement = standardQuota;
                    decimal accruedDays = standardQuota;
                    decimal approvedDays = 0m;
                    decimal inProgressDays = 0m;
                    decimal remaining = 0m;
                    DateTime cycleStart;
                    DateTime cycleEnd;
                    DateTime nextReset;
                    string formulaText = "";
                    decimal ratePerMonth = 0m;

                    decimal currentCycleRemaining = 0m;
                    decimal nextCycleRemaining = 0m;
                    DateTime? currentCycleResetDate = null;
                    decimal currCycleApproved = 0m;
                    decimal currCycleInProgress = 0m;

                    if (isAnnual)
                    {
                        // -------------------------------------------------------------
                        // ANNUAL LEAVE: SERVICE ANNIVERSARY RESET & DYNAMIC CYCLE BALANCE
                        // 30 days quota per cycle. Reset date based on JoinedDate.
                        // Must complete 1 year of service before becoming eligible.
                        // -------------------------------------------------------------
                        DateTime oneYearCompletionDate = emp.JoinedDate.Date.AddYears(1);
                        bool completedOneYear = evalDate.Date >= oneYearCompletionDate;

                        if (!completedOneYear)
                        {
                            cycleStart = emp.JoinedDate.Date;
                            cycleEnd = oneYearCompletionDate.AddDays(-1);
                            nextReset = oneYearCompletionDate;
                            currentCycleResetDate = oneYearCompletionDate;
                            entitlement = 0m;
                            accruedDays = 0m;
                            approvedDays = 0m;
                            inProgressDays = 0m;
                            remaining = 0m;
                            currentCycleRemaining = 0m;
                            nextCycleRemaining = 0m;
                            ratePerMonth = Math.Round(standardQuota / 12.0m, 4);
                            formulaText = $"Service Requirement Not Met: Employees must complete 1 year of service before being eligible for Annual Leave. Eligible from {oneYearCompletionDate:dd/MM/yyyy} (Joined: {emp.JoinedDate:dd/MM/yyyy}).";
                        }
                        else
                        {
                            var todayAccrual = CalculateAnnualLeaveAccrual(emp.JoinedDate, standardQuota, DateTime.Today);
                            DateTime currResetDate = todayAccrual.nextReset;
                            DateTime currCycleStart = todayAccrual.cycleStart;
                            DateTime currCycleEnd = todayAccrual.cycleEnd;
                            currentCycleResetDate = currResetDate;
                            ratePerMonth = todayAccrual.ratePerMonth;

                            if (hasSnapshot)
                            {
                                // Snapshot bookedDays represents days booked in current service year up to snapshot date
                                decimal postSnapApproved = allEmpLeaves
                                    .Where(l => l.LvTypId == lt.LvTypId &&
                                                l.CrtDateTime > snapDate.Value &&
                                                l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date &&
                                                (l.RecStat == 2 || l.WfStat == "Approved"))
                                    .Sum(l => (decimal)l.Days);

                                decimal postSnapInProgress = allEmpLeaves
                                    .Where(l => l.LvTypId == lt.LvTypId &&
                                                l.CrtDateTime > snapDate.Value &&
                                                l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date &&
                                                l.RecStat != 0 && l.RecStat != 2 && l.RecStat != 3 &&
                                                l.WfStat != "Approved" && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                                    .Sum(l => (decimal)l.Days);

                                currCycleApproved = snapshotBooked + postSnapApproved;
                                currCycleInProgress = postSnapInProgress;
                            }
                            else
                            {
                                currCycleApproved = allEmpLeaves
                                    .Where(l => l.LvTypId == lt.LvTypId &&
                                                l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date &&
                                                (l.RecStat == 2 || l.WfStat == "Approved"))
                                    .Sum(l => (decimal)l.Days);

                                currCycleInProgress = allEmpLeaves
                                    .Where(l => l.LvTypId == lt.LvTypId &&
                                                l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date &&
                                                l.RecStat != 0 && l.RecStat != 2 && l.RecStat != 3 &&
                                                l.WfStat != "Approved" && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                                    .Sum(l => (decimal)l.Days);
                            }

                            decimal currCycleEntitlement = standardQuota;
                            currentCycleRemaining = Math.Max(0m, currCycleEntitlement - currCycleApproved - currCycleInProgress);

                            // NEXT service cycle (starts on currResetDate)
                            DateTime nextCycleStart = currResetDate;
                            DateTime nextResetDate2 = currResetDate.AddYears(1);
                            DateTime nextCycleEnd = nextResetDate2.AddDays(-1);

                            decimal nextCycleApproved = allEmpLeaves
                                .Where(l => l.LvTypId == lt.LvTypId &&
                                            l.StDate.Date >= nextCycleStart.Date && l.StDate.Date < nextResetDate2.Date &&
                                            (l.RecStat == 2 || l.WfStat == "Approved"))
                                .Sum(l => (decimal)l.Days);

                            decimal nextCycleInProgress = allEmpLeaves
                                .Where(l => l.LvTypId == lt.LvTypId &&
                                            l.StDate.Date >= nextCycleStart.Date && l.StDate.Date < nextResetDate2.Date &&
                                            l.RecStat != 0 && l.RecStat != 2 && l.RecStat != 3 &&
                                            l.WfStat != "Approved" && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                                .Sum(l => (decimal)l.Days);

                            decimal nextCycleEntitlement = standardQuota;
                            nextCycleRemaining = Math.Max(0m, nextCycleEntitlement - nextCycleApproved - nextCycleInProgress);

                            // Evaluate based on requested evaluation date (e.g. leave StDate or Today)
                            bool isPostResetEval = evalDate.Date >= currResetDate.Date;

                            if (isPostResetEval)
                            {
                                cycleStart = nextCycleStart;
                                cycleEnd = nextCycleEnd;
                                nextReset = nextResetDate2;
                                entitlement = nextCycleEntitlement;
                                approvedDays = nextCycleApproved;
                                inProgressDays = nextCycleInProgress;
                                remaining = nextCycleRemaining;
                                accruedDays = standardQuota;
                                formulaText = $"New Service Cycle ({cycleStart:dd/MM/yyyy} to {cycleEnd:dd/MM/yyyy}): {remaining:0} day(s) available for leave starting after reset date ({currResetDate:dd/MM/yyyy}). Next Reset Date: {nextReset:dd/MM/yyyy}.";
                            }
                            else
                            {
                                cycleStart = currCycleStart;
                                cycleEnd = currCycleEnd;
                                nextReset = currResetDate;
                                entitlement = currCycleEntitlement;
                                approvedDays = currCycleApproved;
                                inProgressDays = currCycleInProgress;
                                remaining = currentCycleRemaining;
                                accruedDays = standardQuota;
                                formulaText = $"Current Service Cycle ({cycleStart:dd/MM/yyyy} to {cycleEnd:dd/MM/yyyy}): {remaining:0} day(s) remaining ({entitlement:0} annual quota - {approvedDays:0} used). Next Reset Date: {nextReset:dd/MM/yyyy}.";
                            }
                        }
                    }
                    else if (hasSnapshot)
                    {
                        // -------------------------------------------------------------
                        // MASTER SNAPSHOT IMPORT: Base figures imported from master Excel
                        // -------------------------------------------------------------
                        entitlement = snapshotBooked + openingDays;
                        accruedDays = entitlement;
                        cycleStart = snapDate.Value;
                        cycleEnd = new DateTime(year, 12, 31);
                        nextReset = new DateTime(year + 1, 1, 1);

                        // Leaves created / approved in system strictly AFTER snapshot date
                        var postSnapApproved = allEmpLeaves
                            .Where(l => l.LvTypId == lt.LvTypId &&
                                        l.CrtDateTime > snapDate.Value &&
                                        (l.RecStat == 2 || l.WfStat == "Approved"))
                            .Sum(l => (decimal)l.Days);

                        var postSnapInProgress = allEmpLeaves
                            .Where(l => l.LvTypId == lt.LvTypId &&
                                        l.CrtDateTime > snapDate.Value &&
                                        l.RecStat != 0 && l.RecStat != 2 && l.RecStat != 3 &&
                                        l.WfStat != "Approved" && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                            .Sum(l => (decimal)l.Days);

                        approvedDays = snapshotBooked + postSnapApproved;
                        inProgressDays = postSnapInProgress;
                        formulaText = $"Master Balance: {openingDays:0.##} bal, {snapshotBooked:0.##} booked (Imported: {snapDate:dd/MM/yyyy}).";
                    }
                    else
                    {
                        // -------------------------------------------------------------
                        // SICK / CASUAL / OTHER LEAVES: CALENDAR YEAR RESET (JAN 1)
                        // -------------------------------------------------------------
                        cycleStart = new DateTime(year, 1, 1);
                        cycleEnd = new DateTime(year, 12, 31);
                        nextReset = new DateTime(year + 1, 1, 1);
                        entitlement = standardQuota + openingDays;
                        accruedDays = standardQuota;

                        // Filter approved and in-progress leaves in this calendar year
                        approvedDays = allEmpLeaves
                            .Where(l => l.LvTypId == lt.LvTypId &&
                                        (l.LvYear == year || (l.StDate.Date >= cycleStart && l.StDate.Date <= cycleEnd)) &&
                                        (l.RecStat == 2 || l.WfStat == "Approved"))
                            .Sum(l => (decimal)l.Days);

                        inProgressDays = allEmpLeaves
                            .Where(l => l.LvTypId == lt.LvTypId &&
                                        (l.LvYear == year || (l.StDate.Date >= cycleStart && l.StDate.Date <= cycleEnd)) &&
                                        l.RecStat != 0 && l.RecStat != 2 && l.RecStat != 3 &&
                                        l.WfStat != "Approved" && l.WfStat != "Rejected" && l.WfStat != "Cancelled")
                            .Sum(l => (decimal)l.Days);

                        if (isSick)
                        {
                            formulaText = $"Calendar Year Entitlement ({standardQuota:0} days). Resets on 01/01/{year + 1}.";
                        }
                        else if (isCasual)
                        {
                            formulaText = $"Calendar Year Entitlement (Max {standardQuota:0} paid days). Resets on 01/01/{year + 1}.";
                        }
                        else
                        {
                            formulaText = $"Calendar Year Entitlement ({standardQuota:0} days). Resets on 01/01/{year + 1}.";
                        }
                    }

                    // Available remaining balance (cannot be negative)
                    if (!isAnnual)
                    {
                        remaining = Math.Max(0m, entitlement - approvedDays - inProgressDays);
                    }

                    // Maintain Running Balance sync in database
                    var runBal = runningBalances.FirstOrDefault(r => r.LvTypId == lt.LvTypId);
                    DateTime oneYearCheckDate = emp.JoinedDate.Date.AddYears(1);
                    bool isOneYearDone = evalDate.Date >= oneYearCheckDate;
                    decimal effectiveEntitled = isAnnual ? (isOneYearDone ? standardQuota : 0m) : (hasSnapshot ? entitlement : standardQuota);
                    decimal effectiveUsed = isAnnual ? currCycleApproved : approvedDays;
                    decimal effectiveBalance = isAnnual ? (isOneYearDone ? currentCycleRemaining : 0m) : Math.Max(0, effectiveEntitled - approvedDays);

                    if (runBal != null)
                    {
                        if (runBal.EntitledDays != effectiveEntitled || runBal.UsedDays != effectiveUsed || runBal.BalanceDays != effectiveBalance)
                        {
                            runBal.EntitledDays = effectiveEntitled;
                            runBal.UsedDays = effectiveUsed;
                            runBal.BalanceDays = effectiveBalance;
                            runBal.LastUpdated = DateTime.Now;
                            dbChanged = true;
                        }
                    }
                    else
                    {
                        runBal = new HrLvRunningBalance
                        {
                            EmpSysId = empSysId,
                            LvTypId = lt.LvTypId,
                            LvYear = year,
                            EntitledDays = effectiveEntitled,
                            UsedDays = effectiveUsed,
                            BalanceDays = effectiveBalance,
                            LastUpdated = DateTime.Now
                        };
                        _hrmContext.HrLvRunningBalance.Add(runBal);
                        dbChanged = true;
                    }

                    string ruleNotice = "";
                    string badgeClass = "badge-success";

                    if (isSick)
                    {
                        ruleNotice = policy != null && policy.HalfPaidDays > 0
                            ? $"First {policy.FullPaidDays:0} days fully paid, followed by {policy.HalfPaidDays:0} days at half pay (Resets Jan 1)"
                            : "First 14 days are fully paid, followed by 28 days at half pay (Resets Jan 1)";
                        badgeClass = "badge-danger";
                    }
                    else if (isCasual)
                    {
                        ruleNotice = "Maximum 3 paid days per calendar year (Resets Jan 1)";
                        badgeClass = "badge-warning";
                    }
                    else if (code == "LV_EMG_PD" || code == "LV_EMG" || name.Contains("Emergency-Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        ruleNotice = "Emergency Leave (Paid, max 30 days, subject to approval)";
                        badgeClass = "badge-info";
                    }
                    else if (code == "LV_EMG_UPD" || name.Contains("Emergency-Unpaid", StringComparison.OrdinalIgnoreCase))
                    {
                        ruleNotice = "Emergency Leave (Unpaid / No-Pay, max 30 days)";
                        badgeClass = "badge-secondary";
                    }
                    else if (code == "LV_ABS" || name.Contains("Absent", StringComparison.OrdinalIgnoreCase))
                    {
                        ruleNotice = "Absent (Unpaid / No-Pay)";
                        badgeClass = "badge-dark";
                    }
                    else if (isHajji)
                    {
                        if (hasTakenHajjLifetime)
                        {
                            ruleNotice = "Already availed in service (Once in lifetime)";
                            remaining = 0;
                            approvedDays = standardQuota;
                            inProgressDays = 0;
                            badgeClass = "badge-secondary";
                        }
                        else
                        {
                            ruleNotice = "Maximum 14 days (Unpaid, once in lifetime)";
                            badgeClass = "badge-purple";
                        }
                    }
                    else if (isMaternity)
                    {
                        ruleNotice = "90 days full pay (Maternity entitlement)";
                        badgeClass = "badge-pink";
                    }
                    else if (isAnnual)
                    {
                        if (!isOneYearDone)
                        {
                            ruleNotice = $"Annual Leave Policy: Restricted. Employees must complete 1 year of service within the company (Eligible from {oneYearCheckDate:dd/MM/yyyy}).";
                            badgeClass = "badge-secondary";
                        }
                        else
                        {
                            ruleNotice = $"Annual Leave: Accruing at {ratePerMonth:0.##} days/month since {cycleStart:dd/MM/yyyy}. Resets on joining date ({nextReset:dd/MM/yyyy}).";
                            badgeClass = "badge-primary";
                        }
                    }

                    result.Add(new EmpLeaveBalanceViewModel
                    {
                        LvTypId = lt.LvTypId,
                        LeaveTypeCode = code,
                        LeaveTypeName = name,
                        Entitlement = isAnnual ? entitlement : standardQuota,
                        InProgress = inProgressDays,
                        Used = approvedDays,
                        Remaining = remaining,
                        IsApplicable = isAnnual ? isOneYearDone : true,
                        RuleNotice = ruleNotice,
                        BadgeClass = badgeClass,
                        IsAccrualBased = isAnnual,
                        AccruedDays = accruedDays,
                        AnnualQuota = standardQuota,
                        OpeningDays = openingDays,
                        AccrualRatePerMonth = isAnnual ? ratePerMonth : 0m,
                        JoinedDate = emp.JoinedDate,
                        ServicePeriodStart = cycleStart,
                        ServicePeriodEnd = cycleEnd,
                        NextResetDate = nextReset,
                        AccrualFormulaText = formulaText,
                        CurrentCycleRemaining = currentCycleRemaining,
                        NextCycleRemaining = nextCycleRemaining,
                        CurrentCycleResetDate = currentCycleResetDate,
                        HasCompletedOneYear = isAnnual ? isOneYearDone : true,
                        OneYearCompletionDate = isAnnual ? oneYearCheckDate : (DateTime?)null,
                        IsServiceRestricted = isAnnual && !isOneYearDone,
                        RestrictionReason = isAnnual && !isOneYearDone ? $"Employees who have not completed 1 year within the company cannot apply for Annual Leave (Eligible from {oneYearCheckDate:dd/MM/yyyy})." : ""
                    });
                }

                if (dbChanged)
                {
                    await _hrmContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[LeaveBalanceService] Error fetching balances for EmpSysId {empSysId}");
            }

            return result;
        }

        // =====================================================================
        // SYNC ALL RUNNING BALANCES WITH ACTIVE LEAVE TYPES & REGISTER
        // =====================================================================
        public async Task<int> SyncAllRunningBalancesAsync(int? year = null)
        {
            try
            {
                int targetYear = year.HasValue && year.Value > 0 ? year.Value : DateTime.Now.Year;

                // 1. Get active leave types
                var activeLeaveTypes = await _hrmContext.HrLvType
                    .Include(t => t.HrLvPolicy)
                    .Where(t => t.RecStat == 1)
                    .ToListAsync();
                var activeLvTypeIds = activeLeaveTypes.Select(t => t.LvTypId).ToList();

                // 2. Remove obsolete running balances with inactive / non-existent LvTypId
                var obsoleteBalances = await _hrmContext.HrLvRunningBalance
                    .Where(r => !activeLvTypeIds.Contains(r.LvTypId))
                    .ToListAsync();
                if (obsoleteBalances.Any())
                {
                    _hrmContext.HrLvRunningBalance.RemoveRange(obsoleteBalances);
                    await _hrmContext.SaveChangesAsync();
                }

                // 3. Get active employees (CurrStat == 0)
                var activeEmployees = await _hrmContext.HrEmpGenInfo
                    .Where(e => e.CurrStat == 0)
                    .ToListAsync();

                // 4. Query all existing running balances and opening balances for target year
                var existingBalances = await _hrmContext.HrLvRunningBalance
                    .Where(r => r.LvYear == targetYear)
                    .ToListAsync();

                var openingBalances = await _hrmContext.HrLvOpeningBalance
                    .Where(o => o.LvYear == targetYear)
                    .ToListAsync();

                // 5. Query all approved leaves
                var allApprovedLeaves = await _hrmContext.HrLvRegister
                    .Where(l => l.EmpSysId.HasValue && l.LvTypId.HasValue &&
                                (l.RecStat == 2 || l.WfStat == "Approved"))
                    .ToListAsync();

                int syncCount = 0;
                DateTime evalDate = DateTime.Today;

                foreach (var emp in activeEmployees)
                {
                    bool isFemale = string.Equals(emp.Gender, "Female", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(emp.Gender, "F", StringComparison.OrdinalIgnoreCase);

                    foreach (var lt in activeLeaveTypes)
                    {
                        string code = lt.LvTypCode ?? "";
                        string name = lt.NameEng ?? "";

                        bool isMaternity = code == "LV_MTN" || code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase);
                        if (isMaternity && !isFemale) continue;

                        bool isHajji = code == "LV_HAJ" || code == "LV_HAJJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase) || name.Contains("HAJJI", StringComparison.OrdinalIgnoreCase);
                        if (isHajji && emp.RelId != 1) continue;

                        bool isAnnual = code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase);

                        var opnBal = openingBalances.FirstOrDefault(o => o.EmpSysId == emp.EmpSysId && o.LvTypId == lt.LvTypId);
                        var policy = lt.HrLvPolicy?.FirstOrDefault(p => p.RecStat == 1);
                        decimal standardQuota = policy != null && policy.ElgDays > 0 ? policy.ElgDays : GetDefaultEntitlement(lt);

                        decimal entitlement = standardQuota;
                        decimal approvedDays = 0m;

                        if (isAnnual)
                        {
                            DateTime oneYearAnniversary = emp.JoinedDate.Date.AddYears(1);
                            bool completedOneYear = evalDate.Date >= oneYearAnniversary;

                            if (!completedOneYear)
                            {
                                entitlement = 0m;
                                approvedDays = 0m;
                            }
                            else
                            {
                                var todayAccrual = CalculateAnnualLeaveAccrual(emp.JoinedDate, standardQuota, DateTime.Today);
                                DateTime currResetDate = todayAccrual.nextReset;
                                DateTime currCycleStart = todayAccrual.cycleStart;

                                decimal currCycleApproved;
                                if (opnBal != null && opnBal.SnapshotDate.HasValue)
                                {
                                    decimal postSnapApproved = allApprovedLeaves
                                        .Where(l => l.EmpSysId == emp.EmpSysId && l.LvTypId == lt.LvTypId &&
                                                    l.CrtDateTime > opnBal.SnapshotDate.Value &&
                                                    l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date)
                                        .Sum(l => (decimal)l.Days);
                                    currCycleApproved = opnBal.BookedDays + postSnapApproved;
                                }
                                else
                                {
                                    currCycleApproved = allApprovedLeaves
                                        .Where(l => l.EmpSysId == emp.EmpSysId && l.LvTypId == lt.LvTypId &&
                                                    l.StDate.Date >= currCycleStart.Date && l.StDate.Date < currResetDate.Date)
                                        .Sum(l => (decimal)l.Days);
                                }

                                entitlement = standardQuota;
                                approvedDays = currCycleApproved;
                            }
                        }
                        else if (opnBal != null && opnBal.SnapshotDate.HasValue)
                        {
                            // Preserve imported master snapshot
                            entitlement = opnBal.BookedDays + opnBal.OpeningDays;
                            decimal postSnapApproved = allApprovedLeaves
                                .Where(l => l.EmpSysId == emp.EmpSysId && l.LvTypId == lt.LvTypId &&
                                            l.CrtDateTime > opnBal.SnapshotDate.Value)
                                .Sum(l => (decimal)l.Days);
                            approvedDays = opnBal.BookedDays + postSnapApproved;
                        }
                        else
                        {
                            entitlement = standardQuota + (opnBal?.OpeningDays ?? 0m);
                            approvedDays = allApprovedLeaves
                                .Where(l => l.EmpSysId == emp.EmpSysId && l.LvTypId == lt.LvTypId &&
                                            (l.LvYear == targetYear || (l.StDate.Year == targetYear)))
                                .Sum(l => (decimal)l.Days);
                        }

                        var runBal = existingBalances.FirstOrDefault(r => r.EmpSysId == emp.EmpSysId && r.LvTypId == lt.LvTypId);
                        if (runBal == null)
                        {
                            runBal = new HrLvRunningBalance
                            {
                                EmpSysId = emp.EmpSysId,
                                LvTypId = lt.LvTypId,
                                LvYear = targetYear,
                                EntitledDays = entitlement,
                                UsedDays = approvedDays,
                                BalanceDays = Math.Max(0, entitlement - approvedDays),
                                LastUpdated = DateTime.Now
                            };
                            _hrmContext.HrLvRunningBalance.Add(runBal);
                            syncCount++;
                        }
                        else
                        {
                            if (runBal.EntitledDays != entitlement || runBal.UsedDays != approvedDays || runBal.BalanceDays != Math.Max(0, entitlement - approvedDays))
                            {
                                runBal.EntitledDays = entitlement;
                                runBal.UsedDays = approvedDays;
                                runBal.BalanceDays = Math.Max(0, entitlement - approvedDays);
                                runBal.LastUpdated = DateTime.Now;
                                syncCount++;
                            }
                        }
                    }
                }

                await _hrmContext.SaveChangesAsync();
                _logger?.LogInformation($"[LeaveBalanceService] SyncAllRunningBalancesAsync completed: {syncCount} records updated/inserted for year {targetYear}.");
                return syncCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[LeaveBalanceService] Error in SyncAllRunningBalancesAsync");
                return -1;
            }
        }

        // =====================================================================
        // 2. GET AVAILABLE BALANCE FOR A SPECIFIC LEAVE TYPE AS OF A GIVEN DATE
        // =====================================================================
        public async Task<decimal> GetAvailableBalanceAsync(int empSysId, int lvTypId, int year = 0, DateTime? asOfDate = null)
        {
            if (year <= 0) year = asOfDate?.Year ?? DateTime.Now.Year;

            var balances = await GetEmployeeLeaveBalancesAsync(empSysId, year, asOfDate);
            var item = balances.FirstOrDefault(b => b.LvTypId == lvTypId);
            return item != null ? item.Remaining : 0;
        }

        // =====================================================================
        // 3. DEDUCT LEAVE BALANCE UPON FINAL APPROVAL
        // =====================================================================
        public async Task<bool> DeductLeaveBalanceAsync(int lvId, int approverEmpSysId = 0)
        {
            try
            {
                var lvRec = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == lvId);
                if (lvRec == null || !lvRec.EmpSysId.HasValue || !lvRec.LvTypId.HasValue)
                    return false;

                int empSysId = lvRec.EmpSysId.Value;
                int lvTypId = lvRec.LvTypId.Value;
                int year = lvRec.LvYear > 0 ? lvRec.LvYear : (lvRec.StDate.Year > 0 ? lvRec.StDate.Year : DateTime.Now.Year);
                decimal days = lvRec.Days;

                // Check for existing deduction transaction to prevent double deduction
                bool alreadyDeducted = await _hrmContext.HrLvBalanceTransaction
                    .AnyAsync(t => t.LvId == lvId && t.TrnType == "Leave Approved");

                if (alreadyDeducted)
                {
                    _logger?.LogInformation($"[LeaveBalanceService] Leave #{lvId} balance already deducted.");
                    return true;
                }

                var runBal = await _hrmContext.HrLvRunningBalance
                    .FirstOrDefaultAsync(r => r.EmpSysId == empSysId && r.LvTypId == lvTypId && r.LvYear == year);

                if (runBal == null)
                {
                    var lvType = await _hrmContext.HrLvType.Include(t => t.HrLvPolicy).FirstOrDefaultAsync(t => t.LvTypId == lvTypId);
                    var policy = lvType?.HrLvPolicy?.FirstOrDefault(p => p.RecStat == 1);
                    decimal defEnt = policy != null && policy.ElgDays > 0 ? policy.ElgDays : (lvType != null ? GetDefaultEntitlement(lvType) : 0);
                    runBal = new HrLvRunningBalance
                    {
                        EmpSysId = empSysId,
                        LvTypId = lvTypId,
                        LvYear = year,
                        EntitledDays = defEnt,
                        UsedDays = 0,
                        BalanceDays = defEnt,
                        LastUpdated = DateTime.Now
                    };
                    _hrmContext.HrLvRunningBalance.Add(runBal);
                    await _hrmContext.SaveChangesAsync();
                }

                decimal balBefore = runBal.BalanceDays;
                decimal balAfter = balBefore - days;

                runBal.UsedDays += days;
                runBal.BalanceDays = balAfter;
                runBal.LastUpdated = DateTime.Now;

                // Log audit transaction
                var trn = new HrLvBalanceTransaction
                {
                    EmpSysId = empSysId,
                    LvTypId = lvTypId,
                    LvYear = year,
                    TrnType = "Leave Approved",
                    LvId = lvId,
                    Days = -days,
                    BalanceBefore = balBefore,
                    BalanceAfter = balAfter,
                    Remarks = $"Leave approved (Ref #{lvId}, {days} days from {lvRec.StDate:yyyy-MM-dd} to {lvRec.EndDate:yyyy-MM-dd})",
                    CrtId = approverEmpSysId > 0 ? approverEmpSysId : 1,
                    CrtDateTime = DateTime.Now
                };
                _hrmContext.HrLvBalanceTransaction.Add(trn);

                await _hrmContext.SaveChangesAsync();
                _logger?.LogInformation($"[LeaveBalanceService] Deducted {days} days for Leave #{lvId}. Balance: {balBefore} -> {balAfter}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[LeaveBalanceService] Failed to deduct balance for Leave #{lvId}");
                return false;
            }
        }

        // =====================================================================
        // 4. RESTORE LEAVE BALANCE UPON CANCELLATION / REVERSAL / DELETION
        // =====================================================================
        public async Task<bool> RestoreLeaveBalanceAsync(int lvId, string reason = "Leave Cancelled", int userEmpSysId = 0)
        {
            return await RestoreLeaveBalanceInternalAsync(lvId, reason, userEmpSysId);
        }

        public async Task<bool> RestoreLeaveBalanceAsync(int lvId, int userEmpSysId, string reason = "Leave Cancelled")
        {
            return await RestoreLeaveBalanceInternalAsync(lvId, reason, userEmpSysId);
        }

        private async Task<bool> RestoreLeaveBalanceInternalAsync(int lvId, string reason, int userEmpSysId)
        {
            try
            {
                var lvRec = await _hrmContext.HrLvRegister.FirstOrDefaultAsync(l => l.LvId == lvId);
                if (lvRec == null || !lvRec.EmpSysId.HasValue || !lvRec.LvTypId.HasValue)
                    return false;

                int empSysId = lvRec.EmpSysId.Value;
                int lvTypId = lvRec.LvTypId.Value;
                int year = lvRec.LvYear > 0 ? lvRec.LvYear : (lvRec.StDate.Year > 0 ? lvRec.StDate.Year : DateTime.Now.Year);
                decimal days = lvRec.Days;

                var runBal = await _hrmContext.HrLvRunningBalance
                    .FirstOrDefaultAsync(r => r.EmpSysId == empSysId && r.LvTypId == lvTypId && r.LvYear == year);

                if (runBal == null) return false;

                decimal balBefore = runBal.BalanceDays;
                decimal balAfter = balBefore + days;

                runBal.UsedDays = Math.Max(0, runBal.UsedDays - days);
                runBal.BalanceDays = balAfter;
                runBal.LastUpdated = DateTime.Now;

                // Log audit transaction
                var trn = new HrLvBalanceTransaction
                {
                    EmpSysId = empSysId,
                    LvTypId = lvTypId,
                    LvYear = year,
                    TrnType = reason,
                    LvId = lvId,
                    Days = days,
                    BalanceBefore = balBefore,
                    BalanceAfter = balAfter,
                    Remarks = $"Leave balance restored ({reason}, Ref #{lvId}, +{days} days)",
                    CrtId = userEmpSysId > 0 ? userEmpSysId : 1,
                    CrtDateTime = DateTime.Now
                };
                _hrmContext.HrLvBalanceTransaction.Add(trn);

                await _hrmContext.SaveChangesAsync();
                _logger?.LogInformation($"[LeaveBalanceService] Restored {days} days for Leave #{lvId}. Balance: {balBefore} -> {balAfter}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[LeaveBalanceService] Failed to restore balance for Leave #{lvId}");
                return false;
            }
        }

        // =====================================================================
        // 5. GET EMPLOYEE BALANCE SUMMARY
        // =====================================================================
        public async Task<EmpLeaveBalanceSummaryViewModel> GetEmployeeBalanceSummaryAsync(int empSysId, int year = 0, DateTime? asOfDate = null)
        {
            if (empSysId <= 0)
            {
                var defaultEmp = await _hrmContext.HrEmpGenInfo.OrderBy(e => e.EmpSysId).FirstOrDefaultAsync();
                if (defaultEmp != null)
                    empSysId = defaultEmp.EmpSysId;
            }
            DateTime evalDate = asOfDate ?? DateTime.Today;
            if (year <= 0) year = evalDate.Year;

            var balances = await GetEmployeeLeaveBalancesAsync(empSysId, year, evalDate);
            var emp = await _hrmContext.HrEmpGenInfo
                .Include(e => e.Rel)
                .Include(e => e.HrEmpJobAssign)
                    .ThenInclude(j => j.Dept)
                .FirstOrDefaultAsync(e => e.EmpSysId == empSysId);

            string empFullName = "";
            string empCode = "";
            string deptName = "";
            string gender = "";
            string religion = "";
            bool isFemale = false;
            bool isMuslim = false;

            if (emp != null)
            {
                empFullName = !string.IsNullOrWhiteSpace(emp.FullName) 
                    ? emp.FullName 
                    : $"{emp.FirstName} {emp.LastName}".Trim();
                empCode = emp.EmpId ?? "";
                deptName = emp.HrEmpJobAssign?.FirstOrDefault(j => j.WkStat == 1)?.Dept?.NameEng 
                    ?? emp.HrEmpJobAssign?.FirstOrDefault()?.Dept?.NameEng 
                    ?? "";
                gender = emp.Gender ?? "";
                religion = emp.Rel?.RelNameEng ?? "";
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
                        religion = rel.RelNameEng ?? "";
                        isMuslim = rel.RelNameEng.Equals("Islam", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return new EmpLeaveBalanceSummaryViewModel
            {
                EmpSysId = empSysId,
                EmpId = empCode,
                FullName = empFullName,
                EmployeeName = empFullName,
                Department = deptName,
                Gender = gender,
                Religion = religion,
                IsFemale = isFemale,
                IsMuslim = isMuslim,
                Year = year,
                LeaveYear = year,
                TotalEntitlement = balances.Sum(x => x.Entitlement),
                TotalInProgress = balances.Sum(x => x.InProgress),
                TotalUsed = balances.Sum(x => x.Used),
                TotalRemaining = balances.Sum(x => x.Remaining),
                Balances = balances,
                LeaveBalances = balances
            };
        }

        // =====================================================================
        // GET AUDIT HISTORY ALIAS
        // =====================================================================
        public async Task<List<LeaveBalanceTransactionViewModel>> GetAuditHistoryAsync(int empSysId, int? lvTypId, int year)
        {
            return await GetBalanceTransactionsAsync(empSysId, lvTypId, year);
        }

        // =====================================================================
        // 6. SET / INITIALIZE OPENING LEAVE BALANCE
        // =====================================================================
        public async Task<bool> SetOpeningBalanceAsync(int empSysId, int lvTypId, int year, decimal openingDays, string remarks, int userEmpSysId)
        {
            try
            {
                var opnBal = await _hrmContext.HrLvOpeningBalance
                    .FirstOrDefaultAsync(o => o.EmpSysId == empSysId && o.LvTypId == lvTypId && o.LvYear == year);

                if (opnBal == null)
                {
                    opnBal = new HrLvOpeningBalance
                    {
                        EmpSysId = empSysId,
                        LvTypId = lvTypId,
                        LvYear = year,
                        OpeningDays = openingDays,
                        Remarks = remarks,
                        CrtId = userEmpSysId,
                        CrtDateTime = DateTime.Now
                    };
                    _hrmContext.HrLvOpeningBalance.Add(opnBal);
                }
                else
                {
                    opnBal.OpeningDays = openingDays;
                    opnBal.Remarks = remarks;
                    opnBal.MdfId = userEmpSysId;
                    opnBal.MdfDateTime = DateTime.Now;
                }

                // Update Running Balance
                var runBal = await _hrmContext.HrLvRunningBalance
                    .FirstOrDefaultAsync(r => r.EmpSysId == empSysId && r.LvTypId == lvTypId && r.LvYear == year);

                decimal balBefore = runBal?.BalanceDays ?? 0;
                decimal usedDays = runBal?.UsedDays ?? 0;
                decimal balAfter = openingDays - usedDays;

                if (runBal == null)
                {
                    runBal = new HrLvRunningBalance
                    {
                        EmpSysId = empSysId,
                        LvTypId = lvTypId,
                        LvYear = year,
                        EntitledDays = openingDays,
                        UsedDays = 0,
                        BalanceDays = openingDays,
                        LastUpdated = DateTime.Now
                    };
                    _hrmContext.HrLvRunningBalance.Add(runBal);
                }
                else
                {
                    runBal.EntitledDays = openingDays;
                    runBal.BalanceDays = balAfter;
                    runBal.LastUpdated = DateTime.Now;
                }

                // Log audit transaction
                var trn = new HrLvBalanceTransaction
                {
                    EmpSysId = empSysId,
                    LvTypId = lvTypId,
                    LvYear = year,
                    TrnType = "Opening Balance",
                    Days = openingDays,
                    BalanceBefore = balBefore,
                    BalanceAfter = balAfter,
                    Remarks = string.IsNullOrEmpty(remarks) ? "Opening leave balance initialized" : remarks,
                    CrtId = userEmpSysId,
                    CrtDateTime = DateTime.Now
                };
                _hrmContext.HrLvBalanceTransaction.Add(trn);

                await _hrmContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[LeaveBalanceService] Error setting opening balance for EmpSysId {empSysId}");
                return false;
            }
        }

        // =====================================================================
        // 7. GET AUDIT TRANSACTIONS
        // =====================================================================
        public async Task<List<LeaveBalanceTransactionViewModel>> GetBalanceTransactionsAsync(int empSysId, int? lvTypId, int year)
        {
            if (empSysId <= 0)
            {
                var defaultEmp = await _hrmContext.HrEmpGenInfo.OrderBy(e => e.EmpSysId).FirstOrDefaultAsync();
                if (defaultEmp != null)
                    empSysId = defaultEmp.EmpSysId;
            }
            if (year <= 0) year = DateTime.Now.Year;

            var query = _hrmContext.HrLvBalanceTransaction
                .Where(t => t.EmpSysId == empSysId && t.LvYear == year);

            if (lvTypId.HasValue && lvTypId.Value > 0)
            {
                query = query.Where(t => t.LvTypId == lvTypId.Value);
            }

            var list = await (from t in query
                              join lt in _hrmContext.HrLvType on t.LvTypId equals lt.LvTypId into ltJoin
                              from lvType in ltJoin.DefaultIfEmpty()
                              orderby t.CrtDateTime descending
                              select new LeaveBalanceTransactionViewModel
                              {
                                  TrnId = t.TrnId,
                                  LeaveTypeName = lvType != null ? lvType.NameEng : "Leave",
                                  TrnType = t.TrnType,
                                  LvId = t.LvId,
                                  Days = t.Days,
                                  BalanceBefore = t.BalanceBefore,
                                  BalanceAfter = t.BalanceAfter,
                                  Remarks = t.Remarks,
                                  DateFormatted = t.CrtDateTime.ToString("yyyy-MM-dd hh:mm tt")
                              }).ToListAsync();

            return list;
        }

        // =====================================================================
        // HELPER: DEFAULT ENTITLEMENT BY LEAVE TYPE
        // =====================================================================
        private static decimal GetDefaultEntitlement(HrLvType lt)
        {
            string code = lt.LvTypCode ?? "";
            string name = lt.NameEng ?? "";

            if (code == "LV_ANL" || name.Contains("Annual", StringComparison.OrdinalIgnoreCase))
                return 30.00m;
            if (code == "LV_CSL" || name.Contains("Casual", StringComparison.OrdinalIgnoreCase))
                return 3.00m;
            if (code == "LV_SICK" || name.Contains("Sick", StringComparison.OrdinalIgnoreCase))
                return 42.00m;
            if (code == "LV_HAJ" || code == "LV_HAJJ" || name.Contains("Hajj", StringComparison.OrdinalIgnoreCase) || name.Contains("HAJJI", StringComparison.OrdinalIgnoreCase))
                return 14.00m;
            if (code == "LV_MTN" || code.Contains("MAT", StringComparison.OrdinalIgnoreCase) || name.Contains("Maternity", StringComparison.OrdinalIgnoreCase))
                return 90.00m;

            return lt.ElgDays > 0 ? lt.ElgDays : 0;
        }
    }
}
