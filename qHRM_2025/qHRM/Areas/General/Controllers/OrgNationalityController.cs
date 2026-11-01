using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Extensions.Alerts;
using qHRM.Areas.General.Data;
using qHRM.Data;

namespace qHRM.Areas.General.Controllers
{
    [Area("General")]
    //[Authorize]
    public class OrgNationalityController : Controller
    {
        private readonly OrganizationContext _organizationContext;
        private readonly DbCommitContext<OrgNationality> _dbCommitContext;

        public OrgNationalityController(OrganizationContext organizationContext,
                                 DbCommitContext<OrgNationality> dbCommitContext)
        {
            _organizationContext = organizationContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                //if (HttpContext.Session.GetString("ADM_LOC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                ViewBag.DataSource = _organizationContext.GetAllNationalities_N();
                return View(); 
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgNationality/Create
        [HttpGet]
        public IActionResult CreateNew(OrgNationality sysNlty)
        {
            try
            {
                ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
                ViewBag.lstNltyTyp = _organizationContext.GetAllNltyTypView();
                return PartialView("OrgNationality_Create", sysNlty);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Nationality", ex.Message);
            }
        }

        // POST: OrgNationality/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgNationality sysNlty)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, sysNlty, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Nationality", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Nationality", "Adding Record got failed");
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

        // GET: OrgNationality/Edit
        public ActionResult EditGet(int id)
        {
            var nlty = _organizationContext.GetNationalityById(id);
            nlty.ex_NltyNameEng = nlty.NltyNameEng;
            ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
            ViewBag.lstNltyTyp = _organizationContext.GetAllNltyTypView();
            return PartialView("OrgNationality_Edit", nlty);
        }

        // POST: OrgCountry/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgNationality sysNlty)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        sysNlty.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, sysNlty, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Nationality", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Nationality", "Adding Record got failed");
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


        // GET: OrgNationality/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var cntry = _organizationContext.GetNationalityById(id);
                return PartialView("OrgNationality_Delete", cntry);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Nationality", ex.Message);
            }


        }

        // POST: OrgNationality/Delete
        [HttpPost]
        public IActionResult Delete(OrgNationality sysNlty)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, sysNlty, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Nationality", "Record Deleted Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Nationality", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "NltyNameEng")] string value, [Bind(Prefix = "ex_NltyNameEng")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _organizationContext.GetNationalityByNameEng(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _organizationContext.GetNationalityByNameEng(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}