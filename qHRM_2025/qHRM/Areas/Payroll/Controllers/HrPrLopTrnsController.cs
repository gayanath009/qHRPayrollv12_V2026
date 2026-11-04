using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class HrPrLopTrnsController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrLopTrns> _dbCommitContext;

        public HrPrLopTrnsController(PayrollContext payrollContext,
                                     EmployeeContext employeeContext,
                                     SysSelectListContext sysSelectListContext,
                                     DbCommitContext<HrPrLopTrns> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // Use the same Payroll Adjustment privilege gate ("AD_HOC_TR")
            if (HttpContext.Session.GetString("AD_HOC_TR") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            ViewBag.DataSource = _payrollContext.GetHrPrLopTrnViewByEntId(entId, -1);
            return View();
        }

        // GET: Payroll/HrPrLopTrns/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrLopTrns hrPrLopTrns)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;

                var lstSlryPrcHdr = _payrollContext.GetLstSlryPrcHdr(entId);
                DateTime defaultPeriod = DateTime.Today;
                if (lstSlryPrcHdr != null)
                {
                    defaultPeriod = new DateTime(lstSlryPrcHdr.EftYear, lstSlryPrcHdr.EftMonth, 1);
                    if (lstSlryPrcHdr.PrcStat == 3)
                    {
                        defaultPeriod = defaultPeriod.AddMonths(1);
                    }
                }

                hrPrLopTrns.TrnDate = DateTime.Today;
                hrPrLopTrns.EftMonth = defaultPeriod.Month;
                hrPrLopTrns.EftYear = defaultPeriod.Year;
                hrPrLopTrns.LopDays = 1m;

                var lstEmp = _employeeContext.GetOutsourceEmployeeViewByEntId(entId, DateTime.Today).ToList();
                var lstLvTyp = _payrollContext.GetActiveLvTypesByEntId(entId).ToList();
                var lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

                ViewBag.lstEmp = lstEmp;
                ViewBag.lstLvTyp = lstLvTyp;
                ViewBag.lstMonths = lstMonths;

                if (lstEmp.Any())
                {
                    hrPrLopTrns.EmpSysId = lstEmp.First().EmpSysId;
                    var calc = _payrollContext.CalculateOutsourceLopAmount(hrPrLopTrns.EmpSysId, hrPrLopTrns.LopDays, hrPrLopTrns.EftYear, hrPrLopTrns.EftMonth);
                    hrPrLopTrns.LopAmt = calc.LopAmt;
                }

                if (lstLvTyp.Any())
                {
                    hrPrLopTrns.LvTypId = lstLvTyp.First().LvTypId;
                }

                return PartialView("_HrPrLopTrns_Create", hrPrLopTrns);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", ex.Message);
            }
        }

        // GET: Payroll/HrPrLopTrns/CalcLopAmt
        [HttpGet]
        public IActionResult CalcLopAmt(int empSysId, decimal lopDays, int eftYear, int eftMonth)
        {
            try
            {
                var calc = _payrollContext.CalculateOutsourceLopAmount(empSysId, lopDays, eftYear, eftMonth);
                return Json(new
                {
                    success = true,
                    fixedGross = calc.FixedGross,
                    dailyRate = calc.DailyRate,
                    lopAmt = calc.LopAmt
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, lopAmt = 0m });
            }
        }

        // POST: Payroll/HrPrLopTrns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrLopTrns hrPrLopTrns)
        {
            try
            {
                hrPrLopTrns.Ref = hrPrLopTrns.Ref ?? "";
                ModelState.Remove("Ref");
                ModelState.Remove("EmpSys");
                ModelState.Remove("PrcLog");
                ModelState.Remove("LvTyp");

                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrLopTrns.EmpSysId);
                if (empGenInfo == null || empGenInfo.WrktypId != 2)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Restricted to Outsourced Employees only.");
                }

                if (hrPrLopTrns.LopAmt <= 0 && hrPrLopTrns.LopDays > 0)
                {
                    var calc = _payrollContext.CalculateOutsourceLopAmount(hrPrLopTrns.EmpSysId, hrPrLopTrns.LopDays, hrPrLopTrns.EftYear, hrPrLopTrns.EftMonth);
                    hrPrLopTrns.LopAmt = calc.LopAmt;
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        hrPrLopTrns.RecStat = 1;
                        hrPrLopTrns.CrtBy = userEmpSysId;
                        hrPrLopTrns.CrtDateTime = DateTime.Now;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrLopTrns, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Loss of Pay (LOP)", "LOP Record Added Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Adding LOP Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Validation failed for LOP record.");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", ex.Message);
            }
        }

        // GET: Payroll/HrPrLopTrns/EditGet
        public IActionResult EditGet(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Invalid record identifier");
            }

            var hrPrLopTrns = _payrollContext.GetHrPrLopTrnById(id);
            if (hrPrLopTrns == null)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Record not found");
            }

            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrLopTrns.EmpSysId);
            ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

            var lstLvTyp = _payrollContext.GetActiveLvTypesByEntId(entId).ToList();
            var curLvTyp = lstLvTyp.FirstOrDefault(x => x.LvTypId == hrPrLopTrns.LvTypId);
            ViewBag.LvTypDesc = (curLvTyp != null) ? curLvTyp.LvTypDesc : "N/A";
            ViewBag.lstLvTyp = lstLvTyp;
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);

            return PartialView("_HrPrLopTrns_Edit", hrPrLopTrns);
        }

        // POST: Payroll/HrPrLopTrns/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrLopTrns hrPrLopTrns)
        {
            try
            {
                hrPrLopTrns.Ref = hrPrLopTrns.Ref ?? "";
                ModelState.Remove("Ref");
                ModelState.Remove("EmpSys");
                ModelState.Remove("PrcLog");
                ModelState.Remove("LvTyp");

                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrLopTrns.EmpSysId);
                if (empGenInfo == null || empGenInfo.WrktypId != 2)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Restricted to Outsourced Employees only.");
                }

                if (hrPrLopTrns.LopAmt <= 0 && hrPrLopTrns.LopDays > 0)
                {
                    var calc = _payrollContext.CalculateOutsourceLopAmount(hrPrLopTrns.EmpSysId, hrPrLopTrns.LopDays, hrPrLopTrns.EftYear, hrPrLopTrns.EftMonth);
                    hrPrLopTrns.LopAmt = calc.LopAmt;
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrLopTrns.RecStat = 1;
                        int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                        hrPrLopTrns.CrtBy = userEmpSysId;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrLopTrns, userEmpSysId);
                        return RedirectToAction(nameof(Index)).WithSuccess("Loss of Pay (LOP)", "LOP Record Updated Successfully");
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Modifying LOP Record failed: " + ex.Message);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Validation failed for LOP record");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", ex.Message);
            }
        }

        // GET: Payroll/HrPrLopTrns/CancelGet
        public IActionResult CancelGet(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Invalid record identifier");
                }

                var hrPrLopTrns = _payrollContext.GetHrPrLopTrnById(id);
                if (hrPrLopTrns == null)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Record not found");
                }

                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrLopTrns.EmpSysId);
                ViewBag.EmpName = (empGenInfo != null) ? empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName : "N/A";

                var lstLvTyp = _payrollContext.GetActiveLvTypesByEntId(entId).ToList();
                var curLvTyp = lstLvTyp.FirstOrDefault(x => x.LvTypId == hrPrLopTrns.LvTypId);
                ViewBag.LvTypDesc = (curLvTyp != null) ? curLvTyp.LvTypDesc : "N/A";

                ViewBag.EftMonth = hrPrLopTrns.EftYear.ToString() + " - " + new DateTime(2000, hrPrLopTrns.EftMonth, 1).ToString("MMMM", CultureInfo.InvariantCulture);

                return PartialView("_HrPrLopTrns_Cancel", hrPrLopTrns);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", ex.Message);
            }
        }

        // POST: Payroll/HrPrLopTrns/Cancel
        [HttpPost]
        public IActionResult Cancel(HrPrLopTrns hrPrLopTrns)
        {
            try
            {
                var existing = _payrollContext.GetHrPrLopTrnById(hrPrLopTrns.LopId);
                if (existing != null)
                {
                    existing.RecStat = 0;
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, existing, userEmpSysId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Loss of Pay (LOP)", "LOP Record Cancelled Successfully");
                }
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", "Record not found to cancel");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Loss of Pay (LOP)", ex.Message);
            }
        }

        // GET: Payroll/HrPrLopTrns/DeleteGet (alias for CancelGet)
        [HttpGet]
        public IActionResult DeleteGet(int id) => CancelGet(id);

        // POST: Payroll/HrPrLopTrns/Delete (alias for Cancel)
        [HttpPost]
        public IActionResult Delete(HrPrLopTrns hrPrLopTrns) => Cancel(hrPrLopTrns);

        public IActionResult ValidateAmount([Bind(Prefix = "LopAmt")] decimal lopAmt)
        {
            if (lopAmt > 0)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}

