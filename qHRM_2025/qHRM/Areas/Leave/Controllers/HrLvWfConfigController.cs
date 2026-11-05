using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
        private readonly qHRMasterContext    _hrmContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly IConfiguration      _configuration;

        public HrLvWfConfigController(
            qHRMasterContext     hrmContext,
            AdmUserGrpPrvContext prvContext,
            IConfiguration       configuration = null)
        {
            _hrmContext    = hrmContext;
            _prvContext    = prvContext;
            _configuration = configuration;
        }

        // =====================================================================
        // LIST – Leave Workflow Configurations
        // =====================================================================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("LV_WF_CFG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            SetPrivileges("LV_WF_CFG");

            var configs = (from c in _hrmContext.HrLvWfConfig
                           join t in _hrmContext.HrLvType on c.LvTypId equals t.LvTypId into lj
                           from t in lj.DefaultIfEmpty()
                           where c.RecStat == 1
                           select new HrLvWfConfig
                           {
                               WfCfgId        = c.WfCfgId,
                               WfType         = c.WfType ?? "Leave Approval",
                               LvTypId        = c.LvTypId,
                               WfCode         = c.WfCode,
                               IsEnabled      = c.IsEnabled,
                               EffDate        = c.EffDate,
                               ExpDate        = c.ExpDate,
                               ApproverConfig = c.ApproverConfig,
                               PolicyRules    = c.PolicyRules,
                               RecStat        = c.RecStat,
                               CrtDateTime    = c.CrtDateTime,
                               LvTypName      = c.LvTypId.HasValue ? (t != null ? t.NameEng : "—") : (c.WfType ?? "—")
                           })
                           .OrderBy(c => c.WfType).ThenBy(c => c.LvTypName)
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
                IsEnabled                 = true,
                OutsourceRecordOwnerEmpId = 97,  // Step 1 - Outsource Record Owner (Sanuja Jayanthy)
                ReportingManagerEmpId     = 405, // Step 2 - Reporting Manager (Salman Khan)
                HrManagerEmpId            = 6,   // Step 3 - HR (Kaushalya Fonseka)
                FinanceEmpId              = 452, // Step 4 - Finance (Ashhar Zaeem)
                CeoEmpId                  = 11,  // Step 5 - CEO (Hasan Sayed)
                BuHeadEmpId               = 2,   // BU Head (Kelum Tennakoon)
                PayrollCutoffDay          = 20
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
                if (string.IsNullOrEmpty(cfg.WfType)) cfg.WfType = "Leave Approval";

                // Check for duplicate
                bool exists = cfg.WfType == "Leave Approval"
                    ? _hrmContext.HrLvWfConfig.Any(c => (c.WfType == null || c.WfType == "Leave Approval") && c.LvTypId == cfg.LvTypId && c.RecStat == 1)
                    : _hrmContext.HrLvWfConfig.Any(c => c.WfType == cfg.WfType && c.RecStat == 1);

                if (exists)
                    return RedirectToAction(nameof(Index))
                        .WithWarning("Workflow Configuration",
                            $"An active workflow configuration already exists for '{cfg.WfType}'. " +
                            "Please deactivate the existing one first.");

                if (cfg.WfType == "Leave Approval")
                {
                    var approverConfig = new
                    {
                        OutsourceRecordOwnerEmpId = cfg.OutsourceRecordOwnerEmpId,
                        ReportingManagerEmpId     = cfg.ReportingManagerEmpId,
                        HrManagerEmpId            = cfg.HrManagerEmpId,
                        FinanceEmpId              = cfg.FinanceEmpId,
                        CeoEmpId                  = cfg.CeoEmpId,
                        BuHeadEmpId               = cfg.BuHeadEmpId
                    };
                    cfg.ApproverConfig = JsonSerializer.Serialize(approverConfig);
                    cfg.PolicyRules = JsonSerializer.Serialize(BuildPolicyRulesJson(cfg));
                }
                else
                {
                    var approverConfig = new
                    {
                        InitiatorEmpId      = cfg.InitiatorEmpId ?? 6,
                        VerifierEmpId       = cfg.VerifierEmpId ?? 6,
                        Level1ApproverEmpId = cfg.Level1ApproverEmpId ?? 452,
                        FinalApproverEmpId  = cfg.FinalApproverEmpId ?? 11
                    };
                    cfg.ApproverConfig = JsonSerializer.Serialize(approverConfig);
                    cfg.PolicyRules = "{}";
                    cfg.LvTypId = null;
                }

                cfg.CrtId       = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                cfg.CrtDateTime = DateTime.Now;
                cfg.RecStat     = 1;

                _hrmContext.HrLvWfConfig.Add(cfg);
                _hrmContext.SaveChanges();

                if (cfg.WfType == "Leave Approval")
                {
                    SyncWorkflowEngineApprovers(cfg);
                }

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Workflow Configuration", "Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index))
                    .WithWarning("Workflow Configuration", ex.Message);
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
            cfg.LvTypName = cfg.LvTypId.HasValue 
                ? _hrmContext.HrLvType.FirstOrDefault(t => t.LvTypId == cfg.LvTypId)?.NameEng 
                : (cfg.WfType ?? "—");

            if (cfg.WfType != "Leave Approval")
            {
                if (!string.IsNullOrEmpty(cfg.ApproverConfig) && cfg.ApproverConfig.Trim() != "{}")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(cfg.ApproverConfig);
                        var root = doc.RootElement;
                        cfg.InitiatorEmpId      = GetIntOrNull(root, "InitiatorEmpId") ?? 6;
                        cfg.VerifierEmpId       = GetIntOrNull(root, "VerifierEmpId") ?? 6;
                        cfg.Level1ApproverEmpId = GetIntOrNull(root, "Level1ApproverEmpId") ?? 452;
                        cfg.FinalApproverEmpId  = GetIntOrNull(root, "FinalApproverEmpId") ?? 11;
                    }
                    catch { }
                }
                if (!cfg.InitiatorEmpId.HasValue) cfg.InitiatorEmpId = 6;
                if (!cfg.VerifierEmpId.HasValue) cfg.VerifierEmpId = 6;
                if (!cfg.Level1ApproverEmpId.HasValue) cfg.Level1ApproverEmpId = 452;
                if (!cfg.FinalApproverEmpId.HasValue) cfg.FinalApproverEmpId = 11;

                SetPrivileges("LV_WF_CFG");
                LoadDropdowns();
                return PartialView("_HrLvWfConfig_Edit", cfg);
            }

            // Deserialize stored JSON back into individual fields for the form
            if (!string.IsNullOrEmpty(cfg.ApproverConfig) && cfg.ApproverConfig.Trim() != "{}")
            {
                try
                {
                    using var doc = JsonDocument.Parse(cfg.ApproverConfig);
                    var root = doc.RootElement;
                    cfg.OutsourceRecordOwnerEmpId = GetIntOrNull(root, "OutsourceRecordOwnerEmpId");
                    cfg.ReportingManagerEmpId     = GetIntOrNull(root, "ReportingManagerEmpId");
                    cfg.HrManagerEmpId            = GetIntOrNull(root, "HrManagerEmpId");
                    cfg.FinanceEmpId              = GetIntOrNull(root, "FinanceEmpId");
                    cfg.CeoEmpId                  = GetIntOrNull(root, "CeoEmpId");
                    cfg.BuHeadEmpId               = GetIntOrNull(root, "BuHeadEmpId");
                }
                catch { /* Ignore parse errors; form fields remain null */ }
            }

            // Overlay live step approvers from WorkflowEngineDb if configured there
            PopulateApproversFromWorkflowEngine(cfg);

            bool isSickOrCasual = IsSickOrCasualLeave(cfg.WfCode, cfg.LvTypName);

            // Fallback default designated approvers in workflow step order:
            // Step 1 - Outsource Record Owner -> Sanuja Jayanthy (97)
            // Step 2 - Reporting Manager      -> Salman Khan (405)
            // Step 3 - HR                     -> Kaushalya Fonseka (6)
            // Step 4 - Finance                -> Ashhar Zaeem (452)
            // Step 5 - CEO (Final Approver)   -> Hasan Sayed (11) (for 5-step workflows)
            if (!cfg.OutsourceRecordOwnerEmpId.HasValue) cfg.OutsourceRecordOwnerEmpId = 97;  // Sanuja Jayanthy
            if (!cfg.ReportingManagerEmpId.HasValue)     cfg.ReportingManagerEmpId     = 405; // Salman Khan
            if (!cfg.HrManagerEmpId.HasValue)            cfg.HrManagerEmpId            = 6;   // Kaushalya Fonseka
            if (!cfg.FinanceEmpId.HasValue)              cfg.FinanceEmpId              = 452; // Ashhar Zaeem
            if (!cfg.BuHeadEmpId.HasValue)               cfg.BuHeadEmpId               = 2;   // Kelum Tennakoon
            if (!isSickOrCasual && !cfg.CeoEmpId.HasValue)
            {
                cfg.CeoEmpId = 11; // Hasan Sayed
            }

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

                existing.WfCode      = cfg.WfCode;
                existing.IsEnabled   = cfg.IsEnabled;
                existing.EffDate     = cfg.EffDate;
                existing.ExpDate     = cfg.ExpDate;
                existing.MdfId       = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                existing.MdfDateTime = DateTime.Now;

                if (existing.WfType == "Leave Approval" || string.IsNullOrEmpty(existing.WfType))
                {
                    var approverConfig = new
                    {
                        OutsourceRecordOwnerEmpId = cfg.OutsourceRecordOwnerEmpId,
                        ReportingManagerEmpId     = cfg.ReportingManagerEmpId,
                        HrManagerEmpId            = cfg.HrManagerEmpId,
                        FinanceEmpId              = cfg.FinanceEmpId,
                        CeoEmpId                  = cfg.CeoEmpId,
                        BuHeadEmpId               = cfg.BuHeadEmpId
                    };
                    existing.ApproverConfig = JsonSerializer.Serialize(approverConfig);
                    existing.PolicyRules    = JsonSerializer.Serialize(BuildPolicyRulesJson(cfg));

                    _hrmContext.HrLvWfConfig.Update(existing);
                    _hrmContext.SaveChanges();

                    SyncWorkflowEngineApprovers(cfg);
                }
                else
                {
                    var approverConfig = new
                    {
                        InitiatorEmpId      = cfg.InitiatorEmpId ?? 6,
                        VerifierEmpId       = cfg.VerifierEmpId ?? 6,
                        Level1ApproverEmpId = cfg.Level1ApproverEmpId ?? 452,
                        FinalApproverEmpId  = cfg.FinalApproverEmpId ?? 11
                    };
                    existing.ApproverConfig = JsonSerializer.Serialize(approverConfig);
                    existing.PolicyRules    = "{}";

                    _hrmContext.HrLvWfConfig.Update(existing);
                    _hrmContext.SaveChanges();
                }

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Workflow Configuration", "Configuration updated successfully.");
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

        private static bool IsSickOrCasualLeave(string wfCode, string lvTypName)
        {
            string code = wfCode ?? "";
            string name = lvTypName ?? "";
            return code.Contains("SICK", StringComparison.OrdinalIgnoreCase)
                || code.Contains("CSL", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Sick", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Casual", StringComparison.OrdinalIgnoreCase);
        }

        private string GetWorkflowEngineDbConnectionString()
        {
            var configured = _configuration?.GetConnectionString("workflowconstring")
                          ?? _configuration?.GetConnectionString("WorkflowEngineConnection");
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            var constr = _configuration?.GetConnectionString("qhrconstring");
            if (string.IsNullOrWhiteSpace(constr))
                return string.Empty;

            try
            {
                var builder = new SqlConnectionStringBuilder(constr)
                {
                    InitialCatalog = "WorkflowEngineDb"
                };
                return builder.ConnectionString;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void PopulateApproversFromWorkflowEngine(HrLvWfConfig cfg)
        {
            try
            {
                string wfConnStr = GetWorkflowEngineDbConnectionString();
                if (string.IsNullOrWhiteSpace(wfConnStr)) return;

                bool isSickOrCasual = IsSickOrCasualLeave(cfg.WfCode, cfg.LvTypName);
                string targetDefCode = isSickOrCasual ? "OUT-SICK-CSL" : "OUT-SICK-ANNUAL";

                using var conn = new SqlConnection(wfConnStr);
                conn.Open();

                using var cmd = new SqlCommand(@"
                    SELECT ws.StepNumber, ws.Name, u.EmpSysId
                    FROM WorkflowSteps ws
                    INNER JOIN WorkflowVersions wv ON ws.WorkflowVersionId = wv.Id AND wv.IsActive = 1
                    INNER JOIN WorkflowDefinitions wd ON wv.WorkflowDefinitionId = wd.Id AND wd.IsDeleted = 0
                    LEFT JOIN Users u ON TRY_CAST(ws.TargetUserId AS uniqueidentifier) = u.Id
                    WHERE wd.Code = @DefCode AND ws.IsDeleted = 0
                    ORDER BY ws.Sequence, ws.StepNumber", conn);
                cmd.Parameters.AddWithValue("@DefCode", targetDefCode);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int stepNum = reader["StepNumber"] != DBNull.Value ? Convert.ToInt32(reader["StepNumber"]) : 0;
                    int? empSysId = reader["EmpSysId"] != DBNull.Value ? Convert.ToInt32(reader["EmpSysId"]) : (int?)null;
                    if (!empSysId.HasValue || empSysId.Value <= 0) continue;

                    switch (stepNum)
                    {
                        case 1:
                            if (!cfg.OutsourceRecordOwnerEmpId.HasValue)
                                cfg.OutsourceRecordOwnerEmpId = empSysId;
                            break;
                        case 2:
                            if (!cfg.ReportingManagerEmpId.HasValue)
                                cfg.ReportingManagerEmpId = empSysId;
                            break;
                        case 3:
                            if (!cfg.HrManagerEmpId.HasValue)
                                cfg.HrManagerEmpId = empSysId;
                            break;
                        case 4:
                            if (!cfg.FinanceEmpId.HasValue)
                                cfg.FinanceEmpId = empSysId;
                            break;
                        case 5:
                            if (!cfg.CeoEmpId.HasValue)
                                cfg.CeoEmpId = empSysId;
                            break;
                    }
                }
            }
            catch
            {
                // Ignore if WorkflowEngineDb is unreachable; JSON / defaults are used
            }
        }

        private void SyncWorkflowEngineApprovers(HrLvWfConfig cfg)
        {
            try
            {
                string wfConnStr = GetWorkflowEngineDbConnectionString();
                if (string.IsNullOrWhiteSpace(wfConnStr)) return;

                var lvTypName = _hrmContext.HrLvType.FirstOrDefault(t => t.LvTypId == cfg.LvTypId)?.NameEng;
                bool isSickOrCasual = IsSickOrCasualLeave(cfg.WfCode, lvTypName);
                string outsourceDefCode = isSickOrCasual ? "OUT-SICK-CSL" : "OUT-SICK-ANNUAL";
                string inHouseDefCode   = isSickOrCasual ? "INH-SICK-CSL" : "INH-ANUL";

                using var conn = new SqlConnection(wfConnStr);
                conn.Open();

                void UpdateStepTargetUser(string defCode, int stepNumber, int? empSysId)
                {
                    if (!empSysId.HasValue || empSysId.Value <= 0) return;

                    using var updateCmd = new SqlCommand(@"
                        DECLARE @UserId NVARCHAR(100);
                        SELECT TOP 1 @UserId = LOWER(CAST(Id AS NVARCHAR(100)))
                        FROM Users
                        WHERE EmpSysId = @EmpSysId AND IsDeleted = 0;

                        IF @UserId IS NOT NULL
                        BEGIN
                            UPDATE ws
                            SET ws.TargetUserId = @UserId
                            FROM WorkflowSteps ws
                            INNER JOIN WorkflowVersions wv ON ws.WorkflowVersionId = wv.Id AND wv.IsActive = 1
                            INNER JOIN WorkflowDefinitions wd ON wv.WorkflowDefinitionId = wd.Id AND wd.IsDeleted = 0
                            WHERE wd.Code = @DefCode
                              AND ws.StepNumber = @StepNumber
                              AND ws.ApprovalType = 11
                              AND ws.IsDeleted = 0;
                        END", conn);
                    updateCmd.Parameters.AddWithValue("@EmpSysId", empSysId.Value);
                    updateCmd.Parameters.AddWithValue("@DefCode", defCode);
                    updateCmd.Parameters.AddWithValue("@StepNumber", stepNumber);
                    updateCmd.ExecuteNonQuery();
                }

                // Sync Outsource Workflow steps (Step 1: Outsource Record Owner, Step 3: HR, Step 4: Finance, Step 5: CEO)
                UpdateStepTargetUser(outsourceDefCode, 1, cfg.OutsourceRecordOwnerEmpId);
                UpdateStepTargetUser(outsourceDefCode, 3, cfg.HrManagerEmpId);
                UpdateStepTargetUser(outsourceDefCode, 4, cfg.FinanceEmpId);
                if (!isSickOrCasual)
                {
                    UpdateStepTargetUser(outsourceDefCode, 5, cfg.CeoEmpId);
                }

                // Sync In-House Workflow steps (Step 1: HR, Step 4: Finance, Step 5: CEO)
                UpdateStepTargetUser(inHouseDefCode, 1, cfg.HrManagerEmpId);
                UpdateStepTargetUser(inHouseDefCode, 4, cfg.FinanceEmpId);
                if (!isSickOrCasual)
                {
                    UpdateStepTargetUser(inHouseDefCode, 5, cfg.CeoEmpId);
                }
            }
            catch
            {
                // Non-fatal if WorkflowEngineDb is unavailable
            }
        }

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
