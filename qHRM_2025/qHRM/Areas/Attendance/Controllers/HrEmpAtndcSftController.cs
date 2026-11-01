using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.WorkShift.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;
using AspNetCore.Reporting;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.Reports.Data;

namespace qHRM.Areas.Attendance.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAtndcSftController : Controller
    {
        private readonly AttnContext _attnContext;
        private readonly EmployeeContext _employeeContext;
        private readonly EntityContext _entityContext;
        private readonly DbCommitContext<HrEmpAtndcSft> _dbCommitContext;
        private readonly WorkShiftContext _workShiftContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly ReRptCntxt _reRptCntxt;

        public HrEmpAtndcSftController(AttnContext attnContext,
                                         EmployeeContext employeeContext,
                                         EntityContext entityContext,
                                         DbCommitContext<HrEmpAtndcSft> dbCommitContext,
                                         WorkShiftContext workShiftContext,
                                         IWebHostEnvironment webHostEnvironment,
                                         AdmUserGrpPrvContext prvContext,
                                         ReRptCntxt reRptCntxt)
        {
            _attnContext = attnContext;
            _employeeContext = employeeContext;
            _entityContext = entityContext;
            _dbCommitContext = dbCommitContext;
            _workShiftContext = workShiftContext;
            _webHostEnvironment = webHostEnvironment;
            _prvContext = prvContext;
            _reRptCntxt = reRptCntxt;
        }

        public ActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ATN_REG") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                //---Loads Permitted Employees ---//                

                //ViewBag.lstEmployee = _employeeContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"))).OrderBy(o => o.FulNme);
                int usrId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                ViewBag.lstEmployee = _employeeContext.GetEmpView("ALL", 0, usrId).OrderBy(o => o.FullName);
                ViewBag.lstDept = _entityContext.GetDepatmentEntViews(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"))).OrderBy(o => o.DeptName);
                List<PropView> lstFilters = new List<PropView>
                {
                    new PropView { Prop = "1", Val= "Yesterday" },
                    new PropView { Prop = "7", Val= "Last 7 Days" },
                    new PropView { Prop = "30", Val= "Last 30 Days" },
                    new PropView { Prop = "60", Val= "Last 2 Months" },
                    new PropView { Prop = "90", Val= "Last 3 Months" },
                };
                ViewBag.lstFilter = lstFilters;
                var fromDate = DateTime.Now.AddDays(-1).Date;
                var toDate = DateTime.Now.AddDays(-1).Date;

                //---Attendance Detail ---//             
                ViewBag.AtndcDtl = _attnContext.GetFullHrEmpAtndcSmry("A", 0, fromDate, toDate, false, empSysId).ToList();
              
                //---Attendance Summary ---//
                //ViewBag.AttdSummary = _attnContext.GetEmpAttnSummary("_summary", 0, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd")).ToList();

                ViewBag.ChartTitle = "";

                ViewBag.FromDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                ViewBag.ToDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_REG").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return View();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        
        [HttpGet]
        public JsonResult GetAtndcLogs(int refId, int dateFilter, string sDate, string eDate)
        {
            var fromDate = DateTime.Now.AddDays(-1).Date;
            var toDate = fromDate;
            try
            {
                if (dateFilter == 1)
                    fromDate = DateTime.Now.AddDays(-1).Date;
                else if (dateFilter == 7)
                    fromDate = DateTime.Now.AddDays(-7).Date;
                else if (dateFilter == 30)
                    fromDate = DateTime.Now.AddDays(-30).Date;
                else if (dateFilter == 60)
                    fromDate = DateTime.Now.AddDays(-60).Date;
                else if (dateFilter == 90)
                    fromDate = DateTime.Now.AddDays(-90).Date;
                else if (dateFilter == 0) //Date Range
                {
                    fromDate = Convert.ToDateTime(sDate);
                    toDate = Convert.ToDateTime(eDate);
                }

                var schTyp = "";

                if (refId == 0)
                    schTyp = "A";
                else
                    schTyp = "S";

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var atndcDtl = _attnContext.GetFullHrEmpAtndcSmry(schTyp, refId, fromDate, toDate, false,empSysId).ToList();

                    return Json(new { ok = true, data = atndcDtl, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        #region Edit Attendance
        public ActionResult EditGet(int atndcSftId)
        {
            try
            {
                //int usrId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var empAtndcView = _attnContext.GetFullHrEmpAtndcSmry("R", atndcSftId, DateTime.Today, DateTime.Today, false, empSysId).FirstOrDefault();

                HrEmpAttdView attdView = new HrEmpAttdView();
                attdView.EfctvDate = Convert.ToDateTime(empAtndcView.AtndcDate);
                attdView.FullName = empAtndcView.FullName;
                attdView.InTime = empAtndcView.InTime.ToString();
                attdView.OutTime = empAtndcView.OutTime.ToString();
                attdView.RecId = empAtndcView.AtndcSftId;
                attdView.AtdRmks = empAtndcView.Status;
                return PartialView("_HrEmpAtndcSft_Edit", attdView);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HrEmpAttdView attdView)
        {
            try
            {
                var hrEmpAtndcSft = _attnContext.GetHrEmpAtndcSftById(attdView.RecId);
                hrEmpAtndcSft.InTime = TimeSpan.Parse(attdView.InTime);
                hrEmpAtndcSft.OutTime = TimeSpan.Parse(attdView.OutTime);

                _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpAtndcSft, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                _attnContext.ReCalcEmpAtndc(attdView.RecId, 0, false);

                return RedirectToAction("Index", "HrEmpAtndcSft"); // .WithSuccess("Attendance", "Record Updated Successfully");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpAtndcSft"); //.WithWarning("Attendance", "Updating Record got failed");
            }
        }
        #endregion Edit Attendance

        #region Edit Shift
        public ActionResult ShiftGet(int atndcSftId)
        {
            try
            {
                //int usrId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var empAtndcView = _attnContext.GetFullHrEmpAtndcSmry("R", atndcSftId, DateTime.Today, DateTime.Today, false,empSysId).FirstOrDefault();
                ViewBag.lstShift = _workShiftContext.GetShifts();
                HrEmpAttdView attdView = new HrEmpAttdView();
                attdView.EmpSysId = empAtndcView.EmpSysId;
                attdView.EfctvDate = Convert.ToDateTime(empAtndcView.AtndcDate);
                attdView.FullName = empAtndcView.FullName;
                attdView.InTime = empAtndcView.InTime.ToString();
                attdView.OutTime = empAtndcView.OutTime.ToString();
                attdView.RecId = empAtndcView.AtndcSftId;
                attdView.SftId = empAtndcView.SftId;
                attdView.AtdRmks = empAtndcView.Status;


                return PartialView("_HrEmpAtndcSft_Shift", attdView);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShiftUpdate(HrEmpAttdView attdView)
        {
            try
            {
                var hrEmpAtndcSft = _attnContext.GetHrEmpAtndcSftById(attdView.RecId);
                hrEmpAtndcSft.SftId = attdView.SftId;

                _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpAtndcSft, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                _attnContext.ReCalcEmpAtndc(attdView.RecId, attdView.SftId, false);

                return RedirectToAction("Index", "HrEmpAtndcSft").WithSuccess("Shift", "Record Updated Successfully");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Shift", "Updating Record got failed");
            }
        }
        #endregion Edit Shift

        #region Mark Absent
        [HttpGet]
        public IActionResult GetMarkAbsent(HrEmpAttdView attdView)
        {
            try
            {
                attdView.AbsntStat = 1;
                ViewBag.lstEmployee = _employeeContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                attdView.FromDate = DateTime.Today.AddDays(-1);
                attdView.ToDate = DateTime.Today.AddDays(-1);
                return PartialView("_HrEmpAtndcSft_MrkAbsnt", attdView);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public IActionResult MarkAbsent(HrEmpAttdView attdView)
        {
            try
            {
                List<HrEmpAtndcSft> hrEmpAtndcSfts = _attnContext.GetHrEmpAtndcSftByEmpSysIdNRng(attdView.EmpSysId, attdView.FromDate, attdView.ToDate).ToList();
                var recCount = 0;
                int[] prsntStstArr = { 1, 11, 12, 112, 13, 14, 5, 51 };
                foreach (HrEmpAtndcSft atndcT in hrEmpAtndcSfts)
                {
                    HrEmpAtndcSft atndc = new HrEmpAtndcSft();
                    atndc = atndcT;
                    if (prsntStstArr.Contains(atndc.AtndcStatId))
                    {
                        atndc.AtndcStatId = 0;
                        atndc.SlryDctFct = 1;
                    }
                    else if (atndc.AtndcStatId == 21)
                        atndc.AtndcStatId = 2;
                    else if (atndc.AtndcStatId == 31)
                        atndc.AtndcStatId = 3;
                    else if (atndc.AtndcStatId == 41)
                        atndc.AtndcStatId = 4;
                    atndc.MnlMdfd = true;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, atndc, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    _attnContext.ReCalcEmpAtndc(atndc.AtndcSftId, 0, true);
                    recCount = +1;
                }
                if (recCount > 0)
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithSuccess("Attendance", "Employee was marked as Absent Successfully");
                else
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "No attendance record found");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "Updating Record got failed");
            }
        }
        #endregion Mark Absent

        #region Reset Absent
        [HttpGet]
        public IActionResult GetResetMarkAbsent(HrEmpAttdView attdView)
        {
            try
            {
                attdView.AbsntStat = 0;
                ViewBag.lstEmployee = _employeeContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                attdView.FromDate = DateTime.Today.AddDays(-1);
                attdView.ToDate = DateTime.Today.AddDays(-1);
                return PartialView("_HrEmpAtndcSft_ResetAbsnt", attdView);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public IActionResult ResetMarkAbsent(HrEmpAttdView attdView)
        {
            try
            {
                List<HrEmpAtndcSft> hrEmpAtndcSfts = _attnContext.GetHrEmpAtndcSftByEmpSysIdNRng(attdView.EmpSysId, attdView.FromDate, attdView.ToDate).ToList();
                var recCount = 0;
                foreach (HrEmpAtndcSft atndc in hrEmpAtndcSfts)
                {
                    _attnContext.ReCalcEmpAtndc(atndc.AtndcSftId, 0, false);
                    recCount = +1;
                }
                if (recCount > 0)
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithSuccess("Attendance", "Resetting marked as Absent Successfully");
                else
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "No attendance record found");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "Updating Record got failed");
            }
        }
        #endregion Reset Absent

        #region Reload Attendace
        [HttpGet]
        public IActionResult GetReloadAtndc(string frmDate, string toDate, int empSysId)
        {
            try
            {
                var attdView = new HrEmpAttdView();
                attdView.EmpSysId = empSysId;
                if (empSysId == 0)
                    attdView.FullName = "All Employees";
                else
                {
                    var emp = _employeeContext.GetEmpGenInfoByEmpSysId(empSysId);
                    if (emp != null)
                        attdView.FullName = emp.FullName;
                }

                attdView.FromDate = Convert.ToDateTime(frmDate);
                attdView.ToDate = Convert.ToDateTime(toDate);

                return PartialView("_HrEmpAtndcSft_ReloadAtndc", attdView);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public IActionResult ReloadAtndc(HrEmpAttdView attdView)
        {
            try
            {
                var result = 0;
                var prcTyp = "";
                if (attdView.EmpSysId == 0)
                    prcTyp = "A";
                else
                    prcTyp = "S";
                //---------- Execute Load Attendance Process ----------//
                var prcSrcDtl = "Reload Attendance - " + prcTyp + " - " + attdView.EmpSysId;
                result = _attnContext.LoadHrEmpAtndc(prcTyp, attdView.EmpSysId, attdView.FromDate, attdView.ToDate, AttnContext.AtndcLdPrcTyp.ATN_RST, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                //----------------------------------------------------//

                if (result > 0)
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithSuccess("Attendance", "Attendance reloaded Successfully");
                else
                    return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "No attendance record found");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpAtndcSft").WithWarning("Attendance", "Attendance reload got failed");
            }
        }
        #endregion Reload Attendance

        #region Validations
        public IActionResult ValidateTimeRange([Bind(Prefix = "InTime")] DateTime InTime, [Bind(Prefix = "OutTime")] DateTime OutTime, [Bind(Prefix = "OvrNghtStat")] bool? OvrNghtStat)
        {

            TimeSpan _minDiff = OutTime - InTime;
            if (OvrNghtStat == false)
            {
                if (_minDiff.TotalMinutes > 0)
                    return Json(true);
            }
            else if (OvrNghtStat == true)
            {
                if (_minDiff.TotalMinutes < 0)
                    return Json(true);
            }

            return Json(false);
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "FromDate")] DateTime? stDate, [Bind(Prefix = "ToDate")] DateTime? endDate)
        {
            if (endDate > stDate)
                return Json(true);
            else
                return Json(false);
        }
        #endregion Validations

        #region Print Attendance Report

        //public long SetupStgngSsnForAtndcRpt(string prcTyp, int empSysId, DateTime frmDate, DateTime toDate, int entId, int usrId)
        [HttpPost]
        public ActionResult PrintAtndcSht(RptParam rptParam)
        {
            try
            {
                var ssnId = _reRptCntxt.SetupStgngSsnForAtndcRpt("A", 0, DateTime.Today, DateTime.Today, 1, 2);
                return Json(new { ok = true, ssnId, message = "ok" });

            }

            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }



        //[HttpPost]
        //public ActionResult PrintAtndcSht(RptParam rptParam)
        //{
        //    try
        //    {
        //        var fromDate = DateTime.Now.AddDays(-1).Date;
        //        var toDate = fromDate;

        //        string prcTyp = "A";
        //        int empSysId = Convert.ToInt32(rptParam.prm3);

        //        if (empSysId > 0)
        //            prcTyp = "S";


        //        if (rptParam.prm5 == 1)
        //            fromDate = DateTime.Now.AddDays(-1).Date;
        //        else if (rptParam.prm5 == 7)
        //            fromDate = DateTime.Now.AddDays(-7).Date;
        //        else if (rptParam.prm5 == 30)
        //            fromDate = DateTime.Now.AddDays(-30).Date;
        //        else if (rptParam.prm5 == 60)
        //            fromDate = DateTime.Now.AddDays(-60).Date;
        //        else if (rptParam.prm5 == 90)
        //            fromDate = DateTime.Now.AddDays(-90).Date;
        //        else if (rptParam.prm5 == 0) //Date Range
        //        {
        //            fromDate = Convert.ToDateTime(rptParam.prm1);
        //            toDate = Convert.ToDateTime(rptParam.prm2);
        //        }

        //        var dsEmpAtndc = _attnContext.GetEmpAtndcforRpt(prcTyp, empSysId, fromDate, toDate);
        //        List<RptParam> dsRptParam = new List<RptParam> { new RptParam { prmTtl = "Attendance Report", prm1 = rptParam.prm1, prm2 = rptParam.prm2, prm3 = "", prm4 = "", prm5 = 0 } };

        //        string mimetype = "";
        //        int extension = 1;
        //        var path = $"{this._webHostEnvironment.WebRootPath}\\Reports\\rptEmpAtndcSht.rdlc";

        //        Dictionary<string, string> parameters = new Dictionary<string, string>();
        //        LocalReport localReport = new LocalReport(path);
        //        localReport.AddDataSource("dsEmpAtndc", dsEmpAtndc);
        //        localReport.AddDataSource("dsRptParam", dsRptParam);

        //        var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);

        //        return File(result.MainStream, "application/pdf");

        //    }

        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, error = ex.Message });
        //    }
        //}
        #endregion Pring Attendance Report
    }
}
