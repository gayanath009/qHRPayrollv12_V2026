using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.Employee.Data;
using qHRM.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpGradeController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpGrade> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpGradeController(EmployeeContext employeeContext,
                                     DbCommitContext<HrEmpGrade> dbCommitContext,
                                     AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ADM_EM_GRD") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });                
                ViewBag.DataSource = _employeeContext.GetAllGradeByEntId((int)HttpContext.Session.GetInt32("userEntId")).ToList();

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRD").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return View();
            }
            catch (Exception)
            {
                throw;
            }

        }

        // GET: HrEmpGrade/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpGrade empGrd)
        {
            try
            {
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRD").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;    
                //End | Usr Prv//
                return PartialView("_HrEmpGrade_Create", empGrd);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpGrade empGrd)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    empGrd.EntId = 1;//?
                    empGrd.RecStat = 1;
                    empGrd.CrtDateTime = DateTime.Today;                    
                    try
                    {
                        int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, empGrd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction(nameof(Index)).WithSuccess("QCS", "Record Added Successfully");
                        else
                            return RedirectToAction(nameof(Index)).WithWarning("QCS", "Adding Record got failed");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("QCS", "Adding Record got failed");
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

        // GET: HrEmpGrade/Edit/5
        public ActionResult EditGet(int id)
        {
            var empGrd = _employeeContext.GetGradeById(id);
            empGrd.ex_GrdCode = empGrd.GrdCode;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRD").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//
            return PartialView("_HrEmpGrade_Edit", empGrd);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpGrade empGrd)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        empGrd.EntId = 1;
                        empGrd.RecStat = 1;
                        empGrd.CrtDateTime = DateTime.Today;
                        var varChanges = _dbCommitContext.commitChanges(LogEventTypes.UPD, empGrd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("QCS", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("QCS", "Adding Record got failed");
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

        // GET: HrEmpGrade/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var empGrd = _employeeContext.GetGradeById(id);
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ADM_EM_GRD").FirstOrDefault();                
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpGrade_Delete", empGrd);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }


        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpGrade empGrd)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, empGrd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("QCS", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }
        }


        public IActionResult IsAlreadyExisting([Bind(Prefix = "GrdCode")] string value, [Bind(Prefix = "ex_GrdCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _employeeContext.GetGradeByCode(value);
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _employeeContext.GetGradeByCode(value);
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}