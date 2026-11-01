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
                //if (HttpContext.Session.GetString("EMP_SRTORD") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                ViewBag.EmpSrtOrdLst = _employeeContext.GetEmpSrtOdrVws((int)HttpContext.Session.GetInt32("userEntId"), 0).ToList();
                return View();
            }
            catch { throw; }
        }

        // GET: EmpSrtOrdVw/UpdateGet
        public ActionResult UpdateGet(int empSysId)
        {
            var empSrtOrdVw = _employeeContext.GetEmpSrtOdrVws((int)HttpContext.Session.GetInt32("userEntId"), empSysId).First();
            return PartialView("_EmpSrtOrdVw_Updt", empSrtOrdVw);
        }

        //UpdateGet
        // POST: EmpSrtOrdVw/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(EmpSrtOrdVw empSrtOrdVw)
        {
            try
            {
                var nwSrtOrd = empSrtOrdVw.DeptSrtOrd * 1000000 + empSrtOrdVw.ex_EmpSrtOrd;
                if (empSrtOrdVw.EmpSrtOrd != nwSrtOrd)
                {

                    var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSrtOrdVw.EmpSysId);
                    empGenInfo.SrtOrd = nwSrtOrd;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, empGenInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
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
