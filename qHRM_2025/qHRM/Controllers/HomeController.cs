using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.Identity.Data;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Models;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Notify.Data;
using qHRM.Data;
using qHRM.Areas.Attendance.Data;
using Microsoft.AspNetCore.Hosting;
using Syncfusion.Drawing;
using qHRM.Areas.Payroll.Data;
using Microsoft.Extensions.Caching.Memory;
using qHRM.Areas.General.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Attendance.Models;
using AspNetCore.Reporting;
using static qHRM.Areas.Attendance.Data.AttnContext;
using qHRM.Areas.Leave.Data;
using static qHRM.Areas.Payroll.Data.PayrollContext;
using static qHRM.Areas.Employee.Data.EmployeeContext;
using Syncfusion.EJ2.Linq;

namespace qHRM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<qHRMUser> _signInManager;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly SysAdminContext _sysAdminContext;
        private readonly EmployeeContext _empContext;
        private readonly NotifyContext _notifyContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LoginContext _loginContext;
        private readonly DashboardContext _dashboardContext;
        private readonly AttnContext _attnContext;
        private readonly IHostingEnvironment _host;
        private readonly PayrollContext _pyrollContext;
        private readonly OrganizationContext _orgContext;
        private readonly EntityContext _entityContext;
        private readonly IMemoryCache _cache;
        private readonly PayrollContext _payrollContext;
        private readonly LeaveContext _leaveContext;
        private readonly LeaveBalanceService _leaveBalanceService;
        private readonly LeaveWorkflowService _lvWfService;


        public HomeController(ILogger<HomeController> logger,
                              SignInManager<qHRMUser> signInManager,
                              AdmUserGrpPrvContext prvContext,
                              SysAdminContext sysAdminContext,
                              EmployeeContext empContext,
                              NotifyContext notifyContext,
                              IHttpContextAccessor httpContextAccessor,
                              LoginContext loginContext,
                              DashboardContext dashboardContext,
                              AttnContext attnContext,
                              IHostingEnvironment host,
                              PayrollContext pyrollContext,
                              IMemoryCache memoryCache,
                              OrganizationContext orgContext,
                              EntityContext entityContext,
                              PayrollContext payrollContext,
                              LeaveContext leaveContext,
                              LeaveBalanceService leaveBalanceService = null,
                              LeaveWorkflowService lvWfService = null)
        {
            _logger = logger;
            _signInManager = signInManager;
            _prvContext = prvContext;
            _sysAdminContext = sysAdminContext;
            _empContext = empContext;
            _notifyContext = notifyContext;
            _httpContextAccessor = httpContextAccessor;
            _loginContext = loginContext;
            _dashboardContext = dashboardContext;
            _attnContext = attnContext;
            _host = host;
            _pyrollContext = pyrollContext;
            _cache = memoryCache;
            _orgContext = orgContext;
            _entityContext = entityContext;
            _payrollContext = payrollContext;
            _leaveContext = leaveContext;
            _leaveBalanceService = leaveBalanceService;
            _lvWfService = lvWfService;

        }

        Color gray = Color.FromArgb(255, 224, 224, 224);
        Color black = Color.FromArgb(255, 0, 0, 0);
        Color white = Color.FromArgb(255, 255, 255, 255);
        Color violet = Color.FromArgb(255, 151, 108, 174);


        public partial class Year
        {
            // Support both PascalCase and camelCase for runtime binder and views
            public int Value { get; set; }
            public string Text { get; set; }

            public int value { get => Value; set => Value = value; }
            public string text { get => Text; set => Text = value; }
        }

        public partial class Month
        {
            // Support both PascalCase and camelCase for runtime binder and views
            public int Value { get; set; }
            public string Text { get; set; }

            public int value { get => Value; set => Value = value; }
            public string text { get => Text; set => Text = value; }
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("[DEBUG] Index: Entered HomeController.Index()");
            string usrAuthId = null;
            string mnulUsrLog = "";

            try
            {
                mnulUsrLog = HttpContext.Session.GetString("mnulUsrLog");
                _logger.LogInformation($"[DEBUG] Index: mnulUsrLog = '{mnulUsrLog}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG] Index: Failed to read mnulUsrLog from session.");
            }

            if (mnulUsrLog != null)
            {
                _logger.LogInformation("[DEBUG] Index: mnulUsrLog is NOT null. Proceeding to LoadDashBoardsAsync and logging IN.");
                try
                {
                    await LoadDashBoardsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DEBUG] Index: LoadDashBoardsAsync() threw an exception.");
                }
                
                try
                {
                    string _sessionId = _httpContextAccessor.HttpContext.Session.Id;
                    RecordUsrLog("IN", _sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DEBUG] Index: RecordUsrLog or Session ID fetch threw an exception.");
                }
                
                return View();
            }
            else
            {
                _logger.LogInformation("[DEBUG] Index: mnulUsrLog is null. Clearing session and checking authenticated user.");
                HttpContext.Session.Clear(); //Clear Sessions
                try
                {
                    if (string.IsNullOrEmpty(usrAuthId))
                    {
                        try
                        {
                            usrAuthId = User.Identity?.Name;
                            if (string.IsNullOrEmpty(usrAuthId))
                            {
                                usrAuthId = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name 
                                            ?? System.Environment.UserName;
                            }
                        }
                        catch { }
                    }
                    _logger.LogInformation($"[DEBUG] Index: Obtained User.Identity.Name = '{usrAuthId}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DEBUG] Index: Exception while getting User.Identity.Name. Redirecting to Login.");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                if (!string.IsNullOrEmpty(usrAuthId))
                {
                    if (usrAuthId.Contains("\\"))
                    {
                        usrAuthId = usrAuthId.Substring(usrAuthId.LastIndexOf('\\') + 1);
                    }
                    usrAuthId = usrAuthId.Replace("/DOMAIN", "").Replace("DOMAIN/", "");
                    //usrAuthId = "lijo@qcs-qatar.com";

                    _logger.LogInformation($"[DEBUG] Index: usrAuthId after clean domain = '{usrAuthId}'");
                    
                    List<Login> _lstUsr = null;
                    try
                    {
                        _lstUsr = await _loginContext.GetLoginSystemAsync("_login", 0, usrAuthId, "");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] Index: Exception querying GetLoginSystemAsync.");
                    }

                    if (_lstUsr != null && _lstUsr.Count != 0)
                    {
                        var targetUser = _lstUsr.First();
                        
                        List<mvGroupPrivilege> _lstPrv = null;
                        try
                        {
                            _lstPrv = await _prvContext.GetAsync("_user_priv_group", targetUser.Grp_Id.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] Index: Exception querying privileges.");
                        }

                        if (_lstPrv != null && _lstPrv.Count != 0)
                        {
                            foreach (var itm in _lstPrv)
                            {
                                HttpContext.Session.SetString(itm.PrvCode, "y");//Adding Privileges to the Sessions
                            }

                            HttpContext.Session.SetInt32("userID", targetUser.Usr_Id.Value);
                            HttpContext.Session.SetInt32("userEmpSysId", targetUser.Emp_Sys_Id);
                            HttpContext.Session.SetString("userName", targetUser.Usr_Name);
                            HttpContext.Session.SetString("userFullName", targetUser.Full_Name);
                            HttpContext.Session.SetInt32("userEntId", targetUser.Ent_Id.Value);
                            HttpContext.Session.SetString("userPhoto", string.IsNullOrWhiteSpace(targetUser.Photo_Name) ? "user.png" : targetUser.Photo_Name);
                            HttpContext.Session.SetInt32("userGrpId", targetUser.Grp_Id.Value); // Group ID
                            HttpContext.Session.SetInt32("userDptId", targetUser.Dept_Id.Value); // Dpt ID
                            HttpContext.Session.SetString("userDashboard", (targetUser.Dshbrd_Indx_Txt ?? "").Trim()); //Dashboard Index
                            _logger.LogInformation("[DEBUG] Index: User session variables populated.");

                            try
                            {
                                await LoadDashBoardsAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[DEBUG] Index: LoadDashBoardsAsync() inside auth block threw exception.");
                            }
                            
                            try
                            {
                                string _sessionId = _httpContextAccessor.HttpContext.Session.Id;
                                RecordUsrLog("IN", _sessionId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[DEBUG] Index: RecordUsrLog inside auth block threw exception.");
                            }
                            
                            return View();
                        }
                        else
                        {
                            await _signInManager.SignOutAsync();
                            HttpContext.Session.Clear(); //Clear Sessions
                            return RedirectToPage("/Account/Login", new { area = "Identity" });
                        }
                    }
                    else
                    {
                        await _signInManager.SignOutAsync();
                        HttpContext.Session.Clear(); //Clear Sessions
                        return RedirectToPage("/Account/Login", new { area = "Identity" });
                    }
                }
            }

            return View();
        }

        public async Task LoadDashBoardsAsync()
        {
            _logger.LogInformation("[DEBUG] LoadDashBoardsAsync: Entered.");
            string userDashboard = (HttpContext.Session.GetString("userDashboard") ?? "").Trim();
            int usrId = HttpContext.Session.GetInt32("userID") ?? 0;
            int empSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;
            int userEntId = HttpContext.Session.GetInt32("userEntId") ?? 1;
            int userDptId = HttpContext.Session.GetInt32("userDptId") ?? 0;
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;

            //--- 1. HR Admin Dashboard (_hr_index.cshtml) ---//
            if (userDashboard == "_hr_index.cshtml")
            {
                await Task.Run(async () =>
                {
                    ViewBag.LoggedEmpSysId = empSysId;
                    ViewBag.userGrpId = userGrpId;

                    try
                    {
                        var nltyEmpFullSmry = _dashboardContext.GetNltyEmpFullSmryByEntId(userEntId > 0 ? userEntId : 1);
                        ViewBag.dsAllocated = nltyEmpFullSmry.AllocatedNltyEmpList;
                        ViewBag.dsActual = nltyEmpFullSmry.ActualNltyEmpList.OrderBy(o => o.StfRatio);
                        ViewBag.dsNltyEmpSmry = nltyEmpFullSmry.NltyEmpCntSmryList.OrderByDescending(o => o.ActlEmpCnt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetNltyEmpFullSmryByEntId.");
                    }

                    try
                    {
                        ViewBag.dsDeptEmpSmry = _dashboardContext.GetDeptEmpSmry(userEntId > 0 ? userEntId : 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetDeptEmpSmry.");
                    }

                    try
                    {
                        var y = _attnContext.GetDailyAttendanceSmry(DateTime.Today.AddDays(-1), 8);
                        ViewBag.dsDlyAtndSmry = y.Any() ? y : null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetDailyAttendanceSmry.");
                    }

                    try
                    {
                        var x = _attnContext.GetEmpDlyAttnSmry("DAILY_SMRY", false, empSysId, DateTime.Today, DateTime.Today);
                        ViewBag.dsDayAttnSmry = x.Any() ? x : null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetEmpDlyAttnSmry.");
                    }

                    try
                    {
                        ViewBag.dsDayAttnDtl = _attnContext.GetHrEmpAttnCompackedVwByDate("_detail", DateTime.Today, "ALL").Where(z => z.Status == "Present");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetHrEmpAttnCompackedVwByDate.");
                        ViewBag.dsDayAttnDtl = null;
                    }

                    try
                    {
                        ViewBag.lstYear = GetYear();
                        ViewBag.lstMonth = GetMonths();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetYear/GetMonths.");
                    }

                    // --- Pending Leave Approvals Count ---
                    try
                    {
                        if (_lvWfService != null && empSysId > 0)
                        {
                            var pendingList = await _lvWfService.GetPendingApprovalsForUserAsync(empSysId);
                            ViewBag.PendingLeaveCount = pendingList?.Count ?? 0;
                        }
                        else
                        {
                            ViewBag.PendingLeaveCount = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error fetching pending leaves count.");
                        ViewBag.PendingLeaveCount = 0;
                    }

                    // --- Attendance Section: Subordinate hierarchy employees & Initial 7 days ---
                    try
                    {
                        var hierarchyEmps = _empContext.GetHierarchySubordinates(empSysId, userGrpId).ToList();
                        ViewBag.lstHierarchyEmployees = hierarchyEmps;
                        ViewBag.InitAttendanceSummary = _attnContext.GetEmployee7DaysAttendance(empSysId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error loading hierarchy employees or attendance summary.");
                        ViewBag.lstHierarchyEmployees = new List<HrEmpGen>();
                        ViewBag.InitAttendanceSummary = null;
                    }

                    // --- Company Monthly Salary Section: Default to Latest Processed / Active Period ---
                    try
                    {
                        int entId = userEntId > 0 ? userEntId : 1;
                        var latestHdr = _payrollContext.GetLstSlryPrcHdr(entId);
                        DateTime prevMonth = DateTime.Today.AddMonths(-1);
                        int defYear = latestHdr != null ? latestHdr.EftYear : prevMonth.Year;
                        int defMonth = latestHdr != null ? latestHdr.EftMonth : prevMonth.Month;

                        ViewBag.DefaultSalaryYear = defYear;
                        ViewBag.DefaultSalaryMonth = defMonth;
                        ViewBag.SelectedMonth = defMonth;
                        ViewBag.InitSalarySummary = _payrollContext.GetCompanySalarySummary(defYear, defMonth, entId, "ALL");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error loading initial company salary summary.");
                        ViewBag.InitSalarySummary = null;
                    }
                });
            }
            //--- 2. Manager or Employee Dashboard (_mgr_index.cshtml / _emp_index.cshtml / _emp_index_.cshtml) ---//
            else if (userDashboard == "_mgr_index.cshtml" || userDashboard == "_emp_index.cshtml" || userDashboard == "_emp_index_.cshtml")
            {
                await Task.Run(async () =>
                {
                    int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                    bool showAllEmp = _empContext.GetUserShowAllEmployees(empSysId, userGrpId);
                    ViewBag.ShowAllEmployees = showAllEmp;
                    ViewBag.LoggedEmpSysId = empSysId;
                    ViewBag.userGrpId = userGrpId;

                    try
                    {
                        var _objEmp = _empContext.GetEmpView("ALL", 0, usrId);
                        var _loggedEmp = _objEmp.FirstOrDefault(x => x.EmpSysId == empSysId);
                        if (_loggedEmp != null)
                        {
                            ViewBag.Desgntn = _loggedEmp.DesigName;
                            ViewBag.EmpDpt = _loggedEmp.DeptName;
                            ViewBag.EmpDOB = _loggedEmp.Dob.ToString("dd/MM/yyyy");
                            ViewBag.EmpJD = _loggedEmp.JoinedDate.ToString("dd/MM/yyyy");
                            ViewBag.EmpSrvcYrs = _loggedEmp.SrvcInYrs;
                            ViewBag.DeptName = _loggedEmp.DeptName;
                            ViewBag.DptName = _loggedEmp.DeptName;
                        }

                        if (userDashboard == "_mgr_index.cshtml")
                        {
                            ViewBag.lstEmployee = _objEmp.OrderBy(o => o.FullName);
                            ViewBag.empCount = _objEmp.Count();
                        }
                        else if (!showAllEmp && empSysId > 0)
                        {
                            ViewBag.lstEmployee = _objEmp.Where(x => x.EmpSysId == empSysId).OrderBy(o => o.FullName);
                            ViewBag.empCount = 1;
                        }
                        else
                        {
                            ViewBag.lstEmployee = _objEmp.OrderBy(o => o.FullName);
                            ViewBag.empCount = _objEmp.Count();
                        }

                        ViewBag.lstEmp = _empContext.GetActvEmpNameLst(userEntId > 0 ? userEntId : 1, empSysId, userGrpId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying employee views.");
                    }

                    try
                    {
                        var dsTdyAttn = _attnContext.GetHrEmpAttnCompackedVwByDate("TODAY_DTL", DateTime.Today, "ALL");
                        ViewBag.dsTdyAttn = dsTdyAttn;
                        var _objTodayIn = dsTdyAttn.FirstOrDefault(x => x.EmpSysId == empSysId);
                        ViewBag.todayIN = (_objTodayIn != null && !string.IsNullOrEmpty(_objTodayIn.InTime)) ? _objTodayIn.InTime : "Not Punched";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying today's attendance.");
                        ViewBag.todayIN = "Not Punched";
                    }

                    try
                    {
                        ViewBag.dsYstrAttn = _attnContext.GetFullHrEmpAtndcSmry("A", 0, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1), false, empSysId).ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying yesterday's attendance.");
                        ViewBag.dsYstrAttn = new List<dynamic>();
                    }

                    try
                    {
                        ViewBag.dsYstrAttnSmry = _attnContext.GetEmpDlyAttnSmry("DAILY_SMRY", false, empSysId, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying yesterday's attendance summary.");
                    }

                    List<int> psntIds = new List<int> { 1, 5, 13, 14 };
                    List<int> lateIds = new List<int> { 11, 12, 112 };

                    try
                    {
                        var dsAttnSmry = _attnContext.GetEmpDlyAttnSmry("DAILY_SMRY", true, empSysId, DateTime.Today.AddDays(-30), DateTime.Today);
                        ViewBag.AttdFromDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                        ViewBag.AttdToDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

                        ViewBag.absntCount = dsAttnSmry.FirstOrDefault(x => x.RecId == 0)?.Count ?? 0;
                        ViewBag.presentCount = dsAttnSmry.FirstOrDefault(x => psntIds.Contains(x.RecId))?.Count ?? 0;
                        ViewBag.lateCount = dsAttnSmry.FirstOrDefault(x => lateIds.Contains(x.RecId))?.Count ?? 0;
                        ViewBag.leaveCount = dsAttnSmry.FirstOrDefault(x => x.RecId == 2)?.Count ?? 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in attendance summary queries.");
                        ViewBag.absntCount = 0;
                        ViewBag.presentCount = 0;
                        ViewBag.lateCount = 0;
                        ViewBag.leaveCount = 0;
                    }

                    try
                    {
                        bool isSngl = (userDashboard == "_mgr_index.cshtml") ? false : !showAllEmp;
                        ViewBag.leaveEmpCount = _leaveContext.GetLeaveRegister(isSngl, empSysId).Count(x => x.StDate <= DateTime.Today && x.EndDate >= DateTime.Today);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying leave register.");
                        ViewBag.leaveEmpCount = 0;
                    }

                    try
                    {
                        ViewBag.WFH = _attnContext.GetHrEmpAttendExeption(false, empSysId).Count(x => x.StDate <= DateTime.Today && x.EndDate >= DateTime.Today);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying exception count.");
                        ViewBag.WFH = 0;
                    }

                    try
                    {
                        var empTrnOvr = _empContext.GetEmpCntSmry("DPT", userDptId, DateTime.Today.AddDays(-90), DateTime.Today);
                        ViewBag.InCnt = empTrnOvr.InCnt;
                        ViewBag.OutCnt = empTrnOvr.OutCnt;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying turnover.");
                        ViewBag.InCnt = 0;
                        ViewBag.OutCnt = 0;
                    }

                    if (userDashboard == "_mgr_index.cshtml")
                    {
                        // --- Hierarchy Employees & Initial 7-Day Attendance ---
                        try
                        {
                            var hierarchyEmps = _empContext.GetHierarchySubordinates(empSysId, userGrpId).ToList();
                            ViewBag.lstHierarchyEmployees = hierarchyEmps;
                            if (hierarchyEmps.Count > 0)
                            {
                                ViewBag.empCount = hierarchyEmps.Count;
                            }
                            ViewBag.InitAttendanceSummary = _attnContext.GetEmployee7DaysAttendance(empSysId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error loading hierarchy employees or attendance summary in manager dashboard.");
                            ViewBag.lstHierarchyEmployees = new List<HrEmpGen>();
                            ViewBag.InitAttendanceSummary = null;
                        }

                        // --- Pending Leave Approvals Count ---
                        try
                        {
                            if (_lvWfService != null && empSysId > 0)
                            {
                                var pendingList = await _lvWfService.GetPendingApprovalsForUserAsync(empSysId);
                                ViewBag.PendingLeaveCount = pendingList?.Count ?? 0;
                            }
                            else
                            {
                                ViewBag.PendingLeaveCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error fetching pending leaves count in manager dashboard.");
                            ViewBag.PendingLeaveCount = 0;
                        }

                        // --- Card Controls: Nationality, Department, & Daily Attendance datasets ---
                        try
                        {
                            var nltyEmpFullSmry = _dashboardContext.GetNltyEmpFullSmryByEntId(userEntId > 0 ? userEntId : 1);
                            ViewBag.dsAllocated = nltyEmpFullSmry.AllocatedNltyEmpList;
                            ViewBag.dsActual = nltyEmpFullSmry.ActualNltyEmpList.OrderBy(o => o.StfRatio);
                            ViewBag.dsNltyEmpSmry = nltyEmpFullSmry.NltyEmpCntSmryList.OrderByDescending(o => o.ActlEmpCnt);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetNltyEmpFullSmryByEntId for manager dashboard.");
                        }

                        try
                        {
                            ViewBag.dsDeptEmpSmry = _dashboardContext.GetDeptEmpSmry(userEntId > 0 ? userEntId : 1);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetDeptEmpSmry for manager dashboard.");
                        }

                        try
                        {
                            var y = _attnContext.GetDailyAttendanceSmry(DateTime.Today.AddDays(-1), 8);
                            ViewBag.dsDlyAtndSmry = y.Any() ? y : null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetDailyAttendanceSmry for manager dashboard.");
                        }

                        try
                        {
                            var x = _attnContext.GetEmpDlyAttnSmry("DAILY_SMRY", false, empSysId, DateTime.Today, DateTime.Today);
                            ViewBag.dsDayAttnSmry = x.Any() ? x : null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetEmpDlyAttnSmry for manager dashboard.");
                        }

                        try
                        {
                            ViewBag.dsDayAttnDtl = _attnContext.GetHrEmpAttnCompackedVwByDate("_detail", DateTime.Today, "ALL").Where(z => z.Status == "Present");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error in GetHrEmpAttnCompackedVwByDate for manager dashboard.");
                            ViewBag.dsDayAttnDtl = null;
                        }
                    }

                    if ((userDashboard == "_emp_index.cshtml" || userDashboard == "_emp_index_.cshtml") && _leaveBalanceService != null && empSysId > 0)
                    {
                        try
                        {
                            var leaveBalances = await _leaveBalanceService.GetEmployeeLeaveBalancesAsync(empSysId, DateTime.Today.Year);
                            ViewBag.EmpLeaveBalances = leaveBalances;
                            ViewBag.EmpTotalLeaveAvbl = leaveBalances?.Sum(x => x.Remaining) ?? 0;
                            ViewBag.EmpAnnualLeaveAvbl = leaveBalances?.FirstOrDefault(x => x.LeaveTypeCode == "LV_ANL" || (x.LeaveTypeName != null && x.LeaveTypeName.Contains("Annual")))?.Remaining ?? 0;
                            ViewBag.EmpSickLeaveAvbl = leaveBalances?.FirstOrDefault(x => x.LeaveTypeCode == "LV_SICK" || (x.LeaveTypeName != null && x.LeaveTypeName.Contains("Sick")))?.Remaining ?? 0;
                            ViewBag.EmpCasualLeaveAvbl = leaveBalances?.FirstOrDefault(x => x.LeaveTypeCode == "LV_CSL" || (x.LeaveTypeName != null && x.LeaveTypeName.Contains("Casual")))?.Remaining ?? 0;

                            var balSummary = await _leaveBalanceService.GetEmployeeBalanceSummaryAsync(empSysId, DateTime.Today.Year);
                            ViewBag.EmpLeaveBalanceSummary = balSummary;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error fetching employee leave balances.");
                            ViewBag.EmpLeaveBalances = new List<qHRM.Areas.Leave.Models.EmpLeaveBalanceViewModel>();
                        }
                    }

                    try
                    {
                        var objSlry = _payrollContext.GetHrPrEmpSlryStrucViewsByEntId(userEntId, DateTime.Today, false, empSysId);
                        ViewBag.Nt_Slry = "QR. " + String.Format("{0:n0}", objSlry.Sum(x => x.Nt_Slry));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error querying salary structure.");
                        ViewBag.Nt_Slry = "QR. 0";
                    }
                });
            }
            //--- 3. OS Admin Dashboard (_osadm_index.cshtml) ---//
            else if (userDashboard == "_osadm_index.cshtml")
            {
                await Task.Run(async () =>
                {
                    int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
                    ViewBag.LoggedEmpSysId = empSysId;
                    ViewBag.userGrpId = userGrpId;

                    // --- Pending Leave Approvals Count ---
                    try
                    {
                        if (_lvWfService != null && empSysId > 0)
                        {
                            var pendingList = await _lvWfService.GetPendingApprovalsForUserAsync(empSysId);
                            ViewBag.PendingLeaveCount = pendingList?.Count ?? 0;
                        }
                        else
                        {
                            ViewBag.PendingLeaveCount = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error fetching pending leaves count for OS Admin.");
                        ViewBag.PendingLeaveCount = 0;
                    }

                    // --- Attendance Section: Subordinate hierarchy employees & Initial 7 days ---
                    try
                    {
                        var hierarchyEmps = _empContext.GetHierarchySubordinates(empSysId, userGrpId).ToList();
                        ViewBag.lstHierarchyEmployees = hierarchyEmps;
                        ViewBag.InitAttendanceSummary = _attnContext.GetEmployee7DaysAttendance(empSysId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error loading hierarchy employees or attendance summary for OS Admin.");
                        ViewBag.lstHierarchyEmployees = new List<HrEmpGen>();
                        ViewBag.InitAttendanceSummary = null;
                    }

                    // --- Leave Balances of Logged User ---
                    if (_leaveBalanceService != null && empSysId > 0)
                    {
                        try
                        {
                            var leaveBalances = await _leaveBalanceService.GetEmployeeLeaveBalancesAsync(empSysId, DateTime.Today.Year);
                            ViewBag.EmpLeaveBalances = leaveBalances;
                            var balSummary = await _leaveBalanceService.GetEmployeeBalanceSummaryAsync(empSysId, DateTime.Today.Year);
                            ViewBag.EmpLeaveBalanceSummary = balSummary;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DEBUG] LoadDashBoards: Error fetching employee leave balances for OS Admin.");
                            ViewBag.EmpLeaveBalances = new List<qHRM.Areas.Leave.Models.EmpLeaveBalanceViewModel>();
                        }
                    }
                });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
        }

        [HttpPost]
        public IActionResult PrintNltySmry()
        {
            try
            {
                var nltyEmpFullSmry = _dashboardContext.GetNltyEmpFullSmryByEntId(1);
                string mimetype = "";
                int extension = 1;
                var path = $"{this._host.WebRootPath}\\Reports\\rptNltySmry.rdlc";
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                LocalReport localReport = new LocalReport(path);
                localReport.AddDataSource("dsNltyEmpSmry", nltyEmpFullSmry.NltyEmpCntSmryList);
                localReport.AddDataSource("dsNltyEmpRatio", nltyEmpFullSmry.NltyEmpRatioList);
                var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);
                return File(result.MainStream, "application/pdf");
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<Year> GetYear()
        {            
            List<Year> lstYear = new List<Year>();
            int curYear = DateTime.Now.Year;
            for (int i = curYear + 1; i >= curYear - 4; i--)
            {
                Year _objYr = new Year { value = i, text = i.ToString() };
                lstYear.Add(_objYr);
            }
            return lstYear;
        }

        public IEnumerable<Month> GetMonths()
        {   
            List<Month> lstMonth = new List<Month>();                       
            for (int i = 1; i <= 12; i++)
            {
                lstMonth.Add(new Month { value = i, text = new DateTime(2000, i, 1).ToString("MMMM") });
            }
            return lstMonth;
        }

        [HttpGet]
        public JsonResult GetEmployeeByNtlyRtoId(int Id) // Name should be changed
        {
            int entId = (int)HttpContext.Session.GetInt32("userEntId");
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
            var result = _empContext.GetEmpViewGrpByNltyRtoId(entId, Id, userId);
            if (result.Count() > 0)
            {
                return Json(new { ok = true, data = result, message = "ok" });
            }
            else
            { return Json(new { ok = true, data = "", message = "ok" }); }
        }

        public JsonResult GetEmployeeByNtlyName(string Name)
        {
            int entId = (int)HttpContext.Session.GetInt32("userEntId");
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
            var result = _empContext.GetEmpViewGrpByNltyName(entId, Name, userId);
            if (result.Count() > 0)
            {
                return Json(new { ok = true, data = result, message = "ok" });
            }
            else
            { return Json(new { ok = true, data = "", message = "ok" }); }
        }

        [HttpGet]
        public JsonResult GetEmployeeByDpt(string Code)
        {
            try
            {
                int entId = (int)HttpContext.Session.GetInt32("userEntId");
                int userId = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                var dpt = _entityContext.GetDepartmentEntFullViews(entId).Where(x => x.DeptCode == Code).FirstOrDefault();

                var result = _empContext.GetEmpView("ACTV", 0, userId).Where(x => x.DeptId == dpt.DeptId).ToList();
                ViewBag.dsEmp = result;//test code
                if (result.Count() > 0)
                {
                    return Json(new { ok = true, data = result, message = "ok" });
                }
                else
                { return Json(new { ok = true, data = "", message = "ok" }); }
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public IActionResult PrintSalarySheet(int eftMnt, int eftYr)
        {
            try
            {
                int entId = (int)HttpContext.Session.GetInt32("userEntId");
                var _filename = _pyrollContext.GetSalarySheet(eftYr, eftMnt, entId);

                if (_filename !="0")
                    return Json(new { ok = true, data = _filename, message = "ok" });                
                else                
                    return Json(new { ok = true, data = 0, message = "norecs" });                
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = ex.Message, message = "failed" });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetCompanySalarySummary(int year, int month, string staffCat = "ALL")
        {
            try
            {
                int entId = HttpContext.Session.GetInt32("userEntId") ?? 1;
                if (entId <= 0) entId = 1;
                Console.WriteLine($"[DEBUG-SERVER] HomeController.GetCompanySalarySummary called: year={year}, month={month}, staffCat={staffCat}, entId={entId}");
                var summary = _payrollContext.GetCompanySalarySummary(year, month, entId, staffCat);
                return Json(new { ok = true, data = summary });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetEmployee7DayAttendance(int empSysId)
        {
            try
            {
                if (empSysId <= 0)
                    empSysId = HttpContext.Session.GetInt32("userEmpSysId") ?? 0;

                var data = _attnContext.GetEmployee7DaysAttendance(empSysId);
                return Json(new { ok = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        public JsonResult RecordUsrLog(string typ, string sessionId)
        {
            _logger.LogInformation($"[DEBUG] RecordUsrLog: Entered RecordUsrLog(typ='{typ}', sessionId='{sessionId}')");
            try
            {
                AdmUserLogs objUsrLog = new AdmUserLogs();
                int usrID = Convert.ToInt32(HttpContext.Session.GetInt32("userID"));
                _logger.LogInformation($"[DEBUG] RecordUsrLog: Retrieved userID from session: {usrID}");
                objUsrLog.UsrId = usrID;
                objUsrLog.SessionId = sessionId;
                if (typ == "IN")
                    objUsrLog.InTime = DateTime.Now;
                else
                    objUsrLog.OutTime = DateTime.Now;
                objUsrLog.RecStat = 1;
                objUsrLog.CrtDateTime = DateTime.Now;

                _logger.LogInformation($"[DEBUG] RecordUsrLog: Checking if session '{sessionId}' is already logged...");
                var loggedUsrLst = _sysAdminContext.GetLoggedUserList().Where(x => x.SessionId == sessionId);
                if (loggedUsrLst.Count() == 0)
                {
                    _logger.LogInformation("[DEBUG] RecordUsrLog: Session not found in LoggedUserList. Saving to DB...");
                    var result = _sysAdminContext.LogUserLogin(objUsrLog);
                    _logger.LogInformation($"[DEBUG] RecordUsrLog: Save succeeded with result: {result}");
                    return Json(new { ok = true, data = result, message = "ok" });
                }
                else
                {
                    _logger.LogInformation("[DEBUG] RecordUsrLog: Session already exists in LoggedUserList.");
                    return Json(new { ok = true, data = "", message = "ok" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG] RecordUsrLog: Exception occurred inside RecordUsrLog.");
                return Json(new { ok = false, message = ex.Message });
            }
        }

        #region Attendance

        [HttpGet]
        public JsonResult GetAtndcLogs(int refId, string sDate, string eDate)
        {
            var fromDate = DateTime.Now.AddDays(-8).Date;
            var toDate = DateTime.Now.AddDays(-1).Date; ;
            var schTyp = "";
            try
            {
                if (sDate ==null)
                    fromDate = DateTime.Now.AddDays(-8).Date;
                else
                    fromDate = Convert.ToDateTime(sDate);

                if (eDate == null)
                    toDate = DateTime.Now.AddDays(-1).Date;
                else
                    toDate = Convert.ToDateTime(eDate);

                if (refId == 0)
                    schTyp = "A";
                else
                    schTyp = "S";                
                             
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));

                var atndcDtl = _attnContext.GetFullHrEmpAtndcSmry(schTyp, refId, fromDate, toDate, false, empSysId).ToList();
                return Json(new { ok = true, data = atndcDtl, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        //[HttpGet]
        //public JsonResult GetTodayAtndcLogs()
        //{
        //    var fromDate = DateTime.Today;
        //    var toDate = DateTime.Today;
        //    //bool IsSnglEmp = false;
        //    //int empSysId = empId;
        //    try
        //    {
        //        //if (field != "TODAY_SMRY")
        //        //   fromDate = Convert.ToDateTime(sDate); toDate = Convert.ToDateTime(eDate);

        //        //if (empId != 0)                
        //        //    IsSnglEmp = true;               
        //        //else                
        //        //    empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));                               

        //        var result = _attnContext.GetHrEmpAttnCompackedVwByDate("TODAY_DTL", DateTime.Today, "ALL");
        //        if (result.Count() > 0)
        //            return Json(new { ok = true, dataTdy = result, message = "ok" });
        //        else
        //            return Json(new { ok = true, dataTdy = result, message = "ok" });
        //    }
        //    catch (Exception ex) { throw ex; }
        //}

        [HttpGet]
        public JsonResult GetAtndcSmry(string field, int empId, string sDate, string eDate)
        {
            var fromDate = DateTime.Today;
            var toDate = DateTime.Today;
            bool IsSnglEmp = false;
            int empSysId = empId;
            try
            {
                if (field != "TODAY_SMRY")
                   fromDate = Convert.ToDateTime(sDate); toDate = Convert.ToDateTime(eDate);

                if (empId != 0)
                    IsSnglEmp = true;
                else
                    empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));

                var result = _attnContext.GetEmpDlyAttnSmry(field, IsSnglEmp, empSysId, fromDate, toDate);                
                if (result.Count() > 0)
                    return Json(new { ok = true, data = result, message = "ok" });
                else
                    return Json(new { ok = true, data = result, message = "ok" });
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public JsonResult GetTdyAtndcDtls(string status)
        {
            try
            {
                IEnumerable<HrEmpAttnCompackedVw> dsEmpDlyAttn;
                if (status == "Out Sourced")
                    dsEmpDlyAttn = _attnContext.GetHrEmpAttnCompackedVwOutSrc(DateTime.Today);
                else
                    dsEmpDlyAttn = _attnContext.GetHrEmpAttnCompackedVwByDate("TODAY_DTL", DateTime.Today, "ALL").Where(x => x.Status == status || status == "ALL");

                if (dsEmpDlyAttn.Count() > 0)
                    return Json(new { ok = true, data = dsEmpDlyAttn, message = "ok" });
                else
                    return Json(new { ok = true, data = "", message = "ok" });
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public JsonResult GetEmpDlyAttnSmry(string slctdDate)
        {            
            try
            {                 
                var dsEmpDlyAttnSmry = _attnContext.GetEmpDlyAttnSmry("DAILY_SMRY",false, Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId")), Convert.ToDateTime(slctdDate), Convert.ToDateTime(slctdDate));
                var result = dsEmpDlyAttnSmry;
                if (result.Count() > 0)
                    return Json(new { ok = true, data = result, message = "ok" });
                else
                    return Json(new { ok = true, data = result, message = "ok" });
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public JsonResult GetEmpDlyAttn(string slctdDate, string status)
        {
            try
            {
                IEnumerable<HrEmpAttnCompackedVw> dsEmpDlyAttn;
                if (status == "Out Sourced")
                    dsEmpDlyAttn = _attnContext.GetHrEmpAttnCompackedVwOutSrc(Convert.ToDateTime(slctdDate));
                else
                    dsEmpDlyAttn = _attnContext.GetHrEmpAttnCompackedVwByDate("_detail", Convert.ToDateTime(slctdDate), "ALL").Where(x => x.Status == status || status == "ALL");

                if (dsEmpDlyAttn.Count() > 0)
                    return Json(new { ok = true, data = dsEmpDlyAttn, message = "ok" });
                else
                    return Json(new { ok = true, data = "", message = "ok" });
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region Notification
        public JsonResult GetNotifyList()
        {
            try
            {
                int empSysID = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var dsNtfys = _notifyContext.GetPushNotifyList(empSysID);
                return Json(new { ok = true, data = dsNtfys, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        public JsonResult GetNotifyCount()
        {
            try
            {
                int empSysID = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var dsNtfys = _notifyContext.GetPushNotifyCount(empSysID);
                return Json(new { ok = true, data = dsNtfys, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        public JsonResult ReadNotify()
        {
            try
            {
                int empSysID = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var dsNtfys = _notifyContext.ReadPushNotifications(empSysID);
                return Json(new { ok = true, data = dsNtfys, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }
        #endregion Notification


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
