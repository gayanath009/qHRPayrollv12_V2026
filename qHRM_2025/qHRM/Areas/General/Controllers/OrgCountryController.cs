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
    public class OrgCountryController : Controller
    {

        private readonly OrganizationContext _organizationContext;
        private readonly DbCommitContext<OrgCountry> _dbCommitContext;

        public OrgCountryController(OrganizationContext organizationContext,
                                 DbCommitContext<OrgCountry> dbCommitContext)
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

                ViewBag.DataSource = _organizationContext.GetAllCountries_N();
                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: OrgCountry/Create
        [HttpGet]
        public IActionResult CreateNew(OrgCountry sysCntry)
        {
            try
            {
                return PartialView("OrgCountry_Create", sysCntry);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Country", ex.Message);
            }
        }

        // POST: OrgCountry/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrgCountry sysCntry)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, sysCntry, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Country", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Country", "Adding Record got failed");
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

        // GET: OrgCountry/Edit
        public ActionResult EditGet(int id)
        {
            var cntry = _organizationContext.GetCountryById(id);
            cntry.ex_CntryNameEng = cntry.CntryNameEng;
            return PartialView("OrgCountry_Edit", cntry);
        }

        // POST: OrgCountry/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrgCountry sysCntry)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, sysCntry, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Country", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Country", "Adding Record got failed");
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


        // GET: OrgCountry/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var cntry = _organizationContext.GetCountryById(id);
                return PartialView("OrgCountry_Delete", cntry);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Country", ex.Message);
            }


        }

        // POST: OrgCountry/Delete
        [HttpPost]
        public IActionResult Delete(OrgCountry cntry)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, cntry, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Country", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Country", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "CntryNameEng")] string value, [Bind(Prefix = "ex_CntryNameEng")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _organizationContext.GetCountryByNameEng(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _organizationContext.GetCountryByNameEng(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}