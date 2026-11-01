using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAttendExpController : Controller
    {
        private readonly AttnContext _attnContext;
        private readonly DbCommitContext<HrEmpAttendExp> _dbCommitContext;
        private readonly EmployeeContext _empContext;
        private readonly EntityContext _entityContext;
        private readonly qHRMasterContext _hrmContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpAttendExpController(AttnContext attnContext,
                                        DbCommitContext<HrEmpAttendExp> dbCommitContext,
                                        EmployeeContext empContext,
                                        EntityContext entityContext,
                                        qHRMasterContext hrmContext,
                                        AdmUserGrpPrvContext prvContext)
        {
            this._attnContext = attnContext;
            this._dbCommitContext = dbCommitContext;
            this._empContext = empContext;
            this._entityContext = entityContext;
            this._hrmContext = hrmContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpAttendExp
        public ActionResult Index(string tdyFltr)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("ATN_EXP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                if (tdyFltr==null)
                    ViewBag.DataSource = _attnContext.GetHrEmpAttendExeption(false, empSysId);
                else
                    ViewBag.DataSource = _attnContext.GetHrEmpAttendExeption(false, empSysId).Where(x => x.StDate <= DateTime.Today && x.EndDate >= DateTime.Today);

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

        // GET: HrEmpAttendExp/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpAttendExp attdExcp)
        {
            try
            {
                ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                @ViewBag.Today = DateTime.Today;
                attdExcp.empTyp = "A";

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_EXP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;           
                //End | Usr Prv//

                return PartialView("_HrEmpAttendExp_Create", attdExcp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemptions", ex.Message);
            }

        }

        // POST: HrEmpAttendExp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int _result = 0;
                    try
                    {
                        attdExcp.RqstId = attdExcp.EmpSysId;
                        attdExcp.AprvdStatId = 1;
                        attdExcp.AprvdId = Convert.ToInt32(HttpContext.Session.GetInt32("userEntId"));
                        attdExcp.AprvdDateTime = DateTime.Now;
                        _result = _attnContext.SaveHrEmpAttendExp(attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"), Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                        if (_result == 1)
                            return RedirectToAction(nameof(Index)).WithSuccess("Attendance Exemption", "Record(s) Added Successfully");
                        else
                            return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "Adding Record got failed");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "Adding Record got failed");
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

        // GET: HrEmpAttendExp/Edit/5
        public ActionResult EditGet(int id)
        {
            var empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
            ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
            ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
            var attdExp = _attnContext.GetHrEmpAttendExeption(false,empSysId).Where(x => x.AtnExpId == id);

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_EXP").FirstOrDefault();
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//
            return PartialView("_HrEmpAttendExp_Edit", attdExp);
        }

        // POST: HrEmpAttendExp/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {

                        var varChanges = _dbCommitContext.commitChanges(LogEventTypes.UPD, attdExcp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //---------- Execute Load Attendance Process ----------//
                        var prcSrcDtl = "Edit Exemption" + attdExcp.AtnExpId;
                        _attnContext.LoadHrEmpAtndc("S", attdExcp.EmpSysId, attdExcp.StDate, attdExcp.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //----------------------------------------------------//
                        return RedirectToAction(nameof(Index)).WithSuccess("Attendance Exemption", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "Adding Record got failed");
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

        // GET: HrEmpAttendExp/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var empSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                var attdExp = _attnContext.GetHrEmpAttendExeption(false, empSysId).Where(x => x.AtnExpId == id).FirstOrDefault();
                HrEmpAttendExp attdExcpNew = new HrEmpAttendExp();
                attdExcpNew.EmpSysId = attdExp.EmpSysId;
                attdExcpNew.AtnExpId = attdExp.AtnExpId;
                attdExcpNew.StDate = attdExp.StDate;
                attdExcpNew.EndDate = attdExp.EndDate;
                attdExcpNew.Note = attdExp.Note;
                ViewBag.EmployeeName = _empContext.GetAllEmpGenInfo().Where(x => x.EmpSysId == attdExp.EmpSysId).FirstOrDefault().FullName;
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_EXP").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpAttendExp_Delete", attdExcpNew);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", ex.Message);
            }
        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(HrEmpAttendExp attdExcp)
        {
            try
            {
                var hrEmpAttendExp = _attnContext.GetHrEmpAttendExpByAtnExpId(attdExcp.AtnExpId);
                if (hrEmpAttendExp != null)
                {
                    hrEmpAttendExp.RecStat = 0;
                    _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpAttendExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    //---------- Execute Load Attendance Process ----------//
                    var prcSrcDtl = "Delete Exemption" + attdExcp.AtnExpId;
                    _attnContext.LoadHrEmpAtndc("S", attdExcp.EmpSysId, hrEmpAttendExp.StDate, hrEmpAttendExp.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    //----------------------------------------------------//
                    return RedirectToAction(nameof(Index)).WithSuccess("Attendance Exemption", "Record Deleted Successfully.");
                }
                else
                    return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "Record Deletion got failed.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", ex.Message);
            }
        }

        // GET: HrEmpAttendExp/DeleteAllGet/5
        public IActionResult DeleteAllGet()
        {
            try
            {
                ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                @ViewBag.Today = DateTime.Today;
                
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "ATN_EXP").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpAttendExp_Delete_All");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", ex.Message);
            }
        }

        // POST: HrEmpGrade/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAll(HrEmpAttendExp attdExcp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var _delReclist = new List<HrEmpAttendExp>();
                    string prcSrcDtl = "";
                    var currUsrId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                    if (attdExcp.empTyp == "A") //All Employees
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("A", 0, attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Exemption (A)";
                    }
                    else if (attdExcp.empTyp == "D") // Department
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("D", Convert.ToInt32(attdExcp.dptId), attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Exemption (D)";
                    }
                    else if (attdExcp.empTyp == "S") // Selected Employee
                    {
                        _delReclist = _attnContext.GetHrEmpAttendExp("S", attdExcp.empSysId ?? 0, attdExcp.Del_StDate, attdExcp.Del_EndDate).ToList();
                        prcSrcDtl = "Delete All Exemption (S)";
                    }

                    if (_delReclist.Count() > 0)
                    {
                        foreach (var rec in _delReclist)
                        {
                            rec.RecStat = 0;
                            _dbCommitContext.commitChanges(LogEventTypes.UPD, rec, currUsrId);

                            //---------- Execute Load Attendance Process ----------//
                            _attnContext.LoadHrEmpAtndc(attdExcp.empTyp, 0, rec.StDate, rec.EndDate, AttnContext.AtndcLdPrcTyp.EXCMP, prcSrcDtl, false, currUsrId);
                            //----------------------------------------------------//
                        }
                        return RedirectToAction(nameof(Index)).WithSuccess("Attendance Exemption", "Record(s) Deleted Successfully");
                    }
                    else
                        return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "No Matching Records Found");
                }
                else
                    return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index)).WithWarning("Attendance Exemption", "Deleting Record got failed");
            }
        }

        public JsonResult Validate([Bind(Prefix = "EmpSysId")] int empSysId, [Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            IEnumerable<AttnContext.HrEmpAttendExpView> _obj;
            var eSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");

            if (empSysId == 0)
                _obj = _attnContext.GetHrEmpAttendExeption(false, eSysId).Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate));
            else
                _obj = _attnContext.GetHrEmpAttendExeption(false, eSysId).Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate) && x.EmpSysId == empSysId);

            if (_obj.Count() > 0)
            {
                return Json("There is a record already existing for the selected date range");
            }
            if (endDate < stDate)
            {
                return Json("Invalid date range");
            }
            return Json(true);
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "Del_StDate")] DateTime? stDate, [Bind(Prefix = "Del_EndDate")] DateTime? endDate)
        {
            if (endDate >= stDate)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}