using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using Syncfusion.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Syncfusion.Pdf.Security;
using qHRM.Areas.Employee.Data;
using static qHRM.Areas.SysAdmin.Data.SysSelectListContext;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Data
{
    public class PayrollContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        private readonly SysSelectListContext _sysListContext;
        private readonly EmployeeContext _employeeContext;
        private readonly IHostingEnvironment _host;
        #endregion

        #region ==============Constructors==============
        public PayrollContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }

        public PayrollContext(qHRMasterContext hrmContext, SysSelectListContext sysListContext, EmployeeContext employeeContext, IHostingEnvironment host)
        {
            _hrmContext = hrmContext;
            _sysListContext = sysListContext;
            _employeeContext = employeeContext;
            _host = host;
        }
        #endregion

        #region ==============Payroll Configuration==============
        public partial class HrPrEmpSlryStngsSts
        {
            [Key]
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpNm { get; set; }
            public string JbAsgnSts { get; set; }
            public string LocSts { get; set; }
            public string SlrySts { get; set; }
            public string ExcldSts { get; set; }
        }

        public IEnumerable<HrPrEmpSlryStngsSts> GetPndngEmpSlryStngs(DateTime dt)
        {
            try
            {
                object[] param = { dt };
                var x = _hrmContext.HrPrEmpSlryStngsStsVw.FromSqlRaw("[HR_EmpPndngSlrySts] {0}", param).ToList();
                return x;
            }
            catch { throw; }
        }

        public HrPrConfig GetActiveHrPrConfig()
        {
            try
            {
                return _hrmContext.HrPrConfig.Where(x => x.RecStat == 1).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrPrConfig(HrPrConfig hrPrConfig, int empSysId)
        {
            try
            {
                //HrPrConfig hrPrConfig = new HrPrConfig();
                var modifiedPropList = new List<PropView>();
                var extPrCnfGrtvtyLvls = new List<HrPrCnfGrtvtyLvl>();
                var extHrPrConfig = _hrmContext.HrPrConfig.Where(x => x.RecStat == 1).FirstOrDefault();

                if (extHrPrConfig != null)
                {
                    _hrmContext.Entry(extHrPrConfig).State = EntityState.Detached;
                    var newModelType = hrPrConfig.GetType();
                    var extModelType = extHrPrConfig.GetType();

                    var newModelPropList = newModelType.GetProperties();
                    var extModelPropList = extModelType.GetProperties();

                    hrPrConfig.CrtId = extHrPrConfig.CrtId;
                    hrPrConfig.CrtDateTime = extHrPrConfig.CrtDateTime;

                    extPrCnfGrtvtyLvls = _hrmContext.HrPrCnfGrtvtyLvl.Where(x => x.PrCnfId == extHrPrConfig.PrCnfId && x.RecStat == 1).ToList();
                    modifiedPropList = newModelPropList.Join(extModelPropList, ex => ex.Name, nw => nw.Name, (nw, ex) => new { nw, ex })
                                            .Where(x => ((x.nw.GetValue(hrPrConfig) != null) ? x.nw.GetValue(hrPrConfig).ToString() : "") != ((x.ex.GetValue(extHrPrConfig) != null) ? x.ex.GetValue(extHrPrConfig).ToString() : ""))
                                            .Select(z => new PropView
                                            {
                                                Prop = z.nw.Name,
                                                Val = z.ex.GetValue(hrPrConfig) != null ? z.ex.GetValue(hrPrConfig).ToString() : ""
                                            }).ToList();
                    extHrPrConfig.RecStat = 0;
                    _hrmContext.HrPrConfig.Attach(extHrPrConfig);
                    _hrmContext.Entry(extHrPrConfig).State = EntityState.Modified;
                }
                if (modifiedPropList.Count() > 0 || extHrPrConfig == null)
                {
                    hrPrConfig.PrCnfId = 0;
                    hrPrConfig.CrtId = empSysId;
                    hrPrConfig.CrtDateTime = DateTime.Now;
                    _hrmContext.HrPrConfig.Add(hrPrConfig);
                    _hrmContext.SaveChanges();
                    foreach (var extGrt in extPrCnfGrtvtyLvls)
                    {
                        _hrmContext.HrPrCnfGrtvtyLvl.Add(new HrPrCnfGrtvtyLvl { Desc = extGrt.Desc
                                                                                , LwrBndryYrs = extGrt.LwrBndryYrs
                                                                                , UprBndryYrs = extGrt.UprBndryYrs
                                                                                , EftDays = extGrt.EftDays
                                                                                , PrCnfId = hrPrConfig.PrCnfId
                                                                                , RecStat = 1
                                                                                , CrtId = extGrt.CrtId
                                                                                , CrtDateTime = DateTime.Now });
                    }
                    _hrmContext.SaveChanges();
                    return 1;
                }
                else
                    return 0;
            }
            catch (Exception ex) { throw ex; }
        }

        #region Gratuity Functions
        public partial class HrPrCnfGrtvtyLvlVw
        {
            public int GrtLvlId { get; set; }
            public int LwrBndryYrs { get; set; }
            public int UprBndryYrs { get; set; }
            public int EftDays { get; set; }
            public int PrCnfId { get; set; }
        }

        public IEnumerable<HrPrCnfGrtvtyLvlVw> GetHrPrCnfGrtvtyLvlByPrCnfId(int prCnfId)
        {
            try
            {
                return _hrmContext.HrPrCnfGrtvtyLvl
                        .Where(z => z.PrCnfId == prCnfId && z.RecStat == 1)
                        .Select(x => new HrPrCnfGrtvtyLvlVw {
                            GrtLvlId = x.GrtLvlId,
                            LwrBndryYrs = x.LwrBndryYrs,
                            UprBndryYrs = x.UprBndryYrs,
                            EftDays = x.EftDays,
                            PrCnfId = x.PrCnfId
                        });
            }
            catch (Exception ex) { throw ex; }
        }

        public int GetMaxGrtLvlByPrCnfId(int prCnfId)
        {
            try
            {
                var hrPrCnfGrtvtyLvl = _hrmContext.HrPrCnfGrtvtyLvl.Where(z => z.PrCnfId == prCnfId && z.RecStat == 1);
                if (hrPrCnfGrtvtyLvl.Count() > 0)
                    return hrPrCnfGrtvtyLvl.Max(x => x.UprBndryYrs);
                else
                    return 0;
            }
            catch (Exception ex) { throw ex; }
        }

        public HrPrCnfGrtvtyLvl GetHrPrCnfGrtvtyLvlById(int grtLvlId)
        {
            try
            {
                return _hrmContext.HrPrCnfGrtvtyLvl.Where(x => x.GrtLvlId == grtLvlId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion Gratuity Functions
        #endregion

        #region ==============OT Rates==============
        public partial class HrPrOtrateView
        {
            public int OtId { get; set; }
            public string OtDesc { get; set; }
        }

        public HrPrOtrate GetOtRateById(int otId)
        {
            try
            {
                return _hrmContext.HrPrOtrate.Where(x => x.OtId == otId).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrOtrate GetOtRateByCode(string otCode)
        {
            try
            {
                return _hrmContext.HrPrOtrate.Where(x => x.OtCode.ToLower() == otCode.ToLower()).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<HrPrOtrate> GetAllActiveHrPrOtRateByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrPrOtrate.Where(x => x.RecStat == 1).ToList();
            }
            catch { throw; }
        }

        public IEnumerable<HrPrOtrateView> GetHrPrOtrateViewByEntId(int entId) {
            try
            {
                return _hrmContext.HrPrOtrate.Where(z => z.RecStat == 1 && z.EntId == entId)
                                       .Select(x => new HrPrOtrateView
                                       {
                                           OtId = x.OtId,
                                           OtDesc = x.OtCode + " - Normal : " + x.NmlRate.ToString() + " - Extended : " + x.ExtRate.ToString()
                                       });
            }
            catch { throw; }
        }
        #endregion

        #region ==============Ad-hoc Transaction Types==============
        public partial class HrPrAdHocTrnTypeView
        {
            public int TrnTypId { get; set; }
            public string TrnTypDesc { get; set; }
        }

        public IEnumerable<HrPrAdHocTrnTypeView> GetHrPrAdHocTrnTypeViewByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrnType.Where(z => z.EntId == entId && z.RecStat == 1 && z.AutoGen == false)
                            .Select(x => new HrPrAdHocTrnTypeView
                            {
                                TrnTypId = x.TrnTypId,
                                TrnTypDesc = x.TrnType + " - " + x.TrnTypTxt
                            });
            }
            catch { throw; }
        }

        public HrPrAdHocTrnType GetHrPrAdHocTrnTypeById(int trnTypId)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrnType.Where(z => z.TrnTypId == trnTypId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrPrAdHocTrnType> GetHrPrAdHocTrnTypeByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrnType.Where(z => z.EntId == entId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public int GetMaxHrPrAdHocTrnTypeIndex(int entId)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrnType.Where(z => z.EntId == entId).Max(x => x.Indx);
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Ad-hoc Transactions==============
        public partial class HrPrAdHocTrnView
        {
            public int TrnId { get; set; }
            public int TrnTypId { get; set; }
            public string TrnTyp { get; set; }
            public string TrnTypDesc { get; set; }
            public int EmpSysId { get; set; }
            public string EmpDesc { get; set; }
            public DateTime TrnDate { get; set; }
            public string Ref { get; set; } public string Desc { get; set; }
            public decimal TrnAmt { get; set; }
            public string EftMonth { get; set; }
            public int EftMn { get; set; }
            public int EftYear { get; set; }
            public int RecStat { get; set; }
            public string RecStatTxt { get; set; }
            public bool AutoGen { get; set; }
        }

        public IEnumerable<HrPrAdHocTrnView> GetHrPrAdHocTrnViewByEntId(int entId, int recStat)
        {
            try
            {
                var x = _hrmContext.HrPrAdHocTrns
                                    .Join(_hrmContext.HrPrAdHocTrnType, typ => typ.TrnTypId, trn => trn.TrnTypId, (trn, typ) => new { trn, typ })
                                    .Join(_hrmContext.HrEmpGenInfo, p => p.trn.EmpSysId, emp => emp.EmpSysId, (p, emp) => new { p, emp })
                                    .Where(z => z.p.typ.EntId == entId && (recStat == -1 || z.p.trn.RecStat == recStat))
                                    .Select(x => new HrPrAdHocTrnView
                                    {
                                        TrnId = x.p.trn.TrnId,
                                        TrnTypId = x.p.trn.TrnTypId,
                                        TrnTyp = x.p.typ.TrnType,
                                        TrnTypDesc = x.p.typ.TrnType + " - " + x.p.typ.TrnTypTxt,
                                        EmpSysId = x.p.trn.EmpSysId,
                                        EmpDesc = x.emp.FullName,
                                        TrnDate = x.p.trn.TrnDate,
                                        Ref = x.p.trn.Ref,
                                        TrnAmt = x.p.trn.TrnAmt,
                                        EftMonth = x.p.trn.EftYear.ToString() + " - " + new DateTime(2000, x.p.trn.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                                        EftYear = x.p.trn.EftYear,
                                        EftMn = x.p.trn.EftMonth,
                                        RecStat = x.p.trn.RecStat,
                                        RecStatTxt = x.p.trn.ex_RecStatTxt,
                                        AutoGen = x.p.typ.AutoGen
                                    }).OrderByDescending(o => o.TrnDate);
                return x;
            }
            catch { throw; }
        }

        public HrPrAdHocTrns GetHrPrAdHocTrnById(string trnRef, int empSysId, int recStat)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrns.Where(z => z.Ref == trnRef && z.EmpSysId == empSysId && z.RecStat == recStat).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrAdHocTrns GetHrPrAdHocTrnById(int trnId)
        {
            try
            {
                return _hrmContext.HrPrAdHocTrns.Where(z => z.TrnId == trnId).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<HrPrAdHocTrnView> GetHrPrAdHocByProcId(int entId, int eftMnt, int eftYr, string empId, string payType)
        {
            try
            {

                return _hrmContext.HrPrAdHocTrns
                        .Join(_hrmContext.HrPrAdHocTrnType, typ => typ.TrnTypId, trn => trn.TrnTypId, (trn, typ) => new { trn, typ })
                        .Join(_hrmContext.HrEmpGenInfo, p => p.trn.EmpSysId, emp => emp.EmpSysId, (p, emp) => new { p, emp })
                        .Where(z => z.p.typ.EntId == entId && z.p.trn.RecStat == 1 && z.p.trn.EftMonth == eftMnt && z.p.trn.EftYear == eftYr && z.emp.EmpId == empId && z.p.typ.TrnType == payType)
                        .Select(x => new HrPrAdHocTrnView
                        {
                            TrnId = x.p.trn.TrnId,
                            TrnTypId = x.p.trn.TrnTypId,
                            TrnTypDesc = x.p.trn.Desc,
                            EmpSysId = x.p.trn.EmpSysId,
                            EmpDesc = x.emp.EmpId,
                            TrnDate = x.p.trn.TrnDate,
                            Ref = x.p.trn.Ref,
                            TrnAmt = x.p.trn.TrnAmt,
                            EftMonth = x.p.trn.EftYear.ToString() + " - " + new DateTime(2000, x.p.trn.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                            RecStat = x.p.trn.RecStat,
                            RecStatTxt = x.p.trn.ex_RecStatTxt
                        }).OrderByDescending(o => o.TrnDate);

            }
            catch { throw; }
        }

        public IEnumerable<RptAdHocTrnVw> GetRptAdHocTrnVws(int entId, string typ, int eftMnt, int eftYr, string key, string value)
        {
            try
            {
                var x = from trn in _hrmContext.HrPrAdHocTrns
                        join emp in _hrmContext.HrEmpGenInfo
                            on trn.EmpSysId equals emp.EmpSysId
                        join trnTyp in _hrmContext.HrPrAdHocTrnType
                            on trn.TrnTypId equals trnTyp.TrnTypId
                        join jbx in _hrmContext.HrEmpJobAssign
                            on trn.EmpSysId equals jbx.EmpSysId into jbs
                        from jb in jbs.DefaultIfEmpty()
                        join dpx in _hrmContext.OrgDepartment
                            on jb.DeptId equals dpx.DeptId into dps
                        from dp in dps.DefaultIfEmpty()
                        join dsx in _hrmContext.OrgDesignation
                            on jb.DesigId equals dsx.DesigId into dss
                        from ds in dss.DefaultIfEmpty()
                        where dp.EntId == entId
                            && trn.EftYear == eftYr && trn.EftMonth == eftMnt
                            && (typ == "ALL" || trnTyp.TrnType == typ)
                            && (key == "ALL"
                                || (key == "EMPSYSID" && trn.EmpSysId == Convert.ToInt32(value))
                                || (key == "DEPTID" && trn.EmpSysId == Convert.ToInt32(value)))
                        select new RptAdHocTrnVw {
                            EmpId = emp.EmpId,
                            EmpName = emp.FullName,
                            DptId = dp.DeptId,
                            DptName = dp.NameEng,
                            DsgId = ds.DesigId,
                            DsgName = ds.NameEng,
                            TrnDate = trn.TrnDate,
                            Ref = trn.Ref,
                            Desc = trn.Desc,
                            Amt = trn.TrnAmt
                        };
                return new List<RptAdHocTrnVw>().OrderByDescending(o => o.TrnDate);
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Salary Structure==============
        public partial class HrPrEmpSlryStrucView
        {
            public int SlStrcId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpName { get; set; }
            public int DesigId { get; set; }
            public string DesigName { get; set; }
            public int DeptId { get; set; }
            public string DeptName { get; set; }
            public decimal Basic { get; set; }
            public decimal HrAlwnc { get; set; }
            public decimal TrnsAlwnc { get; set; }
            public decimal Tra { get; set; }
            public decimal PstnAlwnc { get; set; }
            public decimal MedAlwnc { get; set; }
            public decimal MobAlwnc { get; set; }
            public decimal ProjAlwnc { get; set; }
            public decimal SpcAlwnc { get; set; }
            public decimal IncntvPrc { get; set; }
            public decimal PrfmAlwnc { get; set; }
            public decimal OthrAlwnc { get; set; }
            public decimal BonusRate { get; set; }
            public decimal Nt_Slry { get; set; }
            public int OtId { get; set; }
            public string OtCode { get; set; }
            public short RecStat { get; set; }
            public int CrtBy { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public HrPrEmpSlryStrucVw GetHrPrEmpSlryStrucById(int slStrcId)
        {
            try
            {
                string sp = "[PR_EmpSlryStruc_Select] {0},{1}";
                object[] param = { "SlStrc_Id", slStrcId };
                return _hrmContext.HrPrEmpSlryStrucVw.FromSqlRaw(sp, param).ToList().FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrEmpSlryStrucVw GetHrPrEmpSlryStrucByEmpSysId(int empSysId)
        {
            try
            {
                string sp = "[PR_EmpSlryStruc_Select] {0},{1}";
                object[] param = { "Emp_Sys_Id", empSysId };
                return _hrmContext.HrPrEmpSlryStrucVw.FromSqlRaw(sp, param).ToList().FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<HrPrEmpSlryStrucView> GetHrPrEmpSlryStrucViewsByEntId(int entId, DateTime curDate, bool isSnglEmp, int empSysId)
        {
            try
            {
                string sp = "[PR_EmpSlryStruc_Select] {0},{1}";
                object[] param = { "ALL", 0 };

                return _hrmContext.HrPrEmpSlryStrucVw.FromSqlRaw(sp, param).ToList()
                        .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                        .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                        .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                        .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                        .Join(_hrmContext.HrPrOtrate, s => s.r.q.p.ss.OtId, ot => ot.OtId, (s, ot) => new { s, ot })
                        .Where(z => z.s.r.q.p.ss.RecStat == 1 && z.s.r.q.p.gi.CurrStat == 0 && z.s.r.q.jb.StDate <= curDate && (z.s.r.q.jb.EndDate == null || z.s.r.q.jb.EndDate > curDate) && z.s.r.dp.EntId == entId
                               && ((isSnglEmp == true && empSysId == z.s.r.q.p.ss.EmpSysId) || (isSnglEmp == false && _employeeContext.GetEmpSbrdnts(empSysId).Contains(z.s.r.q.p.ss.EmpSysId))))
                                    
                        .OrderBy(o => o.s.r.q.p.gi.SrtOrd)
                        .Select(x => new HrPrEmpSlryStrucView {
                            SlStrcId = x.s.r.q.p.ss.SlStrcId,
                            EmpSysId = x.s.r.q.p.ss.EmpSysId,
                            EmpId = x.s.r.q.p.gi.EmpId,
                            EmpName = x.s.r.q.p.gi.FullName,
                            DesigId = x.s.r.q.jb.DesigId,
                            DesigName = x.s.ds.NameEng,
                            DeptId = x.s.r.q.jb.DeptId,
                            DeptName = x.s.r.dp.NameEng,
                            Basic = x.s.r.q.p.ss.Basic ?? 0,
                            HrAlwnc = x.s.r.q.p.ss.HrAlwnc ?? 0,
                            TrnsAlwnc = x.s.r.q.p.ss.TrnsAlwnc ?? 0,
                            Tra = x.s.r.q.p.ss.Tra ?? 0,
                            PstnAlwnc = x.s.r.q.p.ss.PstnAlwnc ?? 0,
                            MedAlwnc = x.s.r.q.p.ss.MedAlwnc ?? 0,
                            MobAlwnc = x.s.r.q.p.ss.MobAlwnc ?? 0,
                            ProjAlwnc = x.s.r.q.p.ss.ProjAlwnc ?? 0,
                            SpcAlwnc = x.s.r.q.p.ss.SpcAlwnc ?? 0,
                            IncntvPrc = x.s.r.q.p.ss.IncntvPrc ?? 0,
                            PrfmAlwnc = x.s.r.q.p.ss.PrfmAlwnc ?? 0,
                            OthrAlwnc = x.s.r.q.p.ss.OthrAlwnc ?? 0,
                            Nt_Slry = ((x.s.r.q.p.ss.Basic ?? 0) + (x.s.r.q.p.ss.HrAlwnc ?? 0) + (x.s.r.q.p.ss.TrnsAlwnc ?? 0) + (x.s.r.q.p.ss.Tra ?? 0) + (x.s.r.q.p.ss.PstnAlwnc ?? 0) + (x.s.r.q.p.ss.MedAlwnc ?? 0) + (x.s.r.q.p.ss.MobAlwnc ?? 0) + (x.s.r.q.p.ss.ProjAlwnc ?? 0) + (x.s.r.q.p.ss.SpcAlwnc ?? 0) + (x.s.r.q.p.ss.OthrAlwnc ?? 0)),   //+ x.r.q.p.ss.Ot                               
                            BonusRate = x.s.r.q.p.ss.BonusRate ?? 0,
                            OtId = x.s.r.q.p.ss.OtId,
                            OtCode = x.ot.OtCode,
                            RecStat = x.s.r.q.p.ss.RecStat,
                            CrtBy = x.s.r.q.p.ss.CrtBy,
                            CrtDateTime = x.s.r.q.p.ss.CrtDateTime
                        });
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrPrEmpSlryStruc(LogEventTypes prcTyp, HrPrEmpSlryStrucVw hrPrEmpSlryStrucVw)
        {
            try
            {
                string sp = "[PR_EmpSlryStruc_Save] {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}";
                object[] param = { prcTyp.ToString()
                                   , hrPrEmpSlryStrucVw.SlStrcId
                                   , hrPrEmpSlryStrucVw.EmpSysId
                                   , hrPrEmpSlryStrucVw.Basic
                                   , hrPrEmpSlryStrucVw.HrAlwnc
                                   , hrPrEmpSlryStrucVw.TrnsAlwnc
                                   , hrPrEmpSlryStrucVw.Tra
                                   , hrPrEmpSlryStrucVw.PstnAlwnc
                                   , hrPrEmpSlryStrucVw.MedAlwnc
                                   , hrPrEmpSlryStrucVw.MobAlwnc
                                   , hrPrEmpSlryStrucVw.SpcAlwnc
                                   , hrPrEmpSlryStrucVw.IncntvPrc
                                   , hrPrEmpSlryStrucVw.PrfmAlwnc
                                   , hrPrEmpSlryStrucVw.OthrAlwnc
                                   , hrPrEmpSlryStrucVw.BonusRate
                                   , hrPrEmpSlryStrucVw.AirTcktFee ?? 0
                                   , hrPrEmpSlryStrucVw.OtId
                                   , hrPrEmpSlryStrucVw.RecStat
                                   , hrPrEmpSlryStrucVw.CrtBy
                                   , hrPrEmpSlryStrucVw.CrtDateTime};
                var x = _hrmContext.HrPrEmpSlryStrucVw.FromSqlRaw(sp, param).ToList();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Salary Structure History==============
        public HrPrEmpSlryStrucHstryVw GetHrPrEmpSlryStrucHstryById(int slStrcId)
        {
            try
            {
                string sp = "[PR_EmpSlryStrucHstry_Select] {0},{1}";
                object[] param = { "SlStrc_Id", slStrcId };
                return _hrmContext.HrPrEmpSlryStrucHstryVw.FromSqlRaw(sp, param).ToList().FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrEmpSlryStrucHstryVw GetHrPrEmpSlryStrucHstryVwByEmpSysId(int empSysId)
        {
            try
            {
                string sp = "[PR_EmpSlryStrucHstry_Select] {0},{1}";
                object[] param = { "Emp_Sys_Id", empSysId };
                return _hrmContext.HrPrEmpSlryStrucHstryVw.FromSqlRaw(sp, param).ToList().FirstOrDefault();
            }
            catch { throw; }
        }
        #endregion

        #region ==============Project Allowances==============

        public partial class EftYear
        {
            public int Key { get; set; }
            public string Value { get; set; }
        }

        public partial class HrPrProjAlwncView
        {
            public int ProjAlwncId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpDesc { get; set; }
            public int EftYear { get; set; }
            public string EftMonth { get; set; }
            public int Prds { get; set; }
            public int EndYear { get; set; }
            public string EndMonth { get; set; }
            public string Ref { get; set; }
            public string Note { get; set; }
            public decimal ProjAlwncAmt { get; set; }
            public int RecStat { get; set; }
            public string RecStatTxt { get; set; }
        }

        public IEnumerable<HrPrProjAlwncView> GetHrPrProjAlwncViewByEntId(int entId, int eftYear, int eftMonth)
        {
            try
            {
                DateTime slctDate = new DateTime(eftYear, eftMonth, 1);

                return _hrmContext.HrPrProjAlwnc
                                    .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, prjalw => prjalw.EmpSysId, (prjalw, gi) => new { prjalw, gi })
                                    .Join(_hrmContext.HrEmpJobAssign, p => p.gi.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                                    .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                                    .Where(z => z.q.p.prjalw.RecStat == 1 && z.q.p.gi.CurrStat == 0 && z.q.jb.WkStat == 1
                                            && (z.q.p.prjalw.EftYear < eftYear || (z.q.p.prjalw.EftYear == eftYear && z.q.p.prjalw.EftMonth <= eftMonth))
                                            && (z.q.p.prjalw.EndYear > eftYear || (z.q.p.prjalw.EndYear == eftYear && z.q.p.prjalw.EndMonth >= eftMonth))
                                            && z.dp.EntId == entId)
                                    .Select(x => new HrPrProjAlwncView
                                    {
                                        ProjAlwncId = x.q.p.prjalw.ProjAlwncId,
                                        EmpSysId = x.q.p.prjalw.EmpSysId,
                                        EmpDesc = x.q.p.gi.EmpId + " - " + x.q.p.gi.FullName,
                                        EftYear = x.q.p.prjalw.EftYear,
                                        EftMonth = x.q.p.prjalw.EftYear.ToString() + " - " + new DateTime(x.q.p.prjalw.EftYear, x.q.p.prjalw.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                                        Prds = x.q.p.prjalw.Prds,
                                        EndYear = x.q.p.prjalw.EndYear,
                                        EndMonth = x.q.p.prjalw.EndYear.ToString() + " - " + new DateTime(x.q.p.prjalw.EndYear, x.q.p.prjalw.EndMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                                        Ref = x.q.p.prjalw.Ref,
                                        ProjAlwncAmt = x.q.p.prjalw.ProjAlwncAmt,
                                        RecStat = x.q.p.prjalw.RecStat,
                                        RecStatTxt = x.q.p.prjalw.ex_RecStatTxt
                                    }).OrderByDescending(x => x.ProjAlwncId);
            }
            catch (Exception ex) { throw ex; }
        }

        public HrPrProjAlwnc GetHrPrProjAlwncById(int projAlwncId)
        {
            try
            {
                return _hrmContext.HrPrProjAlwnc.Where(z => z.ProjAlwncId == projAlwncId).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<EftYear> GetProjAlwncYearList()
        {
            try
            {
                var x = _hrmContext.HrPrProjAlwnc.Where(z => z.RecStat == 1).OrderBy(o => o.EftYear)
                        .Select(x => new EftYear
                        {
                            Key = x.EftYear
                            ,
                            Value = x.EftYear.ToString()
                        }).Distinct().ToList();
                return x;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion ==============Project Allowances==============

        #region ==============Incentive Entries==============
        public partial class HrPrInctvView
        {
            public int InctvId { get; set; }
            public int InctvTyp { get; set; }
            public string InctvTypDesc { get; set; }
            public int InctvMod { get; set; }
            public string InctvModDesc { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
            public string EmpDesc { get; set; }
            public DateTime InctvDate { get; set; }
            public string Ref { get; set; }
            public string Desc { get; set; }
            public string ShtRmk { get; set; }
            public string Note { get; set; }
            public decimal InctvAmt { get; set; }
            public string EftMonth { get; set; }
            public string EftMonthName { get; set; }
            public string StMnthName { get; set; }
            public int EftYear { get; set; }
            public int StYear { get; set; }
            public int NoMnths { get; set; }
            public int RecStat { get; set; }
            public string RecStatTxt { get; set; }
        }


        public partial class HrPrSlrySndEmailvView
        {
            public int Index { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string PstName { get; set; }
            public string IsDlvrd { get; set; }

        }



        public IEnumerable<HrPrInctvView> GetHrPrInctvViewByEntId(int entId, int recStat, DateTime curDate)
        {
            try
            {
                var slcLstCnxt = new SysSelectListContext(_hrmContext);
                var inctvTyp = slcLstCnxt.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_TYP).ToDictionary(t => (int)t.Indx, t => t.ElmntTxt);
                var inctvMod = slcLstCnxt.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_MOD).ToDictionary(m => (int)m.Indx, m => m.ElmntTxt);
                var months = slcLstCnxt.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS).ToDictionary(m => (int)m.Indx, m => m.ElmntTxt);

                var query = (from inctv in _hrmContext.HrPrInctv
                             join gi in _hrmContext.HrEmpGenInfo on inctv.EmpSysId equals gi.EmpSysId
                             join jb in _hrmContext.HrEmpJobAssign on gi.EmpSysId equals jb.EmpSysId
                             join dp in _hrmContext.OrgDepartment on jb.DeptId equals dp.DeptId
                             where inctv.RecStat == 1 
                                && gi.CurrStat == 0 
                                && jb.WkStat == 1 
                                && dp.EntId == entId
                             select new
                             {
                                 inctv.InctvId,
                                 inctv.InctvTyp,
                                 inctv.InctvMod,
                                 inctv.EmpSysId,
                                 gi.EmpId,
                                 gi.FullName,
                                 inctv.InctvDate,
                                 inctv.Ref,
                                 inctv.Desc,
                                 inctv.ShtRmk,
                                 inctv.InctvAmt,
                                 inctv.EftYear,
                                 inctv.EftMonth,
                                 inctv.RecStat,
                                 inctv.ex_RecStatTxt
                             }).ToList();

                var x = query.Select(item => {
                    string monthName = months.ContainsKey(item.EftMonth)
                        ? months[item.EftMonth]
                        : (item.EftMonth >= 1 && item.EftMonth <= 12 ? new DateTime(2000, item.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture) : item.EftMonth.ToString());
                    string noteText = !string.IsNullOrWhiteSpace(item.Desc) ? item.Desc : (!string.IsNullOrWhiteSpace(item.ShtRmk) ? item.ShtRmk : item.Ref);

                    return new HrPrInctvView
                    {
                        InctvId = item.InctvId,
                        InctvTyp = item.InctvTyp,
                        InctvTypDesc = inctvTyp.ContainsKey(item.InctvTyp) ? inctvTyp[item.InctvTyp] : item.InctvTyp.ToString(),
                        InctvMod = item.InctvMod,
                        InctvModDesc = inctvMod.ContainsKey(item.InctvMod) ? inctvMod[item.InctvMod] : item.InctvMod.ToString(),
                        EmpSysId = item.EmpSysId,
                        EmpId = item.EmpId,
                        FullName = item.FullName,
                        EmpDesc = item.EmpId + " - " + item.FullName,
                        InctvDate = item.InctvDate,
                        Ref = item.Ref,
                        Desc = item.Desc,
                        ShtRmk = item.ShtRmk,
                        Note = noteText,
                        InctvAmt = item.InctvAmt,
                        EftYear = item.EftYear,
                        StYear = item.EftYear,
                        EftMonth = item.EftYear.ToString() + " - " + monthName,
                        EftMonthName = monthName,
                        StMnthName = monthName,
                        NoMnths = 1,
                        RecStat = item.RecStat,
                        RecStatTxt = !string.IsNullOrEmpty(item.ex_RecStatTxt) ? item.ex_RecStatTxt : (item.RecStat == 1 ? "Active" : "Inactive")
                    };
                }).OrderByDescending(o => o.InctvDate).ToList();

                return x;
            }
            catch { throw; }
        }

        public HrPrInctv GetHrPrInctvById(int inctvId)
        {
            try
            {
                return _hrmContext.HrPrInctv.Where(z => z.InctvId == inctvId).FirstOrDefault();
            }
            catch { throw; }
        }
        #endregion

        #region ==============Employee Loan Types (Staff Loan Types)==============
        public partial class HrPrStfLoanTypeView
        {
            public int LnTypId { get; set; }
            public string LnTypDesc { get; set; }
        }

        public IEnumerable<HrPrStfLoanTypeView> GetHrPrStfLoanTypeViewByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanType.Where(z => z.EntId == entId && z.RecStat == 1)
                            .Select(x => new HrPrStfLoanTypeView
                            {
                                LnTypId = x.LnTypId,
                                LnTypDesc = x.LnTypCode + " - " + x.Desc
                            });
            }
            catch { throw; }
        }

        public IEnumerable<HrPrStfLoanType> GetHrPrStfLoanTypeByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanType.Where(z => z.EntId == entId && z.RecStat == 1);
            }
            catch { throw; }
        }

        public HrPrStfLoanType GetHrPrStfLoanTypeById(int lnTypId)
        {
            return _hrmContext.HrPrStfLoanType.Where(z => z.LnTypId == lnTypId).FirstOrDefault();
        }

        public HrPrStfLoanType GetHrPrStfLoanTypeByCode(string lnTypCode)
        {
            return _hrmContext.HrPrStfLoanType.Where(z => z.LnTypCode == lnTypCode).FirstOrDefault();
        }
        #endregion

        #region ==============Salary Process Header==============
        public IEnumerable<HrPrSlryPrcHdr> GetSalaryHeaders(int entId)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcHdr.Where(z => z.EntId == entId).OrderByDescending(z=>z.PrcId).OrderByDescending(z=> z.PrcStat).ToList();
            }
            catch { throw; }
        }

        public HrPrSlryPrcHdr GetLstSlryPrcHdr(int entId)
        {
            try
            {
                var query = _hrmContext.HrPrSlryPrcHdr.AsQueryable();
                if (entId > 0)
                {
                    query = query.Where(z => z.EntId == entId);
                }
                var res = query.OrderByDescending(o1 => o1.PrcId).FirstOrDefault();
                if (res == null && entId > 0)
                {
                    res = _hrmContext.HrPrSlryPrcHdr.OrderByDescending(o1 => o1.PrcId).FirstOrDefault();
                }
                return res;
            }
            catch { throw; }
        }

        public HrPrSlryPrcHdr GetTopSalaryHeader(int entId, int eftYear, int eftMonth)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcHdr.OrderByDescending(x => x.PrcId).Where(z => z.EntId == entId && z.EftYear == eftYear && z.EftMonth == eftMonth).OrderByDescending(z => z.PrcId).FirstOrDefault();
            }

            catch { throw; }
        }
        #endregion

        #region ==============Salary Process Detail ==============    
        
        public HrPrSlryPrcDtlPsv GetHrPrSlryPrcDtlPsvById(int prcPsvRecId)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcDtlPsv.Where(x => x.PrcPsvRecId == prcPsvRecId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrPrSlryPrcDtlPsv(HrPrSlryPrcDtlPsv hrPrSlryPrcDtlPsv)
        {
            try
            {
                _hrmContext.HrPrSlryPrcDtlPsv.Attach(hrPrSlryPrcDtlPsv);
                return _hrmContext.SaveChanges();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrPrSlryPrcHdr> GetSalaryDtlPsv(int eftYear, int eftMonth)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcHdr.Where(x => x.EftMonth == eftMonth & x.EftYear == eftYear).OrderByDescending( x=> x.PrcId);
                
            }

            catch { throw; }
        }

        public IEnumerable<PrSalarySheetView> ExecuteSalaryProcess(int prcId,
                                                                   int refId,
                                                                   string prcType,
                                                                   int entId,
                                                                   int eftYear,
                                                                   int eftMnt,
                                                                   string note,
                                                                   int usrId,
                                                                   string staffTyp = "ALL")
        {
            try
            {
                if (entId <= 0) entId = 1;
                if (usrId <= 0) usrId = 1;

                // v3 modular stored procedure: orchestrates 6 child procedures with identical interface & resultset
                string sp = "[PR_SlryPrc_Exc_v3] {0},{1},{2},{3},{4},{5},{6},{7},{8}";
                object[] param = { prcId, refId, prcType, entId, eftYear, eftMnt, note, usrId, staffTyp };
                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                       .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                       .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                       .Where(z => z.r.q.p.ss.RecStat == 1 && z.r.q.jb.WkStat == 1 && z.r.q.p.gi.CurrStat == 0 && z.r.dp.EntId == entId)
                       .OrderBy(o => o.r.q.p.gi.SrtOrd).ThenBy(m => m.r.q.p.gi.EmpId)
                       .Select((x, i) => new PrSalarySheetView
                       {
                           Index = ++i,
                           PrcPsvRecId = x.r.q.p.ss.PrcPsvRecId,
                           Department = x.r.dp.NameEng,
                           Emp_Id = x.r.q.p.gi.EmpId,
                           Full_Name = x.r.q.p.gi.FullName,
                           Designation = x.ds.NameEng,
                           BasicSalary = x.r.q.p.ss.Basic,
                           HouseRent = x.r.q.p.ss.HrAlwnc,
                           Transport = x.r.q.p.ss.TrnsAlwnc,
                           Position = x.r.q.p.ss.PstnAlwnc,
                           ProjectAllowance = x.r.q.p.ss.ProjAlwnc,
                           Incentives = x.r.q.p.ss.IncntvAmt,
                           Mobile = x.r.q.p.ss.MobAlwnc,
                           Overtime = x.r.q.p.ss.OtAmt,
                           MedicalAllowance = x.r.q.p.ss.MedAlwnc,
                           BonusAmt = x.r.q.p.ss.BonusAmt,
                           OthrAlwnc = x.r.q.p.ss.OthrAlwnc,
                           OthrPmt = x.r.q.p.ss.OthrPmt,
                           AirTcktFee = x.r.q.p.ss.AirTcktFee,
                           AnlLvEnchmnt = x.r.q.p.ss.AnlLvEnchmnt,
                           GrossSalary = x.r.q.p.ss.GrsSlry,
                           SalaryAdvance = x.r.q.p.ss.SlryAdv,
                           StaffLoans = x.r.q.p.ss.StfLnPmt,
                           NoPay = x.r.q.p.ss.NoPayAmt,
                           OthrRcvryAmt = x.r.q.p.ss.OthrRcvry,
                           TotalDeductions = x.r.q.p.ss.TotDctAmt,
                           NtSlry = x.r.q.p.ss.NtSlry,
                           PmtMod = x.r.q.p.ss.PmtMod,
                           PmtModTxt = (x.r.q.p.ss.PmtMod == 0) ? "Cash" : "Bank",
                           RecStat = x.r.q.p.ss.RecStat,
                           BnkAcct = x.r.q.p.ss.BnkAcct,
                           PrcId = x.r.q.p.ss.PrcId,
                           PrcLogId = x.r.q.p.ss.PrcLogId
                       });

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<PrSalarySheetView> GetSalaryDtl(int entId, string field, int prcId, int eftMnt, int eftYr)
        {
            try
            {
                string sp = "[PR_SlryPrc_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };

                string strYrMnt = new DateTime(2021, 1, 1).ToString("MMMM", CultureInfo.InvariantCulture) + " " + eftYr.ToString();
                string prntTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");

                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                       .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                       .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                       .Where(z => z.r.q.p.ss.RecStat == 1 && z.r.q.jb.WkStat == 1 && z.r.dp.EntId == entId) //&& z.r.q.jb.WkStat == 1 &&  z.r.q.p.gi.CurrStat == 0 
                       .OrderBy(o => o.r.q.p.gi.SrtOrd).ThenBy(m => m.r.q.p.gi.EmpId)
                       .Select((x, i) => new PrSalarySheetView
                       {
                           Index = ++i, 
                           PrcPsvRecId = x.r.q.p.ss.PrcPsvRecId,
                           Department = x.r.dp.NameEng,
                           Emp_Sys_Id = x.r.q.p.gi.EmpSysId,
                           Emp_Id = x.r.q.p.gi.EmpId,
                           Full_Name = x.r.q.p.gi.FullName,
                           Designation = x.ds.NameEng,
                           BasicSalary = x.r.q.p.ss.Basic,
                           HouseRent = x.r.q.p.ss.HrAlwnc,
                           Transport = x.r.q.p.ss.TrnsAlwnc,
                           Position = x.r.q.p.ss.PstnAlwnc,
                           ProjectAllowance = x.r.q.p.ss.ProjAlwnc,
                           Incentives = x.r.q.p.ss.IncntvAmt,
                           Mobile = x.r.q.p.ss.MobAlwnc,
                           Overtime = x.r.q.p.ss.OtAmt,
                           MedicalAllowance = x.r.q.p.ss.MedAlwnc,
                           BonusAmt = x.r.q.p.ss.BonusAmt,
                           OthrAlwnc = x.r.q.p.ss.OthrAlwnc,
                           OthrPmt = x.r.q.p.ss.OthrPmt,
                           AirTcktFee = x.r.q.p.ss.AirTcktFee,
                           AnlLvEnchmnt = x.r.q.p.ss.AnlLvEnchmnt,
                           GrossSalary = x.r.q.p.ss.GrsSlry,
                           SalaryAdvance = x.r.q.p.ss.SlryAdv,
                           StaffLoans = x.r.q.p.ss.StfLnPmt,
                           NoPay = x.r.q.p.ss.NoPayAmt,
                           OthrRcvryAmt = x.r.q.p.ss.OthrRcvry,
                           TotalDeductions = x.r.q.p.ss.TotDctAmt,
                           NtSlry = x.r.q.p.ss.NtSlry,
                           PmtMod = x.r.q.p.ss.PmtMod,
                           PmtModTxt = (x.r.q.p.ss.PmtMod == 0) ? "Cash" : "Bank",
                           BnkAmt = (x.r.q.p.ss.PmtMod == 1) ? x.r.q.p.ss.NtSlry : 0,
                           CshAmt = (x.r.q.p.ss.PmtMod == 0) ? x.r.q.p.ss.NtSlry : 0,
                           RecStat = x.r.q.p.ss.RecStat,
                           BnkAcct = x.r.q.p.ss.BnkAcct,
                           PrcId = x.r.q.p.ss.PrcId,
                           PrcLogId = x.r.q.p.ss.PrcLogId,
                           Remarks = x.r.q.p.ss.Rmks,
                           YrMntTxt = strYrMnt,
                           PrntTime = prntTime

                       });

                return x;
            }
            catch
            {
                List<PrSalarySheetView> temp = new List<PrSalarySheetView>
                {
                    new PrSalarySheetView {
                    Index = 0,
                    PrcPsvRecId = 0,
                    Department = "No Records Found",
                    Emp_Sys_Id = 0,
                    Emp_Id = "",
                    Full_Name = "No Records Found",
                    Designation = "",
                    BasicSalary = 0,
                    HouseRent = 0,
                    Transport = 0,
                    Position = 0,
                    ProjectAllowance = 0,
                    Incentives = 0,
                    Mobile = 0,
                    Overtime = 0,
                    MedicalAllowance = 0,
                    BonusAmt = 0,
                    OthrAlwnc = 0,
                    OthrPmt = 0,
                    GrossSalary = 0,
                    SalaryAdvance = 0,
                    StaffLoans = 0,
                    NoPay = 0,
                    OthrRcvryAmt = 0,
                    TotalDeductions = 0,
                    NtSlry = 0,
                    PmtMod = 0,
                    PmtModTxt = "",
                    BnkAmt = 0,
                    CshAmt = 0,
                    RecStat = 1,
                    BnkAcct = "",
                    PrcId = 0,
                    PrcLogId = 0
                    }

                };
                return temp;
            }
        }

        public IEnumerable<PrSlryShtVw> GetSlryDtl(int entId, string field, int prcId, int eftMnt, int eftYr, int sctn)
        {
            try
            {
                string sp = "[PR_SlryPrc_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };

                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                       .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                       .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                       .Join(_hrmContext.OrgLocation, s => s.r.q.p.gi.LocId, lc => lc.LocId, (s, lc) => new { s, lc })
                       .Where(z => z.s.r.q.p.ss.RecStat == 1 && z.s.r.q.jb.WkStat == 1 && z.s.r.dp.EntId == entId && ((sctn == 1) ? (z.s.r.q.p.gi.WrktypId == 2) : (z.s.r.q.p.gi.WrktypId == 1)))
                       .OrderBy(o => o.s.r.q.p.gi.SrtOrd).ThenBy(m => m.s.r.q.p.gi.EmpId)
                       .Select((x, i) => new PrSlryShtVw
                       {
                           Index = i + 1,
                           EmpSysId = x.s.r.q.p.gi.EmpSysId,
                           EmpId = x.s.r.q.p.gi.EmpId,
                           FullName = x.s.r.q.p.gi.FullName,
                           DeptName = x.s.r.dp.SrtOrd.ToString("00") + " - " + x.s.r.dp.NameEng,
                           DesigName = x.s.ds.NameEng,
                           InHouse = (x.s.r.q.p.gi.WrktypId == 1),
                           Basic = x.s.r.q.p.ss.Basic,
                           HrAlwnc = x.s.r.q.p.ss.HrAlwnc,
                           TrnsAlwnc = x.s.r.q.p.ss.TrnsAlwnc,
                           PstnAlwnc = x.s.r.q.p.ss.PstnAlwnc,
                           ProjAlwnc = x.s.r.q.p.ss.ProjAlwnc,
                           IncntvAmt = x.s.r.q.p.ss.IncntvAmt,
                           MobAlwnc = x.s.r.q.p.ss.MobAlwnc,
                           OtAmt = x.s.r.q.p.ss.OtAmt,
                           MedAlwnc = x.s.r.q.p.ss.MedAlwnc,
                           BonusAmt = x.s.r.q.p.ss.BonusAmt,
                           OthrAlwnc = x.s.r.q.p.ss.OthrAlwnc,
                           OthrPmt = x.s.r.q.p.ss.OthrPmt,
                           GrsSlry = x.s.r.q.p.ss.GrsSlry,
                           SlryAdv = x.s.r.q.p.ss.SlryAdv,
                           StfLnPmt = x.s.r.q.p.ss.StfLnPmt,
                           NoPayAmt = x.s.r.q.p.ss.NoPayAmt,
                           OthrRcvryAmt = x.s.r.q.p.ss.OthrRcvry,
                           TotDctAmt = x.s.r.q.p.ss.TotDctAmt,
                           NtSlry = x.s.r.q.p.ss.NtSlry,
                           PmtMod = x.s.r.q.p.ss.PmtMod,
                           PmtModTxt = (x.s.r.q.p.ss.PmtMod == 0) ? "Cash" : "Bank",
                           BnkAmt = (x.s.r.q.p.ss.PmtMod == 1) ? x.s.r.q.p.ss.NtSlry : 0,
                           CshAmt = (x.s.r.q.p.ss.PmtMod == 0) ? x.s.r.q.p.ss.NtSlry : 0,
                           RecStat = x.s.r.q.p.ss.RecStat,
                           BnkAcct = x.s.r.q.p.ss.BnkAcct,
                           WkDayCnt = x.s.r.q.p.ss.WkDayCnt,
                           AbFct = x.s.r.q.p.ss.AbFct,
                           PrcId = x.s.r.q.p.ss.PrcId,
                           PrcLogId = x.s.r.q.p.ss.PrcLogId,
                           Rmks = x.s.r.q.p.ss.Rmks
                       });
                return x;
            }
            catch
            {
                List<PrSlryShtVw> tmpPrSlryShtVw = new List<PrSlryShtVw>()
                {
                    new PrSlryShtVw{
                        Index = 0,
                        EmpSysId = 0,
                        EmpId = "",
                        FullName = "No Records Found",
                        DeptName = "No Records Found",
                        DesigName = "No Records Found",
                        Basic = 0,
                        HrAlwnc = 0,
                        TrnsAlwnc = 0,
                        PstnAlwnc = 0,
                        ProjAlwnc = 0,
                        IncntvAmt = 0,
                        MobAlwnc = 0,
                        OtAmt =0,
                        MedAlwnc = 0,
                        BonusAmt = 0,
                        OthrAlwnc = 0,
                        OthrPmt = 0,
                        GrsSlry = 0,
                        SlryAdv = 0,
                        StfLnPmt = 0,
                        NoPayAmt = 0,
                        OthrRcvryAmt = 0,
                        TotDctAmt = 0,
                        NtSlry = 0,
                        PmtMod = 0,
                        PmtModTxt = "",
                        BnkAmt = 0,
                        CshAmt = 0,
                        RecStat = 1,
                        BnkAcct = "",
                        WkDayCnt = 0,
                        AbFct = 0,
                        PrcId = 1,
                        PrcLogId = 0,
                        Rmks = ""
                    }
                };
                return tmpPrSlryShtVw;
            }
        }

        public IEnumerable<HrPrSlrySndEmailvView> GetSlrySlipSndMail(string field, int prcId, int eftMnt, int eftYr)
        {
            try
            {
                string sp = "[PR_SlrySlip_SndMail] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };
                var x = _hrmContext.HrPrSlrySlipSndMailVw.FromSqlRaw(sp, param).ToList()
                    .Select((x, i) => new HrPrSlrySndEmailvView
                    {
                        Index = i + 1,
                        EmpSysId = x.EmpSysId,
                        EmpId = x.EmpId,
                        PstName = x.PstName,
                        IsDlvrd = x.IsDlvrd,
                    });
                return x;
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
       }

        public IEnumerable<PrSlryShtVw> GetSlryDtl(int entId, string field, int prcId, int eftMnt, int eftYr)
        {
            try
            {
                string sp = "[PR_SlryPrc_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };

                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                       .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                       .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                       .Join(_hrmContext.OrgLocation, s => s.r.q.p.gi.LocId, lc => lc.LocId, (s, lc) => new { s, lc })
                       .Where(z => z.s.r.q.p.ss.RecStat == 1 && z.s.r.q.jb.WkStat == 1 && z.s.r.dp.EntId == entId) //&& z.r.q.jb.WkStat == 1 &&  z.r.q.p.gi.CurrStat == 0 
                       .OrderBy(o => o.s.r.q.p.gi.SrtOrd).ThenBy(m => m.s.r.q.p.gi.EmpId)
                       .Select((x, i) => new PrSlryShtVw
                       {
                           Index = i + 1,
                           EmpSysId = x.s.r.q.p.gi.EmpSysId,
                           EmpId = x.s.r.q.p.gi.EmpId,
                           FullName = x.s.r.q.p.gi.FullName,
                           DeptName = x.s.r.dp.NameEng,
                           DesigName = x.s.ds.NameEng,
                           InHouse = (x.s.r.q.p.gi.WrktypId == 1),
                           Basic = x.s.r.q.p.ss.Basic,
                           HrAlwnc = x.s.r.q.p.ss.HrAlwnc,
                           TrnsAlwnc = x.s.r.q.p.ss.TrnsAlwnc,
                           PstnAlwnc = x.s.r.q.p.ss.PstnAlwnc,
                           ProjAlwnc = x.s.r.q.p.ss.ProjAlwnc,
                           IncntvAmt = x.s.r.q.p.ss.IncntvAmt,
                           MobAlwnc = x.s.r.q.p.ss.MobAlwnc,
                           OtAmt = x.s.r.q.p.ss.OtAmt,
                           MedAlwnc = x.s.r.q.p.ss.MedAlwnc,
                           BonusAmt = x.s.r.q.p.ss.BonusAmt,
                           OthrAlwnc = x.s.r.q.p.ss.OthrAlwnc,
                           OthrPmt = x.s.r.q.p.ss.OthrPmt,
                           GrsSlry = x.s.r.q.p.ss.GrsSlry,
                           SlryAdv = x.s.r.q.p.ss.SlryAdv,
                           StfLnPmt = x.s.r.q.p.ss.StfLnPmt,
                           NoPayAmt = x.s.r.q.p.ss.NoPayAmt,
                           OthrRcvryAmt = x.s.r.q.p.ss.OthrRcvry,
                           TotDctAmt = x.s.r.q.p.ss.TotDctAmt,
                           NtSlry = x.s.r.q.p.ss.NtSlry,
                           PmtMod = x.s.r.q.p.ss.PmtMod,
                           PmtModTxt = (x.s.r.q.p.ss.PmtMod == 0) ? "Cash" : "Bank",
                           BnkAmt = (x.s.r.q.p.ss.PmtMod == 1) ? x.s.r.q.p.ss.NtSlry : 0,
                           CshAmt = (x.s.r.q.p.ss.PmtMod == 0) ? x.s.r.q.p.ss.NtSlry : 0,
                           RecStat = x.s.r.q.p.ss.RecStat,
                           BnkAcct = x.s.r.q.p.ss.BnkAcct,
                           WkDayCnt = x.s.r.q.p.ss.WkDayCnt,
                           AbFct = x.s.r.q.p.ss.AbFct,
                           PrcId = x.s.r.q.p.ss.PrcId,
                           PrcLogId = x.s.r.q.p.ss.PrcLogId,
                           Rmks = x.s.r.q.p.ss.Rmks
                       });
                return x;
            }
            catch
            {
                List<PrSlryShtVw> tmpPrSlryShtVw = new List<PrSlryShtVw>()
                {
                    new PrSlryShtVw{
                        Index = 0,
                        EmpSysId = 0,
                        EmpId = "",
                        FullName = "No Records Found",
                        DeptName = "No Records Found",
                        DesigName = "No Records Found",
                        Basic = 0,
                        HrAlwnc = 0,
                        TrnsAlwnc = 0,
                        PstnAlwnc = 0,
                        ProjAlwnc = 0,
                        IncntvAmt = 0,
                        MobAlwnc = 0,
                        OtAmt =0,
                        MedAlwnc = 0,
                        BonusAmt = 0,
                        OthrAlwnc = 0,
                        OthrPmt = 0,
                        GrsSlry = 0,
                        SlryAdv = 0,
                        StfLnPmt = 0,
                        NoPayAmt = 0,
                        OthrRcvryAmt = 0,
                        TotDctAmt = 0,
                        NtSlry = 0,
                        PmtMod = 0,
                        PmtModTxt = "",
                        BnkAmt = 0,
                        CshAmt = 0,
                        RecStat = 1,
                        BnkAcct = "",
                        WkDayCnt = 0,
                        AbFct = 0,
                        PrcId = 1,
                        PrcLogId = 0,
                        Rmks = ""
                    }
                };
                return tmpPrSlryShtVw;
            }
        }
        #endregion

        #region ==============Salary Process Excluded ==============   
        public partial class HrPrSlryPrcExcldVw
        {
            public int ExcldId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string FullName { get; set; }
            public int StYear { get; set; }
            public int StMonth { get; set; }
            public string StMnthName { get; set; }
            public int NoMnths { get; set; }
            public string Note { get; set; }
            public short RecStat { get; set; }
            public string RecStatTxt { get; set; }
            public int CrtBy { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public IEnumerable<HrPrSlryPrcExcldVw> GetAllHrPrSlryPrcExcldVwByEntId(int entId)
        {
            try
            {
                var lstSlryPrcHdr = this.GetLstSlryPrcHdr(entId);
                var curSlryMnt = lstSlryPrcHdr != null ? new DateTime(lstSlryPrcHdr.EftYear, lstSlryPrcHdr.EftMonth, 01) : DateTime.Today;

                if (lstSlryPrcHdr != null && lstSlryPrcHdr.PrcStat == 3)
                {
                    curSlryMnt = curSlryMnt.AddMonths(1);
                }

                var months = _sysListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS)
                    .ToDictionary(m => (int)m.Indx, m => m.ElmntTxt);

                var query = (from ex in _hrmContext.HrPrSlryPrcExcld
                             join gi in _hrmContext.HrEmpGenInfo on ex.EmpSysId equals gi.EmpSysId
                             join jb in _hrmContext.HrEmpJobAssign on ex.EmpSysId equals jb.EmpSysId
                             join dp in _hrmContext.OrgDepartment on jb.DeptId equals dp.DeptId
                             where ex.RecStat == 1 
                                && (gi.CurrStat == 0 || gi.SrvEndDate >= curSlryMnt) 
                                && dp.EntId == entId 
                                && jb.WkStat == 1
                             select new
                             {
                                 ex.ExcldId,
                                 ex.EmpSysId,
                                 gi.EmpId,
                                 gi.FullName,
                                 ex.StYear,
                                 ex.StMonth,
                                 ex.NoMnths,
                                 ex.Note,
                                 ex.RecStat,
                                 ex.CrtBy,
                                 ex.CrtDateTime
                             }).ToList();

                var x = query.Select(item => new HrPrSlryPrcExcldVw
                {
                    ExcldId = item.ExcldId,
                    EmpSysId = item.EmpSysId,
                    EmpId = item.EmpId,
                    FullName = item.FullName,
                    StYear = item.StYear,
                    StMonth = item.StMonth,
                    StMnthName = months.ContainsKey(item.StMonth) ? months[item.StMonth] : new DateTime(2020, item.StMonth, 1).ToString("MMMM"),
                    NoMnths = item.NoMnths,
                    Note = item.Note,
                    RecStat = item.RecStat,
                    CrtBy = item.CrtBy,
                    CrtDateTime = item.CrtDateTime
                }).OrderByDescending(o => o.StYear).ThenByDescending(o => o.StMonth).ToList();

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public HrPrSlryPrcExcld GetHrPrSlryPrcExcldById(int excldId)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcExcld.Where(x => x.ExcldId == excldId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; } 
        }

        public HrPrSlryPrcExcld GetHrPrSlryPrcExcldByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrPrSlryPrcExcld.Where(x => x.EmpSysId == empSysId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Leave Salary Encashment Process==============
        public partial class HrPrLvSlryVw
        {
            public int LvEncshId { get; set; }
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpName { get; set; }
            public string DptName { get; set; }
            public string DsgName { get; set; }
            public int RefId { get; set; }
            public string RefTyp { get; set; }
            public bool IncldLstMnthSlry { get; set; }
            public bool IncldPrjAlwnc { get; set; }
            public bool IncldTktFee { get; set; }
            public DateTime JndDt { get; set; }
            public string JndDtTxt { get; set; }
            public DateTime RjndDt { get; set; }
            public string Elgblty { get; set; }
            public string RjndDtTxt { get; set; }
            public string FrmDt { get; set; }
            public DateTime ToDt { get; set; }
            public int StatIndx { get; set; }
            public string StatTxt { get; set; }
            public string Rmks { get; set; }
            public int PlcyId { get; set; }
            public string LvPlcyName { get; set; }
            public int EftWrkDys { get; set; }
            public int NpCnt { get; set; }
            public int WrkDys { get; set; }
            public int ErndLvCnt { get; set; }
            public decimal GrsSlry { get; set; }
            public decimal PrjAlwnc { get; set; }
            public decimal PdySlry { get; set; }
            public decimal LvSlry { get; set; }
            public decimal LstMntSlry { get; set; }
            public decimal TktFee { get; set; }
            public decimal TtlAmt { get; set; }
            public short RecStat { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public partial class LvSlryEmpVw
        {
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string EmpName { get; set; }
            public string JndDtTxt { get; set; }
            public string RjndDtTxt { get; set; }
            public string LvPlcyName { get; set; }
            public int EftWrkgDys { get; set; }
            public string GrsSlryTxt { get; set; }
            public string PrjAlwncTxt { get; set; }
        }

        public partial class LstMntSlryVw
        {
            [Key]
            public int EmpSysId { get; set; }
            public decimal Basic { get; set; }
            public decimal FxdSlry { get; set; }
            public decimal IncntvAmt { get; set; }
            public decimal OthrPmt { get; set; }
            public decimal OthrRcvry { get; set; }
            public decimal OtAmt { get; set; }
            public decimal WkDayCnt { get; set; }
            public decimal NoPay { get; set; }
            public decimal AtnPmt { get; set; }
        }

        public partial class LvSlryClcRsltVw
        {
            [Key]
            public int EmpSysId { get; set; }
            public string Elgbl { get; set; }
            public int WrkDys { get; set; }
            public decimal NpCnt { get; set; }
            public int ErndLvCnt { get; set; }
            public decimal GrsSlry { get; set; }
            public decimal PdySlry { get; set; }
            public decimal LvSlry { get; set; }
            public decimal LstMntSlry { get; set; }
            public decimal TktFee { get; set; }

            [NotMapped]
            public decimal TtlAmt { get { return LvSlry + LstMntSlry + TktFee; } }
        }

        public IEnumerable<HrPrLvSlryVw> GetHrPrLvSlryVw(int entId)
        {
            try
            {
                IEnumerable<ListElement> stsLst = _sysListContext.GetElements(SysSelectListContext.SelectListTypeEnum.REC_STAT);

                var x = _hrmContext.HrPrLvEncsh
                        .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, le => le.EmpSysId, (le, gi) => new { le, gi })
                        .Join(_hrmContext.HrEmpJobAssign, p => p.le.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                        .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                        .Join(stsLst, s => s.q.p.le.StatIndx, sl => sl.Indx, (s, sl) => new { s, sl })
                        .Where(z => z.s.q.jb.WkStat == 1 && z.s.dp.EntId == entId)
                        .Select(x => new HrPrLvSlryVw {
                            LvEncshId = x.s.q.p.le.LvEncshId,
                            EmpSysId = x.s.q.p.le.EmpSysId,
                            EmpId = x.s.q.p.gi.EmpId,
                            EmpName = x.s.q.p.gi.FirstName + " " + x.s.q.p.gi.LastName,
                            DptName = x.s.dp.DescEng,
                            DsgName = "",
                            RefId = x.s.q.p.le.RefId,
                            RefTyp = x.s.q.p.le.RefTyp,
                            IncldLstMnthSlry = x.s.q.p.le.IncldLstMnthSlry,
                            IncldPrjAlwnc = x.s.q.p.le.IncldPrjAlwnc,
                            IncldTktFee = x.s.q.p.le.IncldTktFee,
                            FrmDt = x.s.q.p.le.FrmDt.ToString("yyyy-mm-dd") + " (" + x.s.q.p.le.FrmDtTyp + ")",
                            ToDt = x.s.q.p.le.ToDt,
                            Elgblty = "Yes",
                            StatIndx = x.s.q.p.le.StatIndx,
                            StatTxt = x.sl.ElmntTxt,
                            Rmks = x.s.q.p.le.Rmks,
                            PlcyId = 0,
                            LvPlcyName = string.Empty,
                            EftWrkDys = x.s.q.p.le.EftWrkDys,
                            NpCnt = x.s.q.p.le.NpCnt,
                            ErndLvCnt = x.s.q.p.le.ErndLvCnt,
                            GrsSlry = x.s.q.p.le.GrsSlry,
                            LvSlry = x.s.q.p.le.LvSlry,
                            LstMntSlry = x.s.q.p.le.LstMntSlry,
                            TktFee = 0, //TODO: Get total ticket fee form the table
                            TtlAmt = x.s.q.p.le.LvSlry + x.s.q.p.le.LstMntSlry + 0,
                            RecStat = x.s.q.p.le.RecStat,
                            CrtDateTime = x.s.q.p.le.CrtDateTime
                        });
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public LvSlryEmpVw GetEmpInfoForLvSlry(int empSysId)
        {
            try
            {
                var lv = _hrmContext.HrLvRegister.Where(x => x.EmpSysId == empSysId && x.RecStat == 1 && x.RjnDate != null).OrderByDescending(o => o.RjnDate).FirstOrDefault();
                var slry = this.GetHrPrEmpSlryStrucByEmpSysId(empSysId);

                var z = from em in _hrmContext.HrEmpGenInfo
                        join jb in _hrmContext.HrEmpJobAssign
                            on em.EmpSysId equals jb.EmpSysId
                        where em.EmpSysId == empSysId
                        select new LvSlryEmpVw
                        {
                            EmpSysId = em.EmpSysId,
                            EmpId = em.EmpId,
                            EmpName = em.FirstName + " " + em.LastName,
                            JndDtTxt = em.JoinedDate.ToString("dd/MM/yyyy"),
                            RjndDtTxt = lv == null? "-" : ((DateTime)lv.RjnDate).ToString("dd/MM/yyyy"),
                            LvPlcyName = string.Empty,
                            EftWrkgDys = 365,
                            GrsSlryTxt = ((slry.Basic ?? 0) + (slry.HrAlwnc ?? 0) + (slry.TrnsAlwnc ?? 0) + (slry.Tra ?? 0) + (slry.MedAlwnc ?? 0) + (slry.MobAlwnc ?? 0) + (slry.OthrAlwnc ?? 0) + (slry.PrfmAlwnc ?? 0) + (slry.PstnAlwnc ?? 0)).ToString("0.00"),
                            PrjAlwncTxt = (slry.ProjAlwnc ?? 0).ToString("0.00")
                        };
                return z.FirstOrDefault();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public LvSlryClcRsltVw GetLvSlryClcRslt(string prcType, int empSysId, DateTime frmDt, DateTime toDt, int lvTypId, bool lstMnthSlry, bool prjAlwnc, bool tktFee, decimal tktFeeVal)
        {
            try
            {
                string sp = "[PR_LvEncshCalc] {0},{1},{2},{3},{4},{5},{6}";
                object[] param = { frmDt, prcType, empSysId, lvTypId, prjAlwnc, lstMnthSlry, tktFee };
                var lvSlryClcRsltVw = _hrmContext.LvSlryClcRsltVw.FromSqlRaw(sp, param).ToList().FirstOrDefault();
                if (tktFee)
                    lvSlryClcRsltVw.TktFee = tktFeeVal;
                else
                    lvSlryClcRsltVw.TktFee = 0;

                if (lstMnthSlry)
                {
                    string sp2 = "[PR_LvMntSlry] {0},{1},{2},{3},{4}";
                    object[] param2 = { frmDt, prcType, empSysId, frmDt.Year, frmDt.Month };
                    var x2 = _hrmContext.LstMntSlryVw.FromSqlRaw(sp2, param2).ToList().First();
                    lvSlryClcRsltVw.LstMntSlry = x2.FxdSlry + x2.IncntvAmt + x2.OthrPmt + x2.AtnPmt + x2.OtAmt - (x2.NoPay + x2.OthrRcvry);
                }
                else
                    lvSlryClcRsltVw.LstMntSlry = 0;

                return lvSlryClcRsltVw;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============WPS ==============
        public partial class HrPrSlryWPSVw
        {
   
            public string QID { get; set; }
            public string VisaId { get; set; }
            public string AcctName { get; set; }
            public string BnkShrtNme { get; set; }
            public string BnkAcctNo { get; set; }
            public decimal NetSlry { get; set; }
            public decimal BscSlry { get; set; }
            public int ExtrHrs  { get; set; }
            public decimal Incm { get; set; }
            public decimal Deduct { get; set; }
            public string PayTyp { get; set; }
            public string Cmnt { get; set; }            
        }

        public IEnumerable<HrPrSlryWPSVw> GetWPSFile(string field,
                                                     int prcId,
                                                     int eftMnt,
                                                     int eftYr)
        {
            try
            {
                string sp = "[PR_SlryPrc_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };
                

                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpBankAcct, p => p.ss.EmpSysId, ac => ac.EmpSysId, (p, ac) => new { p, ac })
                       .Select(x => new HrPrSlryWPSVw
                       {
                           QID = x.p.gi.PmtId,
                           VisaId = "",
                           AcctName = x.ac.AcctName,
                           BnkShrtNme = x.ac.ShtBankName,
                           BnkAcctNo = x.ac.AcctNo,
                           NetSlry = x.p.ss.NtSlry,
                           BscSlry = x.p.ss.Basic,
                           ExtrHrs = 0,
                           Incm = (x.p.ss.HrAlwnc + x.p.ss.TrnsAlwnc + x.p.ss.Tra + x.p.ss.PstnAlwnc + x.p.ss.MedAlwnc + x.p.ss.MobAlwnc + x.p.ss.ProjAlwnc + x.p.ss.SpcAlwnc + x.p.ss.IncntvAmt + x.p.ss.PrfmAlwnc + x.p.ss.OthrAlwnc + x.p.ss.OthrPmt + x.p.ss.OtAmt + x.p.ss.BonusAmt),
                           Deduct = (x.p.ss.SlryAdv + x.p.ss.StfLnPmt + x.p.ss.NoPayAmt + x.p.ss.OthrRcvry),
                           PayTyp = x.p.ss.PmtMod.ToString(),
                           Cmnt = ""
                       });
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        #endregion WPS

        #region  ==============SalarySheet ==============
        Color gray = Color.FromArgb(255, 224, 224, 224);
        Color black = Color.FromArgb(255, 0, 0, 0);
        Color white = Color.FromArgb(255, 255, 255, 255);
        Color violet = Color.FromArgb(255, 151, 108, 174);

        public string GetSalarySheet(int eftYr, int eftMnt, int entId)
        {
            try
            {
                
                var _filename = "";
                var _filepath = "";

                PdfDocument doc = new PdfDocument();
                PdfStandardFont _headerTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Regular);
                PdfStandardFont _smallTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
                PdfStandardFont _medinumTxt12 = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Regular);
                PdfStandardFont _medinumTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Regular);
                PdfStandardFont _largeTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Regular);
                PdfStandardFont _largeTxtBld = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);



                PdfGridCellStyle _gridCellSmallTxtStyle = new PdfGridCellStyle();
                _gridCellSmallTxtStyle.Font = _smallTxt;

                PdfGridCellStyle _gridCellMediumTxtStyle = new PdfGridCellStyle();
                _gridCellSmallTxtStyle.Font = _medinumTxt;

                PdfGridCellStyle _gridCellLargeTxtStyle = new PdfGridCellStyle();
                _gridCellLargeTxtStyle.Font = _largeTxt;

                PdfGridCellStyle _gridCellColorStyle = new PdfGridCellStyle();
                _gridCellColorStyle.BackgroundBrush = PdfBrushes.LightGray;

                PdfStringFormat _formatRTL = new PdfStringFormat();
                PdfStringFormat _formatLTL = new PdfStringFormat();
                PdfStringFormat _formatMD = new PdfStringFormat();
                PdfStringFormat _formatCustom = new PdfStringFormat();

                PdfStringFormat _formatRTL_H = new PdfStringFormat();
                PdfStringFormat _formatLTL_H = new PdfStringFormat();

                _formatRTL.Alignment = PdfTextAlignment.Right;
                _formatLTL.Alignment = PdfTextAlignment.Left;
                _formatMD.Alignment = PdfTextAlignment.Center;

                _formatCustom.Alignment = PdfTextAlignment.Left;
                _formatCustom.WordWrap = PdfWordWrapType.Word;


                _formatRTL_H.Alignment = PdfTextAlignment.Center;
                _formatRTL_H.LineAlignment = PdfVerticalAlignment.Middle;
                _formatLTL_H.Alignment = PdfTextAlignment.Center;
                _formatLTL_H.LineAlignment = PdfVerticalAlignment.Middle;

                List<PrSalarySheetView> obj = GetSalaryDtl(entId, "_paysheet", 0, eftMnt, eftYr).ToList();

                if (obj.Count() > 0)
                {

                    doc.PageSettings.Size = PdfPageSize.A2;
                    doc.PageSettings.Orientation = PdfPageOrientation.Landscape;
                    PdfPage page = doc.Pages.Add();

                    PdfGrid pdfGrid = new PdfGrid();
                    PdfGraphics gphc = page.Graphics;
                    
                    var _logopath = _host.WebRootPath + "\\images\\qcs.png";
                    var _rubbersealpath = _host.WebRootPath + "\\images\\rubbrStmp.png";


                    //Header

                    DateTime SalryDate = new DateTime(eftYr, eftMnt, 1);
                    gphc.DrawRectangle(new PdfSolidBrush(gray), new Syncfusion.Drawing.RectangleF(0, 0, page.Graphics.ClientSize.Width, 40));
                    gphc.DrawString("Salary Sheet", _largeTxt, new PdfSolidBrush(black), new Syncfusion.Drawing.PointF(750, 15));
                    gphc.DrawString(new DateTime(eftYr, eftMnt, 1).ToString("MMMM", CultureInfo.InvariantCulture) + "  " + eftYr, _largeTxt, new PdfSolidBrush(black), new Syncfusion.Drawing.PointF(1500, 15));

                    //Logo
                    FileStream imageStream = new FileStream(_logopath, FileMode.Open, FileAccess.Read);
                    PdfImage _logo = PdfImage.FromStream(imageStream);
                    page.Graphics.DrawImage(_logo, 10, 10, 70, 25); //QCS Logo
                                        
                    MemoryStream stream = new MemoryStream();
                    //List
                    //gphc.DrawString("Processing", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(10, 60));
                    //gphc.DrawString(":", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(75, 60));
                    //gphc.DrawString("Yes", _largeTxt, PdfBrushes.Green, new Syncfusion.Drawing.PointF(90, 60));

                    //gphc.DrawString("Verified", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(10, 75));
                    //gphc.DrawString(":", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(75, 75));
                    //gphc.DrawString("No", _largeTxt, PdfBrushes.Red, new Syncfusion.Drawing.PointF(90, 75));

                    //gphc.DrawString("Completed", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(10, 90));
                    //gphc.DrawString(":", _largeTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(75, 90));
                    //gphc.DrawString("No", _largeTxt, PdfBrushes.Red, new Syncfusion.Drawing.PointF(90, 90));


                    int x = 1;
                    decimal _basic = 0;
                    decimal _housing = 0;
                    decimal _transport = 0;
                    decimal _position = 0;
                    decimal _projAlwnc = 0;
                    decimal _incntvs = 0;
                    decimal _mobile = 0;
                    decimal _overtime = 0;
                    decimal _othrAlwnc = 0;
                    decimal _medical = 0;
                    //decimal _otherAlwnc = 0;
                    decimal _otherPmt = 0;
                    decimal _gross = 0;
                    decimal _slryAdv = 0;
                    decimal _stfLn = 0;
                    decimal _noPay = 0;
                    decimal _othrDedctn = 0;
                    decimal _totDedctn = 0;
                    decimal _netSlry = 0;

                    float columnWidthA = 35;
                    float columnWidthB = 50;
                    float columnWidthC = 150;
                    float columnWidthD = 140;
                    float columnWidthE = 60;


                    //Add Columns.
                    pdfGrid.Columns.Add(23);

                    pdfGrid.Columns[0].Width = columnWidthA;
                    pdfGrid.Columns[1].Width = columnWidthB;
                    pdfGrid.Columns[2].Width = columnWidthD;
                    pdfGrid.Columns[3].Width = columnWidthD;
                    pdfGrid.Columns[4].Width = columnWidthE;
                    pdfGrid.Columns[5].Width = columnWidthE;
                    pdfGrid.Columns[6].Width = columnWidthE;
                    pdfGrid.Columns[7].Width = columnWidthE;
                    pdfGrid.Columns[8].Width = columnWidthE;
                    pdfGrid.Columns[9].Width = columnWidthE;
                    pdfGrid.Columns[10].Width = columnWidthE;
                    pdfGrid.Columns[11].Width = columnWidthE;
                    pdfGrid.Columns[12].Width = columnWidthE;
                    pdfGrid.Columns[13].Width = columnWidthE;
                    pdfGrid.Columns[14].Width = columnWidthE;
                    pdfGrid.Columns[15].Width = columnWidthE;
                    pdfGrid.Columns[16].Width = columnWidthE;
                    pdfGrid.Columns[17].Width = columnWidthE;
                    pdfGrid.Columns[18].Width = columnWidthE;
                    pdfGrid.Columns[19].Width = columnWidthE;
                    pdfGrid.Columns[20].Width = columnWidthE;
                    pdfGrid.Columns[21].Width = columnWidthE;
                    pdfGrid.Columns[22].Width = columnWidthC;


                    //Add RowSpan
                    PdfGridRow pdfGridHeader1 = pdfGrid.Rows.Add();
                    pdfGridHeader1.Cells[0].Value = "";
                    pdfGridHeader1.Cells[1].Value = "";
                    pdfGridHeader1.Cells[2].Value = "";
                    pdfGridHeader1.Cells[3].Value = "";
                    pdfGridHeader1.Cells[4].Value = "";
                    pdfGridHeader1.Cells[5].Value = "";
                    pdfGridHeader1.Cells[6].Value = "";
                    pdfGridHeader1.Cells[7].Value = "";
                    pdfGridHeader1.Cells[8].Value = "";
                    pdfGridHeader1.Cells[9].Value = "";
                    pdfGridHeader1.Cells[10].Value = "";
                    pdfGridHeader1.Cells[11].Value = "";
                    pdfGridHeader1.Cells[12].Value = "";
                    pdfGridHeader1.Cells[13].Value = "";
                    pdfGridHeader1.Cells[14].Value = "";
                    pdfGridHeader1.Cells[15].Value = "";
                    pdfGridHeader1.Cells[16].Value = "";
                    pdfGridHeader1.Cells[17].Value = "";
                    pdfGridHeader1.Cells[18].Value = "";
                    pdfGridHeader1.Cells[19].Value = "";
                    pdfGridHeader1.Cells[20].Value = "";
                    pdfGridHeader1.Cells[21].Value = "";
                    pdfGridHeader1.Cells[22].Value = "";



                    //Add Header
                    PdfGridRow pdfGridHeader2 = pdfGrid.Rows.Add();
                    pdfGridHeader2.Style.Font = _largeTxt;
                    pdfGridHeader2.Cells[0].Value = " SI No.";                    
                    pdfGridHeader2.Cells[1].Value = "Emp Id.";
                    pdfGridHeader2.Cells[2].Value = "Name";
                    pdfGridHeader2.Cells[3].Value = "Designation";
                    pdfGridHeader2.Cells[4].Value = "Basic";
                    pdfGridHeader2.Cells[5].Value = "HRA";
                    pdfGridHeader2.Cells[6].Value = "TRA";
                    pdfGridHeader2.Cells[7].Value = "Position";
                    pdfGridHeader2.Cells[8].Value = "Project";
                    pdfGridHeader2.Cells[9].Value = "Mobile"; 
                    pdfGridHeader2.Cells[10].Value = "Medical";
                    pdfGridHeader2.Cells[11].Value = "Other"; 
                    pdfGridHeader2.Cells[12].Value = "Incentives";
                    pdfGridHeader2.Cells[13].Value = "Overtime";
                    pdfGridHeader2.Cells[14].Value = "Other Payments";
                    pdfGridHeader2.Cells[15].Value = "Gross Salary";
                    pdfGridHeader2.Cells[16].Value = "Salary Advance";
                    pdfGridHeader2.Cells[17].Value = "Staff Loans";
                    pdfGridHeader2.Cells[18].Value = "No Pay";
                    pdfGridHeader2.Cells[19].Value = "Other";
                    pdfGridHeader2.Cells[20].Value = "Total Deduct.";
                    pdfGridHeader2.Cells[21].Value = "Net Salary";
                    pdfGridHeader2.Cells[22].Value = "Remarks";



                    pdfGridHeader2.Cells[0].StringFormat = _formatLTL; pdfGridHeader2.Cells[1].StringFormat = _formatLTL; pdfGridHeader2.Cells[2].StringFormat = _formatLTL; pdfGridHeader2.Cells[3].StringFormat = _formatLTL; pdfGridHeader2.Cells[4].StringFormat = _formatLTL;
                    pdfGridHeader2.Cells[5].StringFormat = _formatMD; pdfGridHeader2.Cells[6].StringFormat = _formatMD; pdfGridHeader2.Cells[7].StringFormat = _formatMD; pdfGridHeader2.Cells[8].StringFormat = _formatMD; pdfGridHeader2.Cells[9].StringFormat = _formatMD;
                    pdfGridHeader2.Cells[10].StringFormat = _formatMD; pdfGridHeader2.Cells[11].StringFormat = _formatMD; pdfGridHeader2.Cells[12].StringFormat = _formatMD; pdfGridHeader2.Cells[13].StringFormat = _formatMD; pdfGridHeader2.Cells[14].StringFormat = _formatRTL;
                    pdfGridHeader2.Cells[15].StringFormat = _formatRTL; pdfGridHeader2.Cells[16].StringFormat = _formatMD; pdfGridHeader2.Cells[17].StringFormat = _formatMD; pdfGridHeader2.Cells[18].StringFormat = _formatMD; pdfGridHeader2.Cells[19].StringFormat = _formatMD;
                    pdfGridHeader2.Cells[20].StringFormat = _formatMD; pdfGridHeader2.Cells[21].StringFormat = _formatRTL;
                    pdfGridHeader2.Style.Font = _headerTxt;
                    //pdfGridHeader.Style = _gridCellSmallTxtStyle;

                    PdfStringFormat format = new PdfStringFormat();
                    format.WordWrap = PdfWordWrapType.Word;

                    foreach (var itm in obj)
                    {

                        PdfGridRow pdfGridRow1 = pdfGrid.Rows.Add();
                        pdfGridRow1.Cells[0].Value = "  " + x.ToString();
                        pdfGridRow1.Cells[0].Style.Font = _smallTxt;
                        pdfGridRow1.Cells[0].Style.StringFormat = _formatMD;                        
                        pdfGridRow1.Cells[1].Value = itm.Emp_Id;
                        pdfGridRow1.Cells[1].Style.Font = _medinumTxt12;
                        pdfGridRow1.Cells[2].Value = itm.Full_Name;
                        pdfGridRow1.Cells[2].Style.Font = _medinumTxt;
                        pdfGridRow1.Cells[3].Value = itm.Designation;
                        pdfGridRow1.Cells[3].Style.Font = _medinumTxt;
                        pdfGridRow1.Cells[4].Value = String.Format("{0:n0}", itm.BasicSalary);
                        pdfGridRow1.Cells[4].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[5].Value = String.Format("{0:n0}", itm.HouseRent);
                        pdfGridRow1.Cells[5].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[6].Value = String.Format("{0:n0}", itm.Transport);
                        pdfGridRow1.Cells[6].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[7].Value = String.Format("{0:n0}", itm.Position);
                        pdfGridRow1.Cells[7].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[8].Value = String.Format("{0:n0}", itm.ProjectAllowance);
                        pdfGridRow1.Cells[8].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[9].Value = String.Format("{0:n0}", itm.Mobile);
                        pdfGridRow1.Cells[9].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[10].Value = String.Format("{0:n0}", itm.MedicalAllowance);
                        pdfGridRow1.Cells[10].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[11].Value = String.Format("{0:n0}", itm.OthrAlwnc);
                        pdfGridRow1.Cells[11].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[12].Value = String.Format("{0:n0}", itm.Incentives);
                        pdfGridRow1.Cells[12].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[13].Value = String.Format("{0:n0}", itm.Overtime);
                        pdfGridRow1.Cells[13].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[14].Value = String.Format("{0:n0}", itm.OthrPmt);
                        pdfGridRow1.Cells[14].Style.Font = _largeTxt;                        
                        pdfGridRow1.Cells[15].Value = String.Format("{0:n0}", itm.GrossSalary);
                        pdfGridRow1.Cells[15].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[16].Value = String.Format("{0:n0}", itm.SalaryAdvance);
                        pdfGridRow1.Cells[16].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[17].Value = String.Format("{0:n0}", itm.StaffLoans);
                        pdfGridRow1.Cells[17].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[18].Value = String.Format("{0:n0}", itm.NoPay);
                        pdfGridRow1.Cells[18].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[19].Value = String.Format("{0:n0}", itm.OthrRcvryAmt);
                        pdfGridRow1.Cells[19].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[20].Value = String.Format("{0:n0}", itm.TotalDeductions);
                        pdfGridRow1.Cells[20].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[21].Value = String.Format("{0:n0}", itm.NtSlry);
                        pdfGridRow1.Cells[21].Style.Font = _largeTxt;
                        pdfGridRow1.Cells[22].Value = itm.Remarks;
                        pdfGridRow1.Cells[22].Style.Font = _smallTxt;
                        pdfGridRow1.Cells[22].Style.StringFormat = format;


                        _basic += itm.BasicSalary;
                        _housing += itm.HouseRent;
                        _transport += itm.Transport;
                        _position += itm.Position;
                        _projAlwnc += itm.ProjectAllowance;
                        _mobile += itm.Mobile;                        
                        _medical += itm.MedicalAllowance;
                        _othrAlwnc += itm.OthrAlwnc;
                        _incntvs += itm.Incentives;
                        _overtime += itm.Overtime;
                        _otherPmt += itm.OthrPmt;
                        _gross += itm.GrossSalary;
                        _slryAdv += itm.SalaryAdvance;
                        _stfLn += itm.StaffLoans;
                        _noPay += itm.NoPay;
                        _othrDedctn += itm.OthrRcvryAmt;
                        _totDedctn += itm.TotalDeductions;
                        _netSlry += itm.NtSlry;

                        pdfGridRow1.Cells[0].StringFormat = _formatLTL; pdfGridRow1.Cells[1].StringFormat = _formatLTL; pdfGridRow1.Cells[2].StringFormat = _formatLTL; pdfGridRow1.Cells[3].StringFormat = _formatLTL; pdfGridRow1.Cells[4].StringFormat = _formatRTL;
                        pdfGridRow1.Cells[5].StringFormat = _formatRTL; pdfGridRow1.Cells[6].StringFormat = _formatRTL; pdfGridRow1.Cells[7].StringFormat = _formatRTL; pdfGridRow1.Cells[8].StringFormat = _formatRTL; pdfGridRow1.Cells[9].StringFormat = _formatRTL;
                        pdfGridRow1.Cells[10].StringFormat = _formatRTL; pdfGridRow1.Cells[11].StringFormat = _formatRTL; pdfGridRow1.Cells[12].StringFormat = _formatRTL; pdfGridRow1.Cells[13].StringFormat = _formatRTL; pdfGridRow1.Cells[14].StringFormat = _formatRTL;
                        pdfGridRow1.Cells[15].StringFormat = _formatRTL; pdfGridRow1.Cells[16].StringFormat = _formatRTL; pdfGridRow1.Cells[17].StringFormat = _formatRTL; pdfGridRow1.Cells[18].StringFormat = _formatRTL; pdfGridRow1.Cells[19].StringFormat = _formatRTL;
                        pdfGridRow1.Cells[20].StringFormat = _formatRTL; pdfGridRow1.Cells[21].StringFormat = _formatRTL; pdfGridRow1.Cells[22].StringFormat = _formatLTL;
                        //pdfGridRow1.Height = 15;
                        //pdfGridRow1.Style = _gridCellSmallTxtStyle;             


                        FileStream imageRubber = new FileStream(_rubbersealpath, FileMode.Open, FileAccess.Read);
                        PdfImage _seal = PdfImage.FromStream(imageRubber);
                        page.Graphics.DrawImage(_seal, 1475,985,100, 100); //QCS Rubber Seal

                        x += 1;
                    }

                    PdfGridRow pdfGridRowFooter = pdfGrid.Rows.Add();
                    pdfGridRowFooter.Cells[0].Value = "";
                    pdfGridRowFooter.Cells[1].Value = "";
                    pdfGridRowFooter.Cells[2].Value = "";
                    pdfGridRowFooter.Cells[3].Value = "Total";                    
                    pdfGridRowFooter.Cells[4].Value = String.Format("{0:n0}", _basic);
                    pdfGridRowFooter.Cells[5].Value = String.Format("{0:n0}", _housing);
                    pdfGridRowFooter.Cells[6].Value = String.Format("{0:n0}", _transport);
                    pdfGridRowFooter.Cells[7].Value = String.Format("{0:n0}", _position);
                    pdfGridRowFooter.Cells[8].Value = String.Format("{0:n0}", _projAlwnc);
                    pdfGridRowFooter.Cells[9].Value = String.Format("{0:n0}", _mobile); 
                    pdfGridRowFooter.Cells[10].Value = String.Format("{0:n0}", _medical);
                    pdfGridRowFooter.Cells[11].Value = String.Format("{0:n0}", _othrAlwnc);
                    pdfGridRowFooter.Cells[12].Value = String.Format("{0:n0}", _incntvs);
                    pdfGridRowFooter.Cells[13].Value = String.Format("{0:n0}", _overtime);
                    pdfGridRowFooter.Cells[14].Value = String.Format("{0:n0}", _otherPmt);
                    pdfGridRowFooter.Cells[15].Value = String.Format("{0:n0}", _gross);
                    pdfGridRowFooter.Cells[16].Value = String.Format("{0:n0}", _slryAdv);
                    pdfGridRowFooter.Cells[17].Value = String.Format("{0:n0}", _stfLn);
                    pdfGridRowFooter.Cells[18].Value = String.Format("{0:n0}", _noPay);
                    pdfGridRowFooter.Cells[19].Value = String.Format("{0:n0}", _othrDedctn);
                    pdfGridRowFooter.Cells[20].Value = String.Format("{0:n0}", _totDedctn);
                    pdfGridRowFooter.Cells[21].Value = String.Format("{0:n0}", _netSlry);                    


                    pdfGridRowFooter.Cells[0].StringFormat = _formatMD; pdfGridRowFooter.Cells[1].StringFormat = _formatLTL; pdfGridRowFooter.Cells[2].StringFormat = _formatLTL; pdfGridRowFooter.Cells[3].StringFormat = _formatLTL; pdfGridRowFooter.Cells[4].StringFormat = _formatRTL;
                    pdfGridRowFooter.Cells[5].StringFormat = _formatRTL; pdfGridRowFooter.Cells[6].StringFormat = _formatRTL; pdfGridRowFooter.Cells[7].StringFormat = _formatRTL; pdfGridRowFooter.Cells[8].StringFormat = _formatRTL; pdfGridRowFooter.Cells[9].StringFormat = _formatRTL;
                    pdfGridRowFooter.Cells[10].StringFormat = _formatRTL; pdfGridRowFooter.Cells[11].StringFormat = _formatRTL; pdfGridRowFooter.Cells[12].StringFormat = _formatRTL; pdfGridRowFooter.Cells[13].StringFormat = _formatRTL; pdfGridRowFooter.Cells[14].StringFormat = _formatRTL;
                    pdfGridRowFooter.Cells[15].StringFormat = _formatRTL; pdfGridRowFooter.Cells[16].StringFormat = _formatRTL; pdfGridRowFooter.Cells[17].StringFormat = _formatRTL; pdfGridRowFooter.Cells[18].StringFormat = _formatRTL; pdfGridRowFooter.Cells[19].StringFormat = _formatRTL;
                    pdfGridRowFooter.Cells[20].StringFormat = _formatRTL; pdfGridRowFooter.Cells[21].StringFormat = _formatRTL;
                    pdfGridRowFooter.Style = _gridCellColorStyle;
                    pdfGridRowFooter.Style.Font = _largeTxtBld;

                    //---------------------------------------------------
                    PdfGridCell gridRow1 = pdfGrid.Rows[0].Cells[0];
                    gridRow1.RowSpan = 2;
                    gridRow1.StringFormat = _formatLTL_H;
                    gridRow1.Value = "Si No.";
                    gridRow1.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow1.Style.Font = _headerTxt;
                                        

                    PdfGridCell gridRow3 = pdfGrid.Rows[0].Cells[1];
                    gridRow3.RowSpan = 2;
                    gridRow3.StringFormat = _formatLTL_H;
                    gridRow3.Value = "Emp Id.";
                    gridRow3.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow3.Style.Font = _headerTxt;

                    PdfGridCell gridRow4 = pdfGrid.Rows[0].Cells[2];
                    gridRow4.RowSpan = 2;
                    gridRow4.StringFormat = _formatLTL_H;
                    gridRow4.Value = "Name";
                    gridRow4.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow4.Style.Font = _headerTxt;

                    PdfGridCell gridRow5 = pdfGrid.Rows[0].Cells[3];
                    gridRow5.RowSpan = 2;
                    gridRow5.StringFormat = _formatLTL_H;
                    gridRow5.Value = "Designation";
                    gridRow5.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow5.Style.Font = _headerTxt;

                    PdfGridCell gridRow6 = pdfGrid.Rows[0].Cells[4];
                    gridRow6.RowSpan = 2;
                    gridRow6.StringFormat = _formatRTL_H;
                    gridRow6.Value = "Basic";
                    gridRow6.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow6.Style.Font = _headerTxt;


                    PdfGridCell gridRow27 = pdfGrid.Rows[0].Cells[12];
                    gridRow27.RowSpan = 2;
                    gridRow27.StringFormat = _formatRTL_H;
                    gridRow27.Value = "Incentive";
                    gridRow27.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow27.Style.Font = _headerTxt;

                    PdfGridCell gridRow17 = pdfGrid.Rows[0].Cells[13];
                    gridRow17.RowSpan = 2;
                    gridRow17.StringFormat = _formatRTL_H;
                    gridRow17.Value = "Overtime";
                    gridRow17.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow17.Style.Font = _headerTxt;

                    PdfGridCell gridRow7 = pdfGrid.Rows[0].Cells[14];
                    gridRow7.RowSpan = 2;
                    gridRow7.StringFormat = _formatRTL_H;
                    gridRow7.Value = "Other Payments";
                    gridRow7.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow7.Style.Font = _headerTxt;

                    PdfGridCell gridRow8 = pdfGrid.Rows[0].Cells[15];
                    gridRow8.RowSpan = 2;
                    gridRow8.StringFormat = _formatRTL_H;
                    gridRow8.Value = "Gross Salary";
                    gridRow8.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow8.Style.Font = _headerTxt;

                    PdfGridCell gridRow9 = pdfGrid.Rows[0].Cells[21];
                    gridRow9.RowSpan = 2;
                    gridRow9.StringFormat = _formatRTL_H;
                    gridRow9.Value = "Net Salary";
                    gridRow9.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow9.Style.Font = _headerTxt;

                    PdfGridCell gridRow19 = pdfGrid.Rows[0].Cells[22];
                    gridRow19.RowSpan = 2;
                    gridRow19.StringFormat = _formatRTL_H;
                    gridRow19.Value = "Remarks";
                    gridRow19.Style.BackgroundBrush = PdfBrushes.White;
                    gridRow19.Style.Font = _headerTxt;
                    //---------------------------------------------------

                    //---------------------------------------------------
                    PdfGridCell gridColSpan3 = pdfGrid.Rows[0].Cells[5];
                    gridColSpan3.ColumnSpan = 7;
                    gridColSpan3.StringFormat = _formatMD;
                    gridColSpan3.Value = "Allowances";
                    gridColSpan3.Style.BackgroundBrush = PdfBrushes.White;
                    gridColSpan3.Style.Font = _headerTxt;


                    PdfGridCell gridColSpan5 = pdfGrid.Rows[0].Cells[16];
                    gridColSpan5.ColumnSpan = 5;
                    gridColSpan5.StringFormat = _formatMD;
                    gridColSpan5.Value = "Deductions";
                    gridColSpan5.Style.BackgroundBrush = PdfBrushes.White;
                    gridColSpan5.Style.Font = _headerTxt;
                    //---------------------------------------------------


                    //---------------------Footer------------------------
                    RectangleF bounds = new RectangleF(0, 0, doc.Pages[0].GetClientSize().Width, 50);
                    PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);
                    PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
                    PdfFont font2 = new PdfStandardFont(PdfFontFamily.TimesRoman, 10);
                    PdfBrush brush = new PdfSolidBrush(Color.Black);
                    PdfPageNumberField pageNumber = new PdfPageNumberField(font, brush);
                    PdfCompositeField pg = new PdfCompositeField(font, brush, "Pg. {0}", pageNumber);
                    PdfCompositeField prindate = new PdfCompositeField(font, brush, "Date : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"));

                    PdfCompositeField hr = new PdfCompositeField(font, brush, "HR ");
                    PdfCompositeField atant = new PdfCompositeField(font, brush, "Accountant");
                    PdfCompositeField fMgr = new PdfCompositeField(font, brush, "Finance Manager");
                    PdfCompositeField ceo = new PdfCompositeField(font, brush, "Chief Executive Officer");
                    PdfCompositeField md = new PdfCompositeField(font, brush, "Managing Director");
                    PdfCompositeField electronicCopy = new PdfCompositeField(font2, brush, "This is an electronic copy need not to be signed or stamped");



                    pg.Bounds = footer.Bounds;
                    pg.Draw(footer.Graphics, new PointF(5, 35));

                    prindate.Bounds = footer.Bounds;
                    prindate.Draw(footer.Graphics, new PointF(1470, 35));

                    electronicCopy.Draw(footer.Graphics, new PointF(700, 35));

                    //Rubber Seal
                    //FileStream imageRubber = new FileStream(_rubbersealpath, FileMode.Open, FileAccess.Read);
                    //PdfImage _seal = PdfImage.FromStream(imageRubber);
                    //footer.Graphics.DrawImage(_seal, 1455, -20, 75, 75); //QCS Rubber Seal


                    //-------------------------------------------------
                    PdfPen pen = new PdfPen(Color.Black, 0.5f);
                    
                    hr.Bounds = footer.Bounds;
                    hr.Draw(footer.Graphics, new PointF(125, 10));
                    footer.Graphics.DrawLine(pen, 75, 10, 195, 10);

                    atant.Bounds = footer.Bounds;
                    atant.Draw(footer.Graphics, new PointF(350, 10));
                    footer.Graphics.DrawLine(pen, 315, 10, 435, 10);                   

                    fMgr.Bounds = footer.Bounds;
                    fMgr.Draw(footer.Graphics, new PointF(625, 10));                    
                    footer.Graphics.DrawLine(pen, 605, 10, 725, 10);

                    ceo.Bounds = footer.Bounds;
                    ceo.Draw(footer.Graphics, new PointF(940, 10));                    
                    footer.Graphics.DrawLine(pen, 925, 10, 1070, 10);

                    md.Bounds = footer.Bounds;
                    md.Draw(footer.Graphics, new PointF(1255, 10));                    
                    footer.Graphics.DrawLine(pen, 1235, 10, 1365, 10);

                    doc.Template.Bottom = footer;
                    //---------------------Footer------------------------


                    pdfGrid.Draw(page, new PointF(10, 110));
                    doc.Save(stream);
                    stream.Position = 0;
                    doc.Close(true);

                    //---------------------------------------------------------------------------
                    _filename = "Salary_Sheet_" + eftYr.ToString() + "_" + eftMnt.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                    _filepath = _host.WebRootPath + @"\attach\slrySheet\" + $@"\{_filename}";
                    using (FileStream fs = new FileStream(_filepath, FileMode.OpenOrCreate))
                    {
                        stream.CopyTo(fs);
                        stream.Flush();
                    }


                    return _filename;
                }
                else
                {
                    return _filename;

                }
            }
            catch
            {
                return "";
            }
        }
        #endregion SalarySheet

        #region   ==============PaySlip ==============
        public IEnumerable<PrSalarySheetView> GetSlryDtlByEmpSysIds(int entId, string field, string empSysIds, int prcId, int eftMnt, int eftYr)
        {
            try
            {
                string sp = "[PR_SlryPrc_GetDtl] {0},{1},{2},{3}";
                object[] param = { field, prcId, eftYr, eftMnt };

                string strYrMnt = new DateTime(eftYr, eftMnt, 1).ToString("MMMM", CultureInfo.InvariantCulture) + " " + eftYr.ToString();
                string prntTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");

                int[] arrEmpSysIds = string.IsNullOrWhiteSpace(empSysIds)
                    ? Array.Empty<int>()
                    : empSysIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => Convert.ToInt32(n.Trim())).ToArray();

                var x = _hrmContext.HrPrSlryPrcDtlPsvVw.FromSqlRaw(sp, param).ToList()
                       .Join(_hrmContext.HrEmpGenInfo, gi => gi.EmpSysId, ss => ss.EmpSysId, (ss, gi) => new { ss, gi })
                       .Join(_hrmContext.HrEmpJobAssign, p => p.ss.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                       .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                       .Join(_hrmContext.OrgDesignation, r => r.q.jb.DesigId, ds => ds.DesigId, (r, ds) => new { r, ds })
                       .Where(z => z.r.q.p.ss.RecStat == 1 && z.r.q.jb.WkStat == 1 && z.r.dp.EntId == entId && (string.IsNullOrWhiteSpace(empSysIds) || arrEmpSysIds.Contains(z.r.q.p.gi.EmpSysId))) //&& z.r.q.jb.WkStat == 1 &&  z.r.q.p.gi.CurrStat == 0 
                       .OrderBy(o => o.r.q.p.gi.SrtOrd).ThenBy(m => m.r.q.p.gi.EmpId)
                       .Select(x => new PrSalarySheetView
                       {
                           Index = x.r.q.p.ss.PrcPsvRecId,
                           Department = x.r.dp.NameEng,
                           Emp_Sys_Id = x.r.q.p.gi.EmpSysId,
                           Emp_Id = x.r.q.p.gi.EmpId,
                           Full_Name = x.r.q.p.gi.FullName,
                           Designation = x.ds.NameEng,
                           BasicSalary = x.r.q.p.ss.Basic,
                           HouseRent = x.r.q.p.ss.HrAlwnc,
                           Transport = x.r.q.p.ss.TrnsAlwnc,
                           Position = x.r.q.p.ss.PstnAlwnc,
                           ProjectAllowance = x.r.q.p.ss.ProjAlwnc,
                           Incentives = x.r.q.p.ss.IncntvAmt,
                           Mobile = x.r.q.p.ss.MobAlwnc,
                           Overtime = x.r.q.p.ss.OtAmt,
                           MedicalAllowance = x.r.q.p.ss.MedAlwnc,
                           BonusAmt = x.r.q.p.ss.BonusAmt,
                           OthrAlwnc = x.r.q.p.ss.OthrAlwnc,
                           OthrPmt = x.r.q.p.ss.OthrPmt,
                           GrossSalary = x.r.q.p.ss.GrsSlry,
                           SalaryAdvance = x.r.q.p.ss.SlryAdv,
                           StaffLoans = x.r.q.p.ss.StfLnPmt,
                           NoPay = x.r.q.p.ss.NoPayAmt,
                           OthrRcvryAmt = x.r.q.p.ss.OthrRcvry,
                           TotalDeductions = x.r.q.p.ss.TotDctAmt,
                           NtSlry = x.r.q.p.ss.NtSlry,
                           PmtMod = x.r.q.p.ss.PmtMod,
                           PmtModTxt = (x.r.q.p.ss.PmtMod == 0) ? "Cash" : "Bank",
                           BnkAmt = (x.r.q.p.ss.PmtMod == 1) ? x.r.q.p.ss.NtSlry : 0,
                           CshAmt = (x.r.q.p.ss.PmtMod == 0) ? x.r.q.p.ss.NtSlry : 0,
                           RecStat = x.r.q.p.ss.RecStat,
                           BnkAcct = x.r.q.p.ss.BnkAcct,
                           PrcId = x.r.q.p.ss.PrcId,
                           PrcLogId = x.r.q.p.ss.PrcLogId,
                           Remarks = x.r.q.p.ss.Rmks,
                           YrMntTxt = strYrMnt,
                           PrntTime = prntTime

                       });

                return x;

            }
            catch
            {
                List<PrSalarySheetView> temp = new List<PrSalarySheetView>
                {
                    new PrSalarySheetView {
                    Index = 0,
                    Department = "No Records Found",
                    Emp_Sys_Id = 0,
                    Emp_Id = "",
                    Full_Name = "No Records Found",
                    Designation = "",
                    BasicSalary = 0,
                    HouseRent = 0,
                    Transport = 0,
                    Position = 0,
                    ProjectAllowance = 0,
                    Incentives = 0,
                    Mobile = 0,
                    Overtime = 0,
                    MedicalAllowance = 0,
                    BonusAmt = 0,
                    OthrAlwnc = 0,
                    OthrPmt = 0,
                    GrossSalary = 0,
                    SalaryAdvance = 0,
                    StaffLoans = 0,
                    NoPay = 0,
                    OthrRcvryAmt = 0,
                    TotalDeductions = 0,
                    NtSlry = 0,
                    PmtMod = 0,
                    PmtModTxt = "",
                    BnkAmt = 0,
                    CshAmt = 0,
                    RecStat = 1,
                    BnkAcct = "",
                    PrcId = 0,
                    PrcLogId = 0
                    }

                };
                return temp;
            }
        }

        public string GetPaySlip(string empId, int eftYr, int eftMnt, int entId)
        {
            try
            {
                int si_Alwanc = 1;
                int si_Deduct = 1;

                PdfDocument doc = new PdfDocument();
                PdfStandardFont _headerTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Regular);
                PdfStandardFont _smallTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Regular);
                PdfStandardFont _medinumTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
                PdfStandardFont _largeTxt = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                PdfStringFormat _formatRTL = new PdfStringFormat();

                PdfGridCellStyle _gridCellSmallTxtStyle = new PdfGridCellStyle();
                _gridCellSmallTxtStyle.Font = _smallTxt;

                PdfGridCellStyle _gridCellMediumTxtStyle = new PdfGridCellStyle();
                _gridCellSmallTxtStyle.Font = _medinumTxt;

                PdfGridCellStyle _gridCellLargeTxtStyle = new PdfGridCellStyle();
                _gridCellLargeTxtStyle.Font = _largeTxt;

                PdfGridCellStyle _gridCellColorStyle = new PdfGridCellStyle();
                _gridCellColorStyle.BackgroundBrush = PdfBrushes.LightGray;
                _gridCellColorStyle.Font = _medinumTxt;

                _formatRTL.Alignment = PdfTextAlignment.Right;


                var obj = GetSalaryDtl(entId, "_paysheet", 0, eftMnt, eftYr).Where(x => x.Emp_Id == empId).FirstOrDefault();
                PdfPage page = doc.Pages.Add();
                PdfGrid pdfGrid = new PdfGrid();
                PdfGraphics gphc = page.Graphics;
                var _logopath = _host.WebRootPath + "\\images\\qcs.png";
                


                //Header                        
                DateTime SalryDate = new DateTime(eftYr, eftMnt, 1);

                gphc.DrawRectangle(new PdfSolidBrush(gray), new Syncfusion.Drawing.RectangleF(0, 0, page.Graphics.ClientSize.Width, 40));
                gphc.DrawString("Pay Slip", _headerTxt, new PdfSolidBrush(black), new Syncfusion.Drawing.PointF(225, 10));
                gphc.DrawString(SalryDate.ToString("MMMM") + " " + SalryDate.Year.ToString(), _smallTxt, new PdfSolidBrush(black), new Syncfusion.Drawing.PointF(450, 15));

                //Logo
                FileStream imageStream = new FileStream(_logopath, FileMode.Open, FileAccess.Read);
                PdfImage _logo = PdfImage.FromStream(imageStream);
                page.Graphics.DrawImage(_logo, 10, 10, 70, 25); //QCS Logo

              
                //Employee Details
                gphc.DrawString("Employee ID", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(10, 60));
                gphc.DrawString(":", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(75, 60));
                gphc.DrawString(obj.Emp_Id, _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(90, 60));

                gphc.DrawString("Title", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(275, 60));
                gphc.DrawString(":", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(340, 60));
                gphc.DrawString(obj.Designation, _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(355, 60));

                gphc.DrawString("Name", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(10, 75));
                gphc.DrawString(":", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(75, 75));
                gphc.DrawString(obj.Full_Name, _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(90, 75));

                gphc.DrawString("Department", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(275, 75));
                gphc.DrawString(":", _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(340, 75));
                gphc.DrawString(obj.Department, _smallTxt, PdfBrushes.Black, new Syncfusion.Drawing.PointF(355, 75));


                float columnWidthA = 30;
                float columnWidthB = 150;
                float columnWidthC = 100;
                //Add 3 columns.

                pdfGrid.Columns.Add(3);
                pdfGrid.Columns[0].Width = columnWidthA;
                pdfGrid.Columns[1].Width = columnWidthB;
                pdfGrid.Columns[2].Width = columnWidthC;
                pdfGrid.Style.AllowHorizontalOverflow = true;
                //pdfGrid.Columns[3].Width = columnWidthB;


                //Add rows.
                PdfGridRow pdfGridHeader = pdfGrid.Rows.Add();
                pdfGridHeader.Cells[0].Value = " Si";
                pdfGridHeader.Cells[1].Value = " Description";
                pdfGridHeader.Cells[2].Value = "Amount (QR)";
                pdfGridHeader.Cells[2].StringFormat = _formatRTL;
                pdfGridHeader.Style = _gridCellLargeTxtStyle;

                PdfGridRow pdfGridRow1 = pdfGrid.Rows.Add();
                pdfGridRow1.Cells[0].Value = " 1.";
                pdfGridRow1.Cells[1].Value = " Basic Salary";
                pdfGridRow1.Cells[2].Value = String.Format("{0:n0}", obj.BasicSalary).PadRight(1,' '); //{0:n}
                pdfGridRow1.Cells[2].StringFormat = _formatRTL;
                pdfGridRow1.Height = 15;
                pdfGridRow1.Style = _gridCellLargeTxtStyle;


                PdfGridRow pdfGridRow2 = pdfGrid.Rows.Add();
                pdfGridRow2.Cells[0].Value = "";
                pdfGridRow2.Cells[1].Value = " Allowances";
                pdfGridRow2.Cells[2].Value = "";
                pdfGridRow2.Style = _gridCellColorStyle;
                pdfGridRow2.Height = 15;

                PdfGridRow pdfGridRow3 = pdfGrid.Rows.Add();
                pdfGridRow3.Cells[0].Value = "  2.";
                pdfGridRow3.Cells[1].Value = "  House Rent";
                pdfGridRow3.Cells[2].Value = String.Format("{0:n0}", obj.HouseRent);
                pdfGridRow3.Cells[2].StringFormat = _formatRTL;
                pdfGridRow3.Height = 15;
                pdfGridRow3.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow4 = pdfGrid.Rows.Add();
                pdfGridRow4.Cells[0].Value = "  3.";
                pdfGridRow4.Cells[1].Value = "  Transport";
                pdfGridRow4.Cells[2].Value = String.Format("{0:n0}", obj.Transport);
                pdfGridRow4.Cells[2].StringFormat = _formatRTL;
                pdfGridRow4.Height = 15;
                pdfGridRow4.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow5 = pdfGrid.Rows.Add();
                pdfGridRow5.Cells[0].Value = "  4.";
                pdfGridRow5.Cells[1].Value = "  Position";
                pdfGridRow5.Cells[2].Value = String.Format("{0:n0}", obj.Position);
                pdfGridRow5.Cells[2].StringFormat = _formatRTL;
                pdfGridRow5.Height = 15;
                pdfGridRow5.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow7 = pdfGrid.Rows.Add();
                pdfGridRow7.Cells[0].Value = "  5.";
                pdfGridRow7.Cells[1].Value = "  Mobile";
                pdfGridRow7.Cells[2].Value = String.Format("{0:n0}", obj.Mobile);
                pdfGridRow7.Cells[2].StringFormat = _formatRTL;
                pdfGridRow7.Height = 15;
                pdfGridRow7.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow8 = pdfGrid.Rows.Add();
                pdfGridRow8.Cells[0].Value = "  6.";
                pdfGridRow8.Cells[1].Value = "  Overtime";
                pdfGridRow8.Cells[2].Value = String.Format("{0:n0}", obj.Overtime);
                pdfGridRow8.Cells[2].StringFormat = _formatRTL;
                pdfGridRow8.Height = 15;
                pdfGridRow8.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow9 = pdfGrid.Rows.Add();
                pdfGridRow9.Cells[0].Value = "  7.";
                pdfGridRow9.Cells[1].Value = "  Medical";
                pdfGridRow9.Cells[2].Value = String.Format("{0:n0}", obj.MedicalAllowance);
                pdfGridRow9.Cells[2].StringFormat = _formatRTL;
                pdfGridRow9.Height = 15;
                pdfGridRow9.Style = _gridCellSmallTxtStyle;



                PdfGridRow pdfGridRow19 = pdfGrid.Rows.Add();
                pdfGridRow19.Cells[0].Value = "  8.";
                pdfGridRow19.Cells[1].Value = "  Project";
                pdfGridRow19.Cells[2].Value = String.Format("{0:n0}", obj.ProjectAllowance);
                pdfGridRow19.Cells[2].StringFormat = _formatRTL;
                pdfGridRow19.Height = 15;
                pdfGridRow19.Style = _gridCellSmallTxtStyle;


                PdfGridRow pdfGridRow6 = pdfGrid.Rows.Add();
                pdfGridRow6.Cells[0].Value = "  9.";
                pdfGridRow6.Cells[1].Value = "  Incentives";
                pdfGridRow6.Cells[2].Value = String.Format("{0:n0}", obj.Incentives);
                pdfGridRow6.Cells[2].StringFormat = _formatRTL;
                pdfGridRow6.Height = 15;
                pdfGridRow6.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow10 = pdfGrid.Rows.Add();
                pdfGridRow10.Cells[0].Value = "  10.";
                pdfGridRow10.Cells[1].Value = "  Other Allowance";
                pdfGridRow10.Cells[2].Value = String.Format("{0:n0}", obj.OthrAlwnc);
                pdfGridRow10.Cells[2].StringFormat = _formatRTL;
                pdfGridRow10.Height = 15;
                pdfGridRow10.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow11 = pdfGrid.Rows.Add();
                pdfGridRow11.Cells[0].Value = "  11.";
                pdfGridRow11.Cells[1].Value = "  Other Adjustments (Payments)";
                pdfGridRow11.Cells[2].Value = String.Format("{0:n0}", obj.OthrPmt);
                pdfGridRow11.Cells[2].StringFormat = _formatRTL;
                pdfGridRow11.Height = 15;
                pdfGridRow11.Style = _gridCellSmallTxtStyle;


                //Payments
                IEnumerable<HrPrAdHocTrnView> _lstPayment = GetHrPrAdHocByProcId(entId, eftYr, eftMnt, obj.Emp_Id, "PAY");

                foreach (var itm in _lstPayment)
                {
                    PdfGridRow rowOthrAdj = pdfGrid.Rows.Add();
                    rowOthrAdj.Cells[0].Value = "  11." + si_Alwanc.ToString();
                    rowOthrAdj.Cells[1].Value = itm.TrnTypDesc;
                    rowOthrAdj.Cells[2].Value = String.Format("{0:n0}", itm.TrnAmt);
                    rowOthrAdj.Cells[2].StringFormat = _formatRTL;
                    rowOthrAdj.Height = 15;
                    rowOthrAdj.Style = _gridCellSmallTxtStyle;
                    si_Alwanc += 1;
                }


                PdfGridRow pdfGridRow12 = pdfGrid.Rows.Add();
                pdfGridRow12.Cells[0].Value = "  12.";
                pdfGridRow12.Cells[1].Value = " Gross Salary";
                pdfGridRow12.Cells[2].Value = String.Format("{0:n0}", obj.GrossSalary);
                pdfGridRow12.Cells[2].StringFormat = _formatRTL;
                pdfGridRow12.Height = 15;
                pdfGridRow12.Style = _gridCellLargeTxtStyle;


                PdfGridRow pdfGridRow13 = pdfGrid.Rows.Add();
                pdfGridRow13.Cells[0].Value = " ";
                pdfGridRow13.Cells[1].Value = " Deductions";
                pdfGridRow13.Cells[2].Value = "";
                pdfGridRow13.Height = 15;
                pdfGridRow13.Style = _gridCellColorStyle;


                PdfGridRow pdfGridRow14 = pdfGrid.Rows.Add();
                pdfGridRow14.Cells[0].Value = "  13.";
                pdfGridRow14.Cells[1].Value = "  Salary Advance";
                pdfGridRow14.Cells[2].Value = String.Format("{0:n0}", obj.SalaryAdvance);
                pdfGridRow14.Cells[2].StringFormat = _formatRTL;
                pdfGridRow14.Height = 15;
                pdfGridRow14.Style = _gridCellSmallTxtStyle;


                PdfGridRow pdfGridRow15 = pdfGrid.Rows.Add();
                pdfGridRow15.Cells[0].Value = "  14.";
                pdfGridRow15.Cells[1].Value = "  Staff Loans";
                pdfGridRow15.Cells[2].Value = String.Format("{0:n0}", obj.StaffLoans);
                pdfGridRow15.Cells[2].StringFormat = _formatRTL;
                pdfGridRow15.Height = 15;
                pdfGridRow15.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow16 = pdfGrid.Rows.Add();
                pdfGridRow16.Cells[0].Value = "  15.";
                pdfGridRow16.Cells[1].Value = "  No Pay";
                pdfGridRow16.Cells[2].Value = String.Format("{0:n0}", obj.NoPay);
                pdfGridRow16.Cells[2].StringFormat = _formatRTL;
                pdfGridRow16.Height = 15;
                pdfGridRow16.Style = _gridCellSmallTxtStyle;

                PdfGridRow pdfGridRow17 = pdfGrid.Rows.Add();
                pdfGridRow17.Cells[0].Value = "  16.";
                pdfGridRow17.Cells[1].Value = "  Other Adjustments (Deductions)";
                pdfGridRow17.Cells[2].Value = String.Format("{0:n0}", obj.OthrRcvryAmt);
                pdfGridRow17.Cells[2].StringFormat = _formatRTL;
                pdfGridRow17.Height = 15;
                pdfGridRow17.Style = _gridCellSmallTxtStyle;


                //Deductions
                IEnumerable<HrPrAdHocTrnView> _lstDeduct = GetHrPrAdHocByProcId(entId, eftMnt, eftYr, obj.Emp_Id, "DEDUCT");
                foreach (var itm in _lstDeduct)
                {
                    PdfGridRow rowOthrAdj = pdfGrid.Rows.Add();
                    rowOthrAdj.Cells[0].Value = "  16." + si_Deduct.ToString();
                    rowOthrAdj.Cells[1].Value = "   " + itm.TrnTypDesc;
                    rowOthrAdj.Cells[2].Value = String.Format("{0:n0}", itm.TrnAmt);
                    rowOthrAdj.Cells[2].StringFormat = _formatRTL;
                    rowOthrAdj.Height = 15;
                    rowOthrAdj.Style = _gridCellSmallTxtStyle;
                    si_Deduct += 1;
                }

                PdfGridRow pdfGridRow18 = pdfGrid.Rows.Add();
                pdfGridRow18.Cells[0].Value = "  17.";
                pdfGridRow18.Cells[1].Value = "  Total Deductions";
                pdfGridRow18.Cells[2].Value = String.Format("{0:n0}", obj.TotalDeductions);
                pdfGridRow18.Cells[2].StringFormat = _formatRTL;
                pdfGridRow18.Height = 15;
                pdfGridRow18.Style = _gridCellSmallTxtStyle;


                PdfGridRow pdfGridRow21 = pdfGrid.Rows.Add();
                pdfGridRow21.Cells[0].Value = "  18.";
                pdfGridRow21.Cells[1].Value = "  Net Salary";
                pdfGridRow21.Cells[2].Value = String.Format("{0:n0}", obj.NtSlry);
                pdfGridRow21.Cells[2].StringFormat = _formatRTL;
                pdfGridRow21.Height = 15;
                pdfGridRow21.Style = _gridCellLargeTxtStyle;
                pdfGrid.Draw(page, new PointF(120, 110));


                //PDF document security-------------------------------
                PdfSecurity security = doc.Security;
                //Specifies encryption key size, algorithm, and permission
                security.KeySize = PdfEncryptionKeySize.Key256BitRevision6;
                security.Algorithm = PdfEncryptionAlgorithm.AES;
                //Customize permission in the document
                security.Permissions = PdfPermissionsFlags.Print | PdfPermissionsFlags.FullQualityPrint;

                var objEmp = _employeeContext.GetAllEmpGenInfo().Where(x => x.EmpId == empId).FirstOrDefault();
                //Provide owner and user password
                security.OwnerPassword = objEmp.PmtId;
                security.UserPassword = objEmp.PmtId;
                //---------------------------------------------------


                //---------------------Footer------------------------
                RectangleF bounds = new RectangleF(0, 0, doc.Pages[0].GetClientSize().Width, 50);
                PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);
                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 7);
                PdfBrush brush = new PdfSolidBrush(Color.Black);
                PdfPageNumberField pageNumber = new PdfPageNumberField(font, brush);
                PdfCompositeField pg = new PdfCompositeField(font, brush, "Pg. {0}", pageNumber);
                PdfCompositeField prindate = new PdfCompositeField(font, brush, "Date : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
                //---------------------------------------------------

                pg.Bounds = footer.Bounds;
                pg.Draw(footer.Graphics, new PointF(10, 40));
                prindate.Bounds = footer.Bounds;
                prindate.Draw(footer.Graphics, new PointF(400, 40));
                doc.Template.Bottom = footer;
                //---------------------Footer------------------------

                MemoryStream stream = new MemoryStream();
                doc.Save(stream);
                stream.Position = 0;
                doc.Close(true);

                //---------------------------------------------------------------------------
                var _filename = "PaySlip_" + empId + "_" + eftYr.ToString() + "-" + eftMnt.ToString() + ".pdf";
                var _filepath = _host.WebRootPath + @"\attach\empPaySlip\" + $@"\{_filename}";
                using (FileStream fs = new FileStream(_filepath, FileMode.OpenOrCreate))
                {
                    stream.CopyTo(fs);
                    stream.Flush();
                }
                return _filename;
            }
            catch
            {
                return "";
            }
        }

        #endregion PaySlip

        #region ==============Common Functions==============
        public partial class CurEftYM
        {
            public int EftYear { get; set; }
            public int EftMonth { get; set; }
        }

        public CurEftYM GetCurEftYM()
        {
            CurEftYM curEftYM = new CurEftYM();
            var hrPrSlryPrcHdr = _hrmContext.HrPrSlryPrcHdr.Where(x => x.PrcStat != 0).OrderByDescending(o => o.PrcId).FirstOrDefault();
            if (hrPrSlryPrcHdr != null)
            {
                if (hrPrSlryPrcHdr.PrcStat == 1)
                {
                    curEftYM.EftYear = hrPrSlryPrcHdr.EftYear;
                    curEftYM.EftMonth = hrPrSlryPrcHdr.EftMonth;
                }
                else if (hrPrSlryPrcHdr.PrcStat > 1)
                {
                    if (hrPrSlryPrcHdr.EftMonth == 12)
                    {
                        curEftYM.EftYear = hrPrSlryPrcHdr.EftYear + 1;
                        curEftYM.EftMonth = 1;
                    }
                    else
                    {
                        curEftYM.EftYear = hrPrSlryPrcHdr.EftYear;
                        curEftYM.EftMonth = hrPrSlryPrcHdr.EftMonth + 1;
                    }
                }
            }
            else
            {
                curEftYM.EftYear = DateTime.Today.Year;
                curEftYM.EftMonth = DateTime.Today.Month;
            }
            return curEftYM;
        }
        #endregion ==============Common Functions==============

        #region ==============Dashboard Company Salary Summary==============
        public class CompanySalarySummaryDto
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string MonthName { get; set; }
            public string StaffCategory { get; set; } = "ALL";
            public string CategoryText { get; set; } = "All Staff";
            public bool IsProcessed { get; set; }
            public string StatusText { get; set; }
            public int EmployeeCount { get; set; }
            public decimal TotalBasic { get; set; }
            public decimal TotalHousing { get; set; }
            public decimal TotalTransport { get; set; }
            public decimal TotalOtherAllowances { get; set; }
            public decimal TotalOvertime { get; set; }
            public decimal TotalBonus { get; set; }
            public decimal TotalAllowances { get; set; }
            public decimal TotalGross { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal TotalNet { get; set; }
            public decimal StaffLoan { get; set; }
            public decimal SalaryAdvance { get; set; }
            public decimal NoPay { get; set; }
            public decimal OtherRecoveries { get; set; }

            public decimal InHouseGross { get; set; }
            public decimal InHouseNet { get; set; }
            public decimal InHouseBasic { get; set; }
            public int InHouseCount { get; set; }

            public decimal OutsourceGross { get; set; }
            public decimal OutsourceNet { get; set; }
            public decimal OutsourceBasic { get; set; }
            public int OutsourceCount { get; set; }
        }

        private class SlryCatRow
        {
            public int WrktypId { get; set; }
            public int EmpCount { get; set; }
            public decimal TotalBasic { get; set; }
            public decimal TotalHousing { get; set; }
            public decimal TotalTransport { get; set; }
            public decimal TotalOtherAlwnc { get; set; }
            public decimal TotalOvertime { get; set; }
            public decimal TotalBonus { get; set; }
            public decimal TotalSlryAdv { get; set; }
            public decimal TotalStfLnPmt { get; set; }
            public decimal TotalNoPay { get; set; }
            public decimal TotalOthrRcvry { get; set; }
        }

        public CompanySalarySummaryDto GetCompanySalarySummary(int year, int month, int entId = 0, string staffCat = "ALL")
        {
            if (year <= 0 || month <= 0)
            {
                var prev = DateTime.Today.AddMonths(-1);
                year = prev.Year;
                month = prev.Month;
            }

            if (entId <= 0)
            {
                entId = 1;
            }

            if (string.IsNullOrWhiteSpace(staffCat))
            {
                staffCat = "ALL";
            }

            var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            var summary = new CompanySalarySummaryDto
            {
                Year = year,
                Month = month,
                MonthName = monthName,
                StaffCategory = staffCat
            };

            int? targetWrktyp = null;
            if (string.Equals(staffCat, "OUTSRC", StringComparison.OrdinalIgnoreCase) || staffCat == "2")
            {
                targetWrktyp = 2;
                summary.StaffCategory = "OUTSRC";
                summary.CategoryText = "Outsource Staff";
            }
            else if (string.Equals(staffCat, "IN_HOUSE", StringComparison.OrdinalIgnoreCase) || staffCat == "1")
            {
                targetWrktyp = 1;
                summary.StaffCategory = "IN_HOUSE";
                summary.CategoryText = "In-House Staff";
            }
            else
            {
                summary.StaffCategory = "ALL";
                summary.CategoryText = "All Staff";
            }

            Console.WriteLine($"[DEBUG-SERVER] GetCompanySalarySummary ENTER: year={year}, month={month}, entId={entId}, staffCat={staffCat}, targetWrktyp={targetWrktyp}");

            try
            {
                // 1. Locate salary process header for this period
                var hdrQuery = _hrmContext.HrPrSlryPrcHdr.Where(x => x.EftYear == year && x.EftMonth == month);
                HrPrSlryPrcHdr hdr = null;
                if (entId > 0)
                {
                    hdr = hdrQuery.Where(x => x.EntId == entId).OrderByDescending(h => h.PrcId).FirstOrDefault();
                }
                if (hdr == null)
                {
                    hdr = hdrQuery.OrderByDescending(h => h.PrcId).FirstOrDefault();
                }

                Console.WriteLine($"[DEBUG-SERVER] GetCompanySalarySummary: found hdr={hdr?.PrcId}, EftYear={hdr?.EftYear}, EftMonth={hdr?.EftMonth}, EntId={hdr?.EntId}");

                if (hdr != null)
                {
                    var conn = _hrmContext.Database.GetDbConnection();
                    bool wasOpen = conn.State == System.Data.ConnectionState.Open;
                    if (!wasOpen) conn.Open();

                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            string sql = @"
SELECT 
    ISNULL(e.Wrktyp_Id, 1) AS Wrktyp_Id,
    COUNT(*) AS EmpCount,
    ISNULL(SUM(d.Basic), 0) AS TotalBasic,
    ISNULL(SUM(d.HR_Alwnc), 0) AS TotalHousing,
    ISNULL(SUM(d.Trns_Alwnc + d.TRA), 0) AS TotalTransport,
    ISNULL(SUM(d.Med_Alwnc + d.Mob_Alwnc + d.Proj_Alwnc + d.Spc_Alwnc + d.Incntv_Amt + d.Prfm_Alwnc + d.Othr_Alwnc + d.Othr_Pmt + d.AirTckt_Fee + d.Anl_Lv_Enchmnt), 0) AS TotalOtherAlwnc,
    ISNULL(SUM(d.Ot_Amt), 0) AS TotalOvertime,
    ISNULL(SUM(d.Bonus_Amt), 0) AS TotalBonus,
    ISNULL(SUM(d.Slry_Adv), 0) AS TotalSlryAdv,
    ISNULL(SUM(d.Stf_Ln_Pmt), 0) AS TotalStfLnPmt,
    ISNULL(SUM(d.Np_Amt), 0) AS TotalNoPay,
    ISNULL(SUM(d.Othr_Rcvry), 0) AS TotalOthrRcvry
FROM HR_PrSlryPrcDtl_Psv d
INNER JOIN HR_EmpGenInfo e ON d.Emp_Sys_Id = e.Emp_Sys_Id
WHERE d.Prc_Id = @PrcId AND d.Rec_Stat = 1
GROUP BY ISNULL(e.Wrktyp_Id, 1)";

                            var pPrc = cmd.CreateParameter();
                            pPrc.ParameterName = "@PrcId";
                            pPrc.Value = hdr.PrcId;
                            cmd.Parameters.Add(pPrc);

                            cmd.CommandText = sql;

                            var rows = new List<SlryCatRow>();
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    rows.Add(new SlryCatRow
                                    {
                                        WrktypId = Convert.ToInt32(rdr["Wrktyp_Id"]),
                                        EmpCount = Convert.ToInt32(rdr["EmpCount"]),
                                        TotalBasic = Convert.ToDecimal(rdr["TotalBasic"]),
                                        TotalHousing = Convert.ToDecimal(rdr["TotalHousing"]),
                                        TotalTransport = Convert.ToDecimal(rdr["TotalTransport"]),
                                        TotalOtherAlwnc = Convert.ToDecimal(rdr["TotalOtherAlwnc"]),
                                        TotalOvertime = Convert.ToDecimal(rdr["TotalOvertime"]),
                                        TotalBonus = Convert.ToDecimal(rdr["TotalBonus"]),
                                        TotalSlryAdv = Convert.ToDecimal(rdr["TotalSlryAdv"]),
                                        TotalStfLnPmt = Convert.ToDecimal(rdr["TotalStfLnPmt"]),
                                        TotalNoPay = Convert.ToDecimal(rdr["TotalNoPay"]),
                                        TotalOthrRcvry = Convert.ToDecimal(rdr["TotalOthrRcvry"])
                                    });
                                }
                            }

                            // If not yet finalized in HR_PrSlryPrcDtl_Psv, fallback to HR_PrSlryPrcDtl_Actv
                            if (!rows.Any())
                            {
                                cmd.CommandText = sql.Replace("HR_PrSlryPrcDtl_Psv", "HR_PrSlryPrcDtl_Actv");
                                using (var rdr = cmd.ExecuteReader())
                                {
                                    while (rdr.Read())
                                    {
                                        rows.Add(new SlryCatRow
                                        {
                                            WrktypId = Convert.ToInt32(rdr["Wrktyp_Id"]),
                                            EmpCount = Convert.ToInt32(rdr["EmpCount"]),
                                            TotalBasic = Convert.ToDecimal(rdr["TotalBasic"]),
                                            TotalHousing = Convert.ToDecimal(rdr["TotalHousing"]),
                                            TotalTransport = Convert.ToDecimal(rdr["TotalTransport"]),
                                            TotalOtherAlwnc = Convert.ToDecimal(rdr["TotalOtherAlwnc"]),
                                            TotalOvertime = Convert.ToDecimal(rdr["TotalOvertime"]),
                                            TotalBonus = Convert.ToDecimal(rdr["TotalBonus"]),
                                            TotalSlryAdv = Convert.ToDecimal(rdr["TotalSlryAdv"]),
                                            TotalStfLnPmt = Convert.ToDecimal(rdr["TotalStfLnPmt"]),
                                            TotalNoPay = Convert.ToDecimal(rdr["TotalNoPay"]),
                                            TotalOthrRcvry = Convert.ToDecimal(rdr["TotalOthrRcvry"])
                                        });
                                    }
                                }
                            }

                            Console.WriteLine($"[DEBUG-SERVER] GetCompanySalarySummary: read {rows.Count} rows from DB for PrcId {hdr.PrcId}");

                            if (rows.Any())
                            {
                                summary.IsProcessed = true;
                                summary.StatusText = (hdr.PrcStat == 3) ? "Processed & Finalized" : 
                                                     ((hdr.PrcStat == 2) ? "Verified" : "Processed");

                                var inHouseRow = rows.FirstOrDefault(r => r.WrktypId == 1);
                                var outsrcRow = rows.FirstOrDefault(r => r.WrktypId == 2);

                                summary.InHouseCount = inHouseRow?.EmpCount ?? 0;
                                summary.InHouseBasic = inHouseRow?.TotalBasic ?? 0;
                                summary.InHouseGross = (inHouseRow != null) 
                                    ? (inHouseRow.TotalBasic + inHouseRow.TotalHousing + inHouseRow.TotalTransport + inHouseRow.TotalOtherAlwnc + inHouseRow.TotalOvertime + inHouseRow.TotalBonus) 
                                    : 0;
                                summary.InHouseNet = summary.InHouseGross - ((inHouseRow != null) 
                                    ? (inHouseRow.TotalSlryAdv + inHouseRow.TotalStfLnPmt + inHouseRow.TotalNoPay + inHouseRow.TotalOthrRcvry) 
                                    : 0);

                                summary.OutsourceCount = outsrcRow?.EmpCount ?? 0;
                                summary.OutsourceBasic = outsrcRow?.TotalBasic ?? 0;
                                summary.OutsourceGross = (outsrcRow != null) 
                                    ? (outsrcRow.TotalBasic + outsrcRow.TotalHousing + outsrcRow.TotalTransport + outsrcRow.TotalOtherAlwnc + outsrcRow.TotalOvertime + outsrcRow.TotalBonus) 
                                    : 0;
                                summary.OutsourceNet = summary.OutsourceGross - ((outsrcRow != null) 
                                    ? (outsrcRow.TotalSlryAdv + outsrcRow.TotalStfLnPmt + outsrcRow.TotalNoPay + outsrcRow.TotalOthrRcvry) 
                                    : 0);

                                var displayRows = targetWrktyp.HasValue 
                                    ? rows.Where(r => r.WrktypId == targetWrktyp.Value).ToList() 
                                    : rows;

                                if (targetWrktyp.HasValue && !displayRows.Any())
                                {
                                    summary.IsProcessed = true;
                                    summary.StatusText = (hdr.PrcStat == 3) ? "Processed & Finalized" : 
                                                         ((hdr.PrcStat == 2) ? "Verified" : "Processed");
                                    summary.EmployeeCount = 0;
                                    summary.TotalBasic = 0;
                                    summary.TotalHousing = 0;
                                    summary.TotalTransport = 0;
                                    summary.TotalOtherAllowances = 0;
                                    summary.TotalOvertime = 0;
                                    summary.TotalBonus = 0;
                                    summary.TotalAllowances = 0;
                                    summary.TotalGross = 0;
                                    summary.TotalDeductions = 0;
                                    summary.TotalNet = 0;
                                    summary.StaffLoan = 0;
                                    summary.SalaryAdvance = 0;
                                    summary.NoPay = 0;
                                    summary.OtherRecoveries = 0;
                                    return summary;
                                }

                                summary.EmployeeCount = displayRows.Sum(r => r.EmpCount);
                                summary.TotalBasic = displayRows.Sum(r => r.TotalBasic);
                                summary.TotalHousing = displayRows.Sum(r => r.TotalHousing);
                                summary.TotalTransport = displayRows.Sum(r => r.TotalTransport);
                                summary.TotalOtherAllowances = displayRows.Sum(r => r.TotalOtherAlwnc);
                                summary.TotalOvertime = displayRows.Sum(r => r.TotalOvertime);
                                summary.TotalBonus = displayRows.Sum(r => r.TotalBonus);
                                summary.TotalAllowances = summary.TotalHousing + summary.TotalTransport + summary.TotalOtherAllowances + summary.TotalOvertime + summary.TotalBonus;
                                summary.TotalGross = summary.TotalBasic + summary.TotalAllowances;

                                summary.StaffLoan = displayRows.Sum(r => r.TotalStfLnPmt);
                                summary.SalaryAdvance = displayRows.Sum(r => r.TotalSlryAdv);
                                summary.NoPay = displayRows.Sum(r => r.TotalNoPay);
                                summary.OtherRecoveries = displayRows.Sum(r => r.TotalOthrRcvry);
                                summary.TotalDeductions = summary.StaffLoan + summary.SalaryAdvance + summary.NoPay + summary.OtherRecoveries;
                                summary.TotalNet = summary.TotalGross - summary.TotalDeductions;

                                return summary;
                            }
                        }
                    }
                    finally
                    {
                        if (!wasOpen && conn.State == System.Data.ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }

                // Unprocessed / no processed salary for requested period:
                // Show ONLY processed salaries; do not show any projected or forecast figures in the controls.
                summary.IsProcessed = false;
                summary.StatusText = "Not Processed";
                summary.EmployeeCount = 0;
                summary.TotalBasic = 0;
                summary.TotalHousing = 0;
                summary.TotalTransport = 0;
                summary.TotalOtherAllowances = 0;
                summary.TotalOvertime = 0;
                summary.TotalBonus = 0;
                summary.TotalAllowances = 0;
                summary.TotalGross = 0;
                summary.TotalDeductions = 0;
                summary.TotalNet = 0;
                summary.StaffLoan = 0;
                summary.SalaryAdvance = 0;
                summary.NoPay = 0;
                summary.OtherRecoveries = 0;
                summary.InHouseCount = 0;
                summary.InHouseBasic = 0;
                summary.InHouseGross = 0;
                summary.InHouseNet = 0;
                summary.OutsourceCount = 0;
                summary.OutsourceBasic = 0;
                summary.OutsourceGross = 0;
                summary.OutsourceNet = 0;

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] GetCompanySalarySummary: " + ex);
                summary.StatusText = "Unavailable";
                return summary;
            }
        }
        #endregion
    }
}
