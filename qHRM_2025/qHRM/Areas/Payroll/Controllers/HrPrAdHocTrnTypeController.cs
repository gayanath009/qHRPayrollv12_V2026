using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using System;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrAdHocTrnTypeController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly DbCommitContext<HrPrAdHocTrnType> _dbCommitContext;

        public HrPrAdHocTrnTypeController(PayrollContext payrollContext,
                                        DbCommitContext<HrPrAdHocTrnType> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("SLRY_EXCLD") == null) throw new Exception("No Access Granted");

            ViewBag.DataSource = _payrollContext.GetHrPrAdHocTrnTypeByEntId((int)HttpContext.Session.GetInt32("userEntId"));
            return View();
        }

        // GET: HrPrAdHocTrnType/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrAdHocTrnType hrPrAdHocTrnType)
        {
            try
            {
                hrPrAdHocTrnType.TrnType = "PAY";
                return PartialView("_HrPrAdHocTrnType_Create", hrPrAdHocTrnType);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustment Types", ex.Message);
            }
        }

        // POST: HrPrAdHocTrnType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrAdHocTrnType hrPrAdHocTrnType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrAdHocTrnType.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        hrPrAdHocTrnType.Indx = _payrollContext.GetMaxHrPrAdHocTrnTypeIndex(hrPrAdHocTrnType.EntId) + 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrAdHocTrnType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Adjustment Types", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Adjustment Types", "Adding Record got failed");
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

        // GET: HrPrAdHocTrnType/EditGet
        public ActionResult EditGet(int id)
        {
            try { 
            var hrPrAdHocTrnType = _payrollContext.GetHrPrAdHocTrnTypeById(id);
            return PartialView("_HrPrAdHocTrnType_Edit", hrPrAdHocTrnType);
            }
            catch
            {
                return View();
            }
        }

        // POST: HrPrAdHocTrnType/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrAdHocTrnType hrPrAdHocTrnType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrAdHocTrnType.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrAdHocTrnType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Adjustment Types", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Adjustment Types", "Modifying Record got failed");
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

        // GET: HrPrAdHocTrnType/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var hrPrAdHocTrnType = _payrollContext.GetHrPrAdHocTrnTypeById(id);
                return PartialView("_HrPrAdHocTrnType_Delete", hrPrAdHocTrnType);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustment Types", ex.Message);
            }
        }

        // POST: HrPrAdHocTrnType/Delete
        [HttpPost]
        public IActionResult Delete(HrPrAdHocTrnType hrPrAdHocTrnType)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrPrAdHocTrnType, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Adjustment Types", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Adjustment Types", ex.Message);
            }
        }
    }
}
