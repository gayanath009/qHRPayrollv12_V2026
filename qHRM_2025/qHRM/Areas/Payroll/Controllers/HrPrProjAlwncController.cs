using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrProjAlwncController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrProjAlwnc> _dbCommitContext;

        public HrPrProjAlwncController(PayrollContext payrollContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrPrProjAlwnc> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("PRJ_ALWNC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var curEftYM = _payrollContext.GetCurEftYM();

            ViewBag.lstYear = _payrollContext.GetProjAlwncYearList();
            ViewBag.lstMonth = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

            ViewBag.EftYear = curEftYM.EftYear;
            ViewBag.EftMonth = curEftYM.EftMonth;

            ViewBag.DataSource = _payrollContext.GetHrPrProjAlwncViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), curEftYM.EftYear, curEftYM.EftMonth);
            return View();
        }

        // GET: HrPrProjAlwnc/CreateNew
        // GET: HrPrProjAlwnc/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrProjAlwnc hrPrProjAlwnc)
        {
            try
            {
                var curEftYM = _payrollContext.GetCurEftYM();

                hrPrProjAlwnc.EftYear = curEftYM.EftYear;
                hrPrProjAlwnc.EftMonth = curEftYM.EftMonth;

                var lstYears = Enumerable.Range(curEftYM.EftYear - 1, 5).Select(y => new { Indx = y, ElmntTxt = y.ToString() }).ToList();
                var lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
                int userEntId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                var lstEmp = _employeeContext.GetEmployeeViewByEntId(userEntId, DateTime.Today);

                ViewBag.lstYears = lstYears;
                ViewBag.lstMonths = lstMonths;
                ViewBag.lstEmp = lstEmp;

                hrPrProjAlwnc.EftYear = curEftYM.EftYear;
                hrPrProjAlwnc.EftMonth = lstMonths.FirstOrDefault()?.Indx ?? curEftYM.EftMonth;
                if (lstEmp != null && lstEmp.Any())
                {
                    hrPrProjAlwnc.EmpSysId = lstEmp.FirstOrDefault().EmpSysId;
                }

                return PartialView("_HrPrProjAlwnc_Create", hrPrProjAlwnc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", ex.Message);
            }
        }

        // POST: HrPrProjAlwnc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrProjAlwnc hrPrProjAlwnc)
        {
            try
            {
                ModelState.Remove("EmpSys");

                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrProjAlwnc.EndYear = new DateTime(hrPrProjAlwnc.EftYear, hrPrProjAlwnc.EftMonth, 1).AddMonths(hrPrProjAlwnc.Prds - 1).Year;
                        hrPrProjAlwnc.EndMonth = new DateTime(hrPrProjAlwnc.EftYear, hrPrProjAlwnc.EftMonth, 1).AddMonths(hrPrProjAlwnc.Prds - 1).Month;
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrProjAlwnc, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Project Allowance", "Record Added Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Adding Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Validation failed for Project Allowance");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", ex.Message);
            }
        }

        // GET: HrPrProjAlwnc/EditGet
        public IActionResult EditGet(int projAlwncId = 0, int id = 0)
        {
            int targetId = projAlwncId > 0 ? projAlwncId : id;
            if (targetId <= 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Invalid record identifier");
            }

            var hrPrProjAlwnc = _payrollContext.GetHrPrProjAlwncById(targetId);
            if (hrPrProjAlwnc == null)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Record not found");
            }

            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrProjAlwnc.EmpSysId);
            ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

            ViewBag.lstYears = Enumerable.Range(hrPrProjAlwnc.EftYear - 1, 5).Select(y => new { Indx = y, ElmntTxt = y.ToString() }).ToList();
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

            return PartialView("_HrPrProjAlwnc_Edit", hrPrProjAlwnc);
        }

        // POST: HrPrProjAlwnc/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrProjAlwnc hrPrProjAlwnc)
        {
            try
            {
                ModelState.Remove("EmpSys");

                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrProjAlwnc.RecStat = 1;
                        hrPrProjAlwnc.EndYear = new DateTime(hrPrProjAlwnc.EftYear, hrPrProjAlwnc.EftMonth, 1).AddMonths(hrPrProjAlwnc.Prds - 1).Year;
                        hrPrProjAlwnc.EndMonth = new DateTime(hrPrProjAlwnc.EftYear, hrPrProjAlwnc.EftMonth, 1).AddMonths(hrPrProjAlwnc.Prds - 1).Month;
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrProjAlwnc, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Project Allowance", "Record Updated Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Modifying Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Validation failed for Project Allowance");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", ex.Message);
            }
        }

        // GET: HrPrProjAlwnc/Cancel
        public IActionResult CancelGet(int projAlwncId = 0, int id = 0)
        {
            try
            {
                int targetId = projAlwncId > 0 ? projAlwncId : id;
                if (targetId <= 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Invalid record identifier");
                }

                var hrPrProjAlwnc = _payrollContext.GetHrPrProjAlwncById(targetId);
                if (hrPrProjAlwnc == null)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Record not found");
                }

                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrProjAlwnc.EmpSysId);
                ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

                ViewBag.EftMonth = hrPrProjAlwnc.EftYear.ToString() + " - " + new DateTime(2000, hrPrProjAlwnc.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture);
                return PartialView("_HrPrProjAlwnc_Cancel", hrPrProjAlwnc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", ex.Message);
            }
        }

        // POST: HrPrProjAlwnc/Cancel
        [HttpPost]
        public IActionResult Cancel(HrPrProjAlwnc hrPrProjAlwnc)
        {
            try
            {
                var existing = _payrollContext.GetHrPrProjAlwncById(hrPrProjAlwnc.ProjAlwncId);
                if (existing != null)
                {
                    existing.RecStat = 0;
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, existing, userEmpSysId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Project Allowance", "Record Cancelled Successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", "Record not found to cancel");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Project Allowance", ex.Message);
            }
        }

        // GET: HrPrProjAlwnc/DeleteGet (alias for CancelGet)
        [HttpGet]
        public IActionResult DeleteGet(int projAlwncId = 0, int id = 0) => CancelGet(projAlwncId, id);

        // POST: HrPrProjAlwnc/Delete (alias for Cancel)
        [HttpPost]
        public IActionResult Delete(HrPrProjAlwnc hrPrProjAlwnc) => Cancel(hrPrProjAlwnc);

        public IActionResult ValidateAmount([Bind(Prefix = "ProjAlwncAmt")] decimal projAlwncAmt)
        {
            if (projAlwncAmt > 0)
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidatePeriod([Bind(Prefix = "Prds")] decimal Prds)
        {
            if (Prds > 0)
            {
                return Json(true);
            }
            return Json(false);
        }

        [HttpGet]
        public JsonResult GetHrPrProjAlwnc(int year, int month)
        {
            var prjAlwnc = _payrollContext.GetHrPrProjAlwncViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), year, month);

            if (prjAlwnc.Count() > 0)
            {
                return Json(new { ok = true, data = prjAlwnc, message = "ok" });
            }
            else
            { return Json(new { ok = true, data = "", message = "ok" }); }
        }
    }
}
