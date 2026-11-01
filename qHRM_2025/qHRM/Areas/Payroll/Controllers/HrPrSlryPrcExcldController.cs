using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrSlryPrcExcldController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrSlryPrcExcld> _dbCommitContext;

        public HrPrSlryPrcExcldController(PayrollContext payrollContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrPrSlryPrcExcld> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            try
            {
                if (HttpContext.Session.GetString("SLRY_EXCLD") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                ViewBag.DataSource = _payrollContext.GetAllHrPrSlryPrcExcldVwByEntId((int)HttpContext.Session.GetInt32("userEntId"));
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: HrPrSlryPrcExcld/Create
        [HttpGet]
        public IActionResult CreateNew(HrPrSlryPrcExcld hrPrSlryPrcExcld)
        {
            try
            {
                ViewBag.lstEmp = _employeeContext.GetEmployeeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today);
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
                hrPrSlryPrcExcld.StMonth = DateTime.Today.Month;
                hrPrSlryPrcExcld.StYear = DateTime.Today.Year;
                hrPrSlryPrcExcld.NoMnths = 1;
                return PartialView("_HrPrSlryPrcExcld_Create", hrPrSlryPrcExcld);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Exclud from Salary", ex.Message);
            }
        }

        // POST: HrPrSlryPrcExcld/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrSlryPrcExcld hrPrSlryPrcExcld)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrSlryPrcExcld, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Exclude from Salary", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Exclude from Salary", "Adding Record got failed");
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

        // GET: HrPrSlryPrcExcld/Edit
        public ActionResult EditGet(int id)
        {
            var hrPrSlryPrcExcld = _payrollContext.GetHrPrSlryPrcExcldById(id);
            ViewBag.lstEmp = _employeeContext.GetEmployeeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today);
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
            return PartialView("_HrPrSlryPrcExcld_Edit", hrPrSlryPrcExcld);
        }

        // POST: HrPrSlryPrcExcld/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrSlryPrcExcld hrPrSlryPrcExcld)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrSlryPrcExcld.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrSlryPrcExcld, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Exclude from Salary", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Exclude from Salary", "Adding Record got failed");
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

        // GET: HrPrSlryPrcExcld/Delete
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var hrPrSlryPrcExcld = _payrollContext.GetHrPrSlryPrcExcldById(id);
                return PartialView("_HrPrSlryPrcExcld_Delete", hrPrSlryPrcExcld);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Exclude from Salary", ex.Message);
            }
        }

        // POST: HrPrSlryPrcExcld/Delete
        [HttpPost]
        public IActionResult Delete(HrPrSlryPrcExcld hrPrSlryPrcExcld)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrPrSlryPrcExcld, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Exclude from Salary", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Exclude from Salary", ex.Message);
            }
        }
    }
}
