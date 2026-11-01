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
    public class HrPrAdHocTrnsController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrAdHocTrns> _dbCommitContext;

        public HrPrAdHocTrnsController(PayrollContext payrollContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrPrAdHocTrns> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("AD_HOC_TR") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _payrollContext.GetHrPrAdHocTrnViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), -1);
            return View();
        }

        // GET: HrPrAdHocTrns/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrAdHocTrns hrPrAdHocTrns)
        {
            try
            {
                hrPrAdHocTrns.TrnDate = DateTime.Today;
                hrPrAdHocTrns.EftMonth = DateTime.Today.Month;
                hrPrAdHocTrns.EftYear = DateTime.Today.Year;

                var lstEmp = _employeeContext.GetEmployeeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today);
                var lstTrnTyp = _payrollContext.GetHrPrAdHocTrnTypeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"));
                var lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

                ViewBag.lstEmp = lstEmp;
                ViewBag.lstTrnTyp = lstTrnTyp;
                ViewBag.lstMonths = lstMonths;

                hrPrAdHocTrns.EmpSysId = lstEmp.FirstOrDefault().EmpSysId;
                hrPrAdHocTrns.TrnTypId = lstTrnTyp.FirstOrDefault().TrnTypId;

                return PartialView("_HrPrAdHocTrns_Create", hrPrAdHocTrns);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Ad-hoc Payment / Deductions", ex.Message);
            }
        }

        // POST: HrPrAdHocTrns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrAdHocTrns hrPrAdHocTrns)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrAdHocTrns, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Ad-hoc Payment / Deductions", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Ad-hoc Payment / Deductions", "Adding Record got failed");
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

        // GET: HrPrAdHocTrns/EditGet
        public IActionResult EditGet(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Invalid record identifier");
            }

            var hrPrAdHocTrns = _payrollContext.GetHrPrAdHocTrnById(id);
            if (hrPrAdHocTrns == null)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Record not found");
            }

            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrAdHocTrns.EmpSysId);
            ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

            var trnTyp = _payrollContext.GetHrPrAdHocTrnTypeById(hrPrAdHocTrns.TrnTypId);
            ViewBag.TrnTyp = (trnTyp != null) ? trnTyp.TrnType + " - " + trnTyp.TrnTypTxt : "N/A";
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

            return PartialView("_HrPrAdHocTrns_Edit", hrPrAdHocTrns);
        }

        // POST: HrPrAdHocTrns/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrAdHocTrns hrPrAdHocTrns)
        {
            try
            {
                ModelState.Remove("EmpSys");
                ModelState.Remove("PrcLog");
                ModelState.Remove("TrnTyp");

                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrAdHocTrns.RecStat = 1;
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrAdHocTrns, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Adjustments", "Record Updated Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Modifying Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Validation failed for Adjustment record");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", ex.Message);
            }
        }

        // GET: HrPrAdHocTrns/Cancel
        public IActionResult CancelGet(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Invalid record identifier");
                }

                var hrPrAdHocTrns = _payrollContext.GetHrPrAdHocTrnById(id);
                if (hrPrAdHocTrns == null)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Record not found");
                }

                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrAdHocTrns.EmpSysId);
                ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

                var trnTyp = _payrollContext.GetHrPrAdHocTrnTypeById(hrPrAdHocTrns.TrnTypId);
                ViewBag.TrnTyp = (trnTyp != null) ? trnTyp.TrnType + " - " + trnTyp.TrnTypTxt : "N/A";

                ViewBag.EftMonth = hrPrAdHocTrns.EftYear.ToString() + " - " + new DateTime(2000, hrPrAdHocTrns.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture);

                return PartialView("_HrPrAdHocTrns_Cancel", hrPrAdHocTrns);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", ex.Message);
            }
        }

        // POST: HrPrAdHocTrns/Cancel
        [HttpPost]
        public IActionResult Cancel(HrPrAdHocTrns hrPrAdHocTrns)
        {
            try
            {
                var existing = _payrollContext.GetHrPrAdHocTrnById(hrPrAdHocTrns.TrnId);
                if (existing != null)
                {
                    existing.RecStat = 0; 
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, existing, userEmpSysId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Adjustments", "Record Cancelled Successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", "Record not found to cancel");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustments", ex.Message);
            }
        }

        // GET: HrPrAdHocTrns/DeleteGet (alias for CancelGet)
        [HttpGet]
        public IActionResult DeleteGet(int id) => CancelGet(id);

        // POST: HrPrAdHocTrns/Delete (alias for Cancel)
        [HttpPost]
        public IActionResult Delete(HrPrAdHocTrns hrPrAdHocTrns) => Cancel(hrPrAdHocTrns);

        public IActionResult ValidateAmount([Bind(Prefix = "TrnAmt")] decimal trnAmt)
        {
            if (trnAmt > 0)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}
