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
            int grpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
            return (grpId == 1 || grpId == 2 || grpId == 7) && HttpContext.Session.GetString("ZH_CVT") != null;
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

                // Load DB Employees & Departments for fallback metadata and Department/Employee sort order
                var dbEmployees = LoadDbEmployees(out var dbDepartments);

                // Dynamic Column Mapping from DataTable Column Names
                var colMap = BuildColumnMapping(table.Columns);

                if (!colMap.ContainsKey("empid"))
                {
                    return RedirectToAction(nameof(Index)).WithWarning("Zoho Import", "Could not locate an Employee Number or Code column in the Excel header.");
                }

                var employees = new List<PrSlryShtVw>();
                int sNo = 1;

                // Check if Period column gives month & year
                if (colMap.TryGetValue("period", out var periodCols) && periodCols.Count > 0 && table.Rows.Count > 0)
                {
                    string periodVal = table.Rows[0][periodCols[0]]?.ToString()?.Trim() ?? "";
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
                    if (colMap.TryGetValue("inhouse", out var inhouseCols) && inhouseCols.Count > 0)
                    {
                        string val = row[inhouseCols[0]]?.ToString()?.Trim() ?? "";
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

                    // Fixed Allowances & Components (Arrear columns are strictly excluded from fixed components)
                    decimal basic = GetDecimalVal(row, colMap, "basic");
                    decimal housing = GetDecimalVal(row, colMap, "housing");
                    decimal trans = GetDecimalVal(row, colMap, "transport");
                    decimal posit = GetDecimalVal(row, colMap, "position");
                    decimal mobile = GetDecimalVal(row, colMap, "mobile");
                    decimal proj = GetDecimalVal(row, colMap, "project");
                    decimal medical = GetDecimalVal(row, colMap, "medical");
                    decimal special = GetDecimalVal(row, colMap, "special");
                    decimal otherAlw = GetDecimalVal(row, colMap, "otheralw");
                    decimal fixedAlw = GetDecimalVal(row, colMap, "fixedalw");
                    decimal totalOtherAlw = otherAlw + fixedAlw;

                    decimal computedFixed = basic + housing + trans + posit + mobile + proj + medical + special + totalOtherAlw;
                    if (HasColumn(colMap, "totalfixed"))
                    {
                        decimal excelFixed = GetDecimalVal(row, colMap, "totalfixed");
                        if (excelFixed != computedFixed && (excelFixed > 0 || computedFixed > 0))
                        {
                            // Capture any unmapped fixed allowance column into Other Allowance so Fixed Monthly Earnings reconciles
                            totalOtherAlw += (excelFixed - computedFixed);
                            computedFixed = excelFixed;
                        }
                    }
                    decimal totalFixed = computedFixed;

                    // Variable / Arrear / Additional Earnings
                    decimal incent = GetDecimalVal(row, colMap, "incentive");
                    decimal ot = GetDecimalVal(row, colMap, "ot");
                    decimal bonus = GetDecimalVal(row, colMap, "bonus");
                    decimal otherPmt = GetDecimalVal(row, colMap, "otherpmt") + bonus;

                    decimal computedGross = totalFixed + incent + ot + otherPmt;
                    decimal gross;
                    if (HasColumn(colMap, "gross"))
                    {
                        gross = GetDecimalVal(row, colMap, "gross");
                        if (gross != computedGross)
                        {
                            otherPmt += (gross - computedGross);
                        }
                    }
                    else if (HasColumn(colMap, "totalearnings"))
                    {
                        gross = GetDecimalVal(row, colMap, "totalearnings");
                        if (gross != computedGross)
                        {
                            otherPmt += (gross - computedGross);
                        }
                    }
                    else
                    {
                        gross = computedGross;
                    }

                    // Deductions (Loss Of Pay days are mapped to Absent, never to NoPayAmt)
                    decimal slAdv = GetDecimalVal(row, colMap, "sladv");
                    decimal stfLoan = GetDecimalVal(row, colMap, "stfloan");
                    decimal noPay = GetDecimalVal(row, colMap, "nopay");
                    decimal otherDeduct = GetDecimalVal(row, colMap, "otherdeduct");
                    decimal computedDeduct = slAdv + stfLoan + noPay + otherDeduct;
                    decimal totalDeduct;
                    if (HasColumn(colMap, "totaldeduct"))
                    {
                        totalDeduct = GetDecimalVal(row, colMap, "totaldeduct");
                        if (totalDeduct != computedDeduct)
                        {
                            otherDeduct += (totalDeduct - computedDeduct);
                        }
                    }
                    else
                    {
                        totalDeduct = computedDeduct;
                    }

                    // Net Salary
                    decimal net = HasColumn(colMap, "net")
                        ? GetDecimalVal(row, colMap, "net")
                        : (gross - totalDeduct);

                    // Payment Mode (Bank Transfer vs Cash)
                    decimal bankAmt = 0;
                    decimal cashAmt = 0;
                    int pmtMod = 1; // 1 = Bank, 0 = Cash

                    if (HasColumn(colMap, "bankamt") && HasColumn(colMap, "cashamt"))
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
                        MedAlwnc = medical,
                        SpcAlwnc = special,
                        OthrAlwnc = totalOtherAlw,
                        IncntvAmt = incent,
                        OtAmt = ot,
                        OthrPmt = otherPmt,
                        GrsSlry = gross,
                        SlryAdv = slAdv,
                        StfLnPmt = stfLoan,
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

                // Normalize Department names & sort orders across all rows so DepartmentSalarySheetPdfReport groups cleanly
                foreach (var deptGrp in employees.GroupBy(
                    e => string.IsNullOrWhiteSpace(e.DeptName) ? "Other Department" : e.DeptName.Trim(),
                    StringComparer.OrdinalIgnoreCase))
                {
                    string grpKey = deptGrp.Key;
                    string canonName;
                    int canonDeptId;
                    int canonSrtOrd;

                    if (dbDepartments.TryGetValue(grpKey, out var dbDept))
                    {
                        canonName = dbDept.NameEng;
                        canonDeptId = dbDept.DeptId;
                        canonSrtOrd = dbDept.SrtOrd > 0 ? dbDept.SrtOrd : 999;
                    }
                    else
                    {
                        canonName = deptGrp.First().DeptName?.Trim();
                        if (string.IsNullOrWhiteSpace(canonName)) canonName = "Other Department";
                        canonDeptId = 0;

                        // Derive department sort order from matching DB employees in this group if available
                        var matchedOrders = deptGrp
                            .Select(e => dbEmployees.TryGetValue(e.EmpId ?? "", out var dbe) ? dbe.DeptSrtOrd : 999)
                            .Where(ord => ord > 0 && ord < 999)
                            .ToList();
                        canonSrtOrd = matchedOrders.Count > 0 ? matchedOrders.Min() : 999;
                    }

                    foreach (var emp in deptGrp)
                    {
                        emp.DeptName = canonName;
                        emp.DeptId = canonDeptId;
                        emp.DeptSrtOrd = canonSrtOrd;
                    }
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

        [HttpGet]
        public IActionResult PrintDepartmentBreakdown(int sctn = 2)
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

            var dbEmployees = LoadDbEmployees(out _);

            IEnumerable<PrSlryShtVw> filtered;
            if (sctn == 0)
            {
                filtered = model.Employees.Where(e => e.InHouse);
            }
            else if (sctn == 1)
            {
                filtered = model.Employees.Where(e => !e.InHouse);
            }
            else
            {
                filtered = model.Employees;
            }

            // Order employees within each department by Employee Sort Order (Srt_Ord) then Excel row order
            var displayEmployees = filtered
                .OrderBy(e => e.DeptSrtOrd > 0 ? e.DeptSrtOrd : 999)
                .ThenBy(e => e.DeptName ?? "")
                .ThenBy(e => dbEmployees.TryGetValue(e.EmpId ?? "", out var dbe) && dbe.EmpSrtOrd > 0 ? dbe.EmpSrtOrd : int.MaxValue)
                .ThenBy(e => e.Index)
                .ToList();

            byte[] pdfBytes = DepartmentSalarySheetPdfReport.GenerateReport(
                compName,
                model.Year,
                model.Month,
                displayEmployees,
                model.InternalStaffNet,
                model.OutsourcedStaffNet,
                sctn,
                _webHostEnvironment.WebRootPath);

            string sctnName = sctn == 0 ? "InHouse" : (sctn == 1 ? "Outsourced" : "AllStaff");
            Response.Headers["Content-Disposition"] = $"inline; filename=DepartmentSalarySheet_{sctnName}_{model.Year}_{model.Month}.pdf";
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

        private static void AddColOnce(Dictionary<string, List<int>> map, string key, int colIdx)
        {
            if (!map.ContainsKey(key))
            {
                map[key] = new List<int> { colIdx };
            }
        }

        private static void AddColMulti(Dictionary<string, List<int>> map, string key, int colIdx)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<int>();
                map[key] = list;
            }
            if (!list.Contains(colIdx))
            {
                list.Add(colIdx);
            }
        }

        private static bool HasColumn(Dictionary<string, List<int>> map, string key)
        {
            return map.TryGetValue(key, out var list) && list != null && list.Count > 0;
        }

        private static Dictionary<string, List<int>> BuildColumnMapping(DataColumnCollection columns)
        {
            var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < columns.Count; c++)
            {
                string raw = columns[c].ColumnName?.Trim() ?? "";
                string norm = Normalize(raw);
                if (string.IsNullOrEmpty(norm)) continue;

                // 1. Employee Code / Number
                if (norm == "employeeno" || norm == "employeenumber" || norm == "empno" || norm == "empid" || norm == "code" || norm == "employeecode")
                {
                    AddColOnce(map, "empid", c);
                }
                // 2. Employee Name (strictly exclude Account Holder Name / Bank Name)
                else if (norm == "employeename" || norm == "empname" || norm == "fullname" || norm == "name")
                {
                    AddColOnce(map, "fullname", c);
                }
                // 3. Designation
                else if (norm == "designation" || norm == "desig" || norm == "jobtitle")
                {
                    AddColOnce(map, "designation", c);
                }
                // 4. Department
                else if (norm == "department" || norm == "dept" || norm == "departmentname")
                {
                    AddColOnce(map, "department", c);
                }
                // 5. InHouse / Outsourced Tag
                else if (norm.Contains("inhouse") || norm.Contains("outsourced") || norm == "stafftype")
                {
                    AddColOnce(map, "inhouse", c);
                }
                // 6. Period
                else if (norm == "period" || norm == "payperiod" || norm == "salaryperiod")
                {
                    AddColOnce(map, "period", c);
                }
                // 7. Payment Mode
                else if (norm == "paymentmode" || norm == "paymode")
                {
                    AddColOnce(map, "paymode", c);
                }
                // 8. Absent / Loss Of Pay Days (NEVER map Loss Of Pay days to NoPay deduction!)
                else if (norm == "lossofpay" || norm == "lossofpaydays" || norm == "lop" || norm == "lopdays"
                      || norm == "absent" || norm == "absentdays" || norm == "abfct")
                {
                    AddColMulti(map, "absent", c);
                }
                // 9. Skip non-monetary day count columns (e.g. Base Days, New Joinee Arrear Days, Effective Paid Days)
                else if (norm == "basedays" || norm == "effectivepaiddays" || norm == "paiddays"
                      || norm == "workingdays" || norm.EndsWith("arreardays"))
                {
                    continue;
                }
                // 10. Monetary Arrear columns (Basic Arrear, HRA Arrear, Transport Allowance Arrear, Mobile Allowance Arrear, etc.)
                //     Must be checked BEFORE fixed allowance names so arrears sum into Other Payment (OthrPmt)!
                else if (norm.Contains("arrear") && !norm.Contains("day"))
                {
                    AddColMulti(map, "otherpmt", c);
                }
                // 11. Basic Salary
                else if (norm == "basic" || norm == "basicsalary" || norm == "basicpay")
                {
                    AddColOnce(map, "basic", c);
                }
                // 12. HRA / Housing Allowance
                else if (norm == "hra" || norm == "housing" || norm == "housingallowance" || norm == "hrallowance" || norm == "houserentallowance")
                {
                    AddColOnce(map, "housing", c);
                }
                // 13. Transport Allowance
                else if (norm == "tra" || norm == "transport" || norm == "transportallowance" || norm == "transportation" || norm == "transportationallowance" || norm == "trans")
                {
                    AddColOnce(map, "transport", c);
                }
                // 14. Position Allowance
                else if (norm == "position" || norm == "positionallowance" || norm == "posit")
                {
                    AddColOnce(map, "position", c);
                }
                // 15. Mobile Allowance
                else if (norm == "mobile" || norm == "mobileallowance" || norm == "mob")
                {
                    AddColOnce(map, "mobile", c);
                }
                // 16. Project Allowance
                else if (norm == "project" || norm == "projectallowance" || norm == "proj")
                {
                    AddColOnce(map, "project", c);
                }
                // 17. Medical Allowance
                else if (norm == "medical" || norm == "medicalallowance" || norm == "medallowance")
                {
                    AddColOnce(map, "medical", c);
                }
                // 18. Special Allowance
                else if (norm == "special" || norm == "specialallowance" || norm == "spcallowance")
                {
                    AddColOnce(map, "special", c);
                }
                // 19. Fixed Allowance
                else if (norm == "fixedallowance" || norm == "fixedalw")
                {
                    AddColMulti(map, "fixedalw", c);
                }
                // 20. Other Allowance
                else if (norm == "otherallowance" || norm == "otherallowances" || norm == "otheralw")
                {
                    AddColMulti(map, "otheralw", c);
                }
                // 21. Fixed Monthly Earnings / Total Fixed Salary (exclude Fixed Monthly Costs w/ Employer Contributions)
                else if (norm == "fixedmonthlyearnings" || norm == "totalfixedsalary" || norm == "fixedsalary")
                {
                    AddColOnce(map, "totalfixed", c);
                }
                // 22. Incentive
                else if (norm == "incentive" || norm == "incentives" || norm == "incent" || norm == "incentiveamount")
                {
                    AddColMulti(map, "incentive", c);
                }
                // 23. Overtime
                else if (norm == "overtime" || norm == "ot" || norm == "otamt" || norm == "otamount" || norm == "overtimeallowance")
                {
                    AddColMulti(map, "ot", c);
                }
                // 24. Bonus
                else if (norm == "bonus" || norm == "bonusamt" || norm == "bonusamount")
                {
                    AddColMulti(map, "bonus", c);
                }
                // 25. Other Payments / Adjustments / Total Reimbursements (exclude Business Expense Reimbursements outside Gross)
                else if (norm == "otherpayments" || norm == "otherpayment" || norm == "otherpmt"
                      || norm == "othradjstmnt" || norm == "othradjstmntqr" || norm == "totalreimbursements")
                {
                    AddColMulti(map, "otherpmt", c);
                }
                // 26. Gross Pay / Gross Salary (preferred over Total Earnings)
                else if (norm == "grosspay" || norm == "grosssalary" || norm == "gross")
                {
                    AddColOnce(map, "gross", c);
                }
                else if (norm == "totalearnings")
                {
                    AddColOnce(map, "totalearnings", c);
                }
                // 27. Salary Advance
                else if (norm == "salaryadvance" || norm == "sladv" || norm == "salaryadv")
                {
                    AddColMulti(map, "sladv", c);
                }
                // 28. Staff Loan
                else if (norm == "staffloan" || norm == "staffloans" || norm == "stflnpmt" || norm == "loan" || norm == "sladvloan")
                {
                    AddColMulti(map, "stfloan", c);
                }
                // 29. Explicit No-Pay Monetary Deduction ONLY (never Loss Of Pay days)
                else if (norm == "nopay" || norm == "nopayamt" || norm == "nopayamount" || norm == "nopaydeduction")
                {
                    AddColMulti(map, "nopay", c);
                }
                // 30. Other Deductions / Recoveries
                else if (norm == "otherdeductions" || norm == "otherdeduction" || norm == "otherdeduct"
                      || norm == "othrdeductionsqr" || norm == "othrdeductions"
                      || norm == "otherrecovery" || norm == "othrrcvry")
                {
                    AddColMulti(map, "otherdeduct", c);
                }
                // 31. Total Deductions
                else if (norm == "totaldeductions" || norm == "totaldeduct" || norm == "totdeduct")
                {
                    AddColOnce(map, "totaldeduct", c);
                }
                // 32. Net Pay / Net Salary
                else if (norm == "netpay" || norm == "netsalary" || norm == "totalnet" || norm == "net")
                {
                    AddColOnce(map, "net", c);
                }
                // 33. Bank Transfer / Cash Payment
                else if (norm == "banktransfer" || norm == "bankamount")
                {
                    AddColOnce(map, "bankamt", c);
                }
                else if (norm == "cashpayment" || norm == "cashamount")
                {
                    AddColOnce(map, "cashamt", c);
                }
                // 34. Remarks
                else if (norm == "cfremarks" || norm == "remarks" || norm == "remark" || norm == "notes" || norm == "note")
                {
                    AddColOnce(map, "remarks", c);
                }
            }

            return map;
        }

        private static string GetStringVal(DataRow row, Dictionary<string, List<int>> colMap, string key)
        {
            if (colMap.TryGetValue(key, out var list) && list != null && list.Count > 0)
            {
                int idx = list[0];
                if (idx >= 0 && idx < row.Table.Columns.Count)
                {
                    return row[idx]?.ToString()?.Trim() ?? "";
                }
            }
            return "";
        }

        private static decimal GetDecimalVal(DataRow row, Dictionary<string, List<int>> colMap, string key)
        {
            if (!colMap.TryGetValue(key, out var list) || list == null || list.Count == 0)
            {
                return 0m;
            }

            decimal total = 0m;
            foreach (int idx in list)
            {
                if (idx >= 0 && idx < row.Table.Columns.Count)
                {
                    string s = row[idx]?.ToString()?.Trim().Replace(",", "") ?? "";
                    if (decimal.TryParse(s, out decimal d))
                    {
                        total += d;
                    }
                }
            }
            return total;
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

        private Dictionary<string, EmpDbInfo> LoadDbEmployees(out Dictionary<string, DeptDbInfo> departments)
        {
            var dict = new Dictionary<string, EmpDbInfo>(StringComparer.OrdinalIgnoreCase);
            departments = new Dictionary<string, DeptDbInfo>(StringComparer.OrdinalIgnoreCase);
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

                using (var deptCmd = conn.CreateCommand())
                {
                    deptCmd.CommandText = "SELECT Dept_Id, Name_Eng, ISNULL(Srt_Ord, 999) AS Srt_Ord FROM ORG_Department";
                    using var dr = deptCmd.ExecuteReader();
                    while (dr.Read())
                    {
                        string dName = dr["Name_Eng"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(dName) && !departments.ContainsKey(dName))
                        {
                            departments[dName] = new DeptDbInfo
                            {
                                DeptId = dr["Dept_Id"] != DBNull.Value ? Convert.ToInt32(dr["Dept_Id"]) : 0,
                                NameEng = dName,
                                SrtOrd = dr["Srt_Ord"] != DBNull.Value ? Convert.ToInt32(dr["Srt_Ord"]) : 999
                            };
                        }
                    }
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT emp.Emp_Id,
                           ISNULL(emp.Srt_Ord, 0) AS EmpSrtOrd,
                           emp.First_Name + ' ' + ISNULL(emp.Last_Name, '') AS FullName,
                           ds.Name_Eng AS DesigName,
                           ISNULL(dp.Dept_Id, 0) AS DeptId,
                           ISNULL(dp.Srt_Ord, 999) AS DeptSrtOrd,
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
                            EmpSrtOrd = r["EmpSrtOrd"] != DBNull.Value ? Convert.ToInt32(r["EmpSrtOrd"]) : 0,
                            FullName = r["FullName"]?.ToString()?.Trim() ?? "",
                            DesigName = r["DesigName"]?.ToString()?.Trim() ?? "",
                            DeptId = r["DeptId"] != DBNull.Value ? Convert.ToInt32(r["DeptId"]) : 0,
                            DeptSrtOrd = r["DeptSrtOrd"] != DBNull.Value ? Convert.ToInt32(r["DeptSrtOrd"]) : 999,
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
            public int EmpSrtOrd { get; set; }
            public string FullName { get; set; } = "";
            public string DesigName { get; set; } = "";
            public int DeptId { get; set; }
            public int DeptSrtOrd { get; set; }
            public string DeptName { get; set; } = "";
            public bool InHouse { get; set; }
        }

        private class DeptDbInfo
        {
            public int DeptId { get; set; }
            public string NameEng { get; set; } = "";
            public int SrtOrd { get; set; }
        }

        #endregion
    }
}
