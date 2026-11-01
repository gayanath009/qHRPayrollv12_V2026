using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.General.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.General.Data;
using qHRM.Data;
using Microsoft.AspNetCore.Http;

namespace qHRM.Areas.General.Controllers
{
    [Area("General")]
    [Authorize]
    public class OrgInfoController : Controller
    {
        private readonly OrganizationContext _organizationContext;
        private readonly DbCommitContext<OrgInfo> _dbCommitContext;

        public OrgInfoController(OrganizationContext organizationContext,
                                DbCommitContext<OrgInfo> dbCommitContext)
        {
            this._organizationContext = organizationContext;
            _dbCommitContext = dbCommitContext;
        }

        // GET: OrgInfo
        public ActionResult Index()
        {
            try
            {                
                ViewBag.DataSource = _organizationContext.GetOrganizations("_all", 0).ToList();
                return View();
            }
            catch (Exception)
            {
                throw;
            }

        }

        // GET: OrgInfo/Create
        [HttpGet]
        public IActionResult CreateNew(OrgInfo orgInfo)
        {
            try
            {
                return PartialView("_OrgInfo_Create", orgInfo);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("ATS", ex.Message);
            }
        }

        // POST: OrgInfo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgInfo orgInfo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    orgInfo.RecStat = 1;
                    orgInfo.CrtDateTime = DateTime.Today;
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Organization", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Organization", "Adding Record got failed");
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

        // GET: OrgInfo/Edit
        public ActionResult EditGet(int id)
        {
            var orgInfo = _organizationContext.GetOrganizations("_all", 0).Where(x => x.OrgId == id).FirstOrDefault();
            orgInfo.exOrgCode = orgInfo.OrgCode;
            return PartialView("_OrgInfo_Edit", orgInfo);
        }

        // POST: OrgInfo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgInfo orgInfo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        orgInfo.RecStat = 1;
                        orgInfo.CrtDateTime = DateTime.Today;
                        var varChanges = _dbCommitContext.commitChanges(LogEventTypes.UPD, orgInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("ATS", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("ATS", "Adding Record got failed");
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
                var oUsrGrp = _organizationContext.GetOrganizations("_all", 0).Where(x => x.OrgId == id).FirstOrDefault();
                return PartialView("_OrgInfo_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("ATS", ex.Message);
            }


        }

        // POST: OrgInfo/Delete/5
        [HttpPost]
        public IActionResult Delete(OrgInfo orgInfo)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("ATS", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("ATS", ex.Message);
            }
        }


        public IActionResult IsAlreadyExisting([Bind(Prefix = "OrgCode")] string value, [Bind(Prefix = "exOrgCode")] string exvalue)
        {           

            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var itemfound = _organizationContext.GetOrganizations("_all", 0).Where(x => x.OrgCode.ToLower() == value.ToLower()).FirstOrDefault();
                    if (itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var itemfound = _organizationContext.GetOrganizations("_all", 0).Where(x => x.OrgCode.ToLower() == value.ToLower()).FirstOrDefault();
                if (itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}