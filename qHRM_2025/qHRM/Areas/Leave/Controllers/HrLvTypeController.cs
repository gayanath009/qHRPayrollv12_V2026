using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Leave.Data;
using qHRM.Areas.Leave.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    [Authorize]
    public class HrLvTypeController : Controller
    {
        private readonly LeaveContext _leaveContext;
        private readonly DbCommitContext<HrLvType> _dbCommitContext;

        public HrLvTypeController(LeaveContext leaveContext,
                                      DbCommitContext<HrLvType> dbCommitContext)
        {
            _leaveContext = leaveContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {

            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("LV_TYP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                ViewBag.DataSource = _leaveContext.GetAllLeaveTypeViewsByEntId((int)HttpContext.Session.GetInt32("userEntId"));
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: HrLvType/Create
        [HttpGet]
        public IActionResult CreateNew(HrLvType hrLvTyp)
        {
            try
            {
                return PartialView("_HrLvType_Create", hrLvTyp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Leave Type", ex.Message);
            }
        }

        // POST: HrLvType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrLvType hrLvTyp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrLvTyp.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrLvTyp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Type", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Type", "Adding Record got failed");
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

        // GET: HrLvType/Edit
        public ActionResult EditGet(int id)
        {
            var lvTyp = _leaveContext.GetLeaveTypesById(id);
            lvTyp.ex_LvTypCode = lvTyp.LvTypCode;
            return PartialView("_HrLvType_Edit", lvTyp);
        }

        // POST: HrLvType/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrLvType hrLvTyp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrLvTyp.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrLvTyp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Leave Type", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Leave Type", "Modifying Record got failed");
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

        // GET: HrLvType/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var lvTyp = _leaveContext.GetLeaveTypesById(id);
                return PartialView("_HrLvType_Delete", lvTyp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Leave Type", ex.Message);
            }
        }

        // POST: HrLvType/Delete
        [HttpPost]
        public IActionResult Delete(HrLvType hrLvTyp)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrLvTyp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Leave Type", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Leave Type", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "LvTypCode")] string value, [Bind(Prefix = "ex_LvTypCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _leaveContext .GetLeaveTypeByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _leaveContext.GetLeaveTypeByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }
    }
}