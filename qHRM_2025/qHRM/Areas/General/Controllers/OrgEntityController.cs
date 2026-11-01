using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.General.Models;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.General.Data;
using qHRM.Data;
using Microsoft.AspNetCore.Http;

namespace qHRM.Areas.General.Controllers
{
    [Area("General")]
    [Authorize]
    public class OrgEntityController : Controller
    {
        private readonly OrganizationContext _organizationContext;

        private readonly DbCommitContext<OrgEntity> _dbCommitContext;

        public OrgEntityController(OrganizationContext organizationContext,
                                    DbCommitContext<OrgEntity> dbCommitContext)
        {
            this._organizationContext = organizationContext;
            _dbCommitContext = dbCommitContext;
        }

        // GET: OrgEntity
        public ActionResult Index()
        {
            try
            {
                ViewBag.DataSource = _organizationContext.GetAllEntityFullViews().ToList();
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgEntity/Create
        [HttpGet]
        public IActionResult CreateNew(OrgEntity orgEnt)
        {
            try
            {
                return PartialView("OrgEntity_Create", orgEnt);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Entity", ex.Message);
            }
        }

        // POST: OrgEntity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgEntity orgEnt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgEnt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Entity", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Entity", "Adding Record got failed");
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

        // GET: OrgEntity/Edit
        public ActionResult EditGet(int entId)
        {
            var oEntity = _organizationContext.GetAllEntities().Where(x => x.EntId == entId).FirstOrDefault();
            oEntity.ex_EntCode = oEntity.EntCode;
            return PartialView("OrgEntity_Edit", oEntity);
        }

        // POST: OrgEntity/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgEntity orgEnt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var varChanges = _dbCommitContext.commitChanges(LogEventTypes.UPD, orgEnt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Entity", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Entity", "Adding Record got failed");
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

        // GET: OrgEntity/Delete
        public IActionResult DeleteGet(int entId)
        {
            try
            {
                var oEntity = _organizationContext.GetAllEntities().Where(x => x.EntId == entId).FirstOrDefault();
                return PartialView("OrgEntity_Delete", oEntity);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Entity", ex.Message);
            }


        }

        // POST: OrgEntity/Delete
        [HttpPost]
        public IActionResult Delete(OrgEntity orgEnt)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgEnt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Entity", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Entity", ex.Message);
            }
        }


        public IActionResult IsAlreadyExisting([Bind(Prefix = "EntCode")] string value, [Bind(Prefix = "ex_EntCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _organizationContext.GetAllEntities().Where(x => x.EntCode.ToLower() == value.ToLower()).FirstOrDefault();
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _organizationContext.GetAllEntities().Where(x => x.EntCode.ToLower() == value.ToLower()).FirstOrDefault();
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}