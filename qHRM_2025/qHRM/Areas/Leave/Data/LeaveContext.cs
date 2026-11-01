using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.WorkShift.Models;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Leave.Data
{
    public class LeaveContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;
        #endregion

        #region ==============Constructors==============
        public LeaveContext(qHRMasterContext hrmContext,
                            EmployeeContext empContext)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
        }
        #endregion

        #region ==============Leave Types==============
        public partial class HrLvTypeView
        {
            public int LvTypId { get; set; }
            public string LvTypCode { get; set; }
            public string NameEng { get; set; }
            public string NameArb { get; set; }
            public string Desc { get; set; }
            public int ElgDays { get; set; }
            public int MaxCntDays { get; set; }
            public string AprlRqdTxt { get; set; }
            public string PreShldTxt { get; set; }
            public string RjnFmRqdTxt { get; set; }
            public decimal SlryDctFct { get; set; }
            public string EncshTxt { get; set; }
        }

        public IEnumerable<HrLvTypeView> GetAllLeaveTypeViewsByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrLvType.Where(x => x.RecStat == 1 && x.EntId == entId)
                                           .Select(x => new HrLvTypeView
                                           {
                                               LvTypId = x.LvTypId,
                                               LvTypCode = x.LvTypCode,
                                               NameEng = x.NameEng,
                                               NameArb = x.NameArb,
                                               Desc = x.Desc,
                                               ElgDays = x.ElgDays,
                                               MaxCntDays = x.MaxCntDays,
                                               AprlRqdTxt = (x.AprlRqd == true) ? "Required" : "Not Required",
                                               PreShldTxt = (x.PreShld == true) ? "Required" : "Not Required",
                                               RjnFmRqdTxt = (x.RjnFmRqd == true) ? "Required" : "Not Required",
                                               EncshTxt = (x.Encsh == true) ? "Applicable" : "Not",
                                               SlryDctFct = x.SlryDctFct
                                           }).OrderBy(o => o.LvTypCode).ToList();
            }
            catch { throw; }
        }

        public IEnumerable<HrLvType> GetAllLeaveTypesByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrLvType.Where(x => x.EntId == entId && x.RecStat == 1).OrderBy(x => x.LvTypCode).ToList();
            }
            catch { throw; }
        }

        public HrLvType GetLeaveTypesById(int lvTypId)
        {
            try
            {
                return _hrmContext.HrLvType.Where(x => x.LvTypId == lvTypId).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrLvType GetLeaveTypeByCode(string lvTypCode)
        {
            try
            {
                return _hrmContext.HrLvType.Where(x => x.LvTypCode.ToLower() == lvTypCode.ToLower()).FirstOrDefault();
            }
            catch { throw; }
        }
        #endregion

        #region ==============Leave Policy==============
        public IEnumerable<HrLvPolicy> GetLeavePolicies()
        {
            try
            {
                return _hrmContext.HrLvPolicy.Include(p => p.LvTyp).Where(p => p.RecStat == 1).ToList();
            }
            catch { throw; }
        }

        public HrLvPolicy GetLeavePolicyByLeaveTypeId(int lvTypId)
        {
            try
            {
                return _hrmContext.HrLvPolicy.Include(p => p.LvTyp).FirstOrDefault(p => p.LvTypId == lvTypId && p.RecStat == 1);
            }
            catch { throw; }
        }

        public HrLvPolicy GetLeavePolicyByLeaveTypeCode(string lvTypCode)
        {
            try
            {
                return _hrmContext.HrLvPolicy
                    .Include(p => p.LvTyp)
                    .FirstOrDefault(p => p.LvTyp.LvTypCode.ToLower() == lvTypCode.ToLower() && p.RecStat == 1);
            }
            catch { throw; }
        }
        #endregion

        #region ==============Leave Register==============
        public partial class HrLvRegisterView
        {
            public int LvId { get; set; }
            public int exLvId { get; set; }
            public int EmpSysId { get; set; }
            public string Emp_Id { get; set; }
            public string Full_Name { get; set; }
            public int LvTypId { get; set; }
            public string LvTyp { get; set; }
            public int LvYear { get; set; }
            public int Incident { get; set; }
            public DateTime StDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Days { get; set; }
            public int CurrAvblDays { get; set; }
            public DateTime? RjnDate { get; set; }
            public short RjnStat { get; set; }
            public int? LvSdlId { get; set; }
            public int? DocId { get; set; }
            public string Stat { get; set; }
            public string Note { get; set; }
            public string WfStat { get; set; }
            public int? WfInstId { get; set; }
            public Guid? WfInstanceId { get; set; }
            public DateTime? CrtDateTime { get; set; }
        }

        public IEnumerable<HrLvRegisterView> GetLeaveRegister(bool isSnglEmp, int empSysId, int? grpId = null)
        {
            try
            {
                EmployeeContext empContext = new EmployeeContext(_hrmContext);
                HashSet<int> allowedEmpSysIds;
                if (empContext.IsRestrictedSelfOnlyUser(empSysId, grpId))
                {
                    allowedEmpSysIds = new HashSet<int> { empSysId };
                }
                else if (empContext.IsOutsourceAdmin(empSysId, grpId))
                {
                    allowedEmpSysIds = empContext.GetOutsourceEmployeeSysIds(empSysId);
                    if (empSysId > 0) allowedEmpSysIds.Add(empSysId);
                }
                else if (empContext.GetUserShowAllEmployees(empSysId, grpId))
                {
                    allowedEmpSysIds = null;
                }
                else
                {
                    var subordinates = empContext.GetEmpSbrdnts(empSysId).ToHashSet();
                    if (empSysId > 0) subordinates.Add(empSysId);

                    if (isSnglEmp && subordinates.Count <= 1)
                    {
                        allowedEmpSysIds = new HashSet<int> { empSysId };
                    }
                    else
                    {
                        allowedEmpSysIds = subordinates;
                    }
                }

                var lvStsElements = (from el in _hrmContext.SysSelectListElement
                                     join tp in _hrmContext.SysSelectListType on el.LstTypId equals tp.LstTypId
                                     where tp.LstTypKey == "LV_STS"
                                     select el).ToList();

                var rawLeavesQuery = from lv in _hrmContext.HrLvRegister
                                     join lt in _hrmContext.HrLvType on lv.LvTypId equals lt.LvTypId into ltJoin
                                     from lvtyp in ltJoin.DefaultIfEmpty()
                                     join eg in _hrmContext.HrEmpGenInfo on lv.EmpSysId equals eg.EmpSysId into egJoin
                                     from emp in egJoin.DefaultIfEmpty()
                                     select new { lv, emp, lvtyp };

                if (allowedEmpSysIds != null)
                {
                    rawLeavesQuery = rawLeavesQuery.Where(x => x.lv.EmpSysId.HasValue && allowedEmpSysIds.Contains(x.lv.EmpSysId.Value));
                }

                var rawLeaves = rawLeavesQuery.Select(x => new
                {
                    x.lv,
                    EmpId = x.emp != null ? x.emp.EmpId : "",
                    FirstName = x.emp != null ? x.emp.FirstName : "",
                    MdlName = x.emp != null ? x.emp.MdlName : "",
                    LastName = x.emp != null ? x.emp.LastName : "",
                    LvTypName = x.lvtyp != null ? x.lvtyp.NameEng : ""
                }).ToList();

                return rawLeaves.Select(x =>
                {
                    string statText = lvStsElements.FirstOrDefault(e => e.Indx == x.lv.RecStat)?.ElmntTxt;
                    if (string.IsNullOrEmpty(statText))
                    {
                        statText = x.lv.RecStat switch
                        {
                            2 => "Approved",
                            3 => "Rejected",
                            0 => "Cancelled",
                            _ => "Active"
                        };
                    }

                    string effectiveWfStat = x.lv.WfStat;
                    if (string.IsNullOrEmpty(effectiveWfStat))
                    {
                        effectiveWfStat = x.lv.RecStat switch
                        {
                            2 => "Approved",
                            3 => "Rejected",
                            _ => "Draft"
                        };
                    }

                    string fullName = ((x.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(x.MdlName) ? "" : " " + x.MdlName.Trim()) + (string.IsNullOrWhiteSpace(x.LastName) ? "" : " " + x.LastName.Trim())).Trim();

                    return new HrLvRegisterView
                    {
                        LvId = x.lv.LvId,
                        Emp_Id = x.EmpId,
                        EmpSysId = x.lv.EmpSysId.GetValueOrDefault(),
                        Full_Name = fullName,
                        LvTypId = x.lv.LvTypId.GetValueOrDefault(),
                        LvTyp = x.LvTypName,
                        LvYear = x.lv.LvYear,
                        Incident = x.lv.Incident,
                        StDate = x.lv.StDate,
                        EndDate = x.lv.EndDate,
                        Days = x.lv.Days,
                        RjnDate = x.lv.RjnDate,
                        RjnStat = x.lv.RjnStat,
                        Note = x.lv.Note,
                        CurrAvblDays = x.lv.CurrAvblDays,
                        Stat = statText,
                        LvSdlId = x.lv.LvSdlId,
                        WfStat = effectiveWfStat,
                        WfInstId = x.lv.WfInstId,
                        CrtDateTime = x.lv.CrtDateTime
                    };
                }).OrderByDescending(o => o.CrtDateTime).ToList();
            }
            catch { throw; }
        }

        public HrLvRegister GetHrLvRegisterByLvId(int lvId)
        {
            try
            {
                return _hrmContext.HrLvRegister.Where(x => x.LvId == lvId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int GetMaxIncndtNo()
        {
            try
            {
                return _hrmContext.HrLvRegister.Max(x => x.Incident);
            }
            catch { throw; }
        }
        #endregion ==============Leave Register==============

        #region ==============Sandwich Leave Policy==============
        /// <summary>
        /// Validates the Sandwich Leave Policy:
        /// 1. When an employee takes Annual Leave, the system must prevent them from applying for Casual Leave or Sick Leave
        ///    immediately after the Annual Leave period, including any intervening weekend or regional non-working days.
        ///    Example: If an employee is on Annual Leave from 11 to 15 October 2026, and 16 and 17 October are weekend days,
        ///    the employee must not be permitted to apply for Casual Leave or Sick Leave on 18 October 2026.
        /// 2. The restriction must not apply to Maternity Leave, Hajj Leave, or Emergency Leave.
        /// </summary>
        public async Task<(bool isRestricted, string message)> CheckSandwichLeavePolicyAsync(
            int empSysId,
            int lvTypId,
            DateTime stDate,
            DateTime endDate,
            int? lvId = null)
        {
            try
            {
                if (empSysId <= 0 || lvTypId <= 0 || stDate == default || endDate == default)
                {
                    return (false, null);
                }

                // Retrieve the leave type being applied for
                var targetType = await _hrmContext.HrLvType.FirstOrDefaultAsync(t => t.LvTypId == lvTypId);
                if (targetType == null)
                {
                    return (false, null);
                }

                string targetCode = (targetType.LvTypCode ?? "").Trim();
                string targetName = (targetType.NameEng ?? "").Trim();

                bool isCasual = targetCode.Equals("LV_CSL", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("Casual", StringComparison.OrdinalIgnoreCase);

                bool isSick = targetCode.Equals("LV_SICK", StringComparison.OrdinalIgnoreCase) ||
                              targetName.Contains("Sick", StringComparison.OrdinalIgnoreCase);

                bool isAnnual = targetCode.Equals("LV_ANL", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("Annual", StringComparison.OrdinalIgnoreCase);

                // Exempt types explicitly allowed: Maternity, Hajj, Emergency, or any non-Casual/Sick types
                bool isExempt = targetCode.StartsWith("LV_MTN", StringComparison.OrdinalIgnoreCase) ||
                                targetCode.StartsWith("LV_HAJ", StringComparison.OrdinalIgnoreCase) ||
                                targetCode.StartsWith("LV_EMG", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("Maternity", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("Hajj", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("HAJJI", StringComparison.OrdinalIgnoreCase) ||
                                targetName.Contains("Emergency", StringComparison.OrdinalIgnoreCase);

                if (isExempt)
                {
                    return (false, null);
                }

                int currentLvId = lvId.GetValueOrDefault();

                // Fetch active holidays
                var holidays = await _hrmContext.OrgHoliday.Where(h => h.RecStat == 1).ToListAsync();

                // Fetch employee roster off-days / weekends if configured
                var rosterOffDays = await (from er in _hrmContext.HrEmpWkRoster
                                           join rs in _hrmContext.HrWkRosterShift on er.RstId equals rs.RstId
                                           join sft in _hrmContext.HrWkShift on rs.SftId equals sft.SftId
                                           where er.EmpSysId == empSysId
                                                 && er.RecStat == 1
                                                 && (sft.OffDay || sft.WkEnd)
                                           select new { rs.Day, er.StDate, er.EndDate })
                                           .ToListAsync();

                // Helper local function to determine if a specific calendar date is a non-working day
                bool IsNonWorkingDay(DateTime date)
                {
                    // Regional weekend: Friday and Saturday (standard weekend in Qatar / region)
                    if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                    {
                        return true;
                    }

                    // Organization Public Holidays
                    if (holidays.Any(h => (date.Date >= h.StDate.Date && date.Date <= h.EndDate.Date) ||
                                          (h.Recurring && date.Month == h.StDate.Month && date.Day >= h.StDate.Day && date.Day <= h.EndDate.Day)))
                    {
                        return true;
                    }

                    // Employee-specific roster off-day / weekend shift
                    string dayName = date.ToString("dddd");
                    if (rosterOffDays.Any(r => r.Day.Equals(dayName, StringComparison.OrdinalIgnoreCase)
                                            && date.Date >= r.StDate.Date
                                            && (!r.EndDate.HasValue || date.Date <= r.EndDate.Value.Date)))
                    {
                        return true;
                    }

                    return false;
                }

                // Scenario A: Applying for Casual Leave or Sick Leave immediately after an Annual Leave period
                if (isCasual || isSick)
                {
                    // Query active / approved / submitted Annual Leave records for this employee ending before stDate
                    var priorLeaves = await _hrmContext.HrLvRegister
                        .Include(l => l.LvTyp)
                        .Where(l => l.EmpSysId == empSysId
                                 && l.LvId != currentLvId
                                 && l.RecStat != 0 && l.RecStat != 3
                                 && l.WfStat != "Rejected" && l.WfStat != "Cancelled"
                                 && l.EndDate.Date < stDate.Date)
                        .ToListAsync();

                    var annualLeaves = priorLeaves.Where(l =>
                        l.LvTyp != null && (l.LvTyp.LvTypCode.Equals("LV_ANL", StringComparison.OrdinalIgnoreCase) ||
                                            l.LvTyp.NameEng.Contains("Annual", StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    foreach (var al in annualLeaves)
                    {
                        DateTime checkStart = al.EndDate.Date.AddDays(1);
                        DateTime checkEnd = stDate.Date.AddDays(-1);

                        bool allInterveningAreNonWorking = true;
                        if (checkStart <= checkEnd)
                        {
                            for (DateTime cur = checkStart; cur <= checkEnd; cur = cur.AddDays(1))
                            {
                                if (!IsNonWorkingDay(cur))
                                {
                                    allInterveningAreNonWorking = false;
                                    break;
                                }
                            }
                        }

                        if (allInterveningAreNonWorking)
                        {
                            string restrictionMsg = $"Sandwich Leave Policy Restriction: Employees cannot apply for {targetName} immediately following an Annual Leave period ({al.StDate:dd/MM/yyyy} to {al.EndDate:dd/MM/yyyy}), including intervening weekends or regional non-working days.";
                            return (true, restrictionMsg);
                        }
                    }
                }

                // Scenario B: Applying for Annual Leave immediately preceding an existing Casual Leave or Sick Leave
                if (isAnnual)
                {
                    var subsequentLeaves = await _hrmContext.HrLvRegister
                        .Include(l => l.LvTyp)
                        .Where(l => l.EmpSysId == empSysId
                                 && l.LvId != currentLvId
                                 && l.RecStat != 0 && l.RecStat != 3
                                 && l.WfStat != "Rejected" && l.WfStat != "Cancelled"
                                 && l.StDate.Date > endDate.Date)
                        .ToListAsync();

                    var casualSickLeaves = subsequentLeaves.Where(l =>
                        l.LvTyp != null && (
                            l.LvTyp.LvTypCode.Equals("LV_CSL", StringComparison.OrdinalIgnoreCase) ||
                            l.LvTyp.LvTypCode.Equals("LV_SICK", StringComparison.OrdinalIgnoreCase) ||
                            l.LvTyp.NameEng.Contains("Casual", StringComparison.OrdinalIgnoreCase) ||
                            l.LvTyp.NameEng.Contains("Sick", StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToList();

                    foreach (var cs in casualSickLeaves)
                    {
                        DateTime checkStart = endDate.Date.AddDays(1);
                        DateTime checkEnd = cs.StDate.Date.AddDays(-1);

                        bool allInterveningAreNonWorking = true;
                        if (checkStart <= checkEnd)
                        {
                            for (DateTime cur = checkStart; cur <= checkEnd; cur = cur.AddDays(1))
                            {
                                if (!IsNonWorkingDay(cur))
                                {
                                    allInterveningAreNonWorking = false;
                                    break;
                                }
                            }
                        }

                        if (allInterveningAreNonWorking)
                        {
                            string csName = cs.LvTyp?.NameEng ?? "Casual/Sick Leave";
                            string restrictionMsg = $"Sandwich Leave Policy Restriction: Cannot apply for Annual Leave ({stDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}) immediately followed by {csName} ({cs.StDate:dd/MM/yyyy} to {cs.EndDate:dd/MM/yyyy}), including intervening weekends or regional non-working days.";
                            return (true, restrictionMsg);
                        }
                    }
                }

                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }
        #endregion ==============Sandwich Leave Policy==============
    }
}
