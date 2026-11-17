using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Employee.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using System;
using System.Linq;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    //[Authorize]
    public class EmpSrtOrdVwController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpGenInfo> _dbCommitContext;

        public EmpSrtOrdVwController(EmployeeContext employeeContext, DbCommitContext<HrEmpGenInfo> dbCommitContext)
        {
            _employeeContext = employeeContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index()
        {
            try
            {
                int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                var prv = HttpContext.Session.GetString("EM_SRT_ORD");
                if (userGrpId != 1 && userGrpId != 2 && prv == null)
                {
                    return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
                }

                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                ViewBag.EmpSrtOrdLst = _employeeContext.GetEmpSrtOdrVws(entId, 0).ToList();
                return View();
            }
            catch { throw; }
        }

        // GET: EmpSrtOrdVw/UpdateGet
        public ActionResult UpdateGet(int empSysId)
        {
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
            var prv = HttpContext.Session.GetString("EM_SRT_ORD");
            if (userGrpId != 1 && userGrpId != 2 && prv == null)
            {
                return Unauthorized();
            }

            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            var empSrtOrdVw = _employeeContext.GetEmpSrtOdrVws(entId, empSysId).FirstOrDefault();
            if (empSrtOrdVw == null) return NotFound();
            return PartialView("_EmpSrtOrdVw_Updt", empSrtOrdVw);
        }

        // POST: EmpSrtOrdVw/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(EmpSrtOrdVw empSrtOrdVw)
        {
            try
            {
                int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                var prv = HttpContext.Session.GetString("EM_SRT_ORD");
                if (userGrpId != 1 && userGrpId != 2 && prv == null)
                {
                    return Unauthorized();
                }

                var nwSrtOrd = empSrtOrdVw.DeptSrtOrd * 1000000 + empSrtOrdVw.ex_EmpSrtOrd;
                if (empSrtOrdVw.EmpSrtOrd != nwSrtOrd)
                {
                    var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSrtOrdVw.EmpSysId);
                    empGenInfo.SrtOrd = nwSrtOrd;
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, empGenInfo, userEmpSysId);
                    _employeeContext.UpdateEmpSrtOrd(empSrtOrdVw.DeptId, nwSrtOrd, empSrtOrdVw.EmpSysId);
                    return RedirectToAction(nameof(Index)).WithSuccess("Employee Sort Order", "Record updated Successfully");
                }
                else
                    return RedirectToAction(nameof(Index)).WithSuccess("Employee Sort Order", "No changes found to update");
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Sort Order Group", "Update Record got failed");
            }
        }
    }
}
