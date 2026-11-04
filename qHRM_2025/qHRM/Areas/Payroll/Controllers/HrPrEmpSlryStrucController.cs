using System;
using System.Collections.Generic;
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
    public class HrPrEmpSlryStrucController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly DbCommitContext<HrPrEmpSlryStrucVw> _dbCommitContext;
        private readonly EmployeeContext _employeeContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrPrEmpSlryStrucController(PayrollContext payrollContext,
                                            DbCommitContext<HrPrEmpSlryStrucVw> dbCommitContext,
                                            EmployeeContext employeeContext,
                                            AdmUserGrpPrvContext prvContext)
        {
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
            _employeeContext = employeeContext;
            _prvContext = prvContext;
        }

        public IActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("SLRY_STRUC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.DataSource = _payrollContext.GetHrPrEmpSlryStrucViewsByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today, false, Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId")));

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "SLRY_STRUC").FirstOrDefault();
            ViewBag.RecAdd = objprv.RecAdd;
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return View();
        }


        public IActionResult MngrIndex()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("SLRY_STRUC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.DataSource = _payrollContext.GetHrPrEmpSlryStrucViewsByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today, false, Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId")));

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "SLRY_STRUC").FirstOrDefault();
            ViewBag.RecAdd = objprv.RecAdd;
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//

            return View();
        }

        // GET: HrPrEmpSlryStruc/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrEmpSlryStrucVw hrPrEmpSlryStruc)
        {
            try
            {
                ViewBag.lstEmp = _employeeContext.GetNewEmpForSlryStruct((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.lstOT = _payrollContext.GetHrPrOtrateViewByEntId((int)HttpContext.Session.GetInt32("userEntId"));

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "SLRY_STRUC").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;                
                //End | Usr Prv//

                return PartialView("_HrPrEmpSlryStruc_Create", hrPrEmpSlryStruc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Salary Structure", ex.Message);
            }
        }

        // POST: HrPrEmpSlryStruc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrEmpSlryStrucVw hrPrEmpSlryStruc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrEmpSlryStruc.CrtDateTime = DateTime.Now;
                        hrPrEmpSlryStruc.RecStat = 1;
                        hrPrEmpSlryStruc.CrtBy = (int)HttpContext.Session.GetInt32("userEmpSysId");
                        //_dbCommitContext.commitChanges(LogEventTypes.INS, hrPrEmpSlryStruc, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        _payrollContext.SaveHrPrEmpSlryStruc(LogEventTypes.INS, hrPrEmpSlryStruc);
                        return RedirectToAction(nameof(Index)).WithSuccess("Salary Structure", "Record Added Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Salary Structure", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrEmpSlryStruc/EditGet
        public ActionResult EditGet(int id)
        {
            var hrPrEmpSlryStruc = _payrollContext.GetHrPrEmpSlryStrucById(id);
            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrEmpSlryStruc.EmpSysId);
            ViewBag.EmpName = empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName;
            ViewBag.lstOT = _payrollContext.GetHrPrOtrateViewByEntId((int)HttpContext.Session.GetInt32("userEntId"));
            ViewBag.OTStat = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrEmpSlryStruc.EmpSysId).OtEnabled;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "SLRY_STRUC").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//

            return PartialView("_HrPrEmpSlryStruc_Edit", hrPrEmpSlryStruc);
        }

        // POST: HrPrEmpSlryStruc/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrEmpSlryStrucVw hrPrEmpSlryStruc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var ex_hrPrEmpSlryStruc = _payrollContext.GetHrPrEmpSlryStrucById(hrPrEmpSlryStruc.SlStrcId);
                        hrPrEmpSlryStruc.RecStat = ex_hrPrEmpSlryStruc.RecStat;
                        hrPrEmpSlryStruc.CrtDateTime = DateTime.Now; //ex_hrPrEmpSlryStruc.CrtDateTime;
                        hrPrEmpSlryStruc.CrtBy = (int)HttpContext.Session.GetInt32("userEmpSysId"); //ex_hrPrEmpSlryStruc.CrtBy;
                        _payrollContext.SaveHrPrEmpSlryStruc(LogEventTypes.UPD, hrPrEmpSlryStruc);
                        return RedirectToAction(nameof(Index)).WithSuccess("Salary Structure", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Salary Structure", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public JsonResult GetEmpOTStat(int empSysid)
        {           
            try
            {                
                var result = _employeeContext.GetEmpGenInfoByEmpSysId(empSysid).OtEnabled;
                return Json(new { ok = true, data = result, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

    }
}
