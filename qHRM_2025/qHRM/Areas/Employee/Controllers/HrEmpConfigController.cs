using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Employee.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpConfigController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpCnfNtfLvl> _dbCommitContext;

        public HrEmpConfigController(EmployeeContext employeeContext, DbCommitContext<HrEmpCnfNtfLvl> dbCommitContext)
        {
            _employeeContext = employeeContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("EM_CNF") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            var hrEmpConfig = _employeeContext.GetActiveHrEmpConfig();
            if (hrEmpConfig == null)
            {
                hrEmpConfig = new HrEmpConfig();
                hrEmpConfig.DftPrbNtf = 1;
            }
            else
            {
                ViewBag.DsHrEmpCnfNtfLvl = _employeeContext.GetHrEmpCnfNtfLvlByEmpCnfId(hrEmpConfig.EmpCnfId);
            }
            ViewBag.EmpCnfId = hrEmpConfig.EmpCnfId;
            return View(hrEmpConfig);
        }

        // POST: HrEmpConfig/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePrConfig(HrEmpConfig hrEmpConfig)
        {
            try
            {
                hrEmpConfig.RecStat = 1;
                hrEmpConfig.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                int _result = _employeeContext.SaveHrEmpConfig(hrEmpConfig, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                {
                    return RedirectToAction("Index", "HrPrConfig").WithSuccess("Payroll Config", "Record Updated Successfully");
                }
                else
                    return RedirectToAction("Index", "HrPrConfig").WithWarning("Payroll Config", "Updating Record got failed");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "HrPrConfig").WithWarning("Payroll Config", ex.Message.ToString());
            }
        }

        #region Notification Period Functions
        // GET: HrEmpConfig/CreateNtfGet
        [HttpGet]
        public IActionResult CreateNtfGet(int empCnfId)
        {
            try
            {
                HrEmpCnfNtfLvl hrEmpCnfNtfLvl = new HrEmpCnfNtfLvl();
                var maxLvl = _employeeContext.GetMaxNtfLvlByEmpCnfId(empCnfId);

                hrEmpCnfNtfLvl.EmpCnfId = empCnfId;
                hrEmpCnfNtfLvl.LwrBndryYrs = maxLvl;
                hrEmpCnfNtfLvl.UprBndryYrs = maxLvl + 5;

                return PartialView("_HrEmpCnfNtfLvl_Create", hrEmpCnfNtfLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Config (Notification)", ex.Message);
            }
        }

        // POST: HrPrConfig/CreateNtf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNtf(HrEmpCnfNtfLvl hrEmpCnfNtfLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrEmpCnfNtfLvl.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpCnfNtfLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId }).WithSuccess("Employee Config (Notification)", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId }).WithWarning("Employee Config (Notification)", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId });
                }
            }
            catch
            {
                return View();
            }
        }

        public IActionResult ValidateUprBndryYrs([Bind(Prefix = "UprBndryYrs")] int uprValue, [Bind(Prefix = "LwrBndryYrs")] int lwrValue)
        {
            if (uprValue > lwrValue)
                return Json(data: true);
            else
                return Json(data: false);
        }

        // GET: HrEmpConfig/EditNtfGet
        public IActionResult EditNtfGet(int ntfLvlId)
        {
            try
            {
                var hrEmpCnfNtfLvl = _employeeContext.GetHrEmpCnfNtfLvlById(ntfLvlId);

                return PartialView("_HrEmpCnfNtfLvl_Edit", hrEmpCnfNtfLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Config (Notification)", ex.Message);
            }
        }

        // POST: HrEmpConfig/EditNtf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNtf(HrEmpCnfNtfLvl hrEmpCnfNtfLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrEmpCnfNtfLvl.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpCnfNtfLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId }).WithSuccess("Employee Config (Notification)", "Record Updated Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId }).WithWarning("Employee Config (Notification)", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrEmpCnfNtfLvl.EmpCnfId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpConfig/CancelNtfGet
        public IActionResult CancelNtfGet(int ntfLvlId)
        {
            try
            {
                var hrEmpCnfNtfLvl = _employeeContext.GetHrEmpCnfNtfLvlById(ntfLvlId);

                return PartialView("_HrEmpCnfNtfLvl_Cancel", hrEmpCnfNtfLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Config (Notification)", ex.Message);
            }
        }

        // POST: HrEmpConfig/CancelNtf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelNtf(HrEmpCnfNtfLvl hrPrCnfGrtvtyLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrCnfGrtvtyLvl.RecStat = 0;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrCnfGrtvtyLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrPrCnfGrtvtyLvl.EmpCnfId }).WithSuccess("Employee Config (Notification)", "Record Cancelled Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrPrCnfGrtvtyLvl.EmpCnfId }).WithWarning("Employee Config (Notification)", "Cancel Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrEmpConfig", new { stfLnId = hrPrCnfGrtvtyLvl.EmpCnfId });
                }
            }
            catch
            {
                return View();
            }
        }
        #endregion Notification Period Functions
    }
}
