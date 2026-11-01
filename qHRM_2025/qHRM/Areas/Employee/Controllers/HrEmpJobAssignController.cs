using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpJobAssignController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly EntityContext _entityContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        private int _empSysId;

        public HrEmpJobAssignController(EmployeeContext employeeContext,
                                        EntityContext entityContext,
                                        SysSelectListContext sysSelectListContext,
                                        AdmUserGrpPrvContext prvContext)
        {
            _employeeContext = employeeContext;
            _entityContext = entityContext;
            _sysSelectListContext = sysSelectListContext;
            _prvContext = prvContext;
        }

        // GET: HrEmpJobAssigns
        public ActionResult Index(int empSysId)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_JB_ASG") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId")); //TODO Replace with getting selected employee from the Session
                _empSysId = empSysId;
                HrEmpGenInfo genInfo = _employeeContext.GetEmpGenInfoByEmpSysId(_empSysId);
                if (genInfo != null)
                {
                    ViewBag.EmpSysId = _empSysId;
                    ViewBag.EmpId = genInfo.EmpId;
                    ViewBag.EmpName = genInfo.FirstName + " " + genInfo.LastName;
                    var recList = _employeeContext.GetJbViews(_empSysId).ToList();
                    ViewBag.DataSource = recList;

                    //Start | Usr Prv//
                    int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                    var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_JB_ASG").FirstOrDefault();
                    ViewBag.RecAdd = objprv.RecAdd;                    
                    //End | Usr Prv//

                    return View();
                }
                else
                    return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: HrEmpJobAssign/CreateNew
        [HttpGet]
        public IActionResult CreateNew(int empSysId)
        {
            try
            {
                var genInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSysId);
                HrEmpJobAssign empJbAsgn = new HrEmpJobAssign();
                var activeEmpJbAsgn = _employeeContext.GetCurrentAssignment(empSysId);

                if (activeEmpJbAsgn != null)
                    empJbAsgn = activeEmpJbAsgn;

                empJbAsgn.EmpSysId = empSysId;
                empJbAsgn.JbAsngId = 0;
                empJbAsgn.StDate = DateTime.Today;
                empJbAsgn.EndDate = null;

                ViewBag.EmpSysId = empSysId;
                ViewBag.EmpId = genInfo.EmpId;
                ViewBag.EmpName = genInfo.FirstName + " " + genInfo.LastName;
                ViewBag.lstDept = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.lstDesig = _entityContext.GetDesignationViews();
                ViewBag.lstEmpTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_TYP); //_enumContext.lstEmpTyp;
                ViewBag.lstRptMng = _employeeContext.GetRepotingManagers(empSysId);

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_JB_ASG").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;              
                //End | Usr Prv//
                return PartialView("_hrEmpJobAssign_Create", empJbAsgn);
                
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Job Assignment", ex.Message);
            }
        }

        // POST: HrEmpJobAssign/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpJobAssign empJbAsgn)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var confirmationMsg = "";
                        if (_employeeContext.InsertNewJbAssignment(empJbAsgn, (int)HttpContext.Session.GetInt32("userEmpSysId")) == 1)
                            confirmationMsg = "Record Added Successfully";
                        else
                            confirmationMsg = "Record Adding Failed";
                        return RedirectToAction(nameof(Index)).WithSuccess("Job Assignment", confirmationMsg);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Job Assignment", "Adding Record got failed");
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

        // GET: HrEmpJobAssign/EditGet
        [HttpGet]
        public IActionResult EditGet(int jbAsngId)
        {
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var genInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSysId);
                HrEmpJobAssign empJbAsgn = new HrEmpJobAssign();
                var EmpJbAsgn = _employeeContext.GetCurrentAssignment(empSysId, jbAsngId);
          
                ViewBag.EmpSysId = empSysId;
                ViewBag.EmpId = genInfo.EmpId;
                ViewBag.EmpName = genInfo.FirstName + " " + genInfo.LastName;
                ViewBag.lstDept = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.lstDesig = _entityContext.GetDesignationViews();
                ViewBag.lstEmpTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_TYP); //_enumContext.lstEmpTyp;
                ViewBag.lstRptMng = _employeeContext.GetRepotingManagers(empSysId);

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_JB_ASG").FirstOrDefault();                
                ViewBag.RecEdit = objprv.RecEdit;                
                //End | Usr Prv//
                return PartialView("_hrEmpJobAssign_Edit", EmpJbAsgn);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Job Assignment", ex.Message);
            }
        }

        // POST: HrEmpJobAssign/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpJobAssign empJbAsgn)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var confirmationMsg = "";                        
                        if (_employeeContext.UpdateJbAssignment(empJbAsgn)==1)
                            confirmationMsg = "Record Updated Successfully";
                        else
                            confirmationMsg = "Record Updating Failed";
                        return RedirectToAction(nameof(Index)).WithSuccess("Job Assignment", confirmationMsg);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Job Assignment", "Updating Record got failed");
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
    }
}