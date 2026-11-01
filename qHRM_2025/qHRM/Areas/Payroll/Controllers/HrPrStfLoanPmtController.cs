using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class HrPrStfLoanPmtController : Controller
    {
        private readonly PrStfLoanContext _prStfLoanContext;
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly PayrollContext _payrollContext;
        DbCommitContext<HrPrStfLoanPmtExp> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrPrStfLoanPmtController(PrStfLoanContext prStfLoanContext,
                                        EmployeeContext employeeContext,
                                        SysSelectListContext sysSelectListContext,
                                        PayrollContext payrollContext,
                                        DbCommitContext<HrPrStfLoanPmtExp> dbCommitContext,
                                        AdmUserGrpPrvContext prvContext)
        {
            _prStfLoanContext = prStfLoanContext;
            _employeeContext = employeeContext;
            _sysSelectListContext = sysSelectListContext;
            _payrollContext = payrollContext;
            _dbCommitContext = dbCommitContext;
            _prvContext = prvContext;
        }
            
        public IActionResult Index(int stfLnId)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("STF_LN_PMT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var stfLn = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(stfLnId);
                ViewBag.StfLnId = stfLnId;
                ViewBag.EmpDtl = stfLn.EmpDtl;
                ViewBag.LnTypDesc = stfLn.LnTypDesc;
                ViewBag.LnCode = stfLn.LnCode;
                ViewBag.FlAmt = stfLn.FlAmt;
                ViewBag.Instl = stfLn.Instl;
                ViewBag.RmAmt = stfLn.RmAmt;
                ViewBag.DsPmt = _prStfLoanContext.GetHrPrStfLoanPmtVwByStfLnId(stfLnId);
                ViewBag.DsExp = _prStfLoanContext.GetHrPrStfLoanPmtExpVwByStfLnId(stfLnId);

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        #region Payments
        // GET: HrPrStfLoanPmt/CreateNew
        [HttpGet]
        public IActionResult CreateNew(int stfLnId)
        {
            try
            {
                HrPrStfLoanPmt hrPrStfLoanPmt = new HrPrStfLoanPmt();
                var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(stfLnId);
                hrPrStfLoanPmt.StfLnId = stfLnId;
                hrPrStfLoanPmt.PmtDate = DateTime.Today;
                ViewBag.EmpDtl = stfLnView.EmpDtl;
                ViewBag.LnCode = stfLnView.LnCode;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;               
                //End | Usr Prv//

                return PartialView("_HrPrStfLoanPmt_Create", hrPrStfLoanPmt);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Payments", ex.Message);
            }
        }

        // POST: HrPrStfLoanPmt/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrPrStfLoanPmt hrPrStfLoanPmt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanPmt.PmtMod = 1;
                        hrPrStfLoanPmt.RecStat = 1;
                        _prStfLoanContext.SaveHrPrStfLoanPmt(LogEventTypes.INS, hrPrStfLoanPmt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithSuccess("Staff Loan Payments", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithWarning("Staff Loan Payments", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrStfLoanPmt/TagGet
        public ActionResult TagGet(int id)
        {
            var hrPrStfLnPmtVw = _prStfLoanContext.GetHrPrStfLoanPmtViewByLnPmtId(id);
            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLnPmtVw.StfLnId);
            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;
            ViewBag.PmtDate = hrPrStfLnPmtVw.PmtDate.ToString("yyyy-MM-dd");
            ViewBag.PmtAmt = hrPrStfLnPmtVw.PmtAmt.ToString("0.00");
            ViewBag.PmtModTxt = hrPrStfLnPmtVw.PmtModTxt;
            ViewBag.Note = hrPrStfLnPmtVw.Note;
            ViewBag.CrtName = hrPrStfLnPmtVw.CrtName;
            ViewBag.CrtDateTime = hrPrStfLnPmtVw.CrtDateTime;
            ViewBag.RecStatTxt = hrPrStfLnPmtVw.RecStatTxt;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return PartialView("_HrPrStfLoanPmt_Tag");
        }

        // GET: HrPrStfLoanPmt/EditGet
        public ActionResult EditGet(int id)
        {
            var hrPrStfLoanPmt = _prStfLoanContext.GetHrPrStfLoanPmtByLnPmtId(id);

            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLoanPmt.StfLnId);

            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;
            return PartialView("_HrPrStfLoanPmt_Edit", hrPrStfLoanPmt);
        }

        // POST: HrPrStfLoanPmt/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrPrStfLoanPmt hrPrStfLoanPmt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanPmt.RecStat = 1;
                        _prStfLoanContext.SaveHrPrStfLoanPmt(LogEventTypes.UPD, hrPrStfLoanPmt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithSuccess("Staff Loan Payments", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithWarning("Staff Loan Payments", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrStfLoanPmt/Cancel
        public IActionResult CancelGet(int id)
        {
            var hrPrStfLoanPmt = _prStfLoanContext.GetHrPrStfLoanPmtByLnPmtId(id);
            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLoanPmt.StfLnId);
            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();            
            ViewBag.RecDelete = objprv.RecDelete;
            //End | Usr Prv//

            return PartialView("_HrPrStfLoanPmt_Cancel", hrPrStfLoanPmt);
        }

        // POST: HrPrStfLoanPmt/Cancel
        [HttpPost]
        public IActionResult Cancel(HrPrStfLoanPmt hrPrStfLoanPmt)
        {
            try
            {
                hrPrStfLoanPmt = _prStfLoanContext.GetHrPrStfLoanPmtByLnPmtId(hrPrStfLoanPmt.LnPmtId);
                hrPrStfLoanPmt.RecStat = 0;
                _prStfLoanContext.SaveHrPrStfLoanPmt(LogEventTypes.UPD, hrPrStfLoanPmt, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithSuccess("Staff Loan Payments", "Record Cancelled Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmt.StfLnId }).WithWarning("Staff Loan Payments", ex.Message);
            }
        }
        #endregion Payments

        #region Excepmtions
        // GET: HrPrStfLoanPmt/CreateNewExp
        [HttpGet]
        public IActionResult CreateNewExp(int stfLnId)
        {
            try
            {
                HrPrStfLoanPmtExp hrPrStfLoanPmtExp = new HrPrStfLoanPmtExp();
                var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(stfLnId);
                var curEftYM = _payrollContext.GetCurEftYM();
                hrPrStfLoanPmtExp.StfLnId = stfLnId;
                hrPrStfLoanPmtExp.EntryDate = DateTime.Today;
                hrPrStfLoanPmtExp.EftYear = curEftYM.EftYear;
                hrPrStfLoanPmtExp.EftMonth = curEftYM.EftMonth;
                hrPrStfLoanPmtExp.ExpInstl = 1;
                ViewBag.lstYears = Enumerable.Range(curEftYM.EftYear, 2).ToList();
                ViewBag.lstMonths = _sysSelectListContext.GetMonths(SysSelectListContext.SelectListTypeEnum.MONTHS, curEftYM.EftMonth, 8).OrderBy(o => o.CustIndx);
                ViewBag.EmpDtl = stfLnView.EmpDtl;
                ViewBag.LnCode = stfLnView.LnCode;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;              
                //End | Usr Prv//

                return PartialView("_HrPrStfLoanPmtExp_Create", hrPrStfLoanPmtExp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Staff Loan Payments", ex.Message);
            }
        }

        // POST: HrPrStfLoanPmt/CreateExp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateExp(HrPrStfLoanPmtExp hrPrStfLoanPmtExp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanPmtExp.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrPrStfLoanPmtExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithSuccess("Staff Loan Payments", "Record Added Successfully");
                    }
                    catch
                    {
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithWarning("Staff Loan Payments", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrStfLoanPmtExp/TagGetExp
        public ActionResult TagGetExp(int id)
        {
            var hrPrStfLnPmtExpVw = _prStfLoanContext.GetHrPrStfLoanPmtExpVwByLnPmtExpId(id);
            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLnPmtExpVw.StfLnId);
            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;
            ViewBag.EntryDate = hrPrStfLnPmtExpVw.EntryDate.ToString("yyyy-MM-dd");
            ViewBag.ExpPmtAmt = hrPrStfLnPmtExpVw.ExpPmtAmt.ToString("0.00");
            ViewBag.EftYear = hrPrStfLnPmtExpVw.EftYear;
            ViewBag.EftMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(hrPrStfLnPmtExpVw.EftMonth);
            ViewBag.ExpInstl = hrPrStfLnPmtExpVw.ExpInstl;
            ViewBag.Note = hrPrStfLnPmtExpVw.Note;
            ViewBag.CrtName = hrPrStfLnPmtExpVw.CrtName;
            ViewBag.CrtDateTime = hrPrStfLnPmtExpVw.CrtDateTime;
            ViewBag.RecStatTxt = hrPrStfLnPmtExpVw.RecStatTxt;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();
            ViewBag.RecAdd = objprv.RecAdd;
            ViewBag.RecEdit = objprv.RecEdit;
            ViewBag.RecDelete = objprv.RecDelete;
            //End | Usr Prv//

            return PartialView("_HrPrStfLoanPmtExp_Tag");
        }

        // GET: HrPrStfLoanPmtExp/EditGetExp
        public ActionResult EditGetExp(int id)
        {
            var hrPrStfLoanPmtExp = _prStfLoanContext.GetHrPrStfLoanPmtExpByLnPmtExpId(id);
            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLoanPmtExp.StfLnId);
            var curEftYM = _payrollContext.GetCurEftYM();
            if (curEftYM.EftMonth == 12)
            {
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
            }
            else
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS).Where(x => x.Indx >= curEftYM.EftMonth);

            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//
            return PartialView("_HrPrStfLoanPmtExp_Edit", hrPrStfLoanPmtExp);
        }

        // POST: HrPrStfLoanPmtExp/EditExp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditExp(HrPrStfLoanPmtExp hrPrStfLoanPmtExp)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrPrStfLoanPmtExp.RecStat = 1;
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrStfLoanPmtExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithSuccess("Staff Loan Payments", "Record Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithWarning("Staff Loan Payments", "Modifying Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId });
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrStfLoanPmtExp/CancelGetExp
        public IActionResult CancelGetExp(int id)
        {
            var hrPrStfLoanPmtExp = _prStfLoanContext.GetHrPrStfLoanPmtExpByLnPmtExpId(id);
            var stfLnView = _prStfLoanContext.GetHrPrStfLoanViewByStfLnId(hrPrStfLoanPmtExp.StfLnId);
            var curEftYM = _payrollContext.GetCurEftYM();
            if (curEftYM.EftMonth == 12)
            {
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
            }
            else
                ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS).Where(x => x.Indx >= curEftYM.EftMonth);
            ViewBag.EmpDtl = stfLnView.EmpDtl;
            ViewBag.LnCode = stfLnView.LnCode;

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "STF_LN_PMT").FirstOrDefault();
            ViewBag.RecDelete = objprv.RecDelete;
            //End | Usr Prv//

            return PartialView("_HrPrStfLoanPmtExp_Cancel", hrPrStfLoanPmtExp);
        }

        // POST: HrPrStfLoanPmtExp/CancelExp
        [HttpPost]
        public IActionResult CancelExp(HrPrStfLoanPmtExp hrPrStfLoanPmtExp)
        {
            try
            {
                hrPrStfLoanPmtExp = _prStfLoanContext.GetHrPrStfLoanPmtExpByLnPmtExpId(hrPrStfLoanPmtExp.LnPmtExpId);
                hrPrStfLoanPmtExp.RecStat = 0;
                _dbCommitContext.commitChanges(LogEventTypes.UPD, hrPrStfLoanPmtExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithSuccess("Staff Loan Payments", "Record Cancelled Successfully");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index), "HrPrStfLoanPmt", new { stfLnId = hrPrStfLoanPmtExp.StfLnId }).WithWarning("Staff Loan Payments", ex.Message);
            }
        }

        public IActionResult ValidateExpInstl([Bind(Prefix = "ExpInstl")] decimal expInstl)
        {
            if (expInstl > 0)
            {
                return Json(true);
            }
            return Json(false);
        }

        public IActionResult ValidateEftYM([Bind(Prefix = "EftYear")] int eftYear, [Bind(Prefix = "EftMonth")] int eftMonth)
        {
            try
            {
                var curEftYM = _payrollContext.GetCurEftYM();
                if(curEftYM.EftYear > eftYear)
                {
                    return Json(false);
                }
                else if(curEftYM.EftYear == eftYear && curEftYM.EftYear > eftMonth)
                {
                    return Json(false);
                }
                else
                {
                    return Json(true);
                }

            }
            catch { throw; }
        }
        #endregion Excepmtions
    }
}
