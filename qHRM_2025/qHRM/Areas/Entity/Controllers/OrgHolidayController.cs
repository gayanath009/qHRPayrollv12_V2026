using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.General.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Entity.Controllers
{
    [Area("Entity")]
    [Authorize]
    public class OrgHolidayController : Controller
    {
        private readonly EntityContext _entityContext;
        private readonly DbCommitContext<OrgHoliday> _dbCommitContext;
        private readonly AttnContext _attnContext;
 
        public OrgHolidayController(EntityContext entityContext,
                                        DbCommitContext<OrgHoliday> dbCommitContext,
                                        AttnContext attnContext)
        {
            this._entityContext = entityContext;
            _dbCommitContext = dbCommitContext;
            _attnContext = attnContext;
        }


        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ADM_HLDY") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                ViewBag.DataSource = _entityContext.GetAllOrgHolidaysByYear();
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: OrgHoliday/Create
        [HttpGet]
        public IActionResult CreateNew(OrgHoliday orgHldy)
        {
            try
            {
                orgHldy.StDate = DateTime.Today;
                orgHldy.EndDate = DateTime.Today;
                return PartialView("_OrgHoliday_Create", orgHldy);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Holiday", ex.Message);
            }
        }

        // POST: OrgHoliday/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgHoliday orgHldy)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgHldy, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //---------- Execute Load Attendance Process ----------//
                        var prcSrcDtl = "Add new holidays" + orgHldy.HdId;
                        _attnContext.LoadHrEmpAtndc("A", 0, orgHldy.StDate, orgHldy.EndDate, AttnContext.AtndcLdPrcTyp.HLDY, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //----------------------------------------------------//
                        return RedirectToAction(nameof(Index)).WithSuccess("Holiday", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Holiday", "Adding Record got failed");
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

        // GET: OrgHoliday/Edit
        public ActionResult EditGet(int id)
        {
            var orgHldy = _entityContext.GetOrgHolidayById(id);
            return PartialView("_OrgHoliday_Edit", orgHldy);
        }

        // POST: OrgHoliday/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgHoliday orgHldy)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        orgHldy.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, orgHldy, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //---------- Execute Load Attendance Process ----------//
                        var prcSrcDtl = "Edit holidays" + orgHldy.HdId;
                        _attnContext.LoadHrEmpAtndc("A", 0, orgHldy.StDate, orgHldy.EndDate, AttnContext.AtndcLdPrcTyp.HLDY, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //----------------------------------------------------//
                        return RedirectToAction(nameof(Index)).WithSuccess("Holiday", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Holiday", "Adding Record got failed");
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

        // GET: OrgHoliday/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var orgHldy = _entityContext.GetOrgHolidayById(id);
                return PartialView("_OrgHoliday_Delete", orgHldy);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Holiday", ex.Message);
            }


        }

        // POST: OrgHoliday/Delete
        [HttpPost]
        public IActionResult Delete(OrgHoliday orgHldy)
        {
            try
            {
                var orgHldyD = _entityContext.GetOrgHolidayById(orgHldy.HdId);
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgHldyD, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                //---------- Execute Load Attendance Process ----------//
                var prcSrcDtl = "Delete holidays" + orgHldyD.HdId;
                _attnContext.LoadHrEmpAtndc("A", 0, orgHldyD.StDate, orgHldyD.EndDate, AttnContext.AtndcLdPrcTyp.HLDY, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                //----------------------------------------------------//
                return RedirectToAction(nameof(Index)).WithSuccess("Holiday", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Holiday", ex.Message);
            }
        }
        
        public IActionResult ValidateDateRange([Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            if (endDate >= stDate)
            {
                return Json(true);
            }
            return Json(false);
        }

    }
}
