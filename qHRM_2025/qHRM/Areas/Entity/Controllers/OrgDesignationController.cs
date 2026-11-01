using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Data;
using qHRM.Areas.Entity.Data;

namespace qHRM.Areas.Entity.Controllers
{
    [Area("Entity")]
    [Authorize]
    public class OrgDesignationController : Controller
    {
        private readonly EntityContext _entityContext;
        private readonly DbCommitContext<OrgDesignation> _dbCommitContext;

        public OrgDesignationController(EntityContext entityContext,
                                        DbCommitContext<OrgDesignation> dbCommitContext)
        {
            _entityContext = entityContext;
            _dbCommitContext = dbCommitContext;
        }

        // GET: OrgInfo
        public ActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ADM_DESG") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                
                ViewBag.DataSource = _entityContext.GetDesignations("_all", 0).ToList();
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgDesignation/Create
        [HttpGet]
        public IActionResult CreateNew(OrgDesignation orgDesig)
        {
            try
            {
                return PartialView("_OrgDesignation_Create", orgDesig);//need to create partial view kiran
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Designation", ex.Message);
            }
        }

        // POST: OrgDesignation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgDesignation orgDesig)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //kiran temporary make entity to 1
                    orgDesig.EntId = 1;

                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgDesig, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Designation", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Designation", "Adding Record got failed");
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

        // GET: OrgDesignation/Edit/5
        public ActionResult EditGet(int id)
        {
            var oDesignation = _entityContext.GetDesignations("_all", 0).Where(x => x.DesigId == id).FirstOrDefault();

            //oUsrGrp.exOrgCode = oUsrGrp.OrgCode; check what is this kiran

            oDesignation.ex_DesigCode = oDesignation.DesigCode;
            return PartialView("_OrgDesignation_Edit", oDesignation);
        }

        // POST: OrgInfo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgDesignation orgDesig)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, orgDesig, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Designation", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Designation", "Adding Record got failed");
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

        // GET: OrgInfo/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var oUsrGrp = _entityContext.GetDesignations("_all", 0).Where(x => x.DesigId == id).FirstOrDefault();
                return PartialView("_OrgDesignation_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Designation", ex.Message);
            }


        }

        // POST: OrgInfo/Delete/5
        [HttpPost]
        public IActionResult Delete(OrgDesignation orgDesig)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgDesig, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Designation", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Designation", ex.Message);
            }
        }


        public IActionResult IsAlreadyExisting([Bind(Prefix = "DesigCode")] string value, [Bind(Prefix = "ex_DesigCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _entityContext.GetDesignations("_all", 0).Where(x => x.DesigCode.ToLower() == value.ToLower()).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _entityContext.GetDesignations("_all", 0).Where(x => x.DesigCode.ToLower() == value.ToLower()).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}
