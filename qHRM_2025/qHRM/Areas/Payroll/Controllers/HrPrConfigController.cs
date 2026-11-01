using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrConfigController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly DbCommitContext<HrPrCnfGrtvtyLvl> _dbCommitContext;

        public HrPrConfigController(PayrollContext payrollContext, DbCommitContext<HrPrCnfGrtvtyLvl> dbCommitContext)
        {
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("PR_CONF") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            var hrPrConfig = _payrollContext.GetActiveHrPrConfig();
            if (hrPrConfig == null)
            {
                hrPrConfig = new HrPrConfig();
                hrPrConfig.AttnCtEom = false;
                hrPrConfig.AttnCtDay = 20;
                hrPrConfig.SlryPrcEom = false;
                hrPrConfig.SlryPrcDay = 1;
                hrPrConfig.EsGrtActDiM = false;
                hrPrConfig.EsGrtDiMVal = 30;
                hrPrConfig.EsGrtActDiY = true;
                hrPrConfig.EsGrtDiYVal = 0;
                hrPrConfig.EsLmsActDiM = true;
                hrPrConfig.EsLmsDiMVal = 0;
                hrPrConfig.EsLvSActDiY = false;
                hrPrConfig.EsLvSDiYVal = 365;
                hrPrConfig.EsLvSMnWkPrd = 12;
            }
            else
            {
                ViewBag.DsHrPrCnfGrtvtyLvl = _payrollContext.GetHrPrCnfGrtvtyLvlByPrCnfId(hrPrConfig.PrCnfId);
            }
            ViewBag.PrCnfId = hrPrConfig.PrCnfId;
            return View(hrPrConfig);
        }

        // POST: HrPrConfig/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePrConfig(HrPrConfig hrPrConfig)
        {
            try
            {
                hrPrConfig.RecStat = 1;
                hrPrConfig.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                int _result = _payrollContext.SaveHrPrConfig(hrPrConfig, (int)HttpContext.Session.GetInt32("userEmpSysId"));
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

        #region Validations
        #region Attendance cut off date
        public IActionResult ValidateAttnCtDay([Bind(Prefix = "AttnCtDay")] int value, [Bind(Prefix = "AttnCtEom")] bool type)
        {
            return Json(data: ValAttnCtDay(value, type));
        }

        private bool ValAttnCtDay(int value, bool type)
        {
            if (type)
                return true;
            else
            {
                if (value >= 1 && value <= 28)
                    return true;
                else
                    return false;
            }
        }
        #endregion Attendance cut off date

        public IActionResult ValidateSlryPrcDay([Bind(Prefix = "SlryPrcDay")] int value, [Bind(Prefix = "SlryPrcEom")] bool type)
        {
            if (type)
                return Json(data: true);
            else
            {
                if (value >= 1 && value <= 28)
                    return Json(data: true);
                else
                    return Json(data: false);
            }
        }

        public IActionResult ValidateEsLmsDiMVal([Bind(Prefix = "EsLmsDiMVal")] int value, [Bind(Prefix = "EsLmsActDiM")] bool type)
        {
            if (type)
                return Json(data: true);
            else
            {
                if (value >= 1 && value <= 31)
                    return Json(data: true);
                else
                    return Json(data: false);
            }
        }
        #endregion Validation

        #region Gratuity Functions
        // GET: HrEmpConfig/CreateGrtGet
        [HttpGet]
        public IActionResult CreateGrtGet(int prCnfId)
        {

            try
            {
                HrPrCnfGrtvtyLvl hrPrCnfGrtvtyLvl = new HrPrCnfGrtvtyLvl();
                var maxLvl = _payrollContext.GetMaxGrtLvlByPrCnfId(prCnfId);

                hrPrCnfGrtvtyLvl.PrCnfId = prCnfId;
                hrPrCnfGrtvtyLvl.LwrBndryYrs = maxLvl;
                hrPrCnfGrtvtyLvl.UprBndryYrs = maxLvl + 5;

                return PartialView("_HrPrCnfGrtvtyLvl_Create", hrPrCnfGrtvtyLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Payroll Config", ex.Message);
            }
        }

        // POST: HrPrConfig/CreateNtf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGrt(HrPrCnfGrtvtyLvl hrPrCnfGrtvtyLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrCnfGrtvtyLvl.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrCnfGrtvtyLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithSuccess("Employee Config (Notification)", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithWarning("Employee Config (Notification)", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId });
                }
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        
        public JsonResult CreateGrt1(HrPrCnfGrtvtyLvl hrPrCnfGrtvtyLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrCnfGrtvtyLvl.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrCnfGrtvtyLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return Json(new { ok = true, data = 0, message = "ok" });
                    }
                    catch
                    {
                        return Json(new { ok = false, data = 0, message = "failed" });
                    }
                }
                else
                {
                    return Json(new { ok = false, data = 0, message = "failed" });
                }
            }
            catch
            {
                return Json(new { ok = false, data = 0, message = "failed" });
            }
        }

        public IActionResult ValidateUprBndryYrs([Bind(Prefix = "UprBndryYrs")] int uprValue, [Bind(Prefix = "LwrBndryYrs")] int lwrValue)
        {
            if (uprValue > lwrValue)
                return Json(data: true);
            else
                return Json(data: false);
        }

        // GET: HrPrConfig/EditGrtGet
        public IActionResult EditGrtGet(int grtLvlId)
        {
            try
            {
                var hrPrCnfGrtvtyLvl = _payrollContext.GetHrPrCnfGrtvtyLvlById(grtLvlId);

                return PartialView("_HrPrCnfGrtvtyLvl_Edit", hrPrCnfGrtvtyLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Payroll Config", ex.Message);
            }
        }

        // POST: HrPrConfig/EditGrt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditGrt(HrPrCnfGrtvtyLvl hrPrCnfGrtvtyLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrCnfGrtvtyLvl.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrCnfGrtvtyLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithSuccess("Payroll Config", "Record Updated Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithWarning("Payroll Config", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrConfig/CancelGrtGet
        public IActionResult CancelGrtGet(int grtLvlId)
        {
            try
            {
                var hrPrCnfGrtvtyLvl = _payrollContext.GetHrPrCnfGrtvtyLvlById(grtLvlId);

                return PartialView("_HrPrCnfGrtvtyLvl_Cancel", hrPrCnfGrtvtyLvl);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Payroll Config", ex.Message);
            }
        }

        // POST: HrPrConfig/CancelGrt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelGrt(HrPrCnfGrtvtyLvl hrPrCnfGrtvtyLvl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrCnfGrtvtyLvl.RecStat = 0;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrCnfGrtvtyLvl, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithSuccess("Payroll Config", "Record Cancelled Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId }).WithWarning("Payroll Config", "Cancel Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrConfig", new { stfLnId = hrPrCnfGrtvtyLvl.PrCnfId });
                }
            }
            catch
            {
                return View();
            }
        }
        #endregion Gratuity Functions
    }
}
