using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace qHRM.Areas.Attendance.Data
{
    public class AttnContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;

        #endregion

        #region ==============Constructors==============
        public AttnContext(qHRMasterContext hrmContext, 
                           EmployeeContext empContext)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
        }
        #endregion

        #region ==============Employee Attendance ==============     
        public partial class AtndcPeriod
        {
            public DateTime frmDate;
            public DateTime toDate;
        }
        public partial class HrEmpAttnCompackedVw
        {
            public int RecId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
            public int DeptId { get; set; }
            public string DeptName { get; set; }
            public int LocId { get; set; }
            public string LocName { get; set; }
            public string Status { get; set; }
            public string InTime { get; set; }
            public string OutTime { get; set; }            
            public string EftvDate { get; set; }
        }

        public partial class HrEmpAtndcSftFullView
        {
            public int AtndcSftId { get; set; }
            public int SftId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
            public int DeptId { get; set; }
            public string DeptName { get; set; }
            public int LocId { get; set; }
            public string LocName { get; set; }
            public string AtndcDate { get; set; }
            public TimeSpan? InTime { get; set; }
            public TimeSpan? OutTime { get; set; }
            public string WrkMnt { get; set; }
            public int OtMnt { get; set; }
            public int ExtOtMnt { get; set; }
            public int LateInMnt { get; set; }
            public int ErlyLvMnt { get; set; }
            public int AtndcStatId { get; set; }
            public string Status { get; set; }
            public decimal SlryDctFct { get; set; }
            public string Note { get; set; }
        }

        public partial class DailyAttendance
        {
            public string EftDate { get; set; }
            public int EmpCnt { get; set; }
        }

        public partial class HrEmpAtndcStngsSts
        {
            [Key]
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpNm { get; set; }
            public string JbAsgnSts { get; set; }
            public string LocSts { get; set; }
            public string SlrySts { get; set; }
            public string EmpWkRstSts { get; set; }
            public int RstSftCnt { get; set; }
            public int SftCnt { get; set; }
        }

        public IEnumerable<HrEmpAtndcStngsSts> GetPndngEmpAttnStngs(DateTime dt)
        {
            try
            {
                object[] param = { dt };
                var x = _hrmContext.HrEmpAttnStngsStsVw.FromSqlRaw("[HR_EmpPndngAtndcSts] {0}", param).ToList();
                return x;
            }
            catch { throw; }
        }

        public AtndcPeriod GetAtndcPeriod()
        {
            try
            {
                int yr, mnth;
                DateTime frmDate, toDate;

                var payContext = new PayrollContext(_hrmContext);
                var curEftYM = payContext.GetCurEftYM();

                yr = curEftYM.EftYear;
                mnth = curEftYM.EftMonth;

                var hrPrConfig = _hrmContext.HrPrConfig.Where(x => x.RecStat == 1).FirstOrDefault();
                if (hrPrConfig != null)
                {
                    frmDate = new DateTime(yr, mnth, hrPrConfig.AttnCtDay + 1).AddMonths(-1);
                    toDate = new DateTime(yr, mnth, hrPrConfig.AttnCtDay);
                }
                else
                {
                    frmDate = new DateTime(yr, mnth, 1);
                    toDate = new DateTime(yr, mnth, 1).AddMonths(1).AddDays(-1);
                }
                return new AtndcPeriod { frmDate = frmDate, toDate = toDate};
            }
            catch (Exception ex) { throw ex; } 
        }

        public IEnumerable<DailyAttendance> GetDailyAttendanceSmry(DateTime curDate, int days)
        {
            try
            {
                DateTime stDate = curDate.AddDays(-days);
                var antStatIds = new List<int>() { 1, 11, 12, 112, 13, 14, 21, 31, 41, 5, 51, 61 };

                var x = from atn in _hrmContext.HrEmpAtndcSft
                        where (antStatIds.Contains(atn.AtndcStatId)
                                && (atn.AtndcDate >= stDate && atn.AtndcDate <= curDate))
                        group atn by atn.AtndcDate into g
                        orderby g.Key
                        select new DailyAttendance
                        {
                            EftDate = g.Key.ToString("yyyy/MM/dd"),
                            EmpCnt = g.Count()
                        };
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAttnCompackedVw> GetHrEmpAttnCompackedVwByDate(string field, DateTime date, string status)
        {
            try
            {
                string sp = "[HR_EmpAtndc_Day] {0}";
                object[] param = { date };

                var x = (from atn in _hrmContext.HrEmpAtndcSftVw.FromSqlRaw(sp, param).ToList()
                     join emp in _hrmContext.HrEmpGenInfo
                         on atn.EmpSysId equals emp.EmpSysId
                     join jb in _hrmContext.HrEmpJobAssign
                         on emp.EmpSysId equals jb.EmpSysId
                     join dpt in _hrmContext.OrgDepartment
                         on jb.DeptId equals dpt.DeptId
                     join loc in _hrmContext.OrgLocation
                         on emp.LocId equals loc.LocId
                     join sts in _hrmContext.SysAtndcStat
                         on atn.AtndcStatId equals sts.AtndcStatId
                     where emp.CurrStat == 0 && jb.WkStat == 1 && atn.AtndcDate == date && dpt.DeptCode != "Mgmt"
                     select new HrEmpAttnCompackedVw
                     {
                         RecId = atn.AtndcSftId,
                         EmpSysId = emp.EmpSysId,
                         EmpId = emp.EmpId,
                         FullName = emp.FullName,
                         DeptId = jb.DeptId,
                         DeptName = dpt.NameEng,
                         LocId = emp.LocId,
                         LocName = loc.NameEng,
                         Status = sts.AtndcStatTxt,
                         InTime = atn.InTime.ToString(),
                         OutTime = atn.OutTime.ToString(),
                         EftvDate = atn.AtndcDate.ToString("yyyy-MM-dd")
                     }).ToList();
                return x;
            }
           catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAttnCompackedVw> GetHrEmpAttnCompackedVwOutSrc(DateTime date)
        {
            try
            {
                var x = from emp in _hrmContext.HrEmpGenInfo
                        join jb in _hrmContext.HrEmpJobAssign
                            on emp.EmpSysId equals jb.EmpSysId
                        join dpt in _hrmContext.OrgDepartment
                            on jb.DeptId equals dpt.DeptId
                        join loc in _hrmContext.OrgLocation
                            on emp.LocId equals loc.LocId
                        where emp.CurrStat == 0 && jb.WkStat == 1 && loc.ParentId == 3
                        select new HrEmpAttnCompackedVw
                        {
                            RecId = emp.EmpSysId,
                            EmpSysId = emp.EmpSysId,
                            EmpId = emp.EmpId,
                            FullName = emp.FullName,
                            DeptId = jb.DeptId,
                            DeptName = dpt.NameEng,
                            LocId = emp.LocId,
                            LocName = loc.NameEng,
                            Status = "Out Sourced",
                            InTime = "",
                            OutTime = "",
                            EftvDate = date.ToString("yyyy-MM-dd")
                        };

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAttendToday> GetEmpDlyAttnSmry(string field, bool isSnglEmp, int empSysId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var x = new List<HrEmpAttendToday>();
                if (field == "TODAY_SMRY")
                {
                    object[] param = { DateTime.Today };
                    x = (from atn in _hrmContext.HrEmpAtndcSftVw.FromSqlRaw("[HR_EmpAtndc_Day] {0}", param).ToList()
                         join sts in _hrmContext.SysAtndcStat
                            on atn.AtndcStatId equals sts.AtndcStatId
                         join jb in _hrmContext.HrEmpJobAssign
                            on atn.EmpSysId equals jb.EmpSysId
                         join dpt in _hrmContext.OrgDepartment
                            on jb.DeptId equals dpt.DeptId
                         where jb.WkStat == 1 && dpt.DeptCode != "Mgmt" && ((isSnglEmp ==true && empSysId == atn.EmpSysId) || (isSnglEmp ==false && _empContext.GetEmpSbrdnts(empSysId).Contains(atn.EmpSysId))) 
                         group atn by new { atn.AtndcStatId, sts.AtndcStatTxt } into g
                         select new HrEmpAttendToday { RecId = g.Key.AtndcStatId, Status = g.Key.AtndcStatTxt, Count = g.Count() }).ToList();
                }
                else
                {
                    x = (from sts in _hrmContext.SysAtndcStat
                         join atn in _hrmContext.HrEmpAtndcSft
                         on sts.AtndcStatId equals atn.AtndcStatId
                         join emp in _hrmContext.HrEmpGenInfo
                         on atn.EmpSysId equals emp.EmpSysId
                         join jb in _hrmContext.HrEmpJobAssign
                         on emp.EmpSysId equals jb.EmpSysId
                         join dpt in _hrmContext.OrgDepartment
                         on jb.DeptId equals dpt.DeptId
                         where atn.RecStat == 1 && atn.AtndcDate >= fromDate && atn.AtndcDate <= toDate && emp.CurrStat == 0 && jb.WkStat == 1 && dpt.DeptCode != "Mgmt" && ((isSnglEmp == true && empSysId == atn.EmpSysId) || (isSnglEmp == false && (_empContext.GetEmpSbrdnts(empSysId).Contains(atn.EmpSysId))))
                         group atn by new { atn.AtndcStatId, sts.AtndcStatTxt } into g
                         select new HrEmpAttendToday { RecId = g.Key.AtndcStatId, Status = g.Key.AtndcStatTxt, Count = g.Count() }).ToList();
                }

                x.AddRange((from loc in _hrmContext.OrgLocation
                         join emp in _hrmContext.HrEmpGenInfo
                         on loc.LocId equals emp.LocId
                         where loc.ParentId == 3 && emp.CurrStat == 0 && ((isSnglEmp == true && empSysId == emp.EmpSysId) || (isSnglEmp == false && (_empContext.GetEmpSbrdnts(empSysId).Contains(emp.EmpSysId))))
                            group loc by loc.ParentId into g
                         select new HrEmpAttendToday { RecId = 6, Status = "Out Sourced", Count = g.Count() }).ToList());

                return x;

            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAtndcVw> GetEmpAtndcforRpt(string prcTyp, int empSysId, DateTime frmDate, DateTime toDate) 
        {
            try
            {
                string sp = "[HR_EmpAtndcSmry_Get] {0},{1},{2},{3}";
                object[] param = { prcTyp, empSysId, frmDate, toDate };

                var x = _hrmContext.HrEmpAtndcVws.FromSqlRaw(sp, param).ToList()
                        .Select(x => new HrEmpAtndcVw
                        {
                            AtndcDate = x.AtndcDate
                            ,
                            AtndcStat = x.AtndcStat
                            ,
                            EmpName = x.EmpName
                            ,
                            EmpSysId = x.EmpSysId
                        });

                return x;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region HrEmpAtndcSft
        public HrEmpAtndcSft GetHrEmpAtndcSftById(int atndcSftId)
        {
            try
            {
                return _hrmContext.HrEmpAtndcSft.Where(x => x.AtndcSftId == atndcSftId).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<HrEmpAtndcSft> GetHrEmpAtndcSftByEmpSysIdNRng(int empSysId, DateTime frmDate, DateTime toDate)
        {
            try
            {
                return _hrmContext.HrEmpAtndcSft.Where(x => x.EmpSysId == empSysId && x.AtndcDate >= frmDate && x.AtndcDate <= toDate);
            }
            catch { throw; }
        }

        public int LoadMnthHrEmpAtndc(string prcTyp, int refId, int eftYr, int eftMnth, AtndcLdPrcTyp atndcLdPrcTyp, string prcSrcDtl, bool RstAll, int PrcBy)
        {
            try
            {
                DateTime frmDate, toDate;
                var hrPrConfig = _hrmContext.HrPrConfig.Where(x => x.RecStat == 1).FirstOrDefault();
                if (hrPrConfig != null) {
                    frmDate = new DateTime(eftYr, eftMnth, hrPrConfig.AttnCtDay + 1).AddMonths(-1);
                    toDate = new DateTime(eftYr, eftMnth, hrPrConfig.AttnCtDay);
                }
                else {
                    frmDate = new DateTime(eftYr, eftMnth, 1);
                    toDate = new DateTime(eftYr, eftMnth, 1).AddMonths(1).AddDays(-1);
                }

                var frm = (DateTime.Today - frmDate).Days;
                var to = (DateTime.Today - toDate).Days;
                object[] param = { prcTyp, refId, frm, to, atndcLdPrcTyp.ToString(), prcSrcDtl, RstAll, PrcBy };
                _hrmContext.HrEmpAtndcSft.FromSqlRaw("HR_EmpAtndc_Load {0},{1},{2},{3},{4},{5},{6},{7}", param).ToList();
                return 1;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        public int LoadHrEmpAtndc(string prcTyp, int refId, DateTime frmDate, DateTime toDate, AtndcLdPrcTyp atndcLdPrcTyp, string prcSrcDtl, bool RstAll, int PrcBy)
        {
            try
            {
                var frm = (DateTime.Today - frmDate).Days;
                var to = (DateTime.Today - toDate).Days;
                object[] param = { prcTyp, refId, frm, to, atndcLdPrcTyp.ToString(), prcSrcDtl, RstAll, PrcBy };
                _hrmContext.HrEmpAtndcSft.FromSqlRaw("HR_EmpAtndc_Load {0},{1},{2},{3},{4},{5},{6},{7}", param).ToList();
                return 1;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int ReCalcEmpAtndc(int atndcSftId, int sftId, bool mkAbsnt) {
            try
            {
                object[] param = { atndcSftId, sftId, mkAbsnt };
                _hrmContext.HrEmpAtndcSft.FromSqlRaw("HR_EmpAtndc_ReCalc {0},{1},{2}", param).ToList();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAtndcSftFullView> GetFullHrEmpAtndcSmry(string schTyp, int refId, DateTime frmDate, DateTime toDate, bool isSnglEmp, int empSysId)
        {
            try
            {
                var x = from atn in _hrmContext.HrEmpAtndcSft
                        join emp in _hrmContext.HrEmpGenInfo on atn.EmpSysId equals emp.EmpSysId
                        join jb in _hrmContext.HrEmpJobAssign on emp.EmpSysId equals jb.EmpSysId
                        join dpt in _hrmContext.OrgDepartment on jb.DeptId equals dpt.DeptId
                        join loc in _hrmContext.OrgLocation on emp.LocId equals loc.LocId
                        join sts in _hrmContext.SysAtndcStat on atn.AtndcStatId equals sts.AtndcStatId                        
                        where atn.RecStat == 1 && dpt.DeptCode != "Mgmt" && (emp.CurrStat == 0 && jb.WkStat == 1 && (schTyp == "A" || (schTyp == "D" && refId == dpt.DeptId) || (schTyp == "S" && refId == atn.EmpSysId))
                                 && ((isSnglEmp == true && empSysId == atn.EmpSysId) || (isSnglEmp == false && (_empContext.GetEmpSbrdnts(empSysId).Contains(atn.EmpSysId))))
                                && (atn.AtndcDate >= frmDate && atn.AtndcDate <= toDate)) || (schTyp == "R" && atn.AtndcSftId == refId) 
                        orderby(atn.AtndcDate)
                        select new HrEmpAtndcSftFullView
                        {
                            AtndcSftId = atn.AtndcSftId,
                            SftId = atn.SftId,
                            EmpSysId = atn.EmpSysId, 
                            EmpId = emp.EmpId,
                            FullName = emp.FullName,
                            DeptId = jb.DeptId,
                            DeptName = dpt.NameEng,
                            LocId = emp.LocId,
                            LocName = loc.NameEng,
                            AtndcDate = atn.AtndcDate.ToString("yyyy-MM-dd"),
                            InTime = atn.InTime,
                            OutTime = atn.OutTime,
                            WrkMnt = (atn.WrkMnt / 60).ToString("00") + ":" + (atn.WrkMnt % 60).ToString("00"),
                            OtMnt = atn.OtMnt,
                            ExtOtMnt = atn.ExtOtMnt,
                            LateInMnt = atn.LateInMnt,
                            ErlyLvMnt = atn.ErlyLvMnt,
                            AtndcStatId = atn.AtndcStatId,
                            Status = sts.AtndcStatTxt,
                            SlryDctFct = atn.SlryDctFct,
                            Note = atn.Note
                        };

                return x;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion HrEmpAtndcSft
        #endregion

        #region ============== Attendance Exeptions ==============
        public partial class HrEmpAttendExpView
        {
            public int AtnExpId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpName { get; set; }
            public DateTime StDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Note { get; set; }
            public int? RqstId { get; set; }
            public int? AprvdId { get; set; }
            public int CrtId { get; set; }
            public string CrtName { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public IEnumerable<HrEmpAttendExpView> GetHrEmpAttendExeption(bool isSnglEmp, int empSysId)
        {
            try
            {

                var x = _hrmContext.HrEmpAttendExp
                        .Join(_hrmContext.HrEmpGenInfo, atd => atd.EmpSysId, emp => emp.EmpSysId, (atd, emp) => new { atd, emp })
                        .Join(_hrmContext.HrEmpGenInfo, b => b.atd.CrtId, crt => crt.EmpSysId, (b, crt) => new { b, crt })
                         //.Where(z => z.b.atd.RecStat == 1 )
                        .Where(z => ((isSnglEmp == true && empSysId == z.b.atd.EmpSysId) || (isSnglEmp == false && (_empContext.GetEmpSbrdnts(empSysId).Contains(z.b.atd.EmpSysId)))) && z.b.atd.RecStat == 1)
                        .Select(x => new HrEmpAttendExpView
                        {
                            AtnExpId = x.b.atd.AtnExpId,
                            EmpSysId = x.b.atd.EmpSysId,
                            EmpId = x.b.emp.EmpId,
                            EmpName = x.b.emp.FullName,
                            StDate = x.b.atd.StDate,
                            EndDate = x.b.atd.EndDate,
                            Note = x.b.atd.Note,
                            RqstId = x.b.atd.RqstId,
                            AprvdId = x.b.atd.AprvdId,
                            CrtName = x.crt.FullName,
                            CrtDateTime = x.b.atd.CrtDateTime,
                        }).OrderByDescending(o => o.StDate).ThenBy(t => t.EndDate);
               
                return x;
            }
            catch { return null; }
        }

        public HrEmpAttendExp GetHrEmpAttendExpByAtnExpId(int atnExpId)
        {
            try
            {
                var x = _hrmContext.HrEmpAttendExp.Where(x => x.AtnExpId == atnExpId).FirstOrDefault();
                _hrmContext.Entry(x).State = EntityState.Detached;
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAttendExp> GetHrEmpAttendExp(string schTyp, int refId, DateTime stDate, DateTime endDate) {
            try
            {
                var x = new List<HrEmpAttendExp>();
                if (schTyp == "A") 
                {
                    x = _hrmContext.HrEmpAttendExp.Where(x => x.StDate >= stDate && x.EndDate <= endDate && x.RecStat == 1).ToList();
                }
                else if (schTyp == "D")
                {
                    x = _hrmContext.HrEmpAttendExp.Where(x => x.EmpSysId == refId && x.StDate >= stDate && x.EndDate <= endDate && x.RecStat == 1
                                                        && (_hrmContext.HrEmpJobAssign
                                                                .Join(_hrmContext.OrgDepartment, jb =>jb.DeptId, dp => dp.DeptId, (jb, dp) => new { jb, dp })
                                                                .Where(z => z.dp.DeptId == refId && z.jb.WkStat == 1)
                                                                .Select(e => e.jb.EmpSysId).Contains(x.EmpSysId))).ToList();
                }
                else if (schTyp == "S")
                {
                    x = _hrmContext.HrEmpAttendExp.Where(x => x.EmpSysId == refId && x.StDate >= stDate && x.EndDate <= endDate && x.RecStat == 1).ToList();
                }
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAbsntExpVw> GetEmpAbsntExp(string typ, int empsysid, string fromdate, string todate, string slctdlist)
        {
            try
            {
                string sp = "[HR_EmpAtndc_GetAbsntExp] {0},{1},{2},{3},{4}";
                object[] param = { typ, empsysid, fromdate, todate, slctdlist };
                var x = _hrmContext.HrEmpAbsntExpVw.FromSqlRaw(sp, param).ToList();
                //if (x.Count > 0)
                //{
                //    return x;
                //}
                //else
                //{
                //    List<HrEmpAbsntExpVw> y = new List<HrEmpAbsntExpVw>
                //    {
                //        new HrEmpAbsntExpVw {
                //        AtndcSftId =0,
                //        AtndcDate = DateTime.Today,
                //        EmpSysId = 0,
                //        AtndcStatId = 0,
                //        ActionStat = "No Records Found",
                //        AtnExpId = 0,
                //        Note = "No Records Found",
                //        AprvdStatId = 0,
                //        AprvdStatTxt = "",
                //        AprvdId = 0,
                //        AprvdName = "",
                //        AprvdDateTime = DateTime.Today,
                //        AprvdRmks = "",
                //        CrtId = 0,
                //        CrtName = "",
                //        CrtDateTime = DateTime.Today
                //        }
                //    };

                //    return y;
                //}
                return x;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<HrEmpAtndcExpSmryVw> GetHrEmpAtndcExpSmryVw(int empSysId, DateTime stDate, DateTime endDate)
        {
            try
            {
                object[] param = { empSysId, stDate, endDate };
                return _hrmContext.HrEmpAtndcExpSmryVw.FromSqlRaw("HR_EmpAtndc_GetAbsntExpSmry {0},{1},{2}", param).ToList();

            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpAbsntExpVw> GetHrEmpAbsntExpVw(int empSysId, DateTime stDate, DateTime endDate) {
            try
            {
                object[] param = { empSysId, stDate, endDate };
                return _hrmContext.HrEmpAbsntExpVw.FromSqlRaw("HR_EmpAtndc_GetAbsntExp {0},{1},{2}", param).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrEmpAttendExp(HrEmpAttendExp atnExcp, int curUsrId, int entId)
        {
            try
            {
                var x = (from emp in _hrmContext.HrEmpGenInfo
                        join jb in _hrmContext.HrEmpJobAssign 
                            on emp.EmpSysId equals jb.EmpSysId
                        join dp in _hrmContext.OrgDepartment
                            on jb.DeptId equals dp.DeptId
                        where emp.CurrStat == 0 && jb.WkStat == 1 && dp.EntId == entId
                                && (atnExcp.empTyp == "A" 
                                    || (atnExcp.empTyp == "D" && jb.DeptId == atnExcp.dptId) 
                                    || (atnExcp.empTyp == "S" && emp.EmpSysId == atnExcp.empSysId))
                        select new HrEmpAttendExp {
                            EmpSysId = emp.EmpSysId,
                            StDate = atnExcp.StDate,
                            EndDate = atnExcp.EndDate,
                            Note = atnExcp.Note,
                            RqstId = emp.EmpSysId,
                            AprvdId = curUsrId,
                            AprvdStatId = 1,
                            AprvdRmks = "Direct Apply",
                            CrtId = curUsrId
                        }).ToList();
                _hrmContext.HrEmpAttendExp.AddRange(x);
                _hrmContext.SaveChanges();

                //---------- Execute Load Attendance Process ----------//
                var refId = atnExcp.empTyp == "S" ? (atnExcp.empSysId ?? 0) : (atnExcp.empTyp == "D" ? (atnExcp.dptId ?? 0) : 0); 
                LoadHrEmpAtndc(atnExcp.empTyp, refId, atnExcp.StDate, atnExcp.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, "Add new Exemption - " + atnExcp.empTyp, false, curUsrId);
                //----------------------------------------------------//
                return 1;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        //public int DeleteAttdExp(List<HrEmpAttendExp> lst)
        //{
        //    try
        //    {
        //        _hrmContext.HrEmpAttendExp.RemoveRange(lst);
        //        _hrmContext.SaveChanges();
        //        return 1;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}

        #region ============== Overtime / No Pay by Employee  ==============
        public partial class HrAttdOT_NoPayVw
        {
          
            public int EmpSysId { get; set; }          
            public string FullName { get; set; }
            public string EfctvDate { get; set; }            
            public decimal Ot_Amt { get; set; }
            public string OT_Hrs { get; set; }
            public string Ext_OT_Hrs { get; set; }
            public decimal No_Pay_Amt { get; set; }

        }


        public IEnumerable<HrAttdOT_NoPayVw> GetOTNoPayByEmpId(string field, int empysid, string fromdate, string todate)
        {
            try
            {
                string sp = "[PR_Overtime_NoPay_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, empysid, fromdate, todate };
                var x = from atn in _hrmContext.HrOvrtmNoPayView.FromSqlRaw(sp, param).ToList()
                        select new HrAttdOT_NoPayVw
                        {
                            EmpSysId = atn.EmpSysId,
                            FullName = atn.FullName,
                            EfctvDate = atn.EfctvDate,
                            Ot_Amt = atn.Ot_Amt,
                            OT_Hrs = atn.OT_Hrs,
                            Ext_OT_Hrs = atn.Ext_OT_Hrs,
                            No_Pay_Amt = atn.No_Pay_Amt
                        };

                return x;
            }
            catch (Exception )
            {
                DateTime emptydate = new DateTime(2000, 1, 1);
                List<HrAttdOT_NoPayVw> y =
                new List<HrAttdOT_NoPayVw> { new HrAttdOT_NoPayVw() 
                  {
                    EmpSysId = 0,
                    FullName = "No Records Found",
                    EfctvDate =emptydate.ToString(),
                    Ot_Amt = 0,
                    OT_Hrs = "",
                    Ext_OT_Hrs = "",
                    No_Pay_Amt = 0} 
                };
                return y;
            }
        }

        #endregion

        #region ============== Attendance Enums==============
        //Log History
        public enum AtndcLdPrcTyp
        {
            ATMC_LD,    //Automatic Attendance loading process     
            SLRY_PRC,   //Salary Process  
            EXCMP,      //Attendance Exemptions   
            HLDY,       //Holidays    
            LV,         //Leave
            ATN_RST,    //Attendance Reset    
            RST_CNG     //Employee work roster change 
        }

        //Exemption loading
        public enum AtnExpLdTyp
        {
             EMP,   //Employee
             MGR    //Manager
        }
        #endregion

        #region ==============Dashboard 7-Day Attendance Summary==============
        public class EmpAttendanceDayDto
        {
            public DateTime Date { get; set; }
            public string DateFormatted { get; set; }
            public string DayName { get; set; }
            public string InTime { get; set; }
            public string OutTime { get; set; }
            public string WorkHours { get; set; }
            public int WorkMinutes { get; set; }
            public int LateMinutes { get; set; }
            public int EarlyLeaveMinutes { get; set; }
            public int OtMinutes { get; set; }
            public string Status { get; set; }
            public string StatusClass { get; set; }
            public string Note { get; set; }
        }

        public class EmpAttendanceSummaryDto
        {
            public int EmpSysId { get; set; }
            public string EmpName { get; set; }
            public string DateRangeText { get; set; }
            public int TotalDays { get; set; }
            public int PresentDays { get; set; }
            public int AbsentDays { get; set; }
            public int LateCount { get; set; }
            public string TotalWorkHoursFormatted { get; set; }
            public List<EmpAttendanceDayDto> Days { get; set; } = new List<EmpAttendanceDayDto>();
        }

        public EmpAttendanceSummaryDto GetEmployee7DaysAttendance(int empSysId, DateTime? asOfDate = null)
        {
            var endDate = (asOfDate ?? DateTime.Today).Date;
            var startDate = endDate.AddDays(-6).Date;

            var emp = _hrmContext.HrEmpGenInfo.FirstOrDefault(e => e.EmpSysId == empSysId);
            var empName = emp != null ? $"{emp.FirstName} {(string.IsNullOrEmpty(emp.MdlName) ? "" : emp.MdlName + " ")}{emp.LastName}".Trim() : $"Employee #{empSysId}";

            var summary = new EmpAttendanceSummaryDto
            {
                EmpSysId = empSysId,
                EmpName = empName,
                DateRangeText = $"{startDate:dd MMM yyyy} – {endDate:dd MMM yyyy}",
                TotalDays = 7
            };

            try
            {
                var records = (from a in _hrmContext.HrEmpAtndcSft
                               join s in _hrmContext.SysAtndcStat on a.AtndcStatId equals s.AtndcStatId into sts
                               from s in sts.DefaultIfEmpty()
                               where a.EmpSysId == empSysId && a.AtndcDate >= startDate && a.AtndcDate <= endDate && a.RecStat == 1
                               select new
                               {
                                   a.AtndcDate,
                                   a.InTime,
                                   a.OutTime,
                                   a.WrkMnt,
                                   a.OtMnt,
                                   a.LateInMnt,
                                   a.ErlyLvMnt,
                                   StatTxt = s != null ? s.AtndcStatTxt : null,
                                   a.Note
                               }).ToList();

                int totalWrkMins = 0;
                int presentCount = 0;
                int absentCount = 0;
                int lateCount = 0;

                for (var d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    var rec = records.FirstOrDefault(r => r.AtndcDate.Date == d);
                    var dayItem = new EmpAttendanceDayDto
                    {
                        Date = d,
                        DateFormatted = d.ToString("dd MMM yyyy"),
                        DayName = d.ToString("ddd")
                    };

                    if (rec != null)
                    {
                        dayItem.InTime = rec.InTime.HasValue ? DateTime.Today.Add(rec.InTime.Value).ToString("hh:mm tt") : "--";
                        dayItem.OutTime = rec.OutTime.HasValue ? DateTime.Today.Add(rec.OutTime.Value).ToString("hh:mm tt") : "--";
                        dayItem.WorkMinutes = rec.WrkMnt;
                        dayItem.WorkHours = rec.WrkMnt > 0 ? $"{rec.WrkMnt / 60:D2}h {rec.WrkMnt % 60:D2}m" : "--";
                        dayItem.LateMinutes = rec.LateInMnt;
                        dayItem.EarlyLeaveMinutes = rec.ErlyLvMnt;
                        dayItem.OtMinutes = rec.OtMnt;
                        dayItem.Note = rec.Note;

                        var stat = rec.StatTxt;
                        if (string.IsNullOrEmpty(stat))
                        {
                            if (rec.InTime.HasValue) stat = "Present";
                            else if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday) stat = "Off Day";
                            else stat = "Absent";
                        }
                        dayItem.Status = stat;

                        totalWrkMins += rec.WrkMnt;
                        if (dayItem.LateMinutes > 0) lateCount++;

                        if (stat.Equals("Present", StringComparison.OrdinalIgnoreCase))
                        {
                            presentCount++;
                            dayItem.StatusClass = "badge-success";
                        }
                        else if (stat.Contains("Absent", StringComparison.OrdinalIgnoreCase))
                        {
                            absentCount++;
                            dayItem.StatusClass = "badge-danger";
                        }
                        else if (stat.Contains("Leave", StringComparison.OrdinalIgnoreCase))
                        {
                            dayItem.StatusClass = "badge-info";
                        }
                        else if (stat.Contains("Off", StringComparison.OrdinalIgnoreCase) || stat.Contains("Weekend", StringComparison.OrdinalIgnoreCase))
                        {
                            dayItem.StatusClass = "badge-secondary";
                        }
                        else if (stat.Contains("Holiday", StringComparison.OrdinalIgnoreCase))
                        {
                            dayItem.StatusClass = "badge-primary";
                        }
                        else
                        {
                            dayItem.StatusClass = "badge-warning";
                        }
                    }
                    else
                    {
                        // No record found for day
                        dayItem.InTime = "--";
                        dayItem.OutTime = "--";
                        dayItem.WorkHours = "--";
                        if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday)
                        {
                            dayItem.Status = "Off Day";
                            dayItem.StatusClass = "badge-secondary";
                        }
                        else if (d > DateTime.Today)
                        {
                            dayItem.Status = "Upcoming";
                            dayItem.StatusClass = "badge-light";
                        }
                        else
                        {
                            dayItem.Status = "Absent";
                            dayItem.StatusClass = "badge-danger";
                            absentCount++;
                        }
                    }

                    summary.Days.Add(dayItem);
                }

                summary.PresentDays = presentCount;
                summary.AbsentDays = absentCount;
                summary.LateCount = lateCount;
                summary.TotalWorkHoursFormatted = $"{totalWrkMins / 60}h {totalWrkMins % 60:D2}m";
                return summary;
            }
            catch (Exception)
            {
                return summary;
            }
        }
        #endregion
    }
}
