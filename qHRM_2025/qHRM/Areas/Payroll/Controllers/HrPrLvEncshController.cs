using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using System;
using System.Linq;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrLvEncshController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _empContext;
        private readonly DbCommitContext<HrPrLvEncsh> _dbCommitContext;

        public HrPrLvEncshController(PayrollContext payrollContext
                                        , EmployeeContext empContext
                                        , DbCommitContext<HrPrLvEncsh> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
            _empContext = empContext;
        }

        public IActionResult Index()  
        {
            //No Access Granted
            int usrId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
            if (HttpContext.Session.GetString("LV_ENCSH") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.lstEmployee = _empContext.GetEmpView("ACTV", 0, usrId).OrderBy(o => o.FullName);

            ViewBag.DataSource = _payrollContext.GetHrPrLvSlryVw((int)HttpContext.Session.GetInt32("userEntId"));
            return View();
        }

        // GET: HrPrLvEncsh/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrLvEncsh hrPrLvEncsh)
        {
            try
            {
                return PartialView("_HrPrLvEncsh_Create", hrPrLvEncsh);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Leave Encashment", ex.Message);
            }
        }

        // POST: HrPrOtrate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrLvEncsh hrPrLvEncsh)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrLvEncsh, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Encashment", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Encashment", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                return View();
            }
        }

        #region Data loading script functions
        [HttpGet]
        public JsonResult GetEmpData(int empSysId)
        {
            try
            {
                var lvSlryEmp = _payrollContext.GetEmpInfoForLvSlry(empSysId);
                if (lvSlryEmp != null)
                    return Json(new { ok = true, data = lvSlryEmp, message = "ok" });
                else
                    return Json(new { ok = false, data = 0, message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult GetClacResult(int empSysId, DateTime frmDt, DateTime toDt, bool lstMnthSlry, bool prjAlwnc, bool tktFee, decimal tktFeeVal)
        {
            try {
                string prcTyp = "S";
                int lvTypId = 11;
                var lvSlryClcRsltVw = _payrollContext.GetLvSlryClcRslt(prcTyp, empSysId, frmDt, toDt, lvTypId, lstMnthSlry, prjAlwnc, tktFee, tktFeeVal);
                return Json(new { ok = true, data = lvSlryClcRsltVw, message = "ok" });
            }
            catch(Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        #endregion Data loading script functions
    }
}
