using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.WorkShift.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.WorkShift.Controllers
{
    [Area("WorkShift")]
    [Authorize]
    public class HrWkShiftController : Controller
    {
        WorkShiftContext _workShiftContext;
        private readonly DbCommitContext<HrWkShift> _dbCommitContext;
        private readonly SysSelectListContext _sysSelectListContext;

        public HrWkShiftController(WorkShiftContext workShiftContext,
                                   DbCommitContext<HrWkShift> dbCommitContext,
                                   SysSelectListContext _sysSelectListContext)
        {
            _workShiftContext = workShiftContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("WK_SFT") == null)
                {
                    if (HttpContext.Session.GetInt32("userID") != null)
                    {
                        return RedirectToAction("Index", "Home", new { area = "" }).WithWarning("Work Shift", "Access denied: You do not have permission to access Work Shifts.");
                    }
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }
                ViewBag.DataSource = _workShiftContext.GetAllHrWkShift((int)HttpContext.Session.GetInt32("userEntId")).ToList();
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: HrWkShift/Create
        [HttpGet]
        public IActionResult CreateNew(HrWkShift hrWkShift)
        {
            try
            {
                hrWkShift.StTime = Convert.ToDateTime("2017-01-01 00:00");
                hrWkShift.EndTime = Convert.ToDateTime("2017-01-01 23:59");
                hrWkShift.CutOffTimeStart = Convert.ToDateTime("2017-01-01 00:00");
                hrWkShift.CutOffTimeEnd = Convert.ToDateTime("2017-01-01 23:59");
                return PartialView("_HrWkShift_Create", hrWkShift);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Shift", ex.Message);
            }
        }

        // POST: HrWkShift/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrWkShift hrWkShift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkShift.StTime = Convert.ToDateTime("2017-01-01").AddHours(hrWkShift.StTime.Hour).AddMinutes(hrWkShift.StTime.Minute);
                        hrWkShift.EndTime = Convert.ToDateTime("2017-01-01").AddHours(hrWkShift.EndTime.Hour).AddMinutes(hrWkShift.EndTime.Minute);
                        if(hrWkShift.CutOffTimeStart != null)
                            hrWkShift.CutOffTimeStart = Convert.ToDateTime("2017-01-01").AddHours(hrWkShift.CutOffTimeStart.Value.Hour).AddMinutes(hrWkShift.CutOffTimeStart.Value.Minute);
                        if(hrWkShift.CutOffTimeEnd != null)
                            hrWkShift.CutOffTimeEnd = Convert.ToDateTime("2017-01-01").AddHours(hrWkShift.CutOffTimeEnd.Value.Hour).AddMinutes(hrWkShift.CutOffTimeEnd.Value.Minute);
                        hrWkShift.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrWkShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Work Shift", "Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Work Shift", "Adding Record got failed");
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

        // GET: HrWkShift/Edit
        public ActionResult EditGet(int id)
        {
            var hrWkShift = _workShiftContext.GetAllHrWkShiftById(id);
            hrWkShift.ex_SftCode = hrWkShift.SftCode;
            return PartialView("_HrWkShift_Edit", hrWkShift);
        }

        // POST: HrWkShift/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrWkShift hrWkShift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkShift.StTime = Convert.ToDateTime("2000-01-01").AddHours(hrWkShift.StTime.Hour).AddMinutes(hrWkShift.StTime.Minute);
                        hrWkShift.EndTime = Convert.ToDateTime("2000-01-01").AddHours(hrWkShift.EndTime.Hour).AddMinutes(hrWkShift.EndTime.Minute);
                        if (hrWkShift.CutOffTimeStart != null)
                            hrWkShift.CutOffTimeStart = Convert.ToDateTime("2000-01-01").AddHours(hrWkShift.CutOffTimeStart.Value.Hour).AddMinutes(hrWkShift.CutOffTimeStart.Value.Minute);
                        if (hrWkShift.CutOffTimeEnd != null)
                            hrWkShift.CutOffTimeEnd = Convert.ToDateTime("2000-01-01").AddHours(hrWkShift.CutOffTimeEnd.Value.Hour).AddMinutes(hrWkShift.CutOffTimeEnd.Value.Minute);
                        hrWkShift.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrWkShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Work Shift", "Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Work Shift", "Adding Record got failed");
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

        // GET: HrWkShift/Delete
        public IActionResult DeleteGet(int id, int RstCnt)
        {
            try
            {
                var hrWkShift = _workShiftContext.GetAllHrWkShiftById(id);
                ViewBag.RstrCnt = RstCnt;
                return PartialView("_HrWkShift_Delete", hrWkShift);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Shift", ex.Message);
            }
        }

        // POST: HrWkShift/Delete
        [HttpPost]
        public IActionResult Delete(HrWkShift hrWkShift)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrWkShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Work Shift", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Shift", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "SftCode")] string value, [Bind(Prefix = "ex_SftCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _workShiftContext.GetAllHrWkShiftByCode(value);
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _workShiftContext.GetAllHrWkShiftByCode(value);
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }
    }
}
