using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using qHRM.Areas.Payroll.Models;
using Syncfusion.Drawing;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using qHRM.Areas.Payroll.Reports;
using Microsoft.AspNetCore.Http;
using AspNetCore.Reporting;
using qHRM.SharedSysFuncs;
using qHRM.Areas.General.Data;
using qHRM.Data;
using qHRM.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using qHRM.Area.Config;
using System.Threading.Tasks;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Notify.Data;
using qHRM.Areas.Notify.Models;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrSalSlipController : Controller
    {
        private readonly PayrollContext _payrollContext;
        private readonly OrganizationContext _organizationContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHostingEnvironment _host;
        private readonly SysConfigContext _sysConfig;
        private readonly EmployeeContext _empContext;
        private readonly NotifyContext _notifyContext;
        private readonly DashboardContext _dashboardContext;

        public HrSalSlipController(PayrollContext payrollContext
                                        , OrganizationContext organizationContext
                                        , IHostingEnvironment host
                                        , IWebHostEnvironment webHostEnvironment
                                        , DashboardContext dashboardContext
                                        , IEmailSender emailSender
                                        , SysConfigContext sysConfig
                                        , EmployeeContext empContext
                                        , NotifyContext notifyContext)
        {
            _payrollContext = payrollContext;
            _organizationContext = organizationContext;
            _host = host;
            _webHostEnvironment = webHostEnvironment;
            _dashboardContext = dashboardContext;
            _sysConfig = sysConfig;
            _empContext = empContext;
            _notifyContext = notifyContext;
        }

        Color gray = Color.FromArgb(255, 224, 224, 224);
        Color black = Color.FromArgb(255, 0, 0, 0);
        Color white = Color.FromArgb(255, 255, 255, 255);
        Color violet = Color.FromArgb(255, 151, 108, 174);

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
            try
            {
                // No Access Granted
                if (HttpContext.Session.GetString("SLRY_SHT") == null)                
                    return RedirectToPage("/Account/Login", new { area = "Identity" });               

                    List<Month> lstMonth = new List<Month>();
                    List<Year> lstYear = new List<Year>();
                    //Year
                    for (int i = DateTime.Now.Year; i >= DateTime.Now.AddYears(-10).Year; i--)
                    {
                        Year _objYr = new Year { value = i, text = i.ToString() };
                        lstYear.Add(_objYr);
                    }
                    //Month
                    for (int i = 1; i <= 12; i++)
                    {
                        Month _objMn = new Month { value = i, text = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM") };
                        lstMonth.Add(_objMn);
                    }

                    ViewBag.lstMonth = lstMonth;
                    ViewBag.lstYear = lstYear;

                    HrPrSlryPrcHdr slryPrcHdr = _payrollContext.GetLstSlryPrcHdr((int)HttpContext.Session.GetInt32("userEntId"));
                    if (slryPrcHdr == null)
                    {
                        var prConfig = _payrollContext.GetActiveHrPrConfig();
                        lstYear.Add(new Year { value = prConfig.SlryPrcStYear, text = prConfig.SlryPrcStYear.ToString() });
                        lstMonth.Add(new Month { value = prConfig.SlryPrcStMonth, text = new DateTime(prConfig.SlryPrcStYear, prConfig.SlryPrcStMonth, 1).ToString("MMMM") });
                    }
                    else
                    {
                        int entId = (int)HttpContext.Session.GetInt32("userEntId");
                        ViewBag.DataSource = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, slryPrcHdr.EftMonth, slryPrcHdr.EftYear);
                    }

                    ViewBag.lstMonth = lstMonth;
                    ViewBag.lstYear = lstYear;
                    ViewBag.SelectedMonth = slryPrcHdr.EftMonth;
                    ViewBag.SelectedYear = slryPrcHdr.EftYear;
                    return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IActionResult EmailSlrySlipIndex()
        {
            try
            {
                // No Access Granted
                if (HttpContext.Session.GetString("SLRY_SHT") == null)
                    return RedirectToPage("/Account/Login", new { area = "Identity" });

                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                HrPrSlryPrcHdr slryPrcHdr = _payrollContext.GetLstSlryPrcHdr(entId);
                int selYear = slryPrcHdr != null ? slryPrcHdr.EftYear : DateTime.Today.Year;
                int selMonth = slryPrcHdr != null ? slryPrcHdr.EftMonth : DateTime.Today.Month;

                // 1. In the period always year dropdown restricted to this year (2026) only
                List<Year> lstYear = new List<Year>
                {
                    new Year { value = selYear, text = selYear.ToString() }
                };

                // 2. In the period, always shows the current working month only
                List<Month> lstMonth = new List<Month>
                {
                    new Month { value = selMonth, text = new DateTime(selYear, selMonth, 1).ToString("MMMM") }
                };

                ViewBag.lstMonth = lstMonth;
                ViewBag.lstYear = lstYear;
                ViewBag.SelectedMonth = selMonth;
                ViewBag.SelectedYear = selYear;
                ViewBag.DataSource = _payrollContext.GetSlrySlipSndMail("#", 0, selMonth, selYear);

                return View();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GenerateAllPaySlps(int efYr, int efMnt, string empIds = null)
        {
            try
            {
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                string folderName = $"{efYr}-{efMnt:D2}";
                string targetDir = Path.Combine(_webHostEnvironment.WebRootPath, "attach", "empPaySlip", folderName);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var mailEmps = _payrollContext.GetSlrySlipSndMail("#", 0, efMnt, efYr)?.ToList() ?? new List<PayrollContext.HrPrSlrySndEmailvView>();
                var allSalarySlips = _payrollContext.GetSlryDtlByEmpSysIds(entId, "_paysheet", "", 0, efMnt, efYr)?.ToList() ?? new List<PrSalarySheetView>();

                if (!allSalarySlips.Any())
                {
                    return Json(new { ok = false, message = $"No processed salary records found for {new DateTime(efYr, efMnt, 1):MMMM yyyy}." });
                }

                int generatedCount = 0;
                string slryYrMnt = new DateTime(efYr, efMnt, 1).ToString("MMMM yyyy");

                var targetEmpSysIds = mailEmps.Any()
                    ? mailEmps.Select(m => m.EmpSysId).Distinct().ToList()
                    : allSalarySlips.Select(s => s.Emp_Sys_Id).Distinct().ToList();

                if (!string.IsNullOrWhiteSpace(empIds))
                {
                    var filterIds = Array.ConvertAll(empIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse).ToHashSet();
                    targetEmpSysIds = targetEmpSysIds.Where(id => filterIds.Contains(id)).ToList();
                }

                foreach (var empSysId in targetEmpSysIds)
                {
                    var slip = allSalarySlips.FirstOrDefault(s => s.Emp_Sys_Id == empSysId);
                    if (slip == null) continue;

                    var empGenInfo = _empContext.GetEmpGenInfoByEmpSysId(empSysId);
                    string password = !string.IsNullOrWhiteSpace(empGenInfo?.PmtId) ? empGenInfo.PmtId.Trim() : null;

                    byte[] pdfBytes = PaySlipPdfReport.GeneratePaySlipPdf(
                        new[] { slip },
                        _webHostEnvironment.WebRootPath,
                        slryYrMnt,
                        password);

                    string fileName = $"PaySlip_{slip.Emp_Id}_{folderName}.pdf";
                    string filePath = Path.Combine(targetDir, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                    generatedCount++;
                }

                return Json(new { ok = true, count = generatedCount, folder = folderName, message = $"Completed generating {generatedCount} salary slip(s) in folder wwwroot\\attach\\empPaySlip\\{folderName}." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error generating salary slips: " + ex.Message });
            }
        }

        public async Task<IActionResult> SendPaySlps(int efYr, int efMnt, string empIds)
        {
            try
            {
                int n = await SendPaySlipLoop(efYr, efMnt, empIds); 
                var data = _payrollContext.GetSlrySlipSndMail("#", 0, efMnt, efYr);
                return Json(new { ok = true, data = data, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "error" });
            }
        }

        public async Task<int> SendPaySlipLoop(int efYr, int efMnt, string empIds)
        {
            try
            {
                NtfNotification ntf = new NtfNotification();
                string _fromEmail = string.Empty, _toEmail = string.Empty;
                string _sbjct = "Salary Slip - Month of " + new DateTime(efYr, efMnt, 1).ToString("MMMM") + " " + efYr.ToString();
                string folderName = $"{efYr}-{efMnt:D2}";
                string folderDir = Path.Combine(_webHostEnvironment.WebRootPath, "attach", "empPaySlip", folderName);

                int[] empSysIds = Array.ConvertAll(empIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse);

                foreach (var itm in empSysIds)
                {
                    var objEmp = _empContext.GetEmpGenInfoByEmpSysId(itm);
                    if (objEmp == null) continue;

                    var objJbAsg = _empContext.GetCurrentAssignment(itm);

                    // Look for PaySlip in wwwroot\attach\empPaySlip\{efYr}-{efMnt:D2}
                    string fileName = $"PaySlip_{objEmp.EmpId}_{folderName}.pdf";
                    string filePath = Path.Combine(folderDir, fileName);
                    if (!System.IO.File.Exists(filePath))
                    {
                        // Fallback check in case file had unpadded name
                        string altFile = Path.Combine(folderDir, $"PaySlip_{objEmp.EmpId}_{efYr}-{efMnt}.pdf");
                        if (System.IO.File.Exists(altFile))
                            filePath = altFile;
                    }

                    _toEmail = objJbAsg?.OfcEmail;
                    string _msg = "<font face=tahoma size=2>Dear " + objEmp.FirstName + ",</font></br></br><font face=tahoma size=2>Kindly find the attached Salary Slip for the month of " + new DateTime(efYr, efMnt, 1).ToString("MMMM") + " " + efYr + ".</br>You may use your Qatari ID number as the password to open the attached document.</font></br></br><font face=Verdana size=2><b>qHR System</b></font></br></br><font face=tahoma size=1>This is system generated email. Do not reply.</font>";

                    ntf.NtfTypRefId = 5;
                    ntf.ReqId = 0;
                    ntf.EmpSysId = itm;
                    ntf.Subject = _sbjct;
                    ntf.PushReadStat = false;
                    ntf.IsActionTaken = false;
                    ntf.IsActionTakenBy = 0;
                    ntf.CrtDateTime = DateTime.Today;
                    ntf.EftMnth = efMnt;
                    ntf.EftYr = efYr;
                    ntf.MsgId = 0;

                    Console.WriteLine($"[PAYSLIP SEND] EmpSysId={itm}, EmpId={objEmp.EmpId}, Name={objEmp.FirstName}, ToEmail='{_toEmail}', FilePath='{filePath}', FileExists={System.IO.File.Exists(filePath)}");

                    if (string.IsNullOrWhiteSpace(_toEmail))
                    {
                        ntf.IsDelivered = 3; // No Email Address
                        Console.WriteLine($"[PAYSLIP STATUS] EmpSysId={itm}: No Email Address");
                    }
                    else if (!System.IO.File.Exists(filePath))
                    {
                        ntf.IsDelivered = 2; // No Attachment Found
                        Console.WriteLine($"[PAYSLIP STATUS] EmpSysId={itm}: Attachment Not Found ({filePath})");
                    }
                    else
                    {
                        int x = await _sysConfig.SendEmailNotificationAsync(_toEmail, _fromEmail, _sbjct, _msg, filePath);
                        ntf.IsDelivered = x; // 1 = Delivered, 0 = Not Delivered
                        Console.WriteLine($"[PAYSLIP STATUS] EmpSysId={itm}: Send result = {x} (1=Delivered, 0=Not Delivered)");
                    }

                    var objNtf = _notifyContext.GetSlrySlipEmailNotify(ntf);
                    if (objNtf == null)
                        _notifyContext.AddSlrySlipEmailNotify(ntf);
                    else
                    {
                        objNtf.IsDelivered = ntf.IsDelivered;
                        _notifyContext.UpdateSlrySlipEmailNotify(objNtf);
                    }
                }
                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public JsonResult PaySlpsSndStat(int efYr, int efMnt)
        {
            var data = _payrollContext.GetSlrySlipSndMail("#", 0, efMnt, efYr);
            return Json(new { ok = true, data = data, message = "ok" });
        }

        public IActionResult EmpViewIndex()
        {
            try
            {
                // No Access Granted
                //if (HttpContext.Session.GetString("SLRY_SHT_EMP") == null)
                  //  return RedirectToPage("/Account/Login", new { area = "Identity" });


                List<Month> lstMonth = new List<Month>();
                List<Year> lstYear = new List<Year>();
                //Year
                for (int i = DateTime.Now.Year; i >= DateTime.Now.AddYears(-10).Year; i--)
                {
                    Year _objYr = new Year { value = i, text = i.ToString() };
                    lstYear.Add(_objYr);
                }
                //Month
                for (int i = 1; i <= 12; i++)
                {
                    Month _objMn = new Month { value = i, text = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM") };
                    lstMonth.Add(_objMn);
                }

                ViewBag.lstMonth = lstMonth;
                ViewBag.lstYear = lstYear;

                HrPrSlryPrcHdr slryPrcHdr = _payrollContext.GetLstSlryPrcHdr((int)HttpContext.Session.GetInt32("userEntId"));
                if (slryPrcHdr == null)
                {
                    var prConfig = _payrollContext.GetActiveHrPrConfig();
                    lstYear.Add(new Year { value = prConfig.SlryPrcStYear, text = prConfig.SlryPrcStYear.ToString() });
                    lstMonth.Add(new Month { value = prConfig.SlryPrcStMonth, text = new DateTime(prConfig.SlryPrcStYear, prConfig.SlryPrcStMonth, 1).ToString("MMMM") });
                }
                else
                {
                    int entId = (int)HttpContext.Session.GetInt32("userEntId");
                    ViewBag.DataSource = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, slryPrcHdr.EftMonth, slryPrcHdr.EftYear);
                }

                ViewBag.lstMonth = lstMonth;
                ViewBag.lstYear = lstYear;
                ViewBag.SelectedMonth = slryPrcHdr.EftMonth;
                ViewBag.SelectedYear = slryPrcHdr.EftYear;
                return View();

            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        public JsonResult GetPaySheet(int eftMnt, int eftYr)
        {
            try
            {
                int entId = (int)HttpContext.Session.GetInt32("userEntId");
                var _list = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, eftMnt, eftYr);

                if (_list.Count() > 0)
                    return Json(new { ok = true, data = _list, message = "ok" });
                else
                {
                    List<PrSalarySheetView> temp = new List<PrSalarySheetView>
                    {
                        new PrSalarySheetView {
                        Index = 0,
                        Department = "No Records Found",
                        Emp_Sys_Id = 0,
                        Emp_Id = "",
                        Full_Name = "No Records Found",
                        Designation = "",
                        BasicSalary = 0,
                        HouseRent = 0,
                        Transport = 0,
                        Position = 0,
                        ProjectAllowance = 0,
                        Incentives = 0,
                        Mobile = 0,
                        Overtime = 0,
                        MedicalAllowance = 0,
                        BonusAmt = 0,
                        OthrAlwnc = 0,
                        OthrPmt = 0,
                        GrossSalary = 0,
                        SalaryAdvance = 0,
                        StaffLoans = 0,
                        NoPay = 0,
                        OthrRcvryAmt = 0,
                        TotalDeductions = 0,
                        NtSlry = 0,
                        PmtMod = 0,
                        PmtModTxt = "",
                        BnkAmt = 0,
                        CshAmt = 0,
                        RecStat = 1,
                        BnkAcct = "",
                        PrcId = 0,
                        PrcLogId = 0
                        }
                    };
                    return Json(new { ok = false, data = temp, message = "ok" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        public JsonResult GetPaySlipByEmp(int eftMnt, int eftYr )
        {
            try
            {
                int entId = (int)HttpContext.Session.GetInt32("userEntId");
                int EmpSysId = (int)HttpContext.Session.GetInt32("userEmpSysId");
                var _list = _payrollContext.GetSalaryDtl(entId, "_paysheet", 0, eftMnt, eftYr).Where(x => x.Emp_Sys_Id == EmpSysId);

                if (_list.Count() > 0)
                    return Json(new { ok = true, data = _list, message = "ok" });
                else
                {
                    List<PrSalarySheetView> temp = new List<PrSalarySheetView>
                    {
                        new PrSalarySheetView {
                        Index = 0,
                        Department = "No Records Found",
                        Emp_Sys_Id = 0,
                        Emp_Id = "",
                        Full_Name = "No Records Found",
                        Designation = "",
                        BasicSalary = 0,
                        HouseRent = 0,
                        Transport = 0,
                        Position = 0,
                        ProjectAllowance = 0,
                        Incentives = 0,
                        Mobile = 0,
                        Overtime = 0,
                        MedicalAllowance = 0,
                        BonusAmt = 0,
                        OthrAlwnc = 0,
                        OthrPmt = 0,
                        GrossSalary = 0,
                        SalaryAdvance = 0,
                        StaffLoans = 0,
                        NoPay = 0,
                        OthrRcvryAmt = 0,
                        TotalDeductions = 0,
                        NtSlry = 0,
                        PmtMod = 0,
                        PmtModTxt = "",
                        BnkAmt = 0,
                        CshAmt = 0,
                        RecStat = 1,
                        BnkAcct = "",
                        PrcId = 0,
                        PrcLogId = 0
                        }
                    };
                    return Json(new { ok = false, data = temp, message = "failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpPost]
        public IActionResult PrintPaySlip(PrSalarySheetView obj)
        {
            try
            {
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                var ds = _payrollContext.GetSlryDtlByEmpSysIds(entId, "_paysheet", obj.Emp_Id, 0, obj.EftMnt, obj.EftYr);
                var slryYrMnt = (obj.EftYr > 0 && obj.EftMnt > 0)
                    ? new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM yyyy")
                    : DateTime.Today.ToString("MMMM yyyy");
                byte[] pdfBytes = PaySlipPdfReport.GeneratePaySlipPdf(ds, _webHostEnvironment.WebRootPath, slryYrMnt);
                return File(pdfBytes, "application/pdf");
            }
            catch
            {
                throw;
            }
        }

        //[HttpPost]
        //public IActionResult PrintSlrySht(PrSalarySheetView obj)
        //{
        //    var cmp = _organizationContext.GetEntityByEntId((int)HttpContext.Session.GetInt32("userEntId"));
        //    var slryYrMnt = new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString();
        //    var dsSlryDtlVw = _payrollContext.GetSlryDtl((int)HttpContext.Session.GetInt32("userEntId"), "_paysheet", 0, obj.EftMnt, obj.EftYr);

        //    var ttl = dsSlryDtlVw.Sum(x => x.NtSlry);
        //    List<RptParam> dsRptParam = new List<RptParam> { new RptParam { prm1 = cmp.NameEng, prmTtl = "Salary Sheet for the Month of " + new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString(), prm2 = "Qatari Riyal " + NumberToWords.convert(Convert.ToInt32(ttl)), prm3 = "", prm4 = "", prm5 = 0 } };

        //    string mimetype1 = "";
        //    int extension1 = 1;
        //    var path1 = $"{this._webHostEnvironment.WebRootPath}\\Reports\\rptSlrySht.rdlc";
        //    //Dictionary<string, string> parameters1 = new Dictionary<string, string>();
        //    //parameters.Add("prmCmpName", cmp.NameEng);
        //    //parameters.Add("prmTitle", "Salary Sheet for the Month of " + new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString());
        //    //parameters.Add("prmTtlInWrd", "Qatari Riyal " + NumberToWords.convert(Convert.ToInt32(ttl)));

        //    LocalReport localReport1 = new LocalReport(path1);

        //    //Microsoft.Reporting.WinForms.LocalReport viewer = new Microsoft.Reporting.WinForms.LocalReport();

        //    localReport1.AddDataSource("dsSlryDtlVw", dsSlryDtlVw);
        //    localReport1.AddDataSource("dsRptParam", dsRptParam);

        //    var result = localReport1.Execute(RenderType.Pdf, extension1, null, mimetype1);


        //    return File(result.MainStream, "application/pdf");
        //}

        //[HttpPost]
        //public IActionResult PrintSlrySht(PrSalarySheetView obj)
        //{
        //    var cmp = _organizationContext.GetEntityByEntId((int)HttpContext.Session.GetInt32("userEntId"));
        //    var slryYrMnt = new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString();
        //    var dsSlryDtlVw = _payrollContext.GetSlryDtl((int)HttpContext.Session.GetInt32("userEntId"), "_paysheet", 0, obj.EftMnt, obj.EftYr);

        //    var path1 = $"{this._webHostEnvironment.WebRootPath}\\Reports\\TestSmry.rdlc";
        //    LocalReport localReport = new LocalReport(path1);

        //    var ttl = dsSlryDtlVw.Sum(x => x.NtSlry);
        //    //List<RptParam> dsRptParam = new List<RptParam> { new RptParam { prm1 = cmp.NameEng, prmTtl = "Monthly Salary Sheet Summary for the Month of  " + new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString(), prm2 = "Qatari Riyal " + NumberToWords.convert(Convert.ToInt32(ttl)), prm3 = "", prm4 = "", prm5 = 0 } };
        //    //Dictionary<string, string> parameters = new Dictionary<string, string>();
        //    //parameters.Add("prm1", cmp.NameEng);
        //    //parameters.Add("prmTtl", "Monthly Salary Sheet Summary for the Month of  " + new DateTime(obj.EftYr, obj.EftMnt, 1).ToString("MMMM") + " - " + obj.EftYr.ToString());
        //    //parameters.Add("prm2", "Qatari Riyal " + NumberToWords.convert(Convert.ToInt32(ttl)));
        //    //parameters.Add("prm3", "");
        //    //parameters.Add("prm4", "");
        //    //parameters.Add("prm5", "0");

        //    string mimetype1 = "";
        //    int extension1 = 1;

        //    localReport.AddDataSource("dsSlryShtSmry", dsSlryDtlVw);
        //    //localReport.AddDataSource("dsRptParamFl", dsRptParam);

        //    var result = localReport.Execute(RenderType.Pdf, extension1, null, mimetype1);
        //    //var result = localReport.Execute(RenderType.Pdf, extension1, parameters, mimetype1);

        //    return File(result.MainStream, "application/pdf");
        //}

        [HttpPost]
        public IActionResult PrintSlrySht(PrSalarySheetView obj)
        {
            try
            {
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                var cmp = _organizationContext.GetEntityByEntId(entId);
                string compName = cmp?.NameEng ?? "Qatar Computer Services WLL";

                int efYr = obj.EftYr > 0 ? obj.EftYr : DateTime.Today.Year;
                int efMnt = obj.EftMnt > 0 ? obj.EftMnt : DateTime.Today.Month;

                var details = _payrollContext.GetSlryDtl(entId, "_paysheet", 0, efMnt, efYr);
                decimal intlAmt = details != null ? details.Where(y => y.InHouse).Sum(y => y.NtSlry) : 0;
                decimal otscdAmt = details != null ? details.Where(y => !y.InHouse).Sum(y => y.NtSlry) : 0;

                byte[] pdfBytes = SalarySheetSummaryPdfReport.GenerateReport(
                    compName,
                    efYr,
                    efMnt,
                    intlAmt,
                    otscdAmt,
                    _webHostEnvironment.WebRootPath);

                return File(pdfBytes, "application/pdf");
            }
            catch
            {
                throw;
            }
        }

        [HttpPost]
        public IActionResult PrintSlrySht_In(PrSalarySheetView obj)
        {
            return PrintStaffSalarySheet(obj, 0);
        }

        [HttpPost]
        public IActionResult PrintSlrySht_Out(PrSalarySheetView obj)
        {
            return PrintStaffSalarySheet(obj, 1);
        }

        [HttpPost]
        public IActionResult PrintStaffSalarySheet(PrSalarySheetView obj, int sctn = 0)
        {
            try
            {
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                var cmp = _organizationContext.GetEntityByEntId(entId);
                string compName = cmp?.NameEng ?? "Qatar Computer Services WLL";

                int efYr = obj.EftYr > 0 ? obj.EftYr : DateTime.Today.Year;
                int efMnt = obj.EftMnt > 0 ? obj.EftMnt : DateTime.Today.Month;

                // Retrieve all employees for that month to compute correct internal & outsourced totals
                var allEmployees = _payrollContext.GetSlryDtl(entId, "_paysheet", 0, efMnt, efYr)?.ToList() ?? new List<PrSlryShtVw>();

                decimal intlAmt = allEmployees.Where(y => y.InHouse).Sum(y => y.NtSlry);
                decimal otscdAmt = allEmployees.Where(y => !y.InHouse).Sum(y => y.NtSlry);

                IEnumerable<PrSlryShtVw> displayEmployees;
                if (sctn == 0)
                {
                    // In-House Staff
                    displayEmployees = allEmployees.Where(y => y.InHouse).ToList();
                }
                else if (sctn == 1)
                {
                    // Outsourced Staff
                    displayEmployees = allEmployees.Where(y => !y.InHouse).ToList();
                }
                else
                {
                    // All Staff
                    displayEmployees = allEmployees;
                }

                byte[] pdfBytes = StaffSalarySheetPdfReport.GenerateReport(
                    compName,
                    efYr,
                    efMnt,
                    displayEmployees,
                    intlAmt,
                    otscdAmt,
                    sctn,
                    _webHostEnvironment.WebRootPath);

                return File(pdfBytes, "application/pdf");
            }
            catch
            {
                throw;
            }
        }

        public IActionResult PrintSlrySht_Sctn(PrSalarySheetView obj, int sctn) // sctn : 0 => Inhouse, sctn : 1 => OutSourced
        {
            return PrintStaffSalarySheet(obj, sctn);
        }

        [HttpPost]
        public IActionResult GetSlryShtExcel(PrSalarySheetView obj, int sctn = 0)
        {
            try
            {
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);
                var cmp = _organizationContext.GetEntityByEntId(entId);
                string compName = cmp?.NameEng ?? "Qatar Computer Services WLL";

                int efYr = obj.EftYr > 0 ? obj.EftYr : DateTime.Today.Year;
                int efMnt = obj.EftMnt > 0 ? obj.EftMnt : DateTime.Today.Month;

                var allEmployees = _payrollContext.GetSlryDtl(entId, "_paysheet", 0, efMnt, efYr)?.ToList() ?? new List<PrSlryShtVw>();

                decimal intlAmt = allEmployees.Where(y => y.InHouse).Sum(y => y.NtSlry);
                decimal otscdAmt = allEmployees.Where(y => !y.InHouse).Sum(y => y.NtSlry);

                IEnumerable<PrSlryShtVw> displayEmployees;
                if (sctn == 0)
                    displayEmployees = allEmployees.Where(y => y.InHouse).ToList();
                else if (sctn == 1)
                    displayEmployees = allEmployees.Where(y => !y.InHouse).ToList();
                else
                    displayEmployees = allEmployees;

                byte[] excelBytes = StaffSalarySheetExcelReport.GenerateReport(
                    compName,
                    efYr,
                    efMnt,
                    displayEmployees,
                    intlAmt,
                    otscdAmt,
                    sctn);

                string fileName = $"SalarySheet_{efYr}_{efMnt}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch
            {
                throw;
            }
        }
    }
}