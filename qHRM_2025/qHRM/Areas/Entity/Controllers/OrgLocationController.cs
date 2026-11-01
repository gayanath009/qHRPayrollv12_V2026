using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.Entity.Data;
using qHRM.Data;
using qHRM.Areas.General.Data;

namespace qHRM.Areas.Entity.Controllers
{
    [Area("Entity")]
    [Authorize]
    public class OrgLocationController : Controller
    {
        private readonly EntityContext _entityContext;
        private readonly OrganizationContext _organizationContext;
        private readonly DbCommitContext<OrgLocation> _dbCommitContext;

        public OrgLocationController(EntityContext entityContext,
                                        OrganizationContext organizationContext,
                                        DbCommitContext<OrgLocation> dbCommitContext)
        {
            this._entityContext = entityContext;
            this._organizationContext = organizationContext;
            _dbCommitContext = dbCommitContext;
        }

        // GET: OrgLocations
        public ActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ADM_LOC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                ViewBag.DataSource = _entityContext.GetLocFullViews((int)HttpContext.Session.GetInt32("userEntId"));
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgLocation/Create
        [HttpGet]
        public IActionResult CreateNew(OrgLocation orgLoc)
        {
            try
            {
                var entId = 1;
                ViewBag.lstPrntLoc = _entityContext.GetParentLocation(entId, 0).ToList();
                return PartialView("OrgLocation_Create", orgLoc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Location", ex.Message);
            }
        }

        // POST: OrgLocation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgLocation orgLoc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        orgLoc.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgLoc, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Location", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Location", "Adding Record got failed");
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

        // GET: OrgLocation/Edit
        public ActionResult EditGet(int id)
        {
            var entId = (int)HttpContext.Session.GetInt32("userEntId");
            var objLoc = _entityContext.GetAllLocations().Where(x => x.LocId == id).FirstOrDefault();
            ViewBag.lstPrntLoc = _entityContext.GetParentLocation(entId, id).ToList();
            objLoc.ex_LocCode = objLoc.LocCode;
            return PartialView("_OrgLocation_Edit", objLoc);
        }

        // POST: OrgLocation/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgLocation orgLoc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, orgLoc, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Location", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Location", "Adding Record got failed");
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

        // GET: OrgLocation/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var objLoc = _entityContext.GetAllLocations().Where(x => x.LocId == id).FirstOrDefault();
                return PartialView("_OrgLocation_Delete", objLoc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Location", ex.Message);
            }


        }

        // POST: OrgLocation/Delete
        [HttpPost]
        public IActionResult Delete(OrgLocation orgLoc)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgLoc, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Location", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Location", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "LocCode")] string value, [Bind(Prefix = "ex_LocCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _entityContext.GetAllLocations().Where(x => x.LocCode.ToLower() == value.ToLower()).FirstOrDefault();
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _entityContext.GetAllLocations().Where(x => x.LocCode.ToLower() == value.ToLower()).FirstOrDefault();
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}