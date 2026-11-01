using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Leave.Data;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    [Authorize]
    public class HrLvWfConfigController : Controller
    {
        private readonly qHRMasterContext   _hrmContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrLvWfConfigController(
            qHRMasterContext   hrmContext,
            AdmUserGrpPrvContext prvContext)
        {
            _hrmContext  = hrmContext;
            _prvContext  = prvContext;
        }

        // =====================================================================
        // LIST – Leave Workflow Configurations
        // =====================================================================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("LV_WF_CFG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            SetPrivileges("LV_WF_CFG");

            var configs = _hrmContext.HrLvWfConfig
                .Join(_hrmContext.HrLvType,
                      c => c.LvTypId,
                      t => t.LvTypId,
                      (c, t) => new HrLvWfConfig
                      {
                          WfCfgId        = c.WfCfgId,
                          LvTypId        = c.LvTypId,
                          WfCode         = c.WfCode,
                          IsEnabled      = c.IsEnabled,
                          EffDate        = c.EffDate,
                          ExpDate        = c.ExpDate,
                          ApproverConfig = c.ApproverConfig,
                          PolicyRules    = c.PolicyRules,
                          RecStat        = c.RecStat,
                          CrtDateTime    = c.CrtDateTime,
                          LvTypName      = t.NameEng
                      })
                .OrderBy(c => c.LvTypName)
                .ToList();

            ViewBag.DataSource = configs;
            return View();
        }

        // =====================================================================
        // CREATE (GET)
        // =====================================================================
        [HttpGet]
        public IActionResult CreateNew()
        {
            if (HttpContext.Session.GetString("LV_WF_CFG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            SetPrivileges("LV_WF_CFG");
            LoadDropdowns();
            return PartialView("_HrLvWfConfig_Create", new HrLvWfConfig
            {
                IsEnabled = true,
                HrManagerEmpId = 6,
                BuHeadEmpId = 2,
                FinanceEmpId = 452,
                CeoEmpId = 11,
                PayrollCutoffDay = 20
            });
        }

        // =====================================================================
        // CREATE (POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrLvWfConfig cfg)
        {
            try
            {
                // Check for duplicate (same leave type + workflow code)
                bool exists = _hrmContext.HrLvWfConfig
                    .Any(c => c.LvTypId == cfg.LvTypId && c.RecStat == 1);

                if (exists)
                    return RedirectToAction(nameof(Index))
                        .WithWarning("Leave Workflow Config",
                            "An active workflow configuration already exists for this leave type. " +
                            "Please deactivate the existing one first.");

                // Build ApproverConfig JSON from submitted individual employee IDs
                var approverConfig = new
                {
                    HrManagerEmpId = cfg.HrManagerEmpId,
                    FinanceEmpId   = cfg.FinanceEmpId,
                    CeoEmpId       = cfg.CeoEmpId,
                    BuHeadEmpId    = cfg.BuHeadEmpId
                };
                cfg.ApproverConfig = JsonSerializer.Serialize(approverConfig);

                // Build PolicyRules JSON from submitted rule fields
                var policyRules = BuildPolicyRulesJson(cfg);
                cfg.PolicyRules = JsonSerializer.Serialize(policyRules);

                cfg.CrtId       = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                cfg.CrtDateTime = DateTime.Now;
                cfg.RecStat     = 1;

                _hrmContext.HrLvWfConfig.Add(cfg);
                _hrmContext.SaveChanges();

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Leave Workflow Config", "Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index))
                    .WithWarning("Leave Workflow Config", ex.Message);
            }
        }

        // =====================================================================
        // EDIT (GET)
        // =====================================================================
        [HttpGet]
        public IActionResult EditGet(int wfCfgId)
        {
            if (HttpContext.Session.GetString("LV_WF_CFG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var cfg = _hrmContext.HrLvWfConfig.FirstOrDefault(c => c.WfCfgId == wfCfgId);
            if (cfg == null)
                return RedirectToAction(nameof(Index)).WithWarning("Leave Workflow Config", "Record not found.");

            // Populate Leave Type Name for display
            cfg.LvTypName = _hrmContext.HrLvType.FirstOrDefault(t => t.LvTypId == cfg.LvTypId)?.NameEng;

            // Deserialize stored JSON back into individual fields for the form
            if (!string.IsNullOrEmpty(cfg.ApproverConfig) && cfg.ApproverConfig.Trim() != "{}")
            {
                try
                {
                    using var doc = JsonDocument.Parse(cfg.ApproverConfig);
                    var root = doc.RootElement;
                    cfg.HrManagerEmpId = GetIntOrNull(root, "HrManagerEmpId");
                    cfg.FinanceEmpId   = GetIntOrNull(root, "FinanceEmpId");
                    cfg.CeoEmpId       = GetIntOrNull(root, "CeoEmpId");
                    cfg.BuHeadEmpId    = GetIntOrNull(root, "BuHeadEmpId");
                }
                catch { /* Ignore parse errors; form fields remain null */ }
            }

            // Fallback default designated approvers if not configured
            if (!cfg.HrManagerEmpId.HasValue) cfg.HrManagerEmpId = 6;   // Kaushalya Fonseka
            if (!cfg.BuHeadEmpId.HasValue)    cfg.BuHeadEmpId    = 2;   // Kelum Tennakoon
            if (!cfg.FinanceEmpId.HasValue)   cfg.FinanceEmpId   = 452; // Ashhar Zaeem
            if (!cfg.CeoEmpId.HasValue)       cfg.CeoEmpId       = 11;  // Hasan Sayed

            bool hasSavedPolicies = false;
            if (!string.IsNullOrEmpty(cfg.PolicyRules) && cfg.PolicyRules.Trim() != "{}")
            {
                try
                {
                    using var doc = JsonDocument.Parse(cfg.PolicyRules);
                    var root = doc.RootElement;
                    cfg.MedCertRequired  = GetBool(root, "MedCertRequired");
                    cfg.PayrollCutoffDay = GetInt(root, "PayrollCutoffDay", 20);
                    cfg.FullPayDays      = GetInt(root, "FullPayDays", 0);
                    cfg.HalfPayDays      = GetInt(root, "HalfPayDays", 0);
                    cfg.NoPay            = GetBool(root, "NoPay");
                    cfg.MaxDays          = GetInt(root, "MaxDays", 0);
                    cfg.OnceInService    = GetBool(root, "OnceInService");
                    cfg.AnnualEntitlement= GetInt(root, "AnnualEntitlement", 0);
                    cfg.EligibleDays     = GetInt(root, "EligibleDays", 0);
                    cfg.MinServiceYears  = GetInt(root, "MinServiceYears", 0);
                    cfg.GenderRequired   = GetString(root, "GenderRequired");
                    cfg.HoursPerDay      = GetInt(root, "HoursPerDay", 0);
                    cfg.EligibilityDaysFromMaternity = GetInt(root, "EligibilityDaysFromMaternity", 365);
                    hasSavedPolicies = true;
                }
                catch { /* Ignore parse errors */ }
            }

            if (!hasSavedPolicies)
            {
                // Fallback to HrLvPolicy table for this leave type
                var policy = _hrmContext.HrLvPolicy.FirstOrDefault(p => p.LvTypId == cfg.LvTypId && p.RecStat == 1);
                if (policy != null)
                {
                    cfg.FullPayDays       = (int)Math.Round(policy.FullPaidDays);
                    cfg.HalfPayDays       = (int)Math.Round(policy.HalfPaidDays);
                    cfg.AnnualEntitlement = (int)Math.Round(policy.ElgDays);
                    cfg.NoPay             = policy.IsNoPay;
                    cfg.MaxDays           = (int)Math.Round(policy.ElgDays);
                }

                // Default specific known business rules
                string code = cfg.WfCode ?? "";
                string name = cfg.LvTypName ?? "";
                if (code.Contains("SICK", StringComparison.OrdinalIgnoreCase) || name.Contains("Sick", StringComparison.OrdinalIgnoreCase))
                {
                    cfg.MedCertRequired = true;
                    if (cfg.FullPayDays == 0) cfg.FullPayDays = 14;
                    if (cfg.HalfPayDays == 0) cfg.HalfPayDays = 28;
                }
                else if (code.Contains("CSL", StringComparison.OrdinalIgnoreCase) || name.Contains("Casual", StringComparison.OrdinalIgnoreCase))
                {
                    if (cfg.AnnualEntitlement == 0) cfg.AnnualEntitlement = 3;
                    if (cfg.FullPayDays == 0) cfg.FullPayDays = 3;
                }
                else if (code.Contains("HAJ", StringComparison.OrdinalIgnoreCase) || name.Contains("Haj", StringComparison.OrdinalIgnoreCase))
                {
                    cfg.OnceInService = true;
                    cfg.NoPay = true;
                    if (cfg.MaxDays == 0) cfg.MaxDays = 14;
                }
                else if (code.Contains("MTN", StringComparison.OrdinalIgnoreCase) || name.Contains("Matern", StringComparison.OrdinalIgnoreCase))
                {
                    cfg.GenderRequired = "F";
                    if (cfg.FullPayDays == 0) cfg.FullPayDays = 90;
                }
                else if (code.Contains("EMG", StringComparison.OrdinalIgnoreCase) || name.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
                {
                    if (cfg.MaxDays == 0) cfg.MaxDays = 30;
                }
            }

            SetPrivileges("LV_WF_CFG");
            LoadDropdowns();
            return PartialView("_HrLvWfConfig_Edit", cfg);
        }

        // =====================================================================
        // EDIT (POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrLvWfConfig cfg)
        {
            try
            {
                var existing = _hrmContext.HrLvWfConfig.FirstOrDefault(c => c.WfCfgId == cfg.WfCfgId);
                if (existing == null)
                    return RedirectToAction(nameof(Index)).WithWarning("Leave Workflow Config", "Record not found.");

                existing.WfCode    = cfg.WfCode;
                existing.IsEnabled = cfg.IsEnabled;
                existing.EffDate   = cfg.EffDate;
                existing.ExpDate   = cfg.ExpDate;
                existing.MdfId     = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                existing.MdfDateTime = DateTime.Now;

                var approverConfig = new
                {
                    HrManagerEmpId = cfg.HrManagerEmpId,
                    FinanceEmpId   = cfg.FinanceEmpId,
                    CeoEmpId       = cfg.CeoEmpId,
                    BuHeadEmpId    = cfg.BuHeadEmpId
                };
                existing.ApproverConfig = JsonSerializer.Serialize(approverConfig);
                existing.PolicyRules    = JsonSerializer.Serialize(BuildPolicyRulesJson(cfg));

                _hrmContext.HrLvWfConfig.Update(existing);
                _hrmContext.SaveChanges();

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Leave Workflow Config", "Configuration updated successfully.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index))
                    .WithWarning("Leave Workflow Config", ex.Message);
            }
        }

        // =====================================================================
        // TOGGLE ENABLE/DISABLE
        // =====================================================================
        [HttpPost]
        public IActionResult ToggleStatus(int wfCfgId)
        {
            try
            {
                var cfg = _hrmContext.HrLvWfConfig.FirstOrDefault(c => c.WfCfgId == wfCfgId);
                if (cfg == null) return Json(new { success = false, message = "Not found." });

                cfg.IsEnabled    = !cfg.IsEnabled;
                cfg.MdfId        = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                cfg.MdfDateTime  = DateTime.Now;
                _hrmContext.SaveChanges();

                return Json(new { success = true, isEnabled = cfg.IsEnabled });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private object BuildPolicyRulesJson(HrLvWfConfig cfg) =>
            new
            {
                MedCertRequired   = cfg.MedCertRequired,
                PayrollCutoffDay  = cfg.PayrollCutoffDay == 0 ? 20 : cfg.PayrollCutoffDay,
                FullPayDays       = cfg.FullPayDays,
                HalfPayDays       = cfg.HalfPayDays,
                NoPay             = cfg.NoPay,
                MaxDays           = cfg.MaxDays,
                OnceInService     = cfg.OnceInService,
                AnnualEntitlement = cfg.AnnualEntitlement,
                EligibleDays      = cfg.EligibleDays,
                MinServiceYears   = cfg.MinServiceYears,
                GenderRequired    = cfg.GenderRequired,
                HoursPerDay       = cfg.HoursPerDay,
                EligibilityDaysFromMaternity = cfg.EligibilityDaysFromMaternity == 0 ? 365 : cfg.EligibilityDaysFromMaternity
            };

        private void SetPrivileges(string prvCode)
        {
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId)
                .Where(x => x.PrvCode == prvCode).FirstOrDefault();
            if (objprv != null)
            {
                ViewBag.RecAdd    = objprv.RecAdd;
                ViewBag.RecEdit   = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
            }
        }

        private void LoadDropdowns()
        {
            ViewBag.lstLvTyp = _hrmContext.HrLvType
                .Where(t => t.RecStat == 1)
                .OrderBy(t => t.NameEng)
                .ToList();

            // CurrStat == 0 = Active employee (consistent with EmployeeContext convention)
            ViewBag.lstEmp = _hrmContext.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .OrderBy(e => e.FirstName)
                .Select(e => new
                {
                    ESysId = e.EmpSysId,
                    FulNme = (e.FirstName.Trim() + " " + (e.LastName ?? "").Trim()).Trim()
                })
                .ToList();
        }

        private static int? GetIntOrNull(JsonElement element, string propName)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.NameEquals(propName) || string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number) return prop.Value.GetInt32();
                    if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out int val)) return val;
                }
            }
            return null;
        }

        private static int GetInt(JsonElement element, string propName, int defaultValue = 0)
        {
            return GetIntOrNull(element, propName) ?? defaultValue;
        }

        private static bool GetBool(JsonElement element, string propName, bool defaultValue = false)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.NameEquals(propName) || string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.True) return true;
                    if (prop.Value.ValueKind == JsonValueKind.False) return false;
                    if (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out bool b)) return b;
                }
            }
            return defaultValue;
        }

        private static string GetString(JsonElement element, string propName)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.NameEquals(propName) || string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString();
                    if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.ValueKind != JsonValueKind.Undefined) return prop.Value.ToString();
                }
            }
            return null;
        }
    }
}
