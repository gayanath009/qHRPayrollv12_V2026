using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrAirTicketPriceController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly qHRMasterContext _hrmContext;

        public HrPrAirTicketPriceController(PayrollContext payrollContext, qHRMasterContext hrmContext)
        {
            _payrollContext = payrollContext;
            _hrmContext = hrmContext;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("AIR_TKT") == null) 
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _payrollContext.GetAirTicketPrices();
            return View();
        }

        [HttpGet]
        public IActionResult CreateNew()
        {
            try
            {
                var assignedCountries = _hrmContext.HrPrAirTicketPrice.Where(x => x.RecStat == 1).Select(x => x.CntryId).ToList();
                ViewBag.lstCountry = _hrmContext.OrgCountry
                    .Where(x => x.RecStat == 1 && !assignedCountries.Contains(x.CntryId))
                    .OrderBy(x => x.CntryNameEng)
                    .Select(x => new { CntryId = x.CntryId, CntryName = x.CntryNameEng })
                    .ToList();

                return PartialView("_HrPrAirTicketPrice_Create", new HrPrAirTicketPrice { Price = 0.00m });
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrAirTicketPrice model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int userId = HttpContext.Session.GetInt32("userEmpSysId") ?? HttpContext.Session.GetInt32("userID") ?? 1;
                    _payrollContext.SaveAirTicketPrice(model, userId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Air Ticket Prices", "Price added successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", "Invalid data submitted");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", "Failed: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult EditGet(int id)
        {
            try
            {
                var item = _payrollContext.GetAirTicketPriceById(id);
                if (item == null) return NotFound();

                ViewBag.lstCountry = _hrmContext.OrgCountry
                    .Where(x => x.RecStat == 1)
                    .OrderBy(x => x.CntryNameEng)
                    .Select(x => new { CntryId = x.CntryId, CntryName = x.CntryNameEng })
                    .ToList();

                return PartialView("_HrPrAirTicketPrice_Edit", item);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrAirTicketPrice model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int userId = HttpContext.Session.GetInt32("userEmpSysId") ?? HttpContext.Session.GetInt32("userID") ?? 1;
                    _payrollContext.SaveAirTicketPrice(model, userId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Air Ticket Prices", "Price updated successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", "Invalid data submitted");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Air Ticket Prices", "Failed: " + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("userEmpSysId") ?? HttpContext.Session.GetInt32("userID") ?? 1;
                _payrollContext.DeleteAirTicketPrice(id, userId);
                return Json(new { ok = true, message = "Record deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }
    }
}

