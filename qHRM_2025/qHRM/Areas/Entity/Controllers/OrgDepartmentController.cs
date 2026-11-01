using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Extensions.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Data;
using qHRM.Areas.Entity.Data;

namespace qHRM.Areas.Entity.Controllers
{
    [Area("Entity")]
    [Authorize]
    public class OrgDepartmentController : Controller 
    {
        private readonly EntityContext _entityContext;
        private readonly DbCommitContext<OrgDepartment> _dbCommitContext;

        public OrgDepartmentController(EntityContext entityContext,
                                        DbCommitContext<OrgDepartment> dbCommitContext)
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
                if (HttpContext.Session.GetString("ADM_DEPT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                ViewBag.DataSource = _entityContext.GetDepartmentEntFullViews((int)HttpContext.Session.GetInt32("userEntId")).ToList();
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgDesignation/Create
        [HttpGet]
        public IActionResult CreateNew(OrgDepartment orgDept)
        {
            try
            {
                ViewBag.ParentDepartments = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                return PartialView("_OrgDepartment_Create", orgDept);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Department", ex.Message);
            }
        }

        // POST: OrgDesignation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgDepartment orgDept)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    orgDept.EntId = (int)HttpContext.Session.GetInt32("userEntId");

                    try
                    {
                        orgDept.SrtOrd = _entityContext.GetDepartmentMaxSrtOrd() + 10;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, orgDept, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Department", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Department", "Adding Record got failed");
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
            var oDepartment = _entityContext.GetDepartmentById(id);
            oDepartment.ex_DeptCode = oDepartment.DeptCode;
            ViewBag.ParentDepartments = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId")).Where(x => x.DeptId != id);
            return PartialView("_OrgDepartment_Edit", oDepartment);
        }

        // POST: OrgInfo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgDepartment orgDept)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, orgDept, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Department", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Department", "Modifying Record got failed");
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
                var oUsrGrp = _entityContext.GetDepartmentById(id);
                return PartialView("_OrgDepartment_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Department", ex.Message);
            }


        }

        // POST: OrgInfo/Delete/5
        [HttpPost]
        public IActionResult Delete(OrgDepartment orgDept)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, orgDept, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Department", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Department", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "DeptCode")] string value, [Bind(Prefix = "ex_DeptCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _entityContext.GetDepartmentByCode(value);
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _entityContext.GetDepartmentByCode(value);
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}
