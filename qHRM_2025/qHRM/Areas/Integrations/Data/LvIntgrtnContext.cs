using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Integrations.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static qHRM.Areas.Employee.Data.EmployeeContext;

namespace qHRM.Areas.Integrations.Data
{
    public class LvIntgrtnContext
    {
        #region ==============Member Variables==============
        private readonly QCSLEAVEContext _qscLvContext;
        private readonly qHRMasterContext _hrmContext;
        #endregion

        #region ==============Constructors==============
        public LvIntgrtnContext(QCSLEAVEContext qscLvContext, qHRMasterContext hrmContext)
        {
            _qscLvContext = qscLvContext;
            _hrmContext = hrmContext;
        }
        #endregion

        #region ==============Custom Views==============
        public partial class LvMpng
        {
            [Key]
            public int Id { get; set; }
            public int IncidentNo { get; set; }
            public string Qid { get; set; }
            public string EmpName { get; set; }
            public int LvYear { get; set; }
            public int LvDays { get; set; }
            public string Lvtype { get; set; }
            public string LvtypeSub { get; set; }
            public string LvtypeSubPay { get; set; }
            public DateTime? LvStartDate { get; set; }
            public DateTime? LvEndDate { get; set; }
            public DateTime? InitiatedDate { get; set; }
            public string Status { get; set; }
            public int? MpdLvTypId { get; set; }
            public string MpdLvTyp { get; set; }
            public int MpdEmpSysId { get; set; }
            public string MpdEmpId { get; set; }
            public string MpdEmpName { get; set; }
            public string MpdSts { get; set; }
        }
        #endregion

        #region ==============Read Data==============
        //public int GetMaxIncndtNo()
        //{
        //    try
        //    {
        //        var maxLvIncdnt = _hrmContext.HrLvRegister.Max(x => x.Incident);
        //        //var lvAll = _qscLvContext.LvAll.Where(x => x.IncidentNo > 4000).ToListAsync() as IEnumerable<LvAll>;

        //        var lvAll = _qscLvContext.LvAll.Where(x => x.IncidentNo > 4000);

        //        var x = from lv in lvAll
        //                join empx in _hrmContext.HrEmpGenInfo on lv.Qid.Trim() equals empx.PmtId into lvs
        //                from emp in lvs.DefaultIfEmpty()
        //                join typmpx in _hrmContext.TmpLvTypMpng on new { Lvtype = lv.Lvtype.Trim() ?? "", LvtypeSub = lv.LvtypeSub.Trim() ?? "", LvtypeSubPay = lv.LvtypeSubPay.Trim() ?? "" } equals
        //                                                           new { Lvtype = typmpx.Lvtype.Trim() ?? "", LvtypeSub = typmpx.LvtypeSub.Trim() ?? "", LvtypeSubPay = typmpx.LvtypeSubPay.Trim() ?? "" } into lvtyps
        //                from typmp in lvtyps.DefaultIfEmpty()
        //                select lv;
        //        return 0;
        //    }
        //    catch (Exception ex) { throw ex; }
        //}

        public List<LvMpng> GetPndngLvs()
        {
            try
            {
                var maxLvIncdnt = _hrmContext.HrLvRegister.Max(x => x.Incident);
                var logLvLdPrc = _hrmContext.LogLvLdPrc.OrderByDescending(o => o.PrcDateTime).FirstOrDefault();
                var lstSyncDt = Convert.ToDateTime("2021-08-01");
                if (logLvLdPrc != null)
                    lstSyncDt = logLvLdPrc.PrcDateTime;


                var lvTypMpngs = this.GetTmpLvTypMpngs();

                var lvAll = this.GetLvData(maxLvIncdnt, lstSyncDt);

                var qids = lvAll.GroupBy(g => g.Qid.Trim()).Select(x => x.First().Qid.Trim()).ToArray();

                var emps = this.GetEmpsByPmtIds(qids);

                List<LvMpng> lvMpngs = new List<LvMpng>();
                foreach (var lv in lvAll)
                {
                    var lvMpng = new LvMpng();
                    lvMpng.Id = lv.Id;
                    lvMpng.IncidentNo = (int)lv.IncidentNo;
                    lvMpng.Qid = lv.Qid.Trim();
                    lvMpng.EmpName = lv.EmpName;
                    lvMpng.LvYear = (int)lv.LvYear;
                    lvMpng.LvDays = (int)lv.LvDays;
                    lvMpng.Lvtype = lv.Lvtype;
                    lvMpng.LvtypeSub = lv.LvtypeSub;
                    lvMpng.LvtypeSubPay = lv.LvtypeSubPay;
                    lvMpng.LvStartDate = lv.LvStartDate;
                    lvMpng.LvEndDate = lv.LvEndDate;
                    lvMpng.InitiatedDate = lv.LvInitiatedDate;
                    lvMpng.Status = lv.Status;

                    var lvTypMpng = lvTypMpngs.Where(x => x.Lvtype == lv.Lvtype && x.LvtypeSub == lv.LvtypeSub && x.LvtypeSubPay == lv.LvtypeSubPay).FirstOrDefault();
                    if (lvTypMpng != null)
                    {
                        lvMpng.MpdLvTypId = lvTypMpng.LvTypId;
                        lvMpng.MpdLvTyp = lvTypMpng.LvTypName;
                    }
                    else
                    {
                        lvMpng.MpdSts = "Type Mapping Failed";
                    }

                    var emp = emps.Where(x => x.PmtId == lv.Qid.Trim()).FirstOrDefault();
                    if (emp != null)
                    {
                        lvMpng.MpdEmpSysId = emp.EmpSysId;
                        lvMpng.MpdEmpId = emp.EmpId;
                        lvMpng.MpdEmpName = emp.Name;
                    }
                    else { lvMpng.MpdEmpSysId = 0;  lvMpng.MpdSts = lvMpng.MpdSts == "" ? "QID & " + lvMpng.MpdSts : "QID Mapping Failed"; }
                    lvMpng.MpdSts = (lvMpng.MpdSts == "" || lvMpng.MpdSts is null) ? "Mapped" : lvMpng.MpdSts;
                    lvMpngs.Add(lvMpng);
                }
                return lvMpngs;
            }
            catch (Exception ex) { throw ex; }
        }

        private IEnumerable<LvAll> GetLvData(int incdnt, DateTime lstSyncDt)
        {
            try
            {
                string sp = "[Proc_GetPndngLvs] {0},{1}";
                object[] param = { incdnt, lstSyncDt };
                return _qscLvContext.LvAll.FromSqlRaw(sp, param).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        private IEnumerable<TmpLvTypMpng> GetTmpLvTypMpngs()
        {
            try
            {
                var x = _hrmContext.TmpLvTypMpng
                        .Join(_hrmContext.HrLvType, typ => typ.LvTypId, mp => mp.LvTypId, (mp, typ) => new { mp, typ })
                        .Select(x => new TmpLvTypMpng
                        {
                            AutoId = x.mp.AutoId,
                            Lvtype = x.mp.Lvtype,
                            LvtypeSub = x.mp.LvtypeSub,
                            LvtypeSubPay = x.mp.LvtypeSubPay,
                            LvTypId = x.mp.LvTypId,
                            LvTypName = x.typ.NameEng
                        });
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        private IEnumerable<EmpSimpleView2> GetEmpsByPmtIds(string[] pmtIds)
        {
            try
            {
                return _hrmContext.HrEmpGenInfo
                      .Where(x => pmtIds.Contains(x.PmtId))
                      .Select(z => new EmpSimpleView2
                      {
                          EmpSysId = z.EmpSysId,
                          EmpId = z.EmpId,
                          PmtId = z.PmtId,
                          Name = z.FirstName + " " + z.LastName
                      });
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Save Data==============
        public int SaveMpdLvs(List<LvMpng> lvMpngs, int usrId)
        {
            try
            {
                var logLvLdPrc = new LogLvLdPrc
                {
                    PrcSrc = "LvLd",
                    PrcSrcDtl = "Leave Load Utility Screen",
                    PrcTyp = "U",
                    FrmIncdnt = lvMpngs.Min(x => x.IncidentNo),
                    ToIncdnt = lvMpngs.Max(x => x.IncidentNo),
                    PrcSt = DateTime.Now,
                    PrcStat = "Processing",
                    RstAll = false,
                    PrcBy = usrId
                };
                _hrmContext.LogLvLdPrc.Add(logLvLdPrc);
                _hrmContext.SaveChanges();

                var incdnts = lvMpngs.Select(x => x.IncidentNo).ToArray();

                var extLvRegs = _hrmContext.HrLvRegister.Where(z => lvMpngs.Select(x => x.IncidentNo).ToArray().Contains(z.Incident));
                foreach (var lvReg in extLvRegs)
                {
                    var lvMpng = lvMpngs.Where(z => z.IncidentNo == lvReg.Incident).First();
                    if (lvMpng.Status == "Rejected")
                        lvReg.RecStat = -1;
                    else if (lvMpng.Status == "Aborted")
                        lvReg.RecStat = -2;
                    //lvReg.Note = lvReg.Note + " - " + lvMpng.InitiatedDate.ToString();
                }
                var lvRegs = lvMpngs.Where(z => !extLvRegs.Select(y => y.Incident).Contains(z.IncidentNo)) //lvMpngs.Except(lvMpngs.Where(z => extLvRegs.Select(y => y.Incident).Contains(z.IncidentNo)))
                             .Select(x => new HrLvRegister
                             {
                                 EmpSysId = x.MpdEmpSysId,
                                 LvTypId = (int)x.MpdLvTypId,
                                 LvYear = x.LvYear,
                                 Incident = (int)x.IncidentNo,
                                 StDate = (DateTime)x.LvStartDate,
                                 EndDate = (DateTime)x.LvEndDate,
                                 Days = x.LvDays,
                                 CurrAvblDays = 1,
                                 LvSdlId = null,
                                 DocId = null,
                                 Note = "From Ultimus - " + x.IncidentNo,
                                 RecStat = 1,
                                 CrtId = usrId,
                                 CrtDateTime = DateTime.Now
                             });
                _hrmContext.HrLvRegister.AddRange(lvRegs);

                logLvLdPrc.PrcEnd = DateTime.Now;
                _hrmContext.SaveChanges();

                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        //public int SaveMpdLvs(List<LvMpng> lvMpngs, int usrId)
        //{
        //    try
        //    {
        //        var logLvLdPrc = new LogLvLdPrc { PrcSrc = "LvLd", PrcSrcDtl = "Leave Load Utility Screen", PrcTyp = "U"
        //                                            , FrmIncdnt = lvMpngs.Min(x => x.IncidentNo)
        //                                            , ToIncdnt = lvMpngs.Max(x => x.IncidentNo)
        //                                            , PrcSt = DateTime.Now
        //                                            , PrcStat = "Processing"
        //                                            , RstAll = false
        //                                            , PrcBy = usrId
        //                                    };
        //        _hrmContext.LogLvLdPrc.Add(logLvLdPrc);
        //        _hrmContext.SaveChanges();

        //        var incdnts = lvMpngs.Select(x => x.IncidentNo).ToArray();

        //        var extLvRegs = _hrmContext.HrLvRegister.Where(z => lvMpngs.Select(x => x.IncidentNo).ToArray().Contains(z.Incident));
        //        List<HrLvRegister> lvRegs = new List<HrLvRegister>();

        //        //if (extLvRegs.Count() == 0)
        //        //{
        //        //    lvRegs = lvMpngs.Select(x => new HrLvRegister
        //        //                 {
        //        //                     EmpSysId = x.MpdEmpSysId,
        //        //                     LvTypId = (int)x.MpdLvTypId,
        //        //                     LvYear = x.LvYear,
        //        //                     Incident = (int)x.IncidentNo,
        //        //                     StDate = (DateTime)x.LvStartDate,
        //        //                     EndDate = (DateTime)x.LvEndDate,
        //        //                     Days = x.LvDays,
        //        //                     CurrAvblDays = 1,
        //        //                     RjnDate = null,
        //        //                     RjnStat = 0,
        //        //                     LvSdlId = null,
        //        //                     DocId = null,
        //        //                     Note = "From Ultimus - " + x.IncidentNo,
        //        //                     RecStat = 1,
        //        //                     CrtId = usrId,
        //        //                     CrtDateTime = DateTime.Now,
        //        //                     exStDate = (DateTime)x.LvStartDate,
        //        //                     exEndDate = (DateTime)x.LvEndDate
        //        //    });
        //        //}
        //        //else
        //        //{
        //            //foreach (var lvReg in extLvRegs)
        //            //{
        //            //    var lvMpng = lvMpngs.Where(z => z.IncidentNo == lvReg.Incident).First();
        //            //    if (lvMpng.Status == "Rejected")
        //            //        lvReg.RecStat = -1;
        //            //    else if (lvMpng.Status == "Aborted")
        //            //        lvReg.RecStat = -2;
        //            //    lvReg.Note = lvReg.Note + " - " + lvMpng.InitiatedDate.ToString();
        //            //}

        //            foreach(var lvMpng in lvMpngs)
        //            {
        //                var lvReg = extLvRegs.Where(z => z.Incident == lvMpng.IncidentNo).FirstOrDefault();
        //                if (lvReg != null)
        //                {
        //                    if (lvMpng.Status == "Rejected")
        //                        lvReg.RecStat = -1;
        //                    else if (lvMpng.Status == "Aborted")
        //                        lvReg.RecStat = -2;
        //                    lvReg.Note = lvReg.Note + " - " + lvMpng.InitiatedDate.ToString();
        //                }
        //                else {
        //                    lvRegs.Add(new HrLvRegister
        //                    {
        //                        EmpSysId = lvMpng.MpdEmpSysId,
        //                        LvTypId = (int)lvMpng.MpdLvTypId,
        //                        LvYear = lvMpng.LvYear,
        //                        Incident = (int)lvMpng.IncidentNo,
        //                        StDate = (DateTime)lvMpng.LvStartDate,
        //                        EndDate = (DateTime)lvMpng.LvEndDate,
        //                        Days = lvMpng.LvDays,
        //                        CurrAvblDays = 1,
        //                        RjnDate = null,
        //                        RjnStat = 0,
        //                        LvSdlId = null,
        //                        DocId = null,
        //                        Note = "From Ultimus - " + lvMpng.IncidentNo,
        //                        RecStat = 1,
        //                        CrtId = usrId,
        //                        CrtDateTime = DateTime.Now,
        //                        exStDate = (DateTime)lvMpng.LvStartDate,
        //                        exEndDate = (DateTime)lvMpng.LvEndDate
        //                    });
        //                }
        //            }

        //        //    lvRegs = lvMpngs.Where(z => !extLvRegs.Select(y => y.Incident).Contains(z.IncidentNo)) //lvMpngs.Except(lvMpngs.Where(z => extLvRegs.Select(y => y.Incident).Contains(z.IncidentNo)))
        //        //                 .Select(x => new HrLvRegister
        //        //                 {
        //        //                     EmpSysId = x.MpdEmpSysId,
        //        //                     LvTypId = (int)x.MpdLvTypId,
        //        //                     LvYear = x.LvYear,
        //        //                     Incident = (int)x.IncidentNo,
        //        //                     StDate = (DateTime)x.LvStartDate,
        //        //                     EndDate = (DateTime)x.LvEndDate,
        //        //                     Days = x.LvDays,
        //        //                     CurrAvblDays = 1,
        //        //                     LvSdlId = null,
        //        //                     DocId = null,
        //        //                     Note = "From Ultimus - " + x.IncidentNo,
        //        //                     RecStat = 1,
        //        //                     CrtId = usrId,
        //        //                     CrtDateTime = DateTime.Now
        //        //                 });
        //        //}
        //        _hrmContext.HrLvRegister.AddRange(lvRegs);

        //        logLvLdPrc.PrcEnd = DateTime.Now;
        //        _hrmContext.SaveChanges();

        //        return 1;
        //    }
        //    catch (Exception ex) { throw ex; }
        //}
        #endregion
    }
}
