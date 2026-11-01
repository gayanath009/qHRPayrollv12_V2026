using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrOtrateController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly DbCommitContext<HrPrOtrate> _dbCommitContext;

        public HrPrOtrateController(PayrollContext payrollContext,
                                    DbCommitContext<HrPrOtrate> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("OT_RT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _payrollContext.GetAllActiveHrPrOtRateByEntId((int)HttpContext.Session.GetInt32("userEntId"));
            return View();
        }

        // GET: HrPrOtrate/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrOtrate hrPrOtrate)
        {
            try
            {
                return PartialView("_HrPrOtrate_Create", hrPrOtrate);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("OT Rate", ex.Message);
            }
        }

        // POST: HrPrOtrate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrOtrate hrPrOtrate)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrOtrate.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrOtrate, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("OT Rate", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("OT Rate", "Adding Record got failed");
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

        // GET: HrPrOtrate/EditGet
        public ActionResult EditGet(int id)
        {
            var hrPrOtrate = _payrollContext.GetOtRateById(id);
            hrPrOtrate.ex_OtCode = hrPrOtrate.OtCode;
            return PartialView("_HrPrOtrate_Edit", hrPrOtrate);
        }

        // POST: HrPrOtrate/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrOtrate hrPrOtrate)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrOtrate.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        hrPrOtrate.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrOtrate, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("OT Rate", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("OT Rate", "Modifying Record got failed");
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

        // GET: HrPrOtrate/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var hrPrOtrate = _payrollContext.GetOtRateById(id);
                return PartialView("_HrPrOtrate_Delete", hrPrOtrate);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("OT Rate", ex.Message);
            }
        }

        // POST: HrPrOtrate/Delete
        [HttpPost]
        public IActionResult Delete(HrPrOtrate lvGrpPolicy)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, lvGrpPolicy, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("OT Rate", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("OT Rate", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "OtCode")] string value, [Bind(Prefix = "ex_OtCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _payrollContext.GetOtRateByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _payrollContext.GetOtRateByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }
    }
}
