using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrStfLoanController : Controller
    {
        private readonly PrStfLoanContext _prStfLoanContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrPrStfLoan> _dbCommitContext;
        private readonly PayrollContext _payrollContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrPrStfLoanController(PrStfLoanContext prStfLoanContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrPrStfLoan> dbCommitContext,
                                        PayrollContext payrollContext,
                                        AdmUserGrpPrvContext prvContext)
        {
            _prStfLoanContext = prStfLoanContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _dbCommitContext = dbCommitContext;
            _payrollContext = payrollContext;
            _prvContext = prvContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("STF_LN") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.DataSource = _prStfLoanContext.GetHrPrStfLoanByEntId(false, (int)HttpContext.Session.GetInt32("userEmpSysId"),(int)HttpContext.Session.GetInt32("userEntId"));

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN").FirstOrDefault();
            ViewBag.RecAdd = objprv.RecAdd;
            ViewBag.RecEdit = objprv.RecEdit;
            ViewBag.RecDelete = objprv.RecDelete;
            //End | Usr Prv//
            return View();
        }

        // GET: HrPrStfLoan/CreateNew
        [HttpGet]
        public IActionResult CreateNew(HrPrStfLoan hrPrStfLoan)
        {
            try
            {
                hrPrStfLoan.IssuDate = DateTime.Today;
                hrPrStfLoan.EftMonth = DateTime.Today.Month;
                hrPrStfLoan.EftYear = DateTime.Today.Year;

                ViewBag.lstEmp = _employeeContext.GetEmployeeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"), DateTime.Today);
                ViewBag.lstLnTyp = _prStfLoanContext.GetHrPrStfLoanTypeViewByEntId((int)HttpContext.Session.GetInt32("userEntId"));
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
                ViewBag.DsInstlmnts = new List<Instalment>();

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;               
                //End | Usr Prv//

                return PartialView("_HrPrStfLoan_Create", hrPrStfLoan);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loans", ex.Message);
            }
        }

        // POST: HrPrStfLoan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrStfLoan hrPrStfLoan)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrStfLoan, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Staff Loans", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Staff Loans", "Adding Record got failed");
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
        // GET: HrPrStfLoan/EditGet
        public ActionResult EditGet(int id)
        {
            var hrPrStfLoan = _prStfLoanContext.GetHrPrStfLoanByStfLnId(id);

            var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrStfLoan.EmpSysId);
            ViewBag.EmpName = empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName;
            var lnTyp = _prStfLoanContext.GetHrPrStfLoanTypeById(hrPrStfLoan.LnTypId);
            ViewBag.LnTyp = lnTyp.LnTypCode + " - " + lnTyp.Desc;
            ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
            hrPrStfLoan.ex_LnCode = hrPrStfLoan.LnCode;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return PartialView("_HrPrStfLoan_Edit", hrPrStfLoan);
        }

        // POST: HrPrStfLoan/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrStfLoan hrPrStfLoan)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoan.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrStfLoan, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index)).WithSuccess("Staff Loans", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Staff Loans", "Modifying Record got failed");
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

        // GET: HrPrStfLoan/Cancel
        public IActionResult CancelGet(int id)
        {
            try
            {
                var hrPrStfLoan = _prStfLoanContext.GetHrPrStfLoanByStfLnId(id);
                var empGenInfo = _employeeContext.GetEmpGenInfoByEmpSysId(hrPrStfLoan.EmpSysId);
                ViewBag.EmpName = empGenInfo.EmpId.ToString() + " - " + empGenInfo.FullName;
                var lnTyp = _prStfLoanContext.GetHrPrStfLoanTypeById(hrPrStfLoan.LnTypId);
                ViewBag.LnTyp = lnTyp.LnTypCode + " - " + lnTyp.Desc;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN").FirstOrDefault();                
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return PartialView("_HrPrStfLoan_Cancel", hrPrStfLoan);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loans", ex.Message);
            }
        }

        // POST: HrPrStfLoan/Cancel
        [HttpPost]
        public IActionResult Cancel(HrPrStfLoan hrPrStfLoan)
        {
            try
            {
                _dbCommitContext.commitChanges(LogEventTypes.DEL, hrPrStfLoan, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index)).WithSuccess("Staff Loans", "Record Cancelled Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loans", ex.Message);
            }
        }

        public IActionResult IsAlreadyExisting([Bind(Prefix = "LnCode")] string value, [Bind(Prefix = "ex_LnCode")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var recordExist = _prStfLoanContext.GetHrPrStfLoanByCode(value);
                    if (recordExist != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var recordExist = _prStfLoanContext.GetHrPrStfLoanByCode(value);
                if (recordExist != null)
                    return Json(data: false);
                else
                    return Json(data: true);
            }
            return Json(data: true);
        }

        [HttpGet]
        public JsonResult GetStfLoanCount(int lnTypId)
        {
            try
            {
                if (lnTypId != 0)
                {
                    var cnt = _prStfLoanContext.GetHrPrStfLoanCount(lnTypId);
                    return Json(new { ok = true, data = cnt.ToString(), message = "ok" });
                }
                else
                {
                    return Json(new { ok = false, data = 0, message = "" });
                }

            }
            catch (Exception ex)
            {
                return Json(new { ok = true, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult CalcLnInst(int yr, int mnth, decimal lnAmt, int instlmnts)
        {
            try
            {
                var instalments = new List<Instalment>();
                var tmpBlncAmt = lnAmt;
                var tmpStDate = Convert.ToDateTime(yr.ToString() + "-" + mnth.ToString() + "-01");
                var dInstlmntAmt = lnAmt / instlmnts;
                int iInstlmntAmt = 0;
                if (decimal.Round(dInstlmntAmt, 0) < dInstlmntAmt)
                    iInstlmntAmt = (int)decimal.Round(dInstlmntAmt, 0) + 1;
                else
                    iInstlmntAmt = (int)decimal.Round(dInstlmntAmt, 0);

                for(int i = 0; i < instlmnts; i++)
                {
                    var tmpInsDate = tmpStDate.AddMonths(i);
                    instalments.Add(new Instalment { Year = tmpInsDate.Year, Month = tmpInsDate.ToString("MMMM"), Instlmnt = (tmpBlncAmt >= iInstlmntAmt)? iInstlmntAmt: tmpBlncAmt });
                    tmpBlncAmt -= iInstlmntAmt;
                }

                return Json(new { ok = true, data = instalments, message = "ok" });
            }
            catch (Exception ex) { throw ex; }
        }

        partial class Instalment
        {
            public int Year { get; set; }
            public String Month { get; set; }
            public decimal Instlmnt { get; set; }
        }

        public IActionResult ChkwithSlry([Bind(Prefix = "InstlAmt")] int instlAmt, [Bind(Prefix = "EmpSysId")] int empSysId)
        {
            try
            {
                var empSlry = _payrollContext.GetHrPrEmpSlryStrucByEmpSysId(empSysId);
                decimal slry = (empSlry.Basic ?? 0) + (empSlry.HrAlwnc ?? 0) + (empSlry.TrnsAlwnc ?? 0) + (empSlry.Tra ?? 0) + (empSlry.PstnAlwnc ?? 0) + (empSlry.MedAlwnc ?? 0) + (empSlry.MobAlwnc ?? 0)
                                + (empSlry.ProjAlwnc ?? 0) + (empSlry.SpcAlwnc ?? 0) + (empSlry.PrfmAlwnc ?? 0) + (empSlry.OthrAlwnc ?? 0);

                if (instlAmt > slry)
                {
                    return Json(false);
                }

                return Json(true); ;

            }
            catch { throw; }
        }
    }
}