using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using qHRM.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.General.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpResidInfoController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly OrganizationContext _organizationContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpResidInfo> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpResidInfoController(EmployeeContext employeeContext,
                                       OrganizationContext organizationContext,
                                       SysSelectListContext sysSelectListContext,
                                       DbCommitContext<HrEmpResidInfo> dbCommitContext,
                                       AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._sysSelectListContext = sysSelectListContext;
            this._organizationContext = organizationContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index(int Id)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_RES_DTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var list = _employeeContext.GetResidanceByEmpSysId(Id).ToList();
                ViewBag.DataSource = list;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_RES_DTL").FirstOrDefault();
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

        // GET: HrEmpGrade/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpResidInfo hrResid)
        {
            try
            {

                ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
                ViewBag.lstRestype = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_RSD_TYP);
                @ViewBag.Today = DateTime.Today;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_RES_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;                
                //End | Usr Prv//
                return PartialView("_HrEmpResidInfo_Create", hrResid);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }
        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpResidInfo hrResid, IList<IFormFile> imgUpload)
        {
            try
            {
                try
                {

                    hrResid.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                    int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrResid, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithSuccess("QCS", "Record Added Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpGrade/Edit/5
        public ActionResult EditGet(int id)
        {
            int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            var hrResid = _employeeContext.GetResidanceById(id).FirstOrDefault();
            ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
            ViewBag.lstRestype = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_RSD_TYP);

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_RES_DTL").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return PartialView("_HrEmpResidInfo_Edit", hrResid);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpResidInfo hrResid, IList<IFormFile> imgUpload)
        {

            try
            {
                try
                {
                    hrResid.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                    var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrResid, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithSuccess("QCS", "Record Updated Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                }
                catch (Exception)
                {

                    return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpGrade/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var hrResid = _employeeContext.GetResidanceById(id).FirstOrDefault();

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_RES_DTL").FirstOrDefault();                
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpResidInfo_Delete", hrResid);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpResidInfo hrResid)
        {
            try
            {
                hrResid.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrResid, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");

            }
            catch
            {
                return RedirectToAction("Index", "HrEmpResidInfo", new { @Id = hrResid.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "InDate")] DateTime? stDate, [Bind(Prefix = "OutDate")] DateTime? endDate, [Bind(Prefix = "IsToDate")] bool? isToDate)
        {
            if (isToDate == null || isToDate == false)
            {             

                if (endDate > stDate)
                {
                    return Json(true);
                }
            }
            return Json(false);

        }
    }
}