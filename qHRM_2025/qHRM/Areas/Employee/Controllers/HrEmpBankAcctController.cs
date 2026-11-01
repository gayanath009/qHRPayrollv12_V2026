using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qHRM.Models;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.Employee.Data;
using qHRM.Data;


namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpBankAcctController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpBankAcct> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;



        public HrEmpBankAcctController(EmployeeContext employeeContext,
                                       SysSelectListContext sysSelectListContext,
                                       DbCommitContext<HrEmpBankAcct> dbCommitContext,
                                       AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._sysSelectListContext = sysSelectListContext;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index(int id)
            {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_BNK_DTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_BNK_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                var list = _employeeContext.GetAccountsByEmpSysId(id).ToList();
                ViewBag.DataSource = list;            
                return View();
            }
            catch (Exception)
            {
                throw;
            }

        }

        // GET: HrEmpGrade/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpBankAcct bankAcct)
        {
            try
            {   //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_BNK_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                //End | Usr Prv//             
                ViewBag.lstAcctTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BNK_ACCT_TYP);
                return PartialView("_HrEmpBankAcct_Create", bankAcct);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpBankAcct bankAcct)
        {
            try
            {
                if (ModelState.IsValid)
                {                                  
                    try
                    {
                        bankAcct.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                        int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, bankAcct, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithSuccess("QCS", "Record Added Successfully");
                        else                            
                        return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                    }
                    catch (Exception)                    {
                        
                        return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                    }
                }
                else
                {
                     return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId });
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
            var bankAcct = _employeeContext.GetAccountById(id).FirstOrDefault();
            bankAcct.ex_AcctNo = bankAcct.AcctNo;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_BNK_DTL").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//
            ViewBag.lstAcctTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BNK_ACCT_TYP);
            return PartialView("_HrEmpBankAcct_Edit", bankAcct);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpBankAcct bankAcct)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        bankAcct.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                        var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, bankAcct, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithSuccess("QCS", "Record Updated Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                    catch (Exception)
                    {
                        
                        return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId });
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
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_BNK_DTL").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                var bankAcct = _employeeContext.GetAccountById(id).FirstOrDefault();                
                return PartialView("_HrEmpBankAcct_Delete", bankAcct);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpBankAcct bankAcct)
        {
            try
            {
                bankAcct.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, bankAcct, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
                
            }
            catch 
            {
                return RedirectToAction("Index", "HrEmpBankAcct", new { @empSysId = bankAcct.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }


        public IActionResult IsExistAcctNo([Bind(Prefix = "AcctNo")] string value, [Bind(Prefix = "ex_AcctNo")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _employeeContext.GetAccountByAcctNo(value).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _employeeContext.GetAccountByAcctNo(value).FirstOrDefault();
                if (_itemfound != null)                
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
               
        
        public IActionResult ValidateBankAccountOpenDate([Bind(Prefix = "OpndDate")] DateTime? actOpnDate)
        {            
            if (actOpnDate >= Convert.ToDateTime("01/01/1970") && actOpnDate <= DateTime.Now )
            {
                return Json(true);
            }
            
            return Json(false);
        }


        public IActionResult IsSetSalaryAcct([Bind(Prefix = "ex_AcctNo")] string exAcct)
        {
            int empSysId = 0;            
            empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));

            if (exAcct == null)
            {
                var defaultAcct = _employeeContext.GetAccountsByEmpSysId(empSysId).Where(x => x.SlryAcct == true).ToList();
                if (defaultAcct.Count == 0)
                {
                    return Json(true);
                }
            }
            else
            {
                var defaultAcct = _employeeContext.GetAccountsByEmpSysId(empSysId).Where(x => x.SlryAcct == true && x.ex_AcctNo != exAcct).ToList();
                if (defaultAcct.Count == 0)
                {
                    return Json(true);
                }
            }           

            return Json(false);
        }
    }
}