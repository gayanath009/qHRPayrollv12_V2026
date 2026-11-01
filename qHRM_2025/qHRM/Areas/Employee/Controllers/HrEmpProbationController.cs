using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Data;
using qHRM.Areas.SysAdmin.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpProbationController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly EntityContext _entityContext;
        private readonly EnumContext _enumContext;
        private readonly DbCommitContext<HrEmpProbation> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpProbationController(EmployeeContext employeeContext,
                                        EntityContext entityContext, 
                                        EnumContext enumContext,
                                        DbCommitContext<HrEmpProbation> dbCommitContext,
                                        AdmUserGrpPrvContext prvContext)
        {
            _employeeContext = employeeContext;
            _entityContext = entityContext;
            _enumContext = enumContext;
            _dbCommitContext = dbCommitContext;
            _prvContext = prvContext;
        }

        // GET: HrEmpProbations
        public ActionResult Index(int empSysId)
        {
            //No Access Granted
            if (HttpContext.Session.GetString("EM_PRB_PRD") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            HrEmpGenInfo genInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSysId);
            if (genInfo != null)
            {
                ViewBag.EmpSysId = empSysId;
                ViewBag.EmpId = genInfo.EmpId;
                ViewBag.Rec_Add = true;
                ViewBag.Rec_Close = true;
                ViewBag.EmpName = genInfo.FirstName + " " + genInfo.LastName;
                var recList = _employeeContext.GetAllProbation(empSysId);
                ViewBag.DataSource = recList;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRB_PRD").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return View();
            }
            else
                return null;
        }

        // GET: HrEmpProbation/Create
        [HttpGet]
        public IActionResult CreateNew(int empSysId)
        {
            try
            {
                var genInfo = _employeeContext.GetEmpGenInfoByEmpSysId(empSysId);
                HrEmpProbation empProb = new HrEmpProbation();
                var activeEmpProb = _employeeContext.GetActvProbation(empSysId);

                if (activeEmpProb != null)
                {
                    empProb = activeEmpProb;
                    empProb.StDate = empProb.EndDate;
                }
                else
                    empProb.StDate = DateTime.Today;

                empProb.EmpSysId = empSysId;
                empProb.ProbId = 0;
                empProb.EndDate = empProb.StDate.AddMonths(3);

                ViewBag.EmpSysId = empSysId;
                ViewBag.EmpId = genInfo.EmpId;
                ViewBag.EmpName = genInfo.FirstName + " " + genInfo.LastName;
                ViewBag.lstDept = _entityContext.GetDepatmentEntViews((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.lstDesig = _entityContext.GetDesignationViews();
                ViewBag.lstEmpTyp = _enumContext.lstEmpTyp;
                ViewBag.lstRptMng = _employeeContext.GetRepotingManagers(empSysId);

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRB_PRD").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;              
                //End | Usr Prv//

                return PartialView("_HrEmpProbation_Create", empProb);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = empSysId }).WithWarning("Probation", ex.Message);
            }
        }

        // POST: HrEmpProbation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpProbation empProb)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var extActvProb = _employeeContext.GetActvProbation(empProb.EmpSysId);
                        if (extActvProb != null)
                        {
                            return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = empProb.EmpSysId }).WithWarning("Probation", "An Active Probation Period is already exist for the given period.");
                        }
                        else
                        {
                            if (_employeeContext.InsertProbation(empProb, (int)HttpContext.Session.GetInt32("userEmpSysId")) == 1) // _dbCommitContext.commitChanges(LogEventTypes.INS, empProb, (int)HttpContext.Session.GetInt32("userEmpSysId")
                                return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = empProb.EmpSysId }).WithSuccess("Probation", "Record Added Successfully");
                            else
                                return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = empProb.EmpSysId }).WithWarning("Probation", "Record Adding Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = empProb.EmpSysId }).WithWarning("Probation", ex.Message);
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

        // GET: HrEmpProbation/Edit
        public ActionResult EditGet(int probId)
        {

            var prob = _employeeContext.GetProbationById(probId);
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRB_PRD").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return PartialView("_HrEmpProbation_Edit", prob);
        }

        // POST: HrEmpProbation/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpProbation prob)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        prob.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                        prob.WkStat = 0;
                        prob.EndDate = DateTime.Today;
                        prob.Note = "Early Closed - " + prob.Note;

                        var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, prob, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = prob.EmpSysId }).WithSuccess("Probation", "Record Updated Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = prob.EmpSysId }).WithWarning("Probation", "Updating Record got failed");
                    }
                    catch (Exception)
                    {

                        return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = prob.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpProbation", new { @empSysId = prob.EmpSysId });
                }

            }
            catch
            {
                return View();
            }
        }

        public IActionResult ValidateDateRange([Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            if (endDate > stDate)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}