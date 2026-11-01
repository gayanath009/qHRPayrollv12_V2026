    using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Data;
using qHRM.Areas.General.Data;
using qHRM.Areas.Leave.Data;

namespace QCSEF.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpGenInfoController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly OrganizationContext _organizationContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly EntityContext _entityContext;
        private readonly LeaveContext _leaveContext;
        private readonly DbCommitContext<HrEmpGenInfo> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        
        public HrEmpGenInfoController(EmployeeContext employeeContext,
                                      SysSelectListContext sysSelectListContext,
                                      OrganizationContext organizationContext,
                                      EntityContext entityContext,
                                      LeaveContext leaveContext,
                                      DbCommitContext<HrEmpGenInfo> dbCommitContext,
                                      AdmUserGrpPrvContext prvContext)
        {
            this._sysSelectListContext = sysSelectListContext;
            this._organizationContext = organizationContext;
            this._entityContext = entityContext;
            this._leaveContext = leaveContext;
            this._employeeContext = employeeContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;

        }

        // GET: HrEmpGenInfo
        public ActionResult Index()
        {
            try
            {
                //No Access Granted               
               if (HttpContext.Session.GetString("EM_GEN_INF") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                int usrId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));

                bool isSelfOnlyUser = _employeeContext.IsRestrictedSelfOnlyUser(empSysId, grpId);
                bool isOutsourceAdmin = !isSelfOnlyUser && _employeeContext.IsOutsourceAdmin(empSysId, grpId);

                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x=>x.PrvCode == "EM_GEN_INF").FirstOrDefault();
                ViewBag.RecAdd = !isSelfOnlyUser && (objprv?.RecAdd ?? false);
                ViewBag.RecEdit = objprv?.RecEdit ?? false;                
                //End | Usr Prv//

                if (isSelfOnlyUser)
                {
                    var selfData = _employeeContext.GetEmpView("SELF", empSysId, usrId);
                    ViewBag.DataSource = empSysId > 0 ? selfData.Where(x => x.EmpSysId == empSysId).ToList() : selfData;
                }
                else if (isOutsourceAdmin)
                {
                    ViewBag.DataSource = _employeeContext.GetEmpView("OUTSOURCE", empSysId > 0 ? empSysId : 97, usrId);
                }
                else
                {
                    ViewBag.DataSource = _employeeContext.GetEmpView("ALL", 0, usrId);
                }
                ViewBag.lstWrkType = _organizationContext.GetAllWrkTypeViews();
                return View();
                
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        // GET: HrEmpGenInfo/CreateGet
        [HttpGet]
        public IActionResult HrEmpGenInfo_CreateNew(HrEmpGenInfo genInfo)
        {
            try
            {                
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (_employeeContext.IsRestrictedSelfOnlyUser(empSysId, grpId))
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Employee", "Access denied. You cannot create new employee records.");
                }

                ViewBag.lstTitle = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TITL);
                ViewBag.lstEmpTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_TYP);
                ViewBag.lstPmtTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.PMT_TYP); 
                ViewBag.lstGender = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.GND);  
                ViewBag.lstMrtlStat = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MRTL_STAT);  
                ViewBag.lstBldGrp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BLD_GRP); 
                ViewBag.lstCurrStat = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_CUR_STAT);
                ViewBag.lstTcktTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TCKT_ALGB);
                ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
                ViewBag.lstNlty = _organizationContext.GetAllNationalityViews();
                ViewBag.lstReligion = _organizationContext.GetAllReligionViews();
                ViewBag.lstWrkType = _organizationContext.GetAllWrkTypeViews();
                ViewBag.lstGrd = _employeeContext.GetFilteredGrades();
                ViewBag.lstGrp = _employeeContext.GetFilteredEmpGroups();
                ViewBag.lstLoc = _entityContext.GetAllLocsToAsgn();

                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_GEN_INF").FirstOrDefault();

                genInfo.JoinedDate = DateTime.Today;
                genInfo.PmtExpDate = DateTime.Today;
                genInfo.Dob = Convert.ToDateTime("2000-01-01");
                genInfo.CntryId = 1;
                genInfo.NltyId = 1;
                genInfo.WrktypId = 1;

                ViewBag.RecAdd = objprv?.RecAdd ?? false;
                //End | Usr Prv//

                return View("HrEmpGenInfo_Create", genInfo);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Emp Info.", ex.Message);
            }

        }

        // POST: HrEmpGenInfo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HrEmpGenInfo_Create(HrEmpGenInfo genInfo)
        {
            int _result;
            try
            {
                if (genInfo.WrktypId == null || genInfo.WrktypId == 0)
                    genInfo.WrktypId = 1;
                if (genInfo.NameArb == null)
                    genInfo.NameArb = genInfo.PstName;
                if (genInfo.PmtExpDate == null)
                    genInfo.PmtExpDate = Convert.ToDateTime("01/01/1900");
                if (genInfo.PstExpDate == null)
                    genInfo.PstExpDate = Convert.ToDateTime("01/01/1900");
                if (genInfo.DrvLcnExpDate == null)
                    genInfo.DrvLcnExpDate = Convert.ToDateTime("01/01/1900");
                if (genInfo.HlthCrdExpDate == null)
                    genInfo.HlthCrdExpDate = Convert.ToDateTime("01/01/1900");

                genInfo.SrtOrd = 1000;

                genInfo.AdId = genInfo.EmpId;
                genInfo.Password = genInfo.EmpId;
                _result = _dbCommitContext.commitChanges(LogEventTypes.INS, genInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction(nameof(Index)).WithSuccess("Emp Info.", "Record Added Successfully");
                else
                    return View("HrEmpGenInfo_Create", genInfo);
            }
            catch (Exception)
            {
                return View("HrEmpGenInfo_Create", genInfo);
            }
        }

        // GET: HrEmpGenInfo/EditGet/5
        public IActionResult HrEmpGenInfo_EditGet(int id)
        {           
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (_employeeContext.IsRestrictedSelfOnlyUser(empSysId, grpId) && id != empSysId)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Employee", "Access denied. You can only view your own employee details.");
                }

                //var objResult = _employeeContext.GetEmpGenInfoView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.Emp_Sys_Id == id).FirstOrDefault();
                var objResult = _employeeContext.GetEmpGenInfoByEmpSysId(id);
                if (objResult == null)
                {
                    return RedirectToAction(nameof(Index)).WithDanger("Employee", "Employee record not found");
                }
                objResult.ex_EmpId = objResult.EmpId;
                objResult.ex_PmtId = objResult.PmtId;

                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_GEN_INF").FirstOrDefault();            
                ViewBag.RecEdit = objprv?.RecEdit ?? false;
                //End | Usr Prv//

                HttpContext.Session.SetInt32("EmpSysId", objResult.EmpSysId);
                HttpContext.Session.SetString("EmpName", objResult.FullName ?? "");
                ViewBag.lstTitle = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TITL);
                ViewBag.lstEmpTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_TYP);
                ViewBag.lstPmtTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.PMT_TYP);
                ViewBag.lstGender = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.GND);
                ViewBag.lstMrtlStat = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MRTL_STAT);
                ViewBag.lstBldGrp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BLD_GRP);
                ViewBag.lstCurrStat = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_CUR_STAT);
                ViewBag.lstTcktTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TCKT_ALGB);
                ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
                ViewBag.lstNlty = _organizationContext.GetAllNationalityViews();
                ViewBag.lstReligion = _organizationContext.GetAllReligionViews();
                ViewBag.lstWrkType = _organizationContext.GetAllWrkTypeViews();
                ViewBag.lstGrd = _employeeContext.GetFilteredGrades();
                ViewBag.lstGrp = _employeeContext.GetFilteredEmpGroups();
                ViewBag.lstLoc = _entityContext.GetFilteredLocations();
                return View("HrEmpGenInfo_Edit", objResult);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithDanger("Employee", "Error loading employee details: " + ex.Message);
            }
        }

        // POST: HrEmpGenInfo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HrEmpGenInfo_Edit(HrEmpGenInfo genInfo)
        {
            int _result;
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        if (genInfo.NameArb == null)
                            genInfo.NameArb = genInfo.FullName;
                        if (genInfo.PmtExpDate == null)
                            genInfo.PmtExpDate = Convert.ToDateTime("01/01/1900");
                        if (genInfo.PstExpDate == null)
                            genInfo.PstExpDate = Convert.ToDateTime("01/01/1900");
                        if (genInfo.DrvLcnExpDate == null)
                            genInfo.DrvLcnExpDate = Convert.ToDateTime("01/01/1900");
                        if (genInfo.HlthCrdExpDate == null)
                            genInfo.HlthCrdExpDate = Convert.ToDateTime("01/01/1900");
                        if (genInfo.CurrStat == 0)
                            genInfo.SrvEndDate = null;
                        _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, genInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction(nameof(Index)).WithSuccess("Emp Info.", "Record Updated Successfully");
                        else
                            return View("HrEmpGenInfo_Edit", genInfo);
                    }
                    catch (Exception)
                    {
                        return View("HrEmpGenInfo_Edit", genInfo);
                    }
                }
                else
                {
                    return View("HrEmpGenInfo_Edit", genInfo);
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: OrgInfo/Delete/5
        public IActionResult HrEmpGenInfo_DeleteGet(int id)
        {
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                if (_employeeContext.IsRestrictedSelfOnlyUser(empSysId, grpId))
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Employee", "Access denied.");
                }

                //var oUsrGrp = _employeeContext.GetEmpGenInfoView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.EmpSysId == id).FirstOrDefault();
                //Start | Usr Prv//
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_GEN_INF").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                var oUsrGrp = _employeeContext.GetEmpGenInfoByEmpSysId(id);
                return PartialView("HrEmpGenInfo_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Emp Info", ex.Message);
            }


        }

        // POST: OrgInfo/Delete/5
        [HttpPost]
        public IActionResult HrEmpGenInfo_Delete(HrEmpGenInfo genInfo)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, genInfo, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Emp Info", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Emp Info", ex.Message);
            }
        }

        public IActionResult IsExistEmpID([Bind(Prefix = "EmpId")] string value, [Bind(Prefix = "ex_EmpId")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    //var _itemfound = _employeeContext.GetView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.Emp_Id.ToLower() == value.ToLower()).FirstOrDefault();
                    var _itemfound = _employeeContext.GetAllEmpGenInfo().Where(x => x.EmpId.ToLower() == value.ToLower()).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                //var _itemfound = _employeeContext.GetView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.Emp_Id.ToLower() == value.ToLower()).FirstOrDefault();
                var _itemfound = _employeeContext.GetAllEmpGenInfo().Where(x => x.EmpId.ToLower() == value.ToLower()).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }

        public IActionResult IsExistQID([Bind(Prefix = "PmtId")] string value, [Bind(Prefix = "ex_PmtId")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    //var _itemfound = _employeeContext.GetView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.Pmt_Id.ToLower() == value.ToLower()).FirstOrDefault();
                    var _itemfound = _employeeContext.GetAllEmpGenInfo().Where(x => x.PmtId.ToLower() == value.ToLower()).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                //var _itemfound = _employeeContext.GetView("_all", 0, HttpContext.Session.GetInt32("userEmpSysId")).Where(x => x.Pmt_Id.ToLower() == value.ToLower()).FirstOrDefault();
                var _itemfound = _employeeContext.GetAllEmpGenInfo().Where(x => x.PmtId.ToLower() == value.ToLower()).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }

        public IActionResult ValidateDOB([Bind(Prefix = "Dob")] DateTime? dob)
        {
            if (dob >= Convert.ToDateTime("01/01/1950") && dob <= DateTime.Now.AddYears(-10))
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidateJoinedDate([Bind(Prefix = "JoinedDate")] DateTime? joinDate)
        {
            if (joinDate >= Convert.ToDateTime("01/01/1950") && joinDate <= DateTime.Now)
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidatePmtExpDate([Bind(Prefix = "PmtExpDate")] DateTime? pmtExpDate)
        {
            if (pmtExpDate >= DateTime.Now.AddMonths(-3))
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidateSvcEndDateDate([Bind(Prefix = "SrvEndDate")] DateTime? srvEndDate)
        {
            if (srvEndDate >= DateTime.Now.AddMonths(-1))
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidateCntryId([Bind(Prefix = "CntryId")] int cntryId)
        {
            if (cntryId > 0)
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidateNltyId([Bind(Prefix = "NltyId")] int nltyId)
        {
            if (nltyId > 0)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}