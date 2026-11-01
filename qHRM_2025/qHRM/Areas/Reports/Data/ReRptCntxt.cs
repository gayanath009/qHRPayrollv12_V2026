using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Reports.Models;
using qHRM.Models;
using System;
using System.Linq;

namespace qHRM.Areas.Reports.Data
{
    public class ReRptCntxt : DbContext
    {
        #region ========== DB Context Members ================
        private readonly qHRMasterContext _hrmCntxt;
        #endregion ======= DB Context Members ================


        #region ========== Constructor =======================
        public ReRptCntxt(qHRMasterContext hrmCntxt)
        {
            _hrmCntxt = hrmCntxt;
        }
        #endregion ======= Constructor =======================


        #region ========== Config Report (ReCnfgRpt) =========
        private ReCnfgRpt GetReCnfgRptByRptKy(string rptKy, int entId)
        {
            try
            {
                var reCnfgRpt = _hrmCntxt.ReCnfgRpts.Where(z => z.RptKy == rptKy && z.EntId == entId).First();
                return GetReCnfgRptByRptId(reCnfgRpt.RptId);
            }
            catch { throw; }
        }

        private ReCnfgRpt GetReCnfgRptByRptId(int rptId)
        {
            try
            {
                var reCnfgRpt = _hrmCntxt.ReCnfgRpts.Where(z => z.RptId == rptId).First();
                reCnfgRpt.ReCnfgRptPrms.ToList().AddRange(_hrmCntxt.ReCnfgRptPrms.Where(z => z.RptId == reCnfgRpt.RptId));
                foreach (var reCnfgSbRpt in _hrmCntxt.ReCnfgRpts.Where(z => z.PrntId == reCnfgRpt.RptId).OrderBy(o => o.SbIndx))
                {
                    reCnfgRpt.InversePrnt.Add(GetReCnfgRptByRptId(reCnfgSbRpt.RptId));
                }
                return reCnfgRpt;
            }
            catch { throw; }
        }

        #endregion ======= Config Report (ReCnfgRpt) =========


        #region ========== Setup Reports =====================
        #region ============== Attendence Reports ============
        public long SetupStgngSsnForAtndcRpt(string prcTyp, int empSysId, DateTime frmDate, DateTime toDate, int entId, int usrId)
        {
            try
            {
                var cnfgRpt = GetReCnfgRptByRptKy("RPT_ATNDC", entId);
                var stgngSsn = new ReStgngSsn { Exprt = true, Prnt = true, RcSts = 1, PrntdDt = DateTime.Now, PrntdId = usrId };
                var stgngRpt = new ReStgngRpt { SsnId = stgngSsn.SsnId, RptId = cnfgRpt.RptId };

                foreach (var cnfgRptPrm in cnfgRpt.ReCnfgRptPrms)
                {
                    switch (cnfgRptPrm.PrmNm)
                    {
                        case "@Prc_Typ":
                            stgngRpt.ReStgngRptPrms.Add(new ReStgngRptPrm { SsnRptId = stgngRpt.SsnRptId, PrmId = cnfgRptPrm.PrmId, PrmVlv = prcTyp });
                            break;
                        case "@Emp_Sys_Id":
                            stgngRpt.ReStgngRptPrms.Add(new ReStgngRptPrm { SsnRptId = stgngRpt.SsnRptId, PrmId = cnfgRptPrm.PrmId, PrmVlv = empSysId.ToString() });
                            break;
                        case "@Frm_Date":
                            stgngRpt.ReStgngRptPrms.Add(new ReStgngRptPrm { SsnRptId = stgngRpt.SsnRptId, PrmId = cnfgRptPrm.PrmId, PrmVlv = frmDate.ToString() });
                            break;
                        case "@To_Date":
                            stgngRpt.ReStgngRptPrms.Add(new ReStgngRptPrm { SsnRptId = stgngRpt.SsnRptId, PrmId = cnfgRptPrm.PrmId, PrmVlv = toDate.ToString() });
                            break;
                        //case "Rpt_Ttl":
                        //    stgngRpt.ReStgngRptPrms.Add(new ReStgngRptPrm { SsnRptId = stgngRpt.SsnRptId, PrmId = cnfgRptPrm.PrmId, PrmVlv = "Attendance Report" });
                        //    break;
                    };
                }

                stgngSsn.ReStgngRpts.Add(stgngRpt);
                _hrmCntxt.ReStgngSsns.Add(stgngSsn);
                _hrmCntxt.SaveChanges();
                return stgngSsn.SsnId;
            }
            catch { throw; }
        }
        #endregion =========== Attendence Reports ============
        #endregion ======= Setup Reports =====================
    }
}
