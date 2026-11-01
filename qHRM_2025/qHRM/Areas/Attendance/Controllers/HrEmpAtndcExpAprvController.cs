using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;

namespace qHRM.Areas.Attendance.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAtndcExpAprvController : Controller
    {
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _atndcContext;
        private readonly DbCommitContext<HrEmpAttendExp> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpAtndcExpAprvController(EmployeeContext empContext,
                                            AttnContext atndcContext,
                                            DbCommitContext<HrEmpAttendExp> dbCommitContext,
                                            AdmUserGrpPrvContext prvContext)
        {
            _empContext = empContext;
            _atndcContext = atndcContext;
            _dbCommitContext = dbCommitContext;
            _prvContext = prvContext;
        }
        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ATN_EXP_AP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                var atndcPeriod = _atndcContext.GetAtndcPeriod();
                var objEmp = _empContext.GetEmpView("ALL", 0, 1).Where(x => x.EmpSysId == 3).FirstOrDefault(); // (int)HttpContext.Session.GetInt32("userEmpSysId")
                ViewBag.EmpId = objEmp.EmpId;
                ViewBag.EmpName = objEmp.FullName;
                ViewBag.Dpt = objEmp.DeptName;
                ViewBag.Dsgntn = objEmp.DesigName;
                ViewBag.FromDate = atndcPeriod.frmDate.ToString("yyyy-MM-dd");
                ViewBag.ToDate = atndcPeriod.toDate.ToString("yyyy-MM-dd");

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_EXP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public JsonResult Get_AbsntExpSmry()
        {
            try
            {
                var atndcPeriod = _atndcContext.GetAtndcPeriod();
                int eSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                var _list = _atndcContext.GetHrEmpAtndcExpSmryVw(eSysId, atndcPeriod.frmDate, atndcPeriod.toDate);
                return Json(new { ok = true, data = _list, message = "ok" });

            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult Set_ApprovAbsntExp(int AtnExpId, string Rmks, string AuthTyp)
        {
            int empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
            int x = 0;
            try
            {
                var objExp = _atndcContext.GetHrEmpAttendExpByAtnExpId(AtnExpId);
                objExp.AprvdId = empSysId;
                objExp.AprvdDateTime = DateTime.Today;
                objExp.AprvdRmks = Rmks;
                if (AuthTyp == "_a")
                    objExp.AprvdStatId = 1;
                else if (AuthTyp == "_r")
                    objExp.AprvdStatId = 2;
                x = _dbCommitContext.commitChanges(LogEventTypes.UPD, objExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));

                return Json(new { ok = true, data = 1, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = true, data = 2, message = "failed" });
            }

        }
    }
}
