using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Data;

namespace qHRM.Areas.Attendance.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAtndcExpRqstController : Controller
    {
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _atndcContext;
        private readonly DbCommitContext<HrEmpAttendExp> _dbCommitContext;

        public HrEmpAtndcExpRqstController(EmployeeContext employeeContext
                                            , AttnContext atndcContext
                                            , DbCommitContext<HrEmpAttendExp> dbCommitContext)
        {
            _empContext = employeeContext;
            _atndcContext = atndcContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ATN_EXP_RQ") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var atndcPeriod = _atndcContext.GetAtndcPeriod();

                var objEmp = _empContext.GetEmpView("ALL", 0, 1).Where(x => x.EmpSysId == (int)HttpContext.Session.GetInt32("userEmpSysId")).FirstOrDefault();
                ViewBag.EmpId = objEmp.EmpId;
                ViewBag.EmpName = objEmp.FullName;
                ViewBag.Dpt = objEmp.DeptName;
                ViewBag.Dsgntn = objEmp.DesigName;
                ViewBag.FromDate = atndcPeriod.frmDate.ToString("yyyy-MM-dd");
                ViewBag.ToDate = atndcPeriod.toDate.ToString("yyyy-MM-dd");
                return View();
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        public JsonResult Get_AbsntExp(string schTyp, string slctdempLst)
        {
            try
            {
                var atndcPeriod = _atndcContext.GetAtndcPeriod();
                int empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");      //Dpt Head Should check with (3)        
                var _list = _atndcContext.GetEmpAbsntExp(schTyp, empSysId, atndcPeriod.frmDate.ToString("yyyy-MM-dd"), atndcPeriod.toDate.ToString("yyyy-MM-dd"), slctdempLst);
                return Json(new { ok = true, data = _list, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult Set_AbsntExp(string Date, string Rmks)
        {
            HrEmpAttendExp attdExcp = new HrEmpAttendExp();
            int empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
            DateTime dtDate = Convert.ToDateTime(Date);
            int x = 0;
            try
            {
                var objExp = _atndcContext.GetHrEmpAttendExeption(false, empSysId).Where(x => x.EmpSysId == empSysId && x.StDate == dtDate && x.EndDate == dtDate).FirstOrDefault();

                attdExcp.EmpSysId = empSysId;
                attdExcp.StDate = Convert.ToDateTime(Date);
                attdExcp.EndDate = Convert.ToDateTime(Date);
                attdExcp.Note = Rmks;
                attdExcp.RqstId = empSysId;
                attdExcp.CrtId = empSysId;
                attdExcp.RecStat = 1;
                if (objExp != null)
                {
                    attdExcp.AtnExpId = objExp.AtnExpId;
                    x = _dbCommitContext.commitChanges(LogEventTypes.UPD, attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                }
                else
                    x = _dbCommitContext.commitChanges(LogEventTypes.INS, attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"));

                return Json(new { ok = true, data = 1, message = "ok" });
            }
            catch (Exception)
            {
                return Json(new { ok = true, data = 2, message = "failed" });
            }

        }

        [HttpGet]
        public JsonResult Cancel_AbsntExp(string Date)
        {
            HrEmpAttendExp attdExcp = new HrEmpAttendExp();
            int empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
            DateTime dtDate = Convert.ToDateTime(Date);
            int x = 0;
            try
            {
                var objExp = _atndcContext.GetHrEmpAttendExeption(false, empSysId).Where(x => x.EmpSysId == empSysId && x.StDate == dtDate && x.EndDate == dtDate).FirstOrDefault();

                if (objExp != null)
                {
                    attdExcp.AtnExpId = objExp.AtnExpId;
                    x = _dbCommitContext.commitChanges(LogEventTypes.DEL, attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                }
                return Json(new { ok = true, data = 1, message = "ok" });
            }
            catch
            {
                return Json(new { ok = true, data = 2, message = "failed" });
            }
        }
    }
}
