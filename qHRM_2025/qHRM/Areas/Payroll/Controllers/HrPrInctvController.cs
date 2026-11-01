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
    public class HrPrInctvController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrInctv> _dbCommitContext;

        public HrPrInctvController(PayrollContext payrollContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrPrInctv> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("PR_INCTV") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _payrollContext.GetHrPrInctvViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), -1, DateTime.Today).OrderByDescending(x => x.InctvId);
            return View();
        }

        // GET: HrPrInctv/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrInctv hrPrInctv)
        {
            try
            {
                var curEftYM = _payrollContext.GetCurEftYM();

                hrPrInctv.InctvDate = DateTime.Today;
                hrPrInctv.EftYear = curEftYM.EftYear;
                hrPrInctv.EftMonth = curEftYM.EftMonth;

                ViewBag.lstYears = Enumerable.Range(curEftYM.EftYear - 1, 5).Select(y => new { Indx = y, ElmntTxt = y.ToString() }).ToList();
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

                var lstEmp = _employeeContext.GetEmployeeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today);
                var lstInctvMod = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_MOD);
                var lstInctvTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_TYP);

                ViewBag.lstEmp = lstEmp;
                ViewBag.lstInctvMod = lstInctvMod;
                ViewBag.lstInctvTyp = lstInctvTyp;

                hrPrInctv.EmpSysId = lstEmp?.FirstOrDefault()?.EmpSysId ?? 0;
                hrPrInctv.InctvTyp = lstInctvTyp?.FirstOrDefault()?.Indx ?? 0;
                hrPrInctv.InctvMod = lstInctvMod?.FirstOrDefault()?.Indx ?? 0;

                return PartialView("_HrPrInctv_Create", hrPrInctv);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", ex.Message);
            }
        }

        // POST: HrPrInctv/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrInctv hrPrInctv)
        {
            try
            {
                ModelState.Remove("EmpSys");
                ModelState.Remove("ex_RecStatTxt");

                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrInctv.RecStat = 1;
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrInctv, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Incentive", "Record Added Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Adding Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Validation failed for Incentive");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", ex.Message);
            }
        }

        // GET: HrPrInctv/EditGet
        [HttpGet]
        public IActionResult EditGet(int inctvId = 0, int id = 0)
        {
            int targetId = inctvId > 0 ? inctvId : id;
            if (targetId <= 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Invalid record identifier");
            }

            var hrPrInctv = _payrollContext.GetHrPrInctvById(targetId);
            if (hrPrInctv == null)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Record not found");
            }

            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrInctv.EmpSysId);
            ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "Employee #" + hrPrInctv.EmpSysId;

            ViewBag.lstYears = Enumerable.Range(hrPrInctv.EftYear - 1, 5).Select(y => new { Indx = y, ElmntTxt = y.ToString() }).ToList();
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
            ViewBag.lstInctvMod = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_MOD);
            ViewBag.lstInctvTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_TYP);

            return PartialView("_HrPrInctv_Edit", hrPrInctv);
        }

        // POST: HrPrInctv/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrInctv hrPrInctv)
        {
            try
            {
                ModelState.Remove("EmpSys");
                ModelState.Remove("ex_RecStatTxt");

                if (ModelState.IsValid)
                {
                    try
                    {
                        var existing = _payrollContext.GetHrPrInctvById(hrPrInctv.InctvId);
                        if (existing != null)
                        {
                            existing.InctvTyp = hrPrInctv.InctvTyp;
                            existing.InctvMod = hrPrInctv.InctvMod;
                            existing.InctvDate = hrPrInctv.InctvDate;
                            existing.EftYear = hrPrInctv.EftYear;
                            existing.EftMonth = hrPrInctv.EftMonth;
                            existing.Ref = hrPrInctv.Ref;
                            existing.InctvAmt = hrPrInctv.InctvAmt;
                            existing.ShtRmk = hrPrInctv.ShtRmk;
                            existing.Desc = hrPrInctv.Desc;
                            existing.RecStat = 1;

                            int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                            _dbCommitContext.commitChanges(LogEventTypes.UPD, existing, userEmpSysId);
                            return RedirectToAction(nameof(Index)).WithSuccess("Incentive", "Record Updated Successfully");
                        }
                        else
                        {
                            hrPrInctv.RecStat = 1;
                            int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                            _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrInctv, userEmpSysId);
                            return RedirectToAction(nameof(Index)).WithSuccess("Incentive", "Record Updated Successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Modifying Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Validation failed for Incentive");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", ex.Message);
            }
        }

        // GET: HrPrInctv/Cancel
        [HttpGet]
        public IActionResult CancelGet(int inctvId = 0, int id = 0)
        {
            try
            {
                int targetId = inctvId > 0 ? inctvId : id;
                if (targetId <= 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Invalid record identifier");
                }

                var hrPrInctv = _payrollContext.GetHrPrInctvById(targetId);
                if (hrPrInctv == null)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Record not found");
                }

                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrInctv.EmpSysId);
                ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "Employee #" + hrPrInctv.EmpSysId;

                var modElem = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_MOD).FirstOrDefault(x => x.Indx == hrPrInctv.InctvMod);
                ViewBag.InctvModTxt = modElem?.ElmntTxt ?? hrPrInctv.InctvMod.ToString();

                var typElem = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.INCTV_TYP).FirstOrDefault(x => x.Indx == hrPrInctv.InctvTyp);
                ViewBag.InctvTypTxt = typElem?.ElmntTxt ?? hrPrInctv.InctvTyp.ToString();

                string monthName = (hrPrInctv.EftMonth >= 1 && hrPrInctv.EftMonth <= 12)
                    ? new DateTime(2000, hrPrInctv.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture)
                    : hrPrInctv.EftMonth.ToString();
                ViewBag.EftMonth = hrPrInctv.EftYear.ToString() + " - " + monthName;

                return PartialView("_HrPrInctv_Cancel", hrPrInctv);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", ex.Message);
            }
        }

        // POST: HrPrInctv/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(HrPrInctv hrPrInctv)
        {
            try
            {
                var existing = _payrollContext.GetHrPrInctvById(hrPrInctv.InctvId);
                if (existing != null)
                {
                    existing.RecStat = 0;
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, existing, userEmpSysId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Incentive", "Record Cancelled Successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", "Record not found to cancel");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Incentive", ex.Message);
            }
        }

        // GET: HrPrInctv/DeleteGet (alias for CancelGet)
        [HttpGet]
        public IActionResult DeleteGet(int inctvId = 0, int id = 0) => CancelGet(inctvId, id);

        // POST: HrPrInctv/Delete (alias for Cancel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(HrPrInctv hrPrInctv) => Cancel(hrPrInctv);

        public IActionResult ValidateAmount([Bind(Prefix = "InctvAmt")] decimal inctvAmt)
        {
            if (inctvAmt > 0)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}
