using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Data
{
    public class PrStfLoanContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;

        #endregion

        #region ==============Constructors==============
        public PrStfLoanContext(qHRMasterContext hrmContext,
                                EmployeeContext empContext)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
        }
        #endregion

        #region ==============Staff Loan Types==============
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

        #region ==============Staff Loans==============
        public partial class HrPrStfLoanView
        {
            public int StfLnId { get; set; }
            public string LnCode { get; set; }
            public int LnTypId { get; set; }
            public string LnTypDesc { get; set; }
            public int EmpSysId { get; set; }
            public string EmpDtl { get; set; }
            public DateTime IssuDate { get; set; }
            public decimal FlAmt { get; set; }
            public int Instl { get; set; }
            public decimal InstlAmt { get; set; }
            public decimal RmAmt { get; set; }
            public int EftYear { get; set; }
            public int EftMonth { get; set; }
            public int? RefStfLnId { get; set; }
            public string Note { get; set; }
            public short RecStat { get; set; }
            public string RecStatTxt { get; set; }
        }

        public IEnumerable<HrPrStfLoanView> GetHrPrStfLoanByEntId(bool isSnglEmp, int empSysId, int entId)
        {
            try
            {
                return _hrmContext.HrPrStfLoan
                        .Join(_hrmContext.HrPrStfLoanType, typ => typ.LnTypId, ln => ln.LnTypId, (ln, typ) => new { ln, typ })
                        .Join(_hrmContext.HrEmpGenInfo, p => p.ln.EmpSysId, emp => emp.EmpSysId, (p, emp) => new { p, emp })
                        .Where(z => z.p.typ.EntId == entId && z.p.ln.RecStat == 1 && ((isSnglEmp == true && empSysId == z.emp.EmpSysId) || (isSnglEmp == false && (_empContext.GetEmpSbrdnts(empSysId).Contains(z.emp.EmpSysId)))))
                            .Select(x => new HrPrStfLoanView
                            {
                                StfLnId = x.p.ln.StfLnId,
                                LnCode = x.p.ln.LnCode,
                                LnTypId = x.p.ln.LnTypId,
                                LnTypDesc = x.p.typ.LnTypCode + " - " + x.p.typ.Desc,
                                EmpSysId = x.p.ln.EmpSysId,
                                EmpDtl = x.emp.EmpId + " - " + x.emp.FullName,
                                IssuDate = x.p.ln.IssuDate,
                                FlAmt = x.p.ln.FlAmt,
                                Instl = x.p.ln.Instl,
                                InstlAmt = x.p.ln.InstlAmt,
                                RmAmt = x.p.ln.RmAmt,
                                EftYear = x.p.ln.EftYear,
                                EftMonth = x.p.ln.EftMonth,
                                RefStfLnId = x.p.ln.RefStfLnId,
                                Note = x.p.ln.Note,
                                RecStat = x.p.ln.RecStat,
                                RecStatTxt = (x.p.ln.RecStat == 0) ? "Cancelled" : ((x.p.ln.RecStat == 1) ? "Active" : (x.p.ln.RecStat == 2) ? "Closed" : "Other")
                            });
            }
            catch { throw; }
        }   

        public HrPrStfLoanView GetHrPrStfLoanViewByStfLnId(int stfLnId)
        {
            try
            {
                return _hrmContext.HrPrStfLoan
                        .Join(_hrmContext.HrPrStfLoanType, typ => typ.LnTypId, ln => ln.LnTypId, (ln, typ) => new { ln, typ })
                        .Join(_hrmContext.HrEmpGenInfo, p => p.ln.EmpSysId, emp => emp.EmpSysId, (p, emp) => new { p, emp })
                        .Where(z => z.p.ln.StfLnId == stfLnId)
                        .Select(x => new HrPrStfLoanView
                        {
                            StfLnId = x.p.ln.StfLnId,
                            LnCode = x.p.ln.LnCode,
                            LnTypId = x.p.ln.LnTypId,
                            LnTypDesc = x.p.typ.LnTypCode + " - " + x.p.typ.Desc,
                            EmpSysId = x.p.ln.EmpSysId,
                            EmpDtl = x.emp.EmpId + " - " + x.emp.FullName,
                            IssuDate = x.p.ln.IssuDate,
                            FlAmt = x.p.ln.FlAmt,
                            Instl = x.p.ln.Instl,
                            InstlAmt = x.p.ln.InstlAmt,
                            RmAmt = x.p.ln.RmAmt,
                            EftYear = x.p.ln.EftYear,
                            EftMonth = x.p.ln.EftMonth,
                            RefStfLnId = x.p.ln.RefStfLnId,
                            Note = x.p.ln.Note,
                            RecStat = x.p.ln.RecStat,
                            RecStatTxt = (x.p.ln.RecStat == 0) ? "Cancelled" : ((x.p.ln.RecStat == 1) ? "Active" : (x.p.ln.RecStat == 2) ? "Closed" : "Other")
                        }).FirstOrDefault();

            }
            catch { throw; }
        }

        public HrPrStfLoan GetHrPrStfLoanByStfLnId(int stfLnId)
        {
            try
            {
                return _hrmContext.HrPrStfLoan.Where(z => z.StfLnId == stfLnId).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrStfLoan GetHrPrStfLoanByCode(string lnCode)
        {
            try
            {
                return _hrmContext.HrPrStfLoan.Where(z => z.LnCode == lnCode).FirstOrDefault();
            }
            catch { throw; }
        }

        public int GetHrPrStfLoanCount(int LnTypId)
        {
            try
            {
                return _hrmContext.HrPrStfLoan.Where(z => z.LnTypId == LnTypId).Count();
                //      .Select(x => new HrPrStfLoanCountView
                //      {
                //          LnCode = x.LnCode
                //      });

                //return _list;

            }
            catch { throw; }
        }
        #endregion

        #region ==============Staff Loan Payments==============
        public partial class HrPrStfLoanPmtView
        {
            public int LnPmtId { get; set; }
            public int StfLnId { get; set; }
            public DateTime PmtDate { get; set; }
            public int Instl { get; set; }
            public decimal PmtAmt { get; set; }
            public int? PrcId { get; set; }
            public int? PrcRvsn { get; set; }
            public int PrcYear { get; set; }
            public int PrcMonth { get; set; }
            public short PmtMod { get; set; }
            public string PmtModTxt { get; set; }
            public string Note { get; set; }
            public short RecStat { get; set; }
            public string RecStatTxt { get; set; }
            public int CrtId { get; set; }
            public string CrtName { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public IEnumerable<HrPrStfLoanPmtView> GetHrPrStfLoanPmtVwByStfLnId(int StfLnId)
        {
            try
            {
                var x = _hrmContext.HrPrStfLoanPmt
                        .Join(_hrmContext.HrEmpGenInfo, pmt => pmt.CrtId, emp => emp.EmpSysId, (pmt, emp) => new { pmt, emp })
                        .Where(z => z.pmt.StfLnId == StfLnId)
                            .Select(x => new HrPrStfLoanPmtView
                            {
                                LnPmtId = x.pmt.LnPmtId,
                                StfLnId = x.pmt.StfLnId,
                                PmtDate = x.pmt.PmtDate,
                                Instl = x.pmt.Instl,
                                PmtAmt = x.pmt.PmtAmt,
                                PrcId = x.pmt.PrcId,
                                PrcRvsn = x.pmt.PrcRvsn,
                                PrcYear = x.pmt.PrcYear,
                                PrcMonth = x.pmt.PrcMonth,
                                PmtMod = x.pmt.PmtMod,
                                PmtModTxt = (x.pmt.PmtMod == 0) ? ("SLRY-" + x.pmt.PrcYear.ToString() + "-" + x.pmt.PrcMonth.ToString()) : "Manual",
                                Note = x.pmt.Note,
                                RecStat = x.pmt.RecStat,
                                RecStatTxt = (x.pmt.RecStat == 0) ? "Cancelled" : ((x.pmt.RecStat == 1) ? "Paid" : "Generated"),
                                CrtId = x.pmt.CrtId,
                                CrtName = (x.pmt.PmtMod == 0) ? "System" : x.emp.FullName,
                                CrtDateTime = x.pmt.CrtDateTime
                            });

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public HrPrStfLoanPmt GetHrPrStfLoanPmtByLnPmtId(int lnPmtId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanPmt.Where(z => z.LnPmtId == lnPmtId).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrPrStfLoanPmtView GetHrPrStfLoanPmtViewByLnPmtId(int lnPmtId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanPmt
                        .Join(_hrmContext.HrEmpGenInfo, pmt => pmt.CrtId, emp => emp.EmpSysId, (pmt, emp) => new { pmt, emp })
                        .Where(z => z.pmt.LnPmtId == lnPmtId)
                            .Select(x => new HrPrStfLoanPmtView
                            {
                                LnPmtId = x.pmt.LnPmtId,
                                StfLnId = x.pmt.StfLnId,
                                PmtDate = x.pmt.PmtDate,
                                Instl = x.pmt.Instl,
                                PmtAmt = x.pmt.PmtAmt,
                                PrcId = x.pmt.PrcId,
                                PrcRvsn = x.pmt.PrcRvsn,
                                PrcYear = x.pmt.PrcYear,
                                PrcMonth = x.pmt.PrcMonth,
                                PmtMod = x.pmt.PmtMod,
                                PmtModTxt = (x.pmt.PmtMod == 0) ? ("SLRY-" + x.pmt.PrcYear.ToString() + "-" + x.pmt.PrcMonth.ToString()) : "Manual",
                                Note = x.pmt.Note,
                                RecStat = x.pmt.RecStat,
                                RecStatTxt = (x.pmt.RecStat == 0) ? "Cancelled" : ((x.pmt.RecStat == 1) ? "Paid" : "Generated"),
                                CrtId = x.pmt.CrtId,
                                CrtName = (x.pmt.PmtMod == 0) ? "System" : x.emp.FullName,
                                CrtDateTime = x.pmt.CrtDateTime
                            }).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrPrStfLoanPmt(LogEventTypes eventType, HrPrStfLoanPmt hrPrStfLoanPmt, int userId)
        {
            try
            {
                var paidAmt = _hrmContext.HrPrStfLoanPmt.Where(x => x.StfLnId == hrPrStfLoanPmt.StfLnId && x.LnPmtId != hrPrStfLoanPmt.LnPmtId && x.RecStat == 1).Sum(s => s.PmtAmt);
                if (eventType == LogEventTypes.INS)
                {
                    hrPrStfLoanPmt.RecStat = 1;
                    hrPrStfLoanPmt.CrtDateTime = DateTime.Today;
                    hrPrStfLoanPmt.CrtId = userId;
                    _hrmContext.HrPrStfLoanPmt.Add(hrPrStfLoanPmt);
                }
                else
                {
                    DbCommitContext<HrPrStfLoanPmt> dbCommitContext = new DbCommitContext<HrPrStfLoanPmt>(_hrmContext);
                    dbCommitContext.commitChanges(eventType, hrPrStfLoanPmt, userId);
                }

                if (hrPrStfLoanPmt.RecStat == 1)
                {
                    paidAmt += hrPrStfLoanPmt.PmtAmt;
                }

                var hrPrStfLoan = _hrmContext.HrPrStfLoan.Where(x => x.StfLnId == hrPrStfLoanPmt.StfLnId).First();
                hrPrStfLoan.RmAmt = hrPrStfLoan.FlAmt - paidAmt;

                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Staff Loan Payment Exceptions==============
        public partial class HrPrStfLoanPmtExpView
        {
            public int LnPmtExpId { get; set; }
            public int StfLnId { get; set; }
            public DateTime EntryDate { get; set; }
            public decimal ExpPmtAmt { get; set; }
            public int EftYear { get; set; }
            public int EftMonth { get; set; }
            public int ExpInstl { get; set; }
            public string Note { get; set; }
            public short RecStat { get; set; }
            public string RecStatTxt { get; set; }
            public int CrtBy { get; set; }
            public string CrtName { get; set; }
            public DateTime CrtDateTime { get; set; }
        }

        public IEnumerable<HrPrStfLoanPmtExpView> GetHrPrStfLoanPmtExpVwByStfLnId(int StfLnId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanPmtExp
                        .Join(_hrmContext.HrEmpGenInfo, pmt => pmt.CrtId, emp => emp.EmpSysId, (pmt, emp) => new { pmt, emp })
                        .Where(z => z.pmt.StfLnId == StfLnId)
                            .Select(x => new HrPrStfLoanPmtExpView
                            {
                                LnPmtExpId = x.pmt.LnPmtExpId,
                                StfLnId = x.pmt.StfLnId,
                                EntryDate = x.pmt.EntryDate,
                                ExpPmtAmt = x.pmt.ExpPmtAmt,
                                EftYear = x.pmt.EftYear,
                                EftMonth = x.pmt.EftMonth,
                                ExpInstl = x.pmt.ExpInstl,
                                Note = x.pmt.Note,
                                RecStat = x.pmt.RecStat,
                                RecStatTxt = (x.pmt.RecStat == 0) ? "Cancelled" : ((x.pmt.RecStat == 1) ? "Active" : "Generated"),
                                CrtBy = x.pmt.CrtId,
                                CrtName = x.emp.EmpId + " - " + x.emp.FullName,
                                CrtDateTime = x.pmt.CrtDateTime
                            });
            }
            catch { throw; }
        }

        public HrPrStfLoanPmtExpView GetHrPrStfLoanPmtExpVwByLnPmtExpId(int lnPmtExpId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanPmtExp
                        .Join(_hrmContext.HrEmpGenInfo, pmt => pmt.CrtId, emp => emp.EmpSysId, (pmt, emp) => new { pmt, emp })
                        .Where(z => z.pmt.LnPmtExpId == lnPmtExpId)
                            .Select(x => new HrPrStfLoanPmtExpView
                            {
                                LnPmtExpId = x.pmt.LnPmtExpId,
                                StfLnId = x.pmt.StfLnId,
                                EntryDate = x.pmt.EntryDate,
                                ExpPmtAmt = x.pmt.ExpPmtAmt,
                                EftYear = x.pmt.EftYear,
                                EftMonth = x.pmt.EftMonth,
                                ExpInstl = x.pmt.ExpInstl,
                                Note = x.pmt.Note,
                                RecStat = x.pmt.RecStat,
                                RecStatTxt = (x.pmt.RecStat == 0) ? "Cancelled" : ((x.pmt.RecStat == 1) ? "Active" : "Generated"),
                                CrtBy = x.pmt.CrtId,
                                CrtName = x.emp.EmpId + " - " + x.emp.FullName,
                                CrtDateTime = x.pmt.CrtDateTime
                            }).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrPrStfLoanPmtExp GetHrPrStfLoanPmtExpByLnPmtExpId(int lnPmtExpId)
        {
            try
            {
                return _hrmContext.HrPrStfLoanPmtExp.Where(z => z.LnPmtExpId == lnPmtExpId).FirstOrDefault();
            }
            catch { throw; }
        }

        //public partial class HrPrStfLoanCountView
        //{
        //    public string LnCode { get; set; }
        //}
    }
    #endregion

}