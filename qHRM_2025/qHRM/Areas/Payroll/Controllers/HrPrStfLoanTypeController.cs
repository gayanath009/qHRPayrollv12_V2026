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
    public class HrPrStfLoanTypeController : Controller
    {
        private readonly PrStfLoanContext _prStfLoanContext;
        private readonly DbCommitContext<HrPrStfLoanType> _dbCommitContext;


        public HrPrStfLoanTypeController(PrStfLoanContext prStfLoanContext,
                                        DbCommitContext<HrPrStfLoanType> dbCommitContext)
        {
            _prStfLoanContext = prStfLoanContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("STF_LN_TYP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _prStfLoanContext.GetHrPrStfLoanTypeByEntId((int)HttpContext.Session.GetInt32("userEntId"));
            return View(ViewBag.DataSource);
        }

        // GET: HrPrStfLoanType/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrStfLoanType hrPrStfLoanType)
        {
            try
            {
                return PartialView("_HrPrStfLoanType_Create", hrPrStfLoanType);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Types", ex.Message);
            }
        }

        // POST: HrPrStfLoanType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrStfLoanType hrPrStfLoanType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanType.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrStfLoanType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Staff Loan Types", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Types", "Adding Record got failed");
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

        // GET: HrPrStfLoanType/EditGet
        public ActionResult EditGet(int id)
        {
            var hrPrStfLoanType = _prStfLoanContext.GetHrPrStfLoanTypeById(id);
            if (hrPrStfLoanType == null)
            {
                return NotFound();
            }

            hrPrStfLoanType.ex_LnTypCode = hrPrStfLoanType.LnTypCode;
            return PartialView("_HrPrStfLoanType_Edit", hrPrStfLoanType);
        }

        // POST: HrPrStfLoanType/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrStfLoanType hrPrStfLoanType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanType.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrStfLoanType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Staff Loan Types", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Types", "Modifying Record got failed");
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

        // GET: HrPrStfLoanType/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var hrPrStfLoanType = _prStfLoanContext.GetHrPrStfLoanTypeById(id);
                return PartialView("_HrPrStfLoanType_Delete", hrPrStfLoanType);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Types", ex.Message);
            }
        }

        // POST: HrPrStfLoanType/Delete
        [HttpPost]
        public IActionResult Delete(HrPrStfLoanType hrPrStfLoanType)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrPrStfLoanType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Staff Loan Types", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Types", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "LnTypCode")] string value, [Bind(Prefix = "ex_LnTypCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _prStfLoanContext.GetHrPrStfLoanTypeByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _prStfLoanContext.GetHrPrStfLoanTypeByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }
    }
}
