using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Area.Config;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using Syncfusion.EJ2.SplitButtons;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrSlryPrcDtlController : Controller
    {
        private readonly EmployeeContext _empContext;
        private readonly EntityContext _entityContext;
        private readonly PayrollContext _payrollContext;
        private readonly AttnContext _attdContext;
        private readonly DbCommitContext<HrPrSlryPrcHdr> _dbCommitContext;
        private readonly IHostingEnvironment _host;
        private readonly PayrollWorkflowService _workflowService;
        

        public HrPrSlryPrcDtlController(EmployeeContext empContext,
                                    EntityContext entityContext,
                                    PayrollContext payrollContext,
                                    AttnContext attdContext,
                                    DbCommitContext<HrPrSlryPrcHdr> dbCommitContext,
                                    IHostingEnvironment host,
                                    PayrollWorkflowService workflowService

            )
        {
            _empContext = empContext;
            _entityContext = entityContext;
            _payrollContext = payrollContext;
            _attdContext = attdContext;
            _dbCommitContext = dbCommitContext;
            _host = host;
            _workflowService = workflowService;
        }

        public partial class Month
        {
            public int value { get; set; }
            public string text { get; set; }
        }

        public partial class Year
        {
            public int value { get; set; }
            public string text { get; set; }
        }

        public IActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("SLRY_PRC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            List<Month> lstMonth = new List<Month>();
            List<Year> lstYear = new List<Year>();
            //int _efMntVal = 0;
            //string _efMntTxt = "";
            //int _efYr = 0;

            //-----------------Salary Process Header -----------------//
            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            if (entId <= 0) entId = 1;
            HrPrSlryPrcHdr slryPrcHdr = _payrollContext.GetLstSlryPrcHdr(entId);
            if (slryPrcHdr == null)
            {
                var prConfig = _payrollContext.GetActiveHrPrConfig();
                lstYear.Add(new Year { value = prConfig.SlryPrcStYear, text = prConfig.SlryPrcStYear.ToString() });
                lstMonth.Add(new Month { value = prConfig.SlryPrcStMonth, text = new DateTime(prConfig.SlryPrcStYear, prConfig.SlryPrcStMonth, 1).ToString("MMMM") });
            }
            else if (slryPrcHdr.PrcStat == 1 || slryPrcHdr.PrcStat == 2) //if the Process under 'Process' OR 'Verify'//
            {
                lstYear.Add(new Year { value = slryPrcHdr.EftYear, text = slryPrcHdr.EftYear.ToString() });
                lstMonth.Add(new Month { value = slryPrcHdr.EftMonth, text = new DateTime(slryPrcHdr.EftYear, slryPrcHdr.EftMonth, 1).ToString("MMMM") });

            }
            else if (slryPrcHdr.PrcStat == 3) //if the Process is Completed//
            {
                var nextMonthDate = new DateTime(slryPrcHdr.EftYear, slryPrcHdr.EftMonth, 1).AddMonths(1);
                lstYear.Add(new Year { value = nextMonthDate.Year, text = nextMonthDate.Year.ToString() });
                lstMonth.Add(new Month { value = nextMonthDate.Month, text = nextMonthDate.ToString("MMMM") });
            }
            else
            {
                lstYear.Add(new Year { value = DateTime.Today.Year, text = DateTime.Today.Year.ToString() });
                lstMonth.Add(new Month { value = DateTime.Today.Month, text = DateTime.Today.ToString("MMMM") });
            }
            ViewBag.lstMonth = lstMonth;
            ViewBag.lstYear = lstYear;

            
            //int entId = (int)HttpContext.Session.GetInt32("userEntId");
            //ViewBag.DataSource = _payrollContext.GetSalaryDtl(entId, "_passive", slryPrcHdr.PrcId, slryPrcHdr.EftMonth, slryPrcHdr.EftYear);

            //-----------------Salary Process Header -----------------//           
            ViewBag.lstEmployee = _empContext.GetActvEmpNameLst(entId);
            ViewBag.lstDepartment = _entityContext.GetDepatmentEntViews(entId);

            //-------------------Progress Button -----------------------------//
            ProgressButtonSpinSettings spinRight = new ProgressButtonSpinSettings() { Position = SpinPosition.Right };
            double isDuration = (1000 * 60 * 59) + (59 * 1000); //maximum duraion 
            ViewBag.spinRight = spinRight;
            ViewBag.isDuration = isDuration;
            return View();
            //-------------------Progress Button -----------------------------//
        }

        //public IActionResult GetSlryGrpByDpt()
        //{
        //    //No Access Granted
        //    //if (HttpContext.Session.GetString("OT_RT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        //    List<Month> lstMonth = new List<Month>();
        //    List<Year> lstYear = new List<Year>();
        //    int _efMntVal = 0;
        //    string _efMntTxt = "";
        //    int _efYr = 0;

        //    //-----------------Salary Process Header -----------------//
        //    HrPrSlryPrcHdr _salPrcHdr = _payrollContext.GetSalaryHeaders((int)HttpContext.Session.GetInt32("userEntId")).FirstOrDefault();
        //    if (_salPrcHdr == null)
        //    {

        //        for (int i = DateTime.Today.Month; i > 0; i--)
        //        {
        //            Month _objMn = new Month { value = i, text = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM") };
        //            lstMonth.Add(_objMn);
        //        }

        //        Year _objYr1 = new Year { value = DateTime.Today.Year, text = DateTime.Today.Year.ToString() };
        //        lstYear.Add(_objYr1);

        //    }
        //    else
        //    {
        //        if (_salPrcHdr.PrcStat == 1 || _salPrcHdr.PrcStat == 2) //if the Process under 'Process' OR 'Verify'//
        //        {
        //            if (_salPrcHdr.EftMonth == 12)
        //            { _efMntVal = 1; _efMntTxt = "Jan"; _efYr = _salPrcHdr.EftYear + 1; }
        //            else
        //            {                     
        //                for (int i = DateTime.Today.Month; i > 0; i--)
        //                {
        //                    Month _objMn = new Month { value = i, text = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM") };
        //                    lstMonth.Add(_objMn);
        //                }
        //            }

        //            Month _objMn1 = new Month { value = _efMntVal, text = _efMntTxt };
        //            Year _objYr1 = new Year { value = _salPrcHdr.EftYear, text = _salPrcHdr.EftYear.ToString() };
        //            lstMonth.Add(_objMn1);
        //            lstYear.Add(_objYr1);

        //        }
        //        else if (_salPrcHdr.PrcStat == 3) //if the Process is Completed//
        //        {
        //            if (_salPrcHdr.EftMonth == 12)
        //            { _efMntVal = 1; _efMntTxt = "Jan"; _efYr = _salPrcHdr.EftYear + 1; }
        //            else
        //            {
        //                for (int i = DateTime.Today.Month; i > 0; i--)
        //                {
        //                    Month _objMn = new Month { value = i, text = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM") };
        //                    lstMonth.Add(_objMn);
        //                }
        //            }
        //            Month _objMn1 = new Month { value = _efMntVal, text = _efMntTxt };
        //            Year _objYr1 = new Year { value = _salPrcHdr.EftYear, text = _salPrcHdr.EftYear.ToString() };
        //            lstMonth.Add(_objMn1);
        //            lstYear.Add(_objYr1);

        //        }
        //        else
        //        {
        //            if (_salPrcHdr.EftMonth == 12)
        //            { _efMntVal = 1; _efMntTxt = "Jan"; _efYr = _salPrcHdr.EftYear + 1; }
        //            else
        //            { _efMntVal = _salPrcHdr.EftMonth + 1; _efMntTxt = DateTime.Now.AddMonths(1).ToString("MMMM"); _efYr = _salPrcHdr.EftYear; }
        //        }
        //    }

        //    ViewBag.lstMonth = lstMonth;
        //    ViewBag.lstYear = lstYear;
        //    return View();
        //}

        [HttpGet]
        public JsonResult IsPrcStarted(int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var _salPrcDtl = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt);
                if (_salPrcDtl != null)
                    return Json(new { ok = true, slrydata = _salPrcDtl, message = "ok" });
                else
                    return Json(new { ok = false, slrydata = 0, message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, slrydata = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult GetLstSlryPrcHdr()
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var slryPrcHdr = _payrollContext.GetLstSlryPrcHdr(entId);                
                if (slryPrcHdr != null)
                    return Json(new { ok = true, data = slryPrcHdr.PrcId, message = "ok" });
                else
                    return Json(new { ok = false, data = 0, message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }


        [HttpGet]
        public JsonResult GetSalaryDtl(int prcId, int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var lstSalDtl = _payrollContext.GetSalaryDtl(entId, "_passive", prcId, eftMnt, eftYr);
                if (lstSalDtl.Count() > 0)
                {
                    ViewBag.PrcId = lstSalDtl.First().PrcId;
                    return Json(new { ok = true, data = lstSalDtl, message = "ok" });
                }
                else
                    return Json(new { ok = false, data = 0, message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult StrtPrc(int eftYr, int eftMnt, string note, string staffTyp = "ALL")
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                int crtId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                if (crtId <= 0) crtId = 1;

                //---------- Execute Load Attendance Process ----------//
                var prcSrcDtl = "Y:" + eftYr + " M:" + eftMnt + " Rvsn:1";
                _attdContext.LoadMnthHrEmpAtndc("A", 0, eftYr, eftMnt, AttnContext.AtndcLdPrcTyp.SLRY_PRC, prcSrcDtl, false, crtId);
                //----------------------------------------------------//

                var _list = _payrollContext.ExecuteSalaryProcess(0, 0, "A", entId, eftYr, eftMnt, note, crtId, staffTyp);

                if (_list.Count() > 0)
                    return Json(new { ok = true, data = _list, message = "ok" });
                else
                    return Json(new { ok = false, data = "empty", message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public IActionResult StrtReProc(int prcId = 0, int refId = 0, string prcTyp = "A", int eftYr = 0, int eftMnt = 0, string note = "Re-Process", string staffTyp = "ALL")
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                int crtId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                if (crtId <= 0) crtId = 1;

               

                var hrPrSlryPrcHdr = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt);
                int xPrcId = 0;
                if (hrPrSlryPrcHdr != null)
                    xPrcId = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt).PrcId;

                //---------- Execute Load Attendance Process ----------//
                var prcSrcDtl = "Y:" + eftYr + " M:" + eftMnt + " PrcId:" + xPrcId + " RefId:" + refId + "(Re-process)";
                _attdContext.LoadMnthHrEmpAtndc(prcTyp, refId, eftYr, eftMnt, AttnContext.AtndcLdPrcTyp.SLRY_PRC, prcSrcDtl, false, crtId);
                //----------------------------------------------------//

                var _list = _payrollContext.ExecuteSalaryProcess(xPrcId, refId, prcTyp, entId, eftYr, eftMnt, note, crtId, staffTyp);

                if (_list.Count() > 0)
                    return Json(new { ok = true, data = _list, message = "ok" });
                else
                    return Json(new { ok = false, data = "empty", message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult IsPrcVerified(int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var _salPrcDtl = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt);
                if (_salPrcDtl != null)
                    return Json(new { ok = true, data = _salPrcDtl.PrcStat, message = "ok" });
                else
                    return Json(new { ok = false, data = 0, message = "failed" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public IActionResult VrfyPrc(int eftYr, int eftMnt, int mthod)
        {
            try
            {

                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                int crtId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                if (crtId <= 0) crtId = 1;
                var obj = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt);
                if (mthod == 3 && obj.WfStat != "Approved" && (HttpContext.Session.GetInt32("userGrpId") ?? 0) != 1)
                {
                    return Json(new { ok = false, message = "Salary process must be approved through the workflow before it can be completed." });
                }
                obj.PrcStat = Convert.ToInt16(mthod); //Update the Process Status | value '2' - Verified ,  value '3' - Completed;
                if (mthod == 2) { obj.WfStat = "Verified"; }
                obj.VrfdBy = crtId;
                obj.VrfdDateTime = DateTime.Now;
                var x = _dbCommitContext.commitChanges(LogEventTypes.UPD, obj, crtId);



                //Generate Salary Slips once the Salary Process is completed ( value '3' - Completed)---------------
                if (mthod == 3)
                {
                    var lstSalDtl = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, eftMnt, eftYr);
                    foreach (var itm in lstSalDtl)
                    {
                        _payrollContext.GetPaySlip(itm.Emp_Id, eftYr, eftMnt, entId);
                    }
                }

                return Json(new { ok = true, data = x, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<JsonResult> GetWfStatus(int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                var hdr = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMnt);
                if (hdr != null)
                {
                    var approvers = await _workflowService.GetWorkflowApproversAsync("Salary Process");
                    int userEmpSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
                    return Json(new
                    {
                        ok = true,
                        prcId = hdr.PrcId,
                        prcStat = hdr.PrcStat,
                        wfStat = string.IsNullOrEmpty(hdr.WfStat) ? (hdr.PrcStat == 2 ? "Verified" : (hdr.PrcStat == 3 ? "Approved" : "Draft")) : hdr.WfStat,
                        comments = hdr.WfComments,
                        submittedBy = hdr.WfSubmittedBy,
                        canSubmit = (userEmpSysId == approvers.InitiatorId || userEmpSysId == 1) && hdr.PrcStat == 2 && (hdr.WfStat == null || hdr.WfStat == "Draft" || hdr.WfStat == "Verified" || hdr.WfStat == "Rejected")
                    });
                }
                return Json(new { ok = false, prcStat = 0, wfStat = "None", canSubmit = false });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> SubmitForApproval(int prcId)
        {
            try
            {
                int crtId = HttpContext.Session.GetInt32("userEmpSysId") ?? 1;
                var res = await _workflowService.SubmitSalaryProcessAsync(prcId, crtId);
                return Json(new { ok = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        public IActionResult GetAdHocTrnsByEmpId(int empSysId, int eftYr, int eftMn, int typ)
        {
            try
            {
                var pay_typ = typ == 1 ? "PAY" : "DEDUCT";

                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                ViewBag.dsAdHocTrns = _payrollContext.GetHrPrAdHocTrnViewByEntId(entId, 1)
                                                     .Where(x => x.EmpSysId == empSysId
                                                            && x.EftYear == eftYr
                                                            && x.EftMn == eftMn
                                                            && x.TrnTyp == pay_typ);

                return PartialView("_HrPrAdHocTrns");
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        //Get Overtime and No Pay By Employee 
        public IActionResult GetOTNoPayByEmpId(string field, int empSysId, int eftYr, int eftMn)
        {
            try
            {
                int fromYear = 0;
                int fromMonth = 0;
                DateTime fromDate;
                DateTime toDate;


                if (eftMn == 1)
                {
                    fromYear = eftYr - 1;
                    fromMonth = 12;
                    fromDate = new DateTime(fromYear, fromMonth, 21);
                    toDate = new DateTime(eftYr, eftMn, 20);
                }
                else
                {
                    fromDate = new DateTime(eftYr, eftMn - 1, 21);
                    toDate = new DateTime(eftYr, eftMn, 20);
                }

                var dsList = _attdContext.GetOTNoPayByEmpId(field, empSysId, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd")).ToList();
                if (field == "_nopay")
                {
                    var lopList = _payrollContext.GetLopNoPayDetailsByEmpId(empSysId, eftYr, eftMn);
                    if (lopList != null && lopList.Any())
                    {
                        dsList.AddRange(lopList);
                    }
                }
                ViewBag.dsOTNoPay = dsList;
                var returnview = "";
                if (field == "_overtime")
                    returnview = "_HrPrOvertime";
                else if (field == "_nopay")
                    returnview = "_HrPrNoPay";
                return PartialView(returnview);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public IActionResult GetRemarks(int PrcPsvRecId, int PrcId, int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                PrSalarySheetView obj = _payrollContext.GetSalaryDtl(entId, "_passive", PrcId, eftMnt, eftYr).Where(x=> x.Index == PrcPsvRecId).FirstOrDefault();
                return PartialView("_HrPrRemaks", obj);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IActionResult SetRemarks(PrSalarySheetView obj)
        {
            try
            {
                var hrPrSlryPrcDtlPsv = _payrollContext.GetHrPrSlryPrcDtlPsvById(obj.PrcPsvRecId);

                hrPrSlryPrcDtlPsv.Rmks = obj.Remarks;

                _payrollContext.SaveHrPrSlryPrcDtlPsv(hrPrSlryPrcDtlPsv);

                return RedirectToAction("Index", "HrPrSlryPrcDtl").WithSuccess("Salary Process", "Record Updated Successfully");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private partial class HrPrSlrySummary
        {
            public string FullName { get; set; }
            public decimal Value { get; set; }
        }

        public IActionResult GetSlrySummary(string field, int eftYr, int eftMn)
        {
            try
            {
                var heading = "";
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var slrySmy = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, eftMn, eftYr);

                ViewBag.dsSlrySummry = slrySmy.Select(x => new HrPrSlrySummary
                {
                    FullName = x.Full_Name,
                    Value = (field == "101") ? x.BasicSalary :
                            (field == "102") ? x.HouseRent :
                            (field == "103") ? x.Transport :
                            (field == "104") ? x.Position :
                            (field == "105") ? x.ProjectAllowance :
                            (field == "106") ? x.Incentives :
                            (field == "107") ? x.Mobile :
                            (field == "108") ? x.Overtime :
                            (field == "109") ? x.MedicalAllowance :
                            (field == "110") ? x.OthrAlwnc :
                            (field == "111") ? x.OthrPmt :
                            (field == "112") ? x.GrossSalary :
                            (field == "113") ? x.SalaryAdvance :
                            (field == "114") ? x.StaffLoans :
                            (field == "115") ? x.NoPay :
                            (field == "116") ? x.OthrRcvryAmt :
                            (field == "117") ? x.TotalDeductions :
                            (field == "118") ? x.NtSlry :
                            1
                }).ToList();


                if (field == "101") heading = "Basic Salary";
                if (field == "102") heading = "Housing";
                if (field == "103") heading = "Transport";
                if (field == "104") heading = "Position";
                if (field == "105") heading = "Project Allowance";
                if (field == "106") heading = "Incentives";
                if (field == "107") heading = "Mobile";
                if (field == "108") heading = "Overtime";
                if (field == "109") heading = "Medical Allowance";
                if (field == "110") heading = "Other Allowance";
                if (field == "111") heading = "Other Payments";
                if (field == "112") heading = "Gross Salary";
                if (field == "113") heading = "Salary Advance";
                if (field == "114") heading = "Staff Loans";
                if (field == "115") heading = "No Pay";
                if (field == "116") heading = "Other Recovery Amount";
                if (field == "117") heading = "Total Deductions";
                if (field == "118") heading = "Net Salary";

                ViewBag.heading = heading;
                return PartialView("_HrPrSummary");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Deletes all payroll process records (header, details, logs) for a given month/year.
        /// This resets the payroll so a fresh process can be started for that month.
        /// </summary>
        [HttpGet]
        public JsonResult DeletePrcByMonth(int eftYr, int eftMnt)
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;

                int deleted = _payrollContext.DeleteSlryPrcByMonth(entId, eftYr, eftMnt);

                if (deleted > 0)
                    return Json(new { ok = true, data = deleted, message = $"Payroll data for {new DateTime(eftYr, eftMnt, 1):MMMM yyyy} deleted successfully. ({deleted} header record(s) removed)" });
                else
                    return Json(new { ok = false, data = 0, message = $"No payroll data found for {new DateTime(eftYr, eftMnt, 1):MMMM yyyy}." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        public JsonResult GetWPSFile(int eftYr, int eftMn)
        {
            try
            {

                int recSeq = 1;
                string _filename = null;
                string _filepath = null;
                MemoryStream stream = new MemoryStream();
                int wrknDays = DateTime.DaysInMonth(eftYr, eftMn); //Number of Days
                _filename = "wps_" + eftYr.ToString() + "_" + eftMn.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
                _filepath = _host.WebRootPath + @"\attach\wps\" + $@"\{_filename}";


                //var wpsVw = _payrollContext.GetWPSFile("_paysheet", 0, eftMn, eftYr);

                // newly added
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                var _salPrcDtl = _payrollContext.GetTopSalaryHeader(entId, eftYr, eftMn);
                var wpsVw = _payrollContext.GetWPSFile("_passive", _salPrcDtl.PrcId, eftMn, eftYr);


                decimal totNetSlry = wpsVw.Sum(x => x.NetSlry);
                int rcrCount = wpsVw.Count();
                using (StreamWriter writer = new StreamWriter(_filepath, true))
                {
                    writer.WriteLine("Employer ID,File Creation Date,File Creation Time,Payer EID,Payer QID,Payer Bank Short Name,Payer IBAN,Salary Year And Month,Total Salaries,Total Records");
                    writer.WriteLine("10169600," + DateTime.Today.ToString("yyyyMMdd") + "," + DateTime.Now.ToString("HHmm") + ",10169600," + ",CBQ," + "QA87CBQA000000006010162429001," + eftYr.ToString() + eftMn.ToString().PadLeft(2, '0') + "," + totNetSlry.ToString() + "," + rcrCount.ToString());
                    writer.WriteLine("Record Sequence,Employee QID,Employee Visa ID,Employee Name,Employee Bank Short Name,Employee Account,Salary Frequency,Number Of Working Days,Net Salary,Basic Salary,Extra hours,Extra Income,Deductions,Payment Type,Notes/Comments");
                }
                using (StreamWriter writer = new StreamWriter(_filepath, true))
                {
                    foreach (var obj in wpsVw)
                    {
                        writer.WriteLine(recSeq + "," + obj.QID + "," + obj.VisaId + "," + obj.AcctName + "," + obj.BnkShrtNme + "," + obj.BnkAcctNo + ",M," + wrknDays.ToString() + "," + obj.NetSlry.ToString() + "," + obj.BscSlry.ToString() + "," + obj.ExtrHrs.ToString() + "," + obj.Incm.ToString() + "," + obj.Deduct.ToString() + ",," + obj.Cmnt);
                    }
                }

                return Json(new { ok = true, data = _filename, message = "ok" });

            }
            catch (Exception ex)
            {
                    throw ex;
            }
        }
    }
}
