using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.WorkShift.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.WorkShift.Controllers
{
    [Area("WorkShift")]
    [Authorize]
    public class HrWkRosterController : Controller
    {
        WorkShiftContext _workShiftContext;
        private readonly DbCommitContext<HrWkRoster> _dbCommitContext;
        private readonly DbCommitContext<HrWkRosterShift> _dbCommitContext1;
        private readonly SysSelectListContext _sysSelectListContext;

        public HrWkRosterController(WorkShiftContext workShiftContext,
                                    SysSelectListContext sysSelectListContext,
                                    DbCommitContext<HrWkRoster> dbCommitContext,
                                    DbCommitContext<HrWkRosterShift> dbCommitContext1)
        {
            _workShiftContext = workShiftContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
            _dbCommitContext1 = dbCommitContext1;
        }

        public IActionResult Index(int rstId)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("WK_RST") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var dsRst = _workShiftContext.GetAllHrWkRoster((int)HttpContext.Session.GetInt32("userEntId")).ToList();
                ViewBag.DsRst = dsRst;
                if (rstId == 0 && dsRst.Count > 0)
                    ViewBag.DsRstSftDtls = _workShiftContext.GetShiftDataByRstId(dsRst.FirstOrDefault().RstId);
                else
                    ViewBag.DsRstSftDtls = _workShiftContext.GetShiftDataByRstId(rstId);
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public JsonResult GetShiftDataByRstId(int rstId)
        {
            var result = _workShiftContext.GetShiftDataByRstId(rstId);
            if (result.Count() > 0)
            {
                return Json(new { ok = true, data = result, message = "ok" });
            }
            else
            { return Json(new { ok = true, data = "", message = "ok" }); }
        }

        [HttpGet]
        public IActionResult RstCreateNew(HrWkRoster hrWkRoster)
        {
            try
            {
                return PartialView("_HrWkRoster_Create", hrWkRoster);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Roster", ex.Message);
            }
        }

        // POST: HrWkRoster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrWkRoster hrWkRoster)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkRoster.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrWkRoster, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Work Roster", "Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Work Roster", "Adding Record got failed");
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

        // GET: HrWkRoster/Detail
        public ActionResult RstDetailGet(int rstId)
        {
            var hrWkRoster = _workShiftContext.GetHrWkRosterByRstId(rstId);
            hrWkRoster.ex_RstCode = hrWkRoster.RstCode;
            return PartialView("_HrWkRoster_Edit", hrWkRoster);
        }

        // GET: HrWkRoster/Edit
        public ActionResult RstEditGet(int rstId)
        {
            var hrWkRoster = _workShiftContext.GetHrWkRosterByRstId(rstId);
            hrWkRoster.ex_RstCode = hrWkRoster.RstCode;
            return PartialView("_HrWkRoster_Edit", hrWkRoster);
        }

        // POST: HrWkRoster/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrWkRoster hrWkRoster)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkRoster.RecStat = 1;
                        hrWkRoster.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrWkRoster, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Work Roster", "Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Work Roster", "Adding Record got failed");
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

        public IActionResult IsAlreadyExist([Bind(Prefix = "RstCode")] string value, [Bind(Prefix = "ex_RstCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _workShiftContext.GetHrWkRosterByRstCode(value);
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _workShiftContext.GetHrWkRosterByRstCode(value);
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }

        #region Roster Shift
        // GET: HrWkRoster/GetRstSftCreate
        public IActionResult GetRstSftCreate(int rstId)
        {
            try
            {
                var hrWkRosterShift = new HrWkRosterShift();
                hrWkRosterShift.RstId = rstId;
                ViewBag.RstName = _workShiftContext.GetHrWkRosterByRstId(hrWkRosterShift.RstId).NameEng;
                var lstDays = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DAYS);
                ViewBag.lstDays = lstDays;
                return PartialView("_HrWkRosterShift_Create", hrWkRosterShift);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Roster Shift", ex.Message);
            }
        }

        // POST: HrWkRoster/CreateSft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSft(HrWkRosterShift hrWkRosterShift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkRosterShift.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext1.commitChanges(LogEventTypes.INS, hrWkRosterShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId }).WithSuccess("Roster Shift", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId }).WithWarning("Roster Shift", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrWkRoster/GetRstSftEdit
        public IActionResult GetRstSftEdit(int schId)
        {
            try
            {
                var hrWkRosterShift = new HrWkRosterShift();
                hrWkRosterShift = _workShiftContext.GetHrWkRosterShiftBySchId(schId);
                ViewBag.RstName = _workShiftContext.GetHrWkRosterByRstId(hrWkRosterShift.RstId).NameEng;
                ViewBag.lstDays = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DAYS);
                ViewBag.lstSft = _workShiftContext.GetShiftByRstIdDay(hrWkRosterShift.RstId, hrWkRosterShift.Day, hrWkRosterShift.SftId);
                //ViewBag.lstSft = _workShiftContext.GetShiftVws((int)HttpContext.Session.GetInt32("userEntId"));
                return PartialView("_HrWkRosterShift_Edit", hrWkRosterShift);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Roster Shift", ex.Message);
            }
        }

        // POST: HrWkRoster/EditSft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSft(HrWkRosterShift hrWkRosterShift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkRosterShift.RecStat = 1;
                        _dbCommitContext1.commitChanges(LogEventTypes.UPD, hrWkRosterShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId }).WithSuccess("Roster Shift", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId }).WithWarning("Roster Shift", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrWkRoster", new { rstId = hrWkRosterShift.RstId });
                }
            }
            catch
            {
                return View();
            }
        }

        // POST: HrWkRoster/DeleteSft
        [HttpGet]
        //[ValidateAntiForgeryToken]
        public JsonResult DeleteSft(int schId)
        {
            try
            {
                var hrWkRosterShift = new HrWkRosterShift();
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrWkRosterShift = _workShiftContext.GetDetachedHrWkRosterShiftBySchId(schId);
                        hrWkRosterShift.RecStat = 0;
                        int xx =  _dbCommitContext1.commitChanges(LogEventTypes.UPD, hrWkRosterShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return Json(new { ok = true, data = xx, message = "ok" });
                    }
                    catch (Exception)
                    {
                        return Json(new { ok = true, data = "Error", message = "ok" });
                    }
                }
                else
                {
                    return Json(new { ok = true, data = "Error", message = "ok" });
                }
            }
            catch
            {
                return Json(new { ok = true, data = "Error", message = "ok" });
            }
        }

        [HttpGet]
        public JsonResult GetShiftByRstIdDay(int rstId, string day, int sftId)
        {
            try { 
                var result = _workShiftContext.GetShiftByRstIdDay(rstId, day, sftId);
                return Json(new { ok = true, data = result, message = "ok" });
            }
            catch
            { return Json(new { ok = true, data = "Error", message = "ok" }); }
        }
        #endregion Roster Shift
    }
}
