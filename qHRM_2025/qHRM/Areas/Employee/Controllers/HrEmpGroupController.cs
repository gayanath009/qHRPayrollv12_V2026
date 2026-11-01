using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Data;
using qHRM.Areas.Employee.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpGroupController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpGroup> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpGroupController(EmployeeContext employeeContext,
                                    DbCommitContext<HrEmpGroup> dbCommitContext,
                                     AdmUserGrpPrvContext _prvContext)
        {
            this._employeeContext = employeeContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = _prvContext;
        }

        // GET: HrEmpGroups
        public ActionResult Index()
        {
            try
            {

                //No Access Granted
                if (HttpContext.Session.GetString("ADM_EM_GRP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });            
                ViewBag.DataSource = _employeeContext.GetAllByEntId((int)HttpContext.Session.GetInt32("userEntId")).ToList();

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;            
                //End | Usr Prv//
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: HrEmpGroup/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpGroup empGrp)
        {
            try
            {
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                //End | Usr Prv//
                return PartialView("_HrEmpGroup_Create", empGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Group", ex.Message);
            }
        }

        // POST: HrEmpGroup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpGroup empGrp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        empGrp.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                        empGrp.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, empGrp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Employee Group", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Employee Group", "Adding Record got failed");
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

        // GET: HrEmpGroup/Edit/5
        public ActionResult EditGet(int id)
        {
            var objEmpGrp = _employeeContext.GetById(id);
            objEmpGrp.ex_GrpCode = objEmpGrp.GrpCode;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRP").FirstOrDefault();
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//
            return PartialView("_HrEmpGroup_Edit", objEmpGrp);
        }

        // POST: HrEmpGroup/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpGroup empGrp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, empGrp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Employee Group", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Employee Group", "Adding Record got failed");
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

        // GET: HrEmpGroup/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var objEmpGrp = _employeeContext.GetById(id);
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRP").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpGroup_Delete", objEmpGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Group", ex.Message);
            }
        }

        // POST: HrEmpGroup/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpGroup empGrp)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, empGrp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Employee Group", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Group", ex.Message);
            }
        }


        public IActionResult IsExisting([Bind(Prefix = "GrpCode")] string value, [Bind(Prefix = "ex_GrpCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _employeeContext.GetByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _employeeContext.GetByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}