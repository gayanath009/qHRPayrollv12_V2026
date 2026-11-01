using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;
using qHRM.Areas.Leave.Data;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    [Authorize]
    public class HrLvWfDelegationController : Controller
    {
        private readonly qHRMasterContext    _hrmContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly LeaveWorkflowService _lvWfService;

        public HrLvWfDelegationController(
            qHRMasterContext     hrmContext,
            AdmUserGrpPrvContext prvContext,
            LeaveWorkflowService lvWfService)
        {
            _hrmContext   = hrmContext;
            _prvContext   = prvContext;
            _lvWfService  = lvWfService;
        }

        // =====================================================================
        // LIST
        // =====================================================================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("LV_WF_DLG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            SetPrivileges("LV_WF_DLG");

            var delegations = (from d in _hrmContext.HrLvWfDelegation
                               join dlgtr in _hrmContext.HrEmpGenInfo on d.DlgtrEmpId equals dlgtr.EmpSysId
                               join dlgte in _hrmContext.HrEmpGenInfo on d.DlgteEmpId equals dlgte.EmpSysId
                               join lvtyp in _hrmContext.HrLvType on d.LvTypId equals lvtyp.LvTypId into lv
                               from lvt in lv.DefaultIfEmpty()
                               select new HrLvWfDelegation
                               {
                                   DelegId       = d.DelegId,
                                   DlgtrEmpId    = d.DlgtrEmpId,
                                   DlgteEmpId    = d.DlgteEmpId,
                                   LvTypId       = d.LvTypId,
                                   FromDate      = d.FromDate,
                                   ToDate        = d.ToDate,
                                   Reason        = d.Reason,
                                   RecStat       = d.RecStat,
                                   CrtDateTime   = d.CrtDateTime,
                                   DelegatorName = dlgtr.FirstName + " " + dlgtr.LastName,
                                   DelegateName  = dlgte.FirstName + " " + dlgte.LastName,
                                   LvTypName     = lvt == null ? "All Leave Types" : lvt.NameEng,
                                   WfDelegationId= d.WfDelegationId
                               })
                              .OrderByDescending(d => d.CrtDateTime)
                              .ToList();

            ViewBag.DataSource = delegations;
            return View();
        }

        // =====================================================================
        // CREATE (GET)
        // =====================================================================
        [HttpGet]
        public IActionResult CreateNew()
        {
            if (HttpContext.Session.GetString("LV_WF_DLG") == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            SetPrivileges("LV_WF_DLG");
            LoadDropdowns();
            return PartialView("_HrLvWfDelegation_Create",
                new HrLvWfDelegation { FromDate = DateTime.Today, ToDate = DateTime.Today });
        }

        // =====================================================================
        // CREATE (POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HrLvWfDelegation deleg)
        {
            try
            {
                if (deleg.DlgtrEmpId == deleg.DlgteEmpId)
                    return RedirectToAction(nameof(Index))
                        .WithWarning("Delegation", "Delegator and Delegate cannot be the same employee.");

                if (deleg.ToDate < deleg.FromDate)
                    return RedirectToAction(nameof(Index))
                        .WithWarning("Delegation", "To Date must be on or after From Date.");

                // Check for overlapping active delegation for the same delegator
                bool overlap = _hrmContext.HrLvWfDelegation.Any(d =>
                    d.DlgtrEmpId == deleg.DlgtrEmpId &&
                    d.RecStat    == 1 &&
                    d.FromDate   <= deleg.ToDate &&
                    d.ToDate     >= deleg.FromDate &&
                    (d.LvTypId == null || deleg.LvTypId == null || d.LvTypId == deleg.LvTypId));

                if (overlap)
                    return RedirectToAction(nameof(Index))
                        .WithWarning("Delegation",
                            "An overlapping active delegation already exists for this employee and date range. " +
                            "Please deactivate the existing delegation first.");

                deleg.CrtId       = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                deleg.CrtDateTime = DateTime.Now;
                deleg.RecStat     = 1;

                _hrmContext.HrLvWfDelegation.Add(deleg);
                await _hrmContext.SaveChangesAsync();

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Delegation", "Delegation created successfully.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index))
                    .WithWarning("Delegation", ex.Message);
            }
        }

        // =====================================================================
        // CANCEL DELEGATION
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> Cancel(int delegId)
        {
            try
            {
                var deleg = _hrmContext.HrLvWfDelegation.FirstOrDefault(d => d.DelegId == delegId);
                if (deleg == null)
                    return RedirectToAction(nameof(Index)).WithWarning("Delegation", "Record not found.");

                deleg.RecStat     = 0;
                deleg.MdfId       = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                deleg.MdfDateTime = DateTime.Now;

                await _hrmContext.SaveChangesAsync();

                return RedirectToAction(nameof(Index))
                    .WithSuccess("Delegation", "Delegation cancelled successfully.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Delegation", ex.Message);
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

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
            // CurrStat == 0 = Active employee (consistent with EmployeeContext convention)
            ViewBag.lstEmp = _hrmContext.HrEmpGenInfo
                .Where(e => e.CurrStat == 0)
                .OrderBy(e => e.FirstName)
                .Select(e => new { ESysId = e.EmpSysId, FulNme = e.FullName })
                .ToList();

            ViewBag.lstLvTyp = _hrmContext.HrLvType
                .Where(t => t.RecStat == 1)
                .OrderBy(t => t.NameEng)
                .Select(t => new { t.LvTypId, t.NameEng })
                .ToList();
        }
    }
}
