using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static qHRM.Areas.Employee.Data.EmployeeContext;
using static qHRM.Areas.SysAdmin.Data.SysSelectListContext;

namespace qHRM.Areas.WorkShift.Data
{
    public class WorkShiftContext 
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        #endregion

        #region ==============Constructors==============
        public WorkShiftContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }
        #endregion

        #region ==============Shift Master ===============
        public partial class Shift
        {
            public int SftId { get; set; }
            public string ShtName { get; set; }
        }

        public partial class ShiftVw
        {
            public int SftId { get; set; }
            public string ShtDtl { get; set; }
        }

        public partial class HrWkShiftVw
        {
            public int SftId { get; set; }
            public string SftCode { get; set; }
            public string NameEng { get; set; }
            public string NameArb { get; set; }
            public string Desc { get; set; }
            public DateTime StTime { get; set; }
            public DateTime EndTime { get; set; }
            public string OvrNghtSft_Txt { get; set; }
            public string OtTyp { get; set; }
            public int RstCount { get; set; }
        }

        public IEnumerable<Shift> GetShifts()
        {
            try
            {
                IEnumerable<Shift> objFiltered = from Context in _hrmContext.HrWkShift
                                                 select new Shift
                                                 {
                                                     SftId = Context.SftId,
                                                     ShtName = Context.NameEng
                                                 };
                return objFiltered;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<ShiftVw> GetShiftVws(int entId)
        {
            try
            {
                var x = _hrmContext.HrWkShift.Where(z => z.EntId == entId)
                        .Select(x => new ShiftVw 
                        {
                            SftId = x.SftId,
                            ShtDtl = x.StTime.ToString("HH:mm") + "-" + x.EndTime.ToString("HH:mm") + " " + x.SftCode + " - " + x.NameEng
                        });

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrWkShiftVw> GetAllHrWkShift(int entId)
        {
            try
            {
                var x =  from s in _hrmContext.HrWkShift.Where(z => z.EntId == entId).Include(sft => sft.HrWkRosterShift)
                            select new HrWkShiftVw
                            {
                                SftId = s.SftId,
                                SftCode = s.SftCode,
                                NameEng = s.NameEng,
                                NameArb = s.NameArb,
                                Desc  = s.Desc,
                                StTime = s.StTime,
                                EndTime = s.EndTime,
                                OvrNghtSft_Txt = s.OvrNghtSft_Txt,
                                RstCount = s.HrWkRosterShift.Count()
                            };
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public HrWkShift GetAllHrWkShiftById(int sftId)
        {
            try
            {
                return _hrmContext.HrWkShift.Where(x => x.SftId == sftId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrWkShift GetAllHrWkShiftByCode(string sftCode)
        {
            try
            {
                return _hrmContext.HrWkShift.Where(x => x.SftCode == sftCode).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Roster Master ===============
        public IEnumerable<HrWkRoster> GetAllHrWkRoster(int entId)
        {
            try
            {
                return _hrmContext.HrWkRoster.Where(x => x.EntId == entId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrWkRoster GetHrWkRosterByRstId(int rstId)
        {
            try
            {
                return _hrmContext.HrWkRoster.Where(x => x.RstId == rstId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrWkRoster GetHrWkRosterByRstCode(string rstCode)
        {
            try
            {
                return _hrmContext.HrWkRoster.Where(x => x.RstCode == rstCode).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Roster Shift===============
        public  partial class HrWkRstSftDaySmryVw
        {
            public int RstId { get; set; }
            public int DayIndx { get; set; }
            public string Day { get; set; }
            public string Sft1Name { get; set; }
            public string Sft1TimeRange { get; set; }
            public string Sft2Name { get; set; }
            public string Sft2TimeRange { get; set; }
            public string Sft3Name { get; set; }
            public string Sft3TimeRange { get; set; }
            public TimeSpan TSpan { get; set; }
            public string TSpanTxt { get { return string.Format("{0:D2}:{1:D2}", (int)TSpan.TotalHours, TSpan.Minutes); } }
        }

        public class ShiftData
        {
            public int Id { get; set; }
            public string Subject { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string CategoryColor { get; set; }
        }

        public partial class HrWkRstDtlVw
        {
            public int DayIndx { get; set; }
            public string Day { get; set; }
            public List<HrWkRstSftVw> lstHrWkRstSftVw { get; set; }
        }

        public partial class HrWkRstSftVw
        {
            public int SchId { get; set; }
            public int RstId { get; set; }
            public string Day { get; set; }
            public int SftId { get; set; }
            public string SftCode { get; set; }
            public string SftName { get; set; }
            public DateTime SftStTime { get; set; }
            public DateTime SftEndTime { get; set; }
            public TimeSpan TSpan { get; set; }
            public string Desc { get; set; }
            public string RecStatTxt { get; set; }
        }

        public IEnumerable<HrWkRstSftDaySmryVw> GetHrWkRstSftDaySmryVwByRstId(int rstId)
        {
            try
            {
                var lstHrWkRstSftDaySmryVw = new List<HrWkRstSftDaySmryVw>();
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 0, Day = "SUN", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 1, Day = "MON", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 2, Day = "TUE", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 3, Day = "WED", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 4, Day = "THU", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 5, Day = "FRI", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });
                lstHrWkRstSftDaySmryVw.Add(new HrWkRstSftDaySmryVw { RstId = rstId, DayIndx = 6, Day = "SAT", Sft1Name = "Not Assigned", Sft1TimeRange = "-" });

                var x = _hrmContext.HrWkRosterShift
                        .Join(_hrmContext.HrWkShift, rs => rs.SftId, s => s.SftId, (rs, s) => new { rs, s })
                        .Where(z => z.rs.RstId == rstId && z.rs.RstId == rstId)
                        .Select(x => new HrWkRstSftVw
                        {
                            SchId = x.rs.SchId,
                            RstId = x.rs.RstId,
                            Day = x.rs.Day,
                            SftId = x.rs.SftId,
                            SftCode = x.s.SftCode,
                            SftName = x.s.NameEng,
                            SftStTime = x.s.StTime,
                            SftEndTime = x.s.EndTime,
                            TSpan = x.s.EndTime - x.s.StTime - TimeSpan.FromMinutes(x.s.BrkMnt),
                            Desc = x.rs.DescEng,
                            RecStatTxt = (x.rs.RecStat == 1) ? "Active" : "Deactive"
                        }).ToList();

                foreach(var hrWkRstSftDaySmryVw in lstHrWkRstSftDaySmryVw)
                {
                    var lstHrWkRstSftVw = x.Where(x => x.Day == hrWkRstSftDaySmryVw.Day).OrderBy(o => o.SftStTime).ToList();
                    int i = 0;
                    foreach (var hrWkRstSftVw in lstHrWkRstSftVw)
                    {
                        if (i == 0){
                            hrWkRstSftDaySmryVw.Sft1Name = hrWkRstSftVw.SftName;
                            hrWkRstSftDaySmryVw.Sft1TimeRange = hrWkRstSftVw.SftStTime.ToString("HH:mm") + "\n-> " + hrWkRstSftVw.SftEndTime.ToString("HH:mm");
                        }
                        else if (i == 1) {
                            hrWkRstSftDaySmryVw.Sft2Name = hrWkRstSftVw.SftName;
                            hrWkRstSftDaySmryVw.Sft2TimeRange = hrWkRstSftVw.SftStTime.ToString("HH:mm") + "\n-> " + hrWkRstSftVw.SftEndTime.ToString("HH:mm");
                        }
                        else if (i == 2)
                        {
                            hrWkRstSftDaySmryVw.Sft3Name = hrWkRstSftVw.SftName;
                            hrWkRstSftDaySmryVw.Sft3TimeRange = hrWkRstSftVw.SftStTime.ToString("HH:mm") + "\n-> " + hrWkRstSftVw.SftEndTime.ToString("HH:mm");
                        }
                        hrWkRstSftDaySmryVw.TSpan += hrWkRstSftVw.TSpan;
                        i++;
                    }
                }

                return lstHrWkRstSftDaySmryVw;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrWkRstDtlVw> GetHrWkRstDtlsByRstId(int rstId)
        {
            try
            {
                var lstHrWkRstDtlVw = new List<HrWkRstDtlVw>();

                var x = _hrmContext.HrWkRosterShift
                        .Join(_hrmContext.HrWkShift, rs => rs.SftId, s => s.SftId, (rs, s) => new { rs, s })
                        .Where(z => z.rs.RstId == rstId && z.rs.RstId == rstId)
                        .Select(x => new HrWkRstSftVw
                        {
                            SchId = x.rs.SchId,
                            RstId = x.rs.RstId,
                            Day = x.rs.Day,
                            SftId = x.rs.SftId,
                            SftCode = x.s.SftCode,
                            SftName = x.s.NameEng,
                            SftStTime = x.s.StTime,
                            Desc = x.rs.DescEng,
                            RecStatTxt = (x.rs.RecStat == 1) ? "Active" : "Deactive"
                        }).ToList();

                //SUN
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 0, Day = "SUN", lstHrWkRstSftVw = x.Where(x => x.Day == "SUN").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 1, Day = "MON", lstHrWkRstSftVw = x.Where(x => x.Day == "MON").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 2, Day = "TUE", lstHrWkRstSftVw = x.Where(x => x.Day == "TUE").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 3, Day = "WEN", lstHrWkRstSftVw = x.Where(x => x.Day == "WEN").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 4, Day = "THU", lstHrWkRstSftVw = x.Where(x => x.Day == "THU").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 5, Day = "FRI", lstHrWkRstSftVw = x.Where(x => x.Day == "FRI").ToList() });
                lstHrWkRstDtlVw.Add(new HrWkRstDtlVw { DayIndx = 6, Day = "SAT", lstHrWkRstSftVw = x.Where(x => x.Day == "SAT").ToList() });

                return lstHrWkRstDtlVw;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<ShiftData> GetShiftDataByRstId(int rstId)
        {
            try
            {
                var x = _hrmContext.HrWkRosterShift
                        .Join(_hrmContext.HrWkShift, rs => rs.SftId, s => s.SftId, (rs, s) => new { rs, s })
                        .Join(_hrmContext.SysSelectListElement, a => a.rs.Day, le => le.ElmntTxt, (a, le) => new { a, le })
                        .Join(_hrmContext.SysSelectListType, b => b.le.LstTypId, lt => lt.LstTypId, (b, lt) => new { b, lt})
                        .Where(z => z.b.a.rs.RstId == rstId && z.lt.LstTypKey == SelectListTypeEnum.DAYS.ToString() && z.b.a.rs.RecStat == 1)
                        .Select(x => new ShiftData
                        {
                            Id = x.b.a.rs.SchId,
                            Subject = x.b.a.s.SftCode + "\n" + x.b.a.s.NameEng,
                            StartTime = x.b.a.s.StTime.AddDays(x.b.le.Indx),
                            EndTime = x.b.a.s.EndTime.AddDays(x.b.le.Indx),
                            CategoryColor = (x.b.le.Indx == 5 || x.b.le.Indx == 6)? "#9D8189" : "#540033"
                        }).ToList();
                return x;
            }
            catch (Exception ex) { throw ex; }
        }
   
        public List<ShiftVw> GetShiftByRstIdDay(int rstId, string day, int sftId)
        {
            try
            {
                string sp = "[HR_WkShift_Day_Select] {0},{1},{2}";
                object[] param = { rstId, day, sftId };
                var list = _hrmContext.HrWkShift.FromSqlRaw(sp, param)
                            .Select(x => new ShiftVw
                            {
                                SftId = x.SftId,
                                ShtDtl = x.StTime.ToString("HH:mm") + "-" + x.EndTime.ToString("HH:mm") + " " + x.SftCode + " - " + x.NameEng
                            }).ToList();

                return list;
            }
            catch (Exception ex) { throw ex; }
        }
                        
        public HrWkRosterShift GetHrWkRosterShiftBySchId(int schId)
        {
            try
            {
                return _hrmContext.HrWkRosterShift.Where(x => x.SchId == schId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrWkRosterShift GetDetachedHrWkRosterShiftBySchId(int schId)
        {
            try
            {
                HrWkRosterShift hrWkRosterShift = _hrmContext.HrWkRosterShift.Where(x => x.SchId == schId).FirstOrDefault();
                _hrmContext.Entry(hrWkRosterShift).State = EntityState.Detached;
                return hrWkRosterShift;
            }
            catch (Exception ex) { throw ex; }
        }

        public int ValidateSchedule(int rstId, int schId, string day, int sftId)
        {
            try
            {
                var selSft = _hrmContext.HrWkShift.Where(x => x.SftId == sftId).FirstOrDefault();

                var x = _hrmContext.HrWkRosterShift
                        .Join(_hrmContext.HrWkShift, rs => rs.SftId, s => s.SftId, (rs, s) => new { rs, s })
                        .Where(z => z.rs.RstId == rstId && z.rs.RecStat == 1 && z.rs.Day == day && (schId == 0 || z.rs.SchId != schId)
                                && ((z.s.StTime <= selSft.StTime && z.s.EndTime > selSft.StTime)
                                    || (z.s.StTime < selSft.EndTime && z.s.EndTime >= selSft.EndTime)
                                    || (z.s.StTime >= selSft.StTime && z.s.EndTime <= selSft.EndTime)))
                        .Select(x => x.rs.SchId);
                return x.Count();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Work Roster ===============
        public partial class HrWkRstDtlVw
        {
            public int RecId { get; set; }
            public int EmpSysId { get; set; }
            public int RstId { get; set; }
            public string FullName { get; set; }
            public string RstName { get; set; }                        
            public DateTime StDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool CndrAtndc { get; set; }
            public string CndrAtndcDesc { get; set; }
        }

        public IEnumerable<HrWkRstDtlVw> GetEmpWrkRstrs()
        {
            try
            {  
                var x = _hrmContext.HrEmpWkRoster
                        .Join(_hrmContext.HrEmpGenInfo, n => n.EmpSysId, emp => emp.EmpSysId, (n, emp) => new { n, emp })
                        .Join(_hrmContext.HrWkRoster, r => r.n.RstId, wr => wr.RstId, (r, wr) => new { r, wr })
                        .Where(z => z.r.n.RecStat == 1)
                        .OrderBy(y=>y.r.emp.SrtOrd)
                        .Select(x => new HrWkRstDtlVw
                        {
                            RecId =  x.r.n.RecId,
                            EmpSysId = x.r.emp.EmpSysId,
                            RstId = x.r.n.RstId,
                            FullName = x.r.emp.FullName,
                            RstName = x.wr.NameEng,
                            StDate = x.r.n.StDate,
                            EndDate =  x.r.n.EndDate,
                            CndrAtndc = x.r.n.CndrAtndc,
                            CndrAtndcDesc = x.r.n.CndrAtndc == true ? "Yes" : "No"
                        }).ToList();
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrWkRstDtlVw> GetEmpWrkRstrs(int Id)
        {
            try
            {
                var x = _hrmContext.HrEmpWkRoster
                        .Join(_hrmContext.HrEmpGenInfo, n => n.EmpSysId, emp => emp.EmpSysId, (n, emp) => new { n, emp })
                        .Join(_hrmContext.HrWkRoster, r => r.n.RstId, wr => wr.RstId, (r, wr) => new { r, wr })
                        .Where(y=> y.r.n.RecId == Id)
                        .Select(x => new HrWkRstDtlVw
                        {
                            RecId = x.r.n.RecId,
                            EmpSysId = x.r.emp.EmpSysId,
                            RstId = x.r.n.RstId,
                            FullName = x.r.emp.FullName,
                            RstName = x.wr.NameEng,
                            StDate = x.r.n.StDate,
                            EndDate = x.r.n.EndDate,
                            CndrAtndc = x.r.n.CndrAtndc,
                            CndrAtndcDesc = x.r.n.CndrAtndc == true ? "Yes" : "No"
                        }).ToList();
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpWkRoster> IsRosterAssign(int empSysId, DateTime stDate, DateTime endDate )
        {
            try
            {
                var x = _hrmContext.HrEmpWkRoster.Where(x => x.EmpSysId == empSysId &&
                                                            ((x.StDate <= stDate && x.EndDate >= stDate) || (x.StDate <= endDate && x.EndDate >= endDate))
                                                            );                       
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public partial class HrEmpNotCrtdRst
        {            
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
                      
        }

        public IEnumerable<HrEmpNotCrtdRst> GetEmpNotCrtdRst()
        {
            try
            {
                var x = _hrmContext.HrEmpGenInfo .Where( x => x.CurrStat == 0 && !_hrmContext.HrEmpWkRoster.Any (y=> y.EmpSysId == x.EmpSysId))
                         .Select(x => new HrEmpNotCrtdRst
                         {
                             EmpSysId = x.EmpSysId,
                             EmpId = x.EmpId,
                             FullName = x.FullName
                             
                         });
                return x;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Work Shift ===============

        public class EmpShiftData
        {
            public int Id { get; set; }
            public int EmpId { get; set; }
            public string Subject { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string CategoryColor { get; set; }
        }

        public partial class EmployeeShiftData
        {
            public IEnumerable<EmpView2> EmployeeDataLst { get; set; }
            public IEnumerable<EmpShiftData> EmpShiftDataLst { get; set; }
        }

        public EmployeeShiftData GetEmployeeShiftData(SftSchTypes typ, int schId, DateTime stDate, DateTime endDate, int sftId)
        {
            try
            {
                EmployeeShiftData employeeShiftData = new EmployeeShiftData();
                EmployeeContext employeeContext = new EmployeeContext(_hrmContext);

                var dates = Enumerable.Range(0, (endDate - stDate).Days + 1).Select(d => stDate.AddDays(d));

                employeeShiftData.EmployeeDataLst = employeeContext.GetEmpView2s(typ, schId);
                if (employeeShiftData.EmployeeDataLst.Count() > 0)
                {

                    var x = employeeShiftData.EmployeeDataLst
                            .Join(_hrmContext.HrEmpWkRoster, emp => emp.EmpSysId, er => er.EmpSysId, (emp, er) => new { emp, er })
                            .Join(_hrmContext.HrWkRosterShift, p => p.er.RstId, rs => rs.RstId, (p, rs) => new { p, rs })
                            .Join(_hrmContext.HrWkShift, q => q.rs.SftId, sf => sf.SftId, (q, sf) => new { q, sf })
                            .Join(dates, r => r.q.rs.Day, dt => dt.Date.ToString("ddd").ToUpper(), (r, dt) => new { r, dt })
                            .Where(z => z.r.q.rs.RecStat == 1)
                            .Select(x => new EmpShiftData
                             {
                                 Id = x.r.q.p.er.RecId,
                                 EmpId = x.r.q.p.emp.EmpSysId,
                                 Subject = x.r.sf.Desc,
                                 StartTime = x.r.sf.StTime,
                                 EndTime = x.r.sf.EndTime,
                                 CategoryColor = "#FF11DD"
                             });
                    employeeShiftData.EmpShiftDataLst = x;
                }

                return employeeShiftData;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpShiftData> GetEmpShiftDataList()
        {

            return new List<EmpShiftData>(); 
        }




        public class WkRstView
        {
            public int Id { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int EmployeeId { get; set; }
            public string Subject { get; set; }
            public int SftId { get; set; }
            public string CategoryColor { get; set; }

            //Id = 1,
            //        Subject = "Explosion of Betelgeuse Star",
            //        StartTime = new DateTime(2020, 2, 11, 9, 30, 0),
            //        EndTime = new DateTime(2020, 2, 11, 11, 0, 0),
            //        CategoryColor = "#1aaa55",
            //        EmployeeId = 1
            
        }
        


        //public IEnumerable<WkRstView> GetWkRstrData(string feild, int? empSysId, string fromDate, string toDate)
        //{
        //    try
        //    {

        //        string sp = "[HR_EmpAttendance_Select] {0},{1},{2},{3}";
        //        object[] param = { feild, empSysId, fromDate, toDate };
        //        IEnumerable<HrEmpWkShift> list = new List<HrEmpWkShift>();
        //        list = _hrmContext.HrEmpWkShift.FromSqlRaw(sp, param).ToList();
        //        var wrkRstEmp = from wr in list
        //                        select new WkRstView
        //                        {
        //                            Id = wr.RecId,
        //                            Subject = "Test",
        //                            StartTime = Convert.ToDateTime(wr.EftDate.ToString("yyyy-MM-dd") + " 07:00:00"),
        //                            EndTime = Convert.ToDateTime(wr.EftDate.ToString("yyyy-MM-dd") + " 17:00:00"),
        //                            CategoryColor = "540033",
        //                            EmployeeId = wr.EmpSysId                                    
        //                        };
        //        return wrkRstEmp.OrderBy(n => n.EmployeeId);

        //    }
        //    catch (Exception ex) { throw ex; }
        //}


        #endregion


        #region Enum
        public enum SftSchTypes
        {
            EMP,
            DEPT
        }
        #endregion Enum
    }
}
