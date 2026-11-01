using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using qHRM.Areas.General.Data;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.Payroll.Reports;
using qHRM.Extensions.Alerts;
using qHRM.SharedSysFuncs;
using Syncfusion.XlsIO;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrZohoConvertController : Controller
    {
        private readonly OrganizationContext _organizationContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        // In-memory cache for parsed datasets, keyed by session token
        private static readonly ConcurrentDictionary<string, ZohoConvertViewModel> _sessionDataCache 
            = new ConcurrentDictionary<string, ZohoConvertViewModel>();

        public HrPrZohoConvertController(
            OrganizationContext organizationContext,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _organizationContext = organizationContext;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        private bool HasAccess()
        {
            return HttpContext.Session.GetString("ZH_CVT") != null || HttpContext.Session.GetString("SY_ADM") != null;
        }

        private string GetSessionKey()
        {
            string key = HttpContext.Session.GetString("ZohoConvertSessionKey");
            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("ZohoConvertSessionKey", key);
            }
            return key;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!HasAccess())
            {
                return RedirectToAction("Index", "Home").WithWarning("Access Denied", "You do not have privilege to access Convert Zoho File.");
            }

            string key = GetSessionKey();
            if (_sessionDataCache.TryGetValue(key, out var cachedModel) && cachedModel != null)
            {
                return View(cachedModel);
            }

            return View(new ZohoConvertViewModel
            {
                Year = DateTime.Today.Year,
                Month = DateTime.Today.Month
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upload(IFormFile excelFile, int year = 0, int month = 0)
        {
            if (!HasAccess())
            {
                return RedirectToAction("Index", "Home").WithWarning("Access Denied", "You do not have privilege to access Convert Zoho File.");
            }

            if (excelFile == null || excelFile.Length == 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "Please select a valid Excel file (.xlsx or .xls) to upload.");
            }

            string ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "Only Excel files (.xlsx or .xls) are supported.");
            }

            int selectedYear = year > 0 ? year : DateTime.Today.Year;
            int selectedMonth = month >= 1 && month <= 12 ? month : DateTime.Today.Month;

            try
            {
                using var excelEngine = new ExcelEngine();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ext == ".xls" ? ExcelVersion.Excel97to2003 : ExcelVersion.Excel2016;

                using var stream = excelFile.OpenReadStream();
                IWorkbook workbook = application.Workbooks.Open(stream);

                if (workbook == null || workbook.Worksheets.Count == 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "The uploaded Excel file does not contain any readable sheets.");
                }

                // Pick the most relevant worksheet (e.g. name contains "salary" or "payrun", or 1st sheet)
                IWorksheet sheet = workbook.Worksheets[0];
                foreach (IWorksheet ws in workbook.Worksheets)
                {
                    string sName = ws.Name.ToLowerInvariant();
                    if (sName.Contains("salary") || sName.Contains("payrun"))
                    {
                        sheet = ws;
                        break;
                    }
                }

                DataTable table = sheet.ExportDataTable(sheet.UsedRange, ExcelExportDataTableOptions.ColumnNames);
                if (table == null || table.Rows.Count == 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "The uploaded sheet does not contain any data rows.");
                }

                // Load DB Employees for fallback metadata (Designation, Department, InHouse status)
                var dbEmployees = LoadDbEmployees();

                // Dynamic Column Mapping from DataTable Column Names
                var colMap = BuildColumnMapping(table.Columns);

                if (!colMap.ContainsKey("empid"))
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "Could not locate an Employee Number or Code column in the Excel header.");
                }

                var employees = new List<PrSlryShtVw>();
                int sNo = 1;

                // Check if Period column gives month & year
                if (colMap.TryGetValue("period", out int periodColIdx) && table.Rows.Count > 0)
                {
                    string periodVal = table.Rows[0][periodColIdx]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(periodVal))
                    {
                        ParsePeriod(periodVal, ref selectedYear, ref selectedMonth);
                    }
                }

                for (int r = 0; r < table.Rows.Count; r++)
                {
                    var row = table.Rows[r];
                    string empCode = GetStringVal(row, colMap, "empid");
                    if (string.IsNullOrWhiteSpace(empCode)) continue;

                    string lowerCode = empCode.ToLowerInvariant();
                    if (lowerCode.Contains("total") || lowerCode.Contains("summary")) continue;

                    string fullName = GetStringVal(row, colMap, "fullname");
                    string desig = GetStringVal(row, colMap, "designation");
                    string dept = GetStringVal(row, colMap, "department");
                    string remarks = GetStringVal(row, colMap, "remarks");
                    string payMode = GetStringVal(row, colMap, "paymode");

                    // InHouse detection from Excel
                    bool? excelInHouse = null;
                    if (colMap.TryGetValue("inhouse", out int inhouseIdx))
                    {
                        string val = row[inhouseIdx]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            string low = val.ToLowerInvariant();
                            if (low.Contains("out")) excelInHouse = false;
                            else if (low.Contains("in")) excelInHouse = true;
                        }
                    }

                    // Fallback to DB info if metadata is missing
                    if (dbEmployees.TryGetValue(empCode, out var dbEmp))
                    {
                        if (string.IsNullOrWhiteSpace(fullName)) fullName = dbEmp.FullName;
                        if (string.IsNullOrWhiteSpace(desig)) desig = dbEmp.DesigName;
                        if (string.IsNullOrWhiteSpace(dept)) dept = dbEmp.DeptName;
                        if (!excelInHouse.HasValue) excelInHouse = dbEmp.InHouse;
                    }

                    bool inHouse = excelInHouse ?? false; // Default to Outsourced if still unresolved

                    // Allowances & Components
                    decimal basic = GetDecimalVal(row, colMap, "basic");
                    decimal housing = GetDecimalVal(row, colMap, "housing");
                    decimal trans = GetDecimalVal(row, colMap, "transport");
                    decimal posit = GetDecimalVal(row, colMap, "position");
                    decimal mobile = GetDecimalVal(row, colMap, "mobile");
                    decimal proj = GetDecimalVal(row, colMap, "project");
                    decimal otherAlw = GetDecimalVal(row, colMap, "otheralw");
                    decimal fixedAlw = GetDecimalVal(row, colMap, "fixedalw");
                    decimal totalOtherAlw = otherAlw + fixedAlw;

                    decimal totalFixed = GetDecimalVal(row, colMap, "totalfixed");
                    if (totalFixed <= 0)
                    {
                        totalFixed = basic + housing + trans + posit + mobile + proj + totalOtherAlw;
                    }

                    decimal incent = GetDecimalVal(row, colMap, "incentive");
                    decimal ot = GetDecimalVal(row, colMap, "ot");
                    decimal otherPmt = GetDecimalVal(row, colMap, "otherpmt");

                    decimal gross = GetDecimalVal(row, colMap, "gross");
                    if (gross <= 0)
                    {
                        gross = totalFixed + incent + ot + otherPmt;
                    }

                    // Deductions
                    decimal slAdv = GetDecimalVal(row, colMap, "sladv");
                    decimal noPay = GetDecimalVal(row, colMap, "nopay");
                    decimal otherDeduct = GetDecimalVal(row, colMap, "otherdeduct");
                    decimal totalDeduct = GetDecimalVal(row, colMap, "totaldeduct");
                    if (totalDeduct <= 0)
                    {
                        totalDeduct = slAdv + noPay + otherDeduct;
                    }

                    // Net Salary
                    decimal net = GetDecimalVal(row, colMap, "net");
                    if (net <= 0)
                    {
                        net = gross - totalDeduct;
                    }

                    // Payment Mode (Bank Transfer vs Cash)
                    decimal bankAmt = 0;
                    decimal cashAmt = 0;
                    int pmtMod = 1; // 1 = Bank, 0 = Cash

                    if (colMap.TryGetValue("bankamt", out int bnkIdx) && colMap.TryGetValue("cashamt", out int cshIdx))
                    {
                        bankAmt = GetDecimalVal(row, colMap, "bankamt");
                        cashAmt = GetDecimalVal(row, colMap, "cashamt");
                        pmtMod = bankAmt >= cashAmt ? 1 : 0;
                    }
                    else if (!string.IsNullOrEmpty(payMode))
                    {
                        if (payMode.ToLowerInvariant().Contains("cash"))
                        {
                            pmtMod = 0;
                            cashAmt = net;
                        }
                        else
                        {
                            pmtMod = 1;
                            bankAmt = net;
                        }
                    }
                    else
                    {
                        pmtMod = 1;
                        bankAmt = net;
                    }

                    decimal absent = GetDecimalVal(row, colMap, "absent");

                    var empItem = new PrSlryShtVw
                    {
                        Index = sNo++,
                        EmpId = empCode,
                        FullName = fullName,
                        DesigName = desig,
                        DeptName = dept,
                        InHouse = inHouse,
                        AbFct = absent,
                        Basic = basic,
                        HrAlwnc = housing,
                        TrnsAlwnc = trans,
                        PstnAlwnc = posit,
                        MobAlwnc = mobile,
                        ProjAlwnc = proj,
                        OthrAlwnc = totalOtherAlw,
                        IncntvAmt = incent,
                        OtAmt = ot,
                        OthrPmt = otherPmt,
                        GrsSlry = gross,
                        SlryAdv = slAdv,
                        NoPayAmt = noPay,
                        OthrRcvryAmt = otherDeduct,
                        TotDctAmt = totalDeduct,
                        NtSlry = net,
                        PmtMod = pmtMod,
                        PmtModTxt = pmtMod == 1 ? "Bank Transfer" : "Cash",
                        BnkAmt = bankAmt,
                        CshAmt = cashAmt,
                        Rmks = remarks,
                        EftYr = selectedYear,
                        EftMnt = selectedMonth
                    };

                    employees.Add(empItem);
                }

                if (employees.Count == 0)
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "No valid employee rows were found in the uploaded file.");
                }

                var model = new ZohoConvertViewModel
                {
                    Year = selectedYear,
                    Month = selectedMonth,
                    FileName = excelFile.FileName,
                    TotalCount = employees.Count,
                    InHouseCount = employees.Count(e => e.InHouse),
                    OutsourcedCount = employees.Count(e => !e.InHouse),
                    TotalBasic = employees.Sum(e => e.Basic),
                    TotalFixedSalary = employees.Sum(e => e.Basic + e.HrAlwnc + e.TrnsAlwnc + e.PstnAlwnc + e.MobAlwnc + e.ProjAlwnc + e.OthrAlwnc),
                    TotalGross = employees.Sum(e => e.GrsSlry),
                    TotalDeductions = employees.Sum(e => e.TotDctAmt),
                    InternalStaffNet = employees.Where(e => e.InHouse).Sum(e => e.NtSlry),
                    OutsourcedStaffNet = employees.Where(e => !e.InHouse).Sum(e => e.NtSlry),
                    TotalNetSalary = employees.Sum(e => e.NtSlry),
                    Employees = employees
                };

                // Store into cache
                string key = GetSessionKey();
                _sessionDataCache[key] = model;

                return View("Index", model).WithSuccess("Zoho Import", $"Successfully parsed {employees.Count} employee records from '{excelFile.FileName}'.");
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "Error parsing Excel file: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult PrintStaffSalarySheet(int sctn = 1)
        {
            if (!HasAccess())
            {
                return Unauthorized();
            }

            string key = GetSessionKey();
            if (!_sessionDataCache.TryGetValue(key, out var model) || model == null || model.Employees.Count == 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Zoho Report", "Please upload and process an Excel file before generating reports.");
            }

            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            var cmp = _organizationContext.GetEntityByEntId(entId);
            string compName = cmp?.NameEng ?? "Qatar Computer Services WLL";

            IEnumerable<PrSlryShtVw> displayEmployees;
            if (sctn == 0)
            {
                displayEmployees = model.Employees.Where(e => e.InHouse).ToList();
            }
            else if (sctn == 1)
            {
                displayEmployees = model.Employees.Where(e => !e.InHouse).ToList();
            }
            else
            {
                displayEmployees = model.Employees;
            }

            byte[] pdfBytes = StaffSalarySheetPdfReport.GenerateReport(
                compName,
                model.Year,
                model.Month,
                displayEmployees,
                model.InternalStaffNet,
                model.OutsourcedStaffNet,
                sctn,
                _webHostEnvironment.WebRootPath);

            string sctnName = sctn == 0 ? "InHouse" : (sctn == 1 ? "Outsourced" : "AllStaff");
            Response.Headers["Content-Disposition"] = $"inline; filename=StaffSalarySheet_{sctnName}_{model.Year}_{model.Month}.pdf";
            return File(pdfBytes, "application/pdf");
        }

        [HttpGet]
        public IActionResult PrintSlrySht()
        {
            if (!HasAccess())
            {
                return Unauthorized();
            }

            string key = GetSessionKey();
            if (!_sessionDataCache.TryGetValue(key, out var model) || model == null || model.Employees.Count == 0)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Zoho Report", "Please upload and process an Excel file before generating reports.");
            }

            int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            var cmp = _organizationContext.GetEntityByEntId(entId);
            string compName = cmp?.NameEng ?? "Qatar Computer Services WLL";

            byte[] pdfBytes = SalarySheetSummaryPdfReport.GenerateReport(
                compName,
                model.Year,
                model.Month,
                model.InternalStaffNet,
                model.OutsourcedStaffNet,
                _webHostEnvironment.WebRootPath);

            Response.Headers["Content-Disposition"] = $"inline; filename=SalarySheetSummary_{model.Year}_{model.Month}.pdf";
            return File(pdfBytes, "application/pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            string key = GetSessionKey();
            _sessionDataCache.TryRemove(key, out _);
            return RedirectToAction(nameof(Index)).WithSuccess("Zoho Import", "Data cleared successfully. You can now upload a new file.");
        }

        #region Helper Methods

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]", "");
        }

        private static Dictionary<string, int> BuildColumnMapping(DataColumnCollection columns)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < columns.Count; c++)
            {
                string raw = columns[c].ColumnName?.Trim() ?? "";
                string norm = Normalize(raw);
                if (string.IsNullOrEmpty(norm)) continue;

                // Employee Code / Number
                if (norm.Contains("employeenumber") || norm.Contains("employeeno") || norm == "empno" || norm == "code" || norm == "empid")
                {
                    if (!map.ContainsKey("empid")) map["empid"] = c;
                }
                // Employee Name
                else if (norm.Contains("employeename") || norm == "empname" || norm == "fullname" || norm == "name")
                {
                    if (!map.ContainsKey("fullname")) map["fullname"] = c;
                }
                // Designation
                else if (norm.Contains("designation") || norm == "desig" || norm == "jobtitle")
                {
                    if (!map.ContainsKey("designation")) map["designation"] = c;
                }
                // Department
                else if (norm.Contains("department") || norm == "dept")
                {
                    if (!map.ContainsKey("department")) map["department"] = c;
                }
                // InHouse / Outsourced Tag
                else if (norm.Contains("inhouse") || norm.Contains("outsourced") || norm.Contains("stafftype"))
                {
                    if (!map.ContainsKey("inhouse")) map["inhouse"] = c;
                }
                // Period
                else if (norm.Contains("period"))
                {
                    if (!map.ContainsKey("period")) map["period"] = c;
                }
                // Payment Mode
                else if (norm.Contains("paymentmode") || norm.Contains("paymode"))
                {
                    if (!map.ContainsKey("paymode")) map["paymode"] = c;
                }
                // Basic
                else if (norm == "basic" || norm.Contains("basicsalary"))
                {
                    if (!map.ContainsKey("basic")) map["basic"] = c;
                }
                // HRA / Housing
                else if (norm == "hra" || norm.Contains("housing"))
                {
                    if (!map.ContainsKey("housing")) map["housing"] = c;
                }
                // Transport Allowance
                else if (norm.Contains("transport") || norm.Contains("trans"))
                {
                    if (!map.ContainsKey("transport")) map["transport"] = c;
                }
                // Position Allowance
                else if (norm.Contains("position") || norm.Contains("posit"))
                {
                    if (!map.ContainsKey("position")) map["position"] = c;
                }
                // Mobile Allowance
                else if (norm.Contains("mobile") || norm.Contains("mob"))
                {
                    if (!map.ContainsKey("mobile")) map["mobile"] = c;
                }
                // Project Allowance
                else if (norm.Contains("project") || norm.Contains("proj"))
                {
                    if (!map.ContainsKey("project")) map["project"] = c;
                }
                // Fixed Allowance
                else if (norm.Contains("fixedallowance") || norm.Contains("fixedalw"))
                {
                    if (!map.ContainsKey("fixedalw")) map["fixedalw"] = c;
                }
                // Other Allowance
                else if (norm.Contains("otherallowance") || norm.Contains("otheralw"))
                {
                    if (!map.ContainsKey("otheralw")) map["otheralw"] = c;
                }
                // Fixed Salary Total
                else if (norm.Contains("totalfixedsalary") || norm.Contains("fixedmonthlyearnings") || norm == "fixedsalary")
                {
                    if (!map.ContainsKey("totalfixed")) map["totalfixed"] = c;
                }
                // Incentive
                else if (norm.Contains("incentive") || norm.Contains("incent"))
                {
                    if (!map.ContainsKey("incentive")) map["incentive"] = c;
                }
                // Overtime
                else if (norm.Contains("overtime") || norm == "ot" || norm.Contains("otamt") || norm.Contains("otamount"))
                {
                    if (!map.ContainsKey("ot")) map["ot"] = c;
                }
                // Other Payments
                else if (norm.Contains("otherpayment") || norm.Contains("otherpmt"))
                {
                    if (!map.ContainsKey("otherpmt")) map["otherpmt"] = c;
                }
                // Gross Pay / Salary
                else if (norm.Contains("grosspay") || norm.Contains("grosssalary") || norm.Contains("totalearnings") || norm == "gross")
                {
                    if (!map.ContainsKey("gross")) map["gross"] = c;
                }
                // Salary Advance / Loan
                else if (norm.Contains("sladv") || norm.Contains("salaryadvance") || norm.Contains("staffloan") || norm == "loan")
                {
                    if (!map.ContainsKey("sladv")) map["sladv"] = c;
                }
                // Loss of pay / No Pay
                else if (norm.Contains("lossofpay") || norm.Contains("nopay") || norm == "lop")
                {
                    if (!map.ContainsKey("nopay")) map["nopay"] = c;
                }
                // Other Deductions
                else if (norm.Contains("otherdeduct") || norm.Contains("otherrecovery") || norm.Contains("othrrcvry"))
                {
                    if (!map.ContainsKey("otherdeduct")) map["otherdeduct"] = c;
                }
                // Total Deductions
                else if (norm.Contains("totaldeduct") || norm.Contains("totdeduct"))
                {
                    if (!map.ContainsKey("totaldeduct")) map["totaldeduct"] = c;
                }
                // Net Pay / Salary
                else if (norm.Contains("netpay") || norm.Contains("netsalary") || norm == "net")
                {
                    if (!map.ContainsKey("net")) map["net"] = c;
                }
                // Bank Transfer
                else if (norm.Contains("banktransfer"))
                {
                    if (!map.ContainsKey("bankamt")) map["bankamt"] = c;
                }
                // Cash Payment
                else if (norm.Contains("cashpayment"))
                {
                    if (!map.ContainsKey("cashamt")) map["cashamt"] = c;
                }
                // Remarks
                else if (norm.Contains("remarks") || norm.Contains("remark") || norm.Contains("note"))
                {
                    if (!map.ContainsKey("remarks")) map["remarks"] = c;
                }
                // Absent
                else if (norm.Contains("absent") || norm == "abfct")
                {
                    if (!map.ContainsKey("absent")) map["absent"] = c;
                }
            }

            return map;
        }

        private static string GetStringVal(DataRow row, Dictionary<string, int> colMap, string key)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < row.Table.Columns.Count)
            {
                return row[idx]?.ToString()?.Trim() ?? "";
            }
            return "";
        }

        private static decimal GetDecimalVal(DataRow row, Dictionary<string, int> colMap, string key)
        {
            if (colMap.TryGetValue(key, out int idx) && idx < row.Table.Columns.Count)
            {
                string s = row[idx]?.ToString()?.Trim().Replace(",", "") ?? "";
                if (decimal.TryParse(s, out decimal d)) return d;
            }
            return 0m;
        }

        private static void ParsePeriod(string periodVal, ref int year, ref int month)
        {
            try
            {
                string[] parts = periodVal.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int val))
                    {
                        if (val >= 2000 && val <= 2100) year = val;
                        else if (val >= 1 && val <= 12 && month == 0) month = val;
                    }
                    else
                    {
                        // Check for month name
                        string mLower = p.ToLowerInvariant();
                        if (mLower.StartsWith("jan")) month = 1;
                        else if (mLower.StartsWith("feb")) month = 2;
                        else if (mLower.StartsWith("mar")) month = 3;
                        else if (mLower.StartsWith("apr")) month = 4;
                        else if (mLower.StartsWith("may")) month = 5;
                        else if (mLower.StartsWith("jun")) month = 6;
                        else if (mLower.StartsWith("jul")) month = 7;
                        else if (mLower.StartsWith("aug")) month = 8;
                        else if (mLower.StartsWith("sep")) month = 9;
                        else if (mLower.StartsWith("oct")) month = 10;
                        else if (mLower.StartsWith("nov")) month = 11;
                        else if (mLower.StartsWith("dec")) month = 12;
                    }
                }
            }
            catch { }
        }

        private Dictionary<string, EmpDbInfo> LoadDbEmployees()
        {
            var dict = new Dictionary<string, EmpDbInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string conStr = _configuration.GetConnectionString("qhrconstring") 
                                ?? _configuration.GetConnectionString("qHRconString");
                if (string.IsNullOrEmpty(conStr))
                {
                    conStr = "server=SOFT-NB-095;database=qHRMasterV27;uid=sa;password=sa#12345678;trustservercertificate=true;";
                }

                using var conn = new SqlConnection(conStr);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT emp.Emp_Id, 
                           emp.First_Name + ' ' + ISNULL(emp.Last_Name, '') AS FullName,
                           ds.Name_Eng AS DesigName,
                           dp.Name_Eng AS DeptName,
                           CAST(IIF(lc.Parent_Id = 3, 0, 1) AS BIT) AS InHouse
                    FROM HR_EmpGenInfo emp
                    INNER JOIN HR_EmpJobAssign jb ON jb.Emp_Sys_Id = emp.Emp_Sys_Id AND jb.Wk_Stat = 1
                    LEFT JOIN ORG_Department dp ON dp.Dept_Id = jb.Dept_Id
                    LEFT JOIN ORG_Designation ds ON ds.Desig_Id = jb.Desig_Id
                    LEFT JOIN ORG_Location lc ON lc.Loc_Id = emp.Loc_Id";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string empId = r["Emp_Id"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(empId))
                    {
                        dict[empId] = new EmpDbInfo
                        {
                            EmpId = empId,
                            FullName = r["FullName"]?.ToString()?.Trim() ?? "",
                            DesigName = r["DesigName"]?.ToString()?.Trim() ?? "",
                            DeptName = r["DeptName"]?.ToString()?.Trim() ?? "",
                            InHouse = r["InHouse"] != DBNull.Value && Convert.ToBoolean(r["InHouse"])
                        };
                    }
                }
            }
            catch { }

            return dict;
        }

        private class EmpDbInfo
        {
            public string EmpId { get; set; } = "";
            public string FullName { get; set; } = "";
            public string DesigName { get; set; } = "";
            public string DeptName { get; set; } = "";
            public bool InHouse { get; set; }
        }

        #endregion
    }
}
