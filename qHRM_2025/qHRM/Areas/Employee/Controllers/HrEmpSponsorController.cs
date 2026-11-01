using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpSponsorController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpSponsor> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;



        public HrEmpSponsorController(EmployeeContext employeeContext,
                                       SysSelectListContext sysSelectListContext,
                                       DbCommitContext<HrEmpSponsor> dbCommitContext,
                                       AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._sysSelectListContext = sysSelectListContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpSponsor
        public ActionResult Index(int id)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_SPR_DTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                ViewBag.DataSource = _employeeContext.GetSponsorsByEmpSysId(id).ToList();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_SPR_DTL").FirstOrDefault();
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

        // GET: HrEmpSponsor/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpSponsor spnsr)
        {
            try
            {
                ViewBag.lstSpnsrType = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.SPNSR_TYP);
                ViewBag.lstRltnType = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.RLTN_TYP);
                spnsr.StDate = DateTime.Today;
                spnsr.EndDate = DateTime.Today;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_SPR_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;             
                //End | Usr Prv//

                return PartialView("_HrEmpSponsor_Create", spnsr);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpSponsor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpSponsor spnsr)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        spnsr.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                        int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, spnsr, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithSuccess("Sponsorship", "Record Added Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Adding Record got failed");
                    }
                    catch (Exception)
                    {

                        return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId });
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpSponsor/Edit
        public ActionResult EditGet(int id)
        {
            var spnsr = _employeeContext.GetSponsorById(id).FirstOrDefault();
            ViewBag.lstSpnsrType = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.SPNSR_TYP);
            ViewBag.lstRltnType = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.RLTN_TYP);
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_SPR_DTL").FirstOrDefault();
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//

            return PartialView("_HrEmpSponsor_Edit", spnsr);
        }

        // POST: HrEmpSponsor/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpSponsor spnsr)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        spnsr.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                        var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, spnsr, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithSuccess("Sponsorship", "Record Updated Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Updating Record got failed");
                    }
                    catch (Exception)
                    {

                        return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Updating Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId });
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpSponsor/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {

                var spnsr = _employeeContext.GetSponsorById(id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_SPR_DTL").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpSponsor_Delete", spnsr);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpSponsor/Delete
        [HttpPost]
        public IActionResult Delete(HrEmpSponsor spnsr)
        {
            try
            {
                spnsr.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, spnsr, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithSuccess("Sponsorship", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Deleting Record got failed");

            }
            catch
            {
                return RedirectToAction("Index", "HrEmpSponsor", new { @id = spnsr.EmpSysId }).WithWarning("Sponsorship", "Deleting Record got failed");
            }
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            if (endDate > stDate)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}
