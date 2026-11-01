using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.SysAdmin.Data;
using System;
using qHRM.Areas.Identity.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.General.Data;
using System.Collections.Generic;
using qHRM.SharedSysFuncs;

namespace qHRM.Areas.SysAdmin.Controllers
{
    [Area("SysAdmin")]
    [Authorize]
    public class AdmUserController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly SignInManager<qHRMUser> _signInManager;
        private readonly UserManager<qHRMUser> _userManager;
        private readonly OrganizationContext _orgContext;
        private readonly SysAdminContext _sysAdminContext;


        // GET: AdmUser
        public AdmUserController(EmployeeContext employeeContext,
                              SignInManager<qHRMUser> signInManager,
                              UserManager<qHRMUser> userManager,
                              OrganizationContext orgContext,
                              SysAdminContext sysAdminContext)
        {
            this._employeeContext = employeeContext;
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._orgContext = orgContext;
            this._sysAdminContext = sysAdminContext;
        }

        [HttpGet]
        public ActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("SY_USR") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.DataSource = _sysAdminContext.GetView("_all", 0, "", "").ToList();
            return View();
        }


        private int? GetUserActivityLogFilterUsrId()
        {
            int userGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
            int userId = HttpContext.Session.GetInt32("userID") ?? 0;

            // If session is missing or empty, attempt fallback from authenticated Identity
            if (userId == 0 && User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var currentUserName = User.Identity.Name;
                var dbUser = _sysAdminContext.GetView("_all", 0, "", "")
                    .FirstOrDefault(x => string.Equals(x.Usr_Name, currentUserName, StringComparison.OrdinalIgnoreCase));
                if (dbUser != null)
                {
                    userId = dbUser.Usr_Id;
                    userGrpId = dbUser.Grp_Id;
                }
            }

            // Only Admin users (Grp Id : 1 in ADM_UserGroup table) can see all logged details
            if (userGrpId == 1)
            {
                return null; // No user filter -> see all logs
            }

            // All other users are restricted to their individual logged details only
            return userId > 0 ? userId : -1;
        }

        [HttpGet]
        public IActionResult UserActivityLogInit(string strDate)
        {
            try
            {               
                DateTime dtDate = DateTime.Today;
                int? filterUsrId = GetUserActivityLogFilterUsrId();

                if (strDate == null)
                    ViewBag.UserLog = _sysAdminContext.GetUserActivityLog(dtDate, filterUsrId);
                else
                    ViewBag.UserLog = _sysAdminContext.GetUserActivityLog(Convert.ToDateTime(strDate), filterUsrId);

                return View("UserActivityLog");
            }
            catch (Exception)
            {
                return View("UserActivityLog");
            }
        }


        [HttpGet]
        public JsonResult UserActivityLog(string strDate)
        {
            try
            {
                IEnumerable<AdmUserLogsView> result;
                DateTime dtDate = DateTime.Today;               
                int? filterUsrId = GetUserActivityLogFilterUsrId();

                if (strDate == null)
                    result = _sysAdminContext.GetUserActivityLog(dtDate, filterUsrId);                       
                else
                    result = _sysAdminContext.GetUserActivityLog(Convert.ToDateTime(strDate), filterUsrId);               

                return Json(new { ok = true, data = result, message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = true, data = ex.Message, message = "ok" });
            }

        }


        // GET: User/Create
        [HttpGet]
        public IActionResult CreateNew(AdmUserView admUsr)
        {
            try
            {
                ViewBag.lstUG = _sysAdminContext.GetFltrdAdmUsrGrps(); //User Group
                ViewBag.lstEmp = _employeeContext.GetEmps_NotCreatedAsUsers().ToList(); //Employees
                ViewBag.lstEntity = _orgContext.GetEntities().ToList(); //Entity
                return PartialView("_User_Create", admUsr);
            }
            catch (Exception ex)
            {
                    throw ex;
            }

        }

        // POST: AdmUser/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdmUserView admView)
        {
            try
            {
                //Adm User
                AdmUser admUsr = new AdmUser();
                admUsr.UsrId = admView.Usr_Id;
                admUsr.UsrName = admView.Usr_Name;
                admUsr.GrpId = admView.Grp_Id;
                admUsr.EmpSysId = admView.Emp_Sys_Id;
                admUsr.Password = qHRM.SharedSysFuncs.PasswordSecurityHelper.HashPassword(admView.Password);
                admUsr.Remarks = admView.Remarks;
                admUsr.RecStat = 1;



                var _result = _sysAdminContext.Insert(admUsr);
                if (_result == 1)
                {

                    var user = new qHRMUser { Email = admView.email, NormalizedEmail = admView.email, UserName = admUsr.UsrName, Usr_Id = admUsr.UsrId, Emp_Sys_Id = admUsr.EmpSysId };

                    //--- [Start] Adm Entity User [Start]---
                    AdmEntityUser admEntUsr = new AdmEntityUser();
                    admEntUsr.EntId = admView.EntityId;
                    admEntUsr.UsrId = user.Usr_Id;
                    admEntUsr.RecStat = 1;
                    admEntUsr.CrtDateTime = DateTime.Now;
                    var rst = _sysAdminContext.Insert(admEntUsr);
                    //--- [End] Adm Entity User [End]---


                    var result = await _userManager.CreateAsync(user, admView.Password);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "AdmUser").WithSuccess("QCS", "Record Added Successfully");
                    }

                    string errDesc = string.Empty;
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        errDesc = error.Description;
                    }
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", errDesc);

                }
                else
                {
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Updating Record got failed");
                }

            }

            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Adding Record got failed " + ex.Message.ToString());
            }
        }

        // GET: User/Edit/5
        public ActionResult EditGet(int id)
        {
            try
            {
                var objUsr = _sysAdminContext.GetView("_all", 0, "", "").Where(x => x.Usr_Id == id).FirstOrDefault();
                var objEnt = _sysAdminContext.Get().Where(x => x.UsrId == id).FirstOrDefault();
                // objEnt can be null for users that were created without an entity assignment
                objUsr.EntityId = objEnt?.EntId ?? 0;

                ViewBag.lstUG = _sysAdminContext.GetFltrdAdmUsrGrps();//User Groups
                // Include the user's own employee record so the dropdown always shows the current employee
                ViewBag.lstEmp = _employeeContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")))
                                     .Where(x => x.ESysId == objUsr.Emp_Sys_Id).ToList();//Employees
                ViewBag.lstEntity = _orgContext.GetEntities().ToList(); //Entity
                return PartialView("_User_Edit", objUsr);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



        // POST: UserGroup/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdmUserView admView)
        {
            try
            {

                AdmUser admUsr = new AdmUser();
                admUsr.UsrId = admView.Usr_Id;
                admUsr.UsrName = admView.Usr_Name;
                admUsr.GrpId = admView.Grp_Id;
                admUsr.EmpSysId = admView.Emp_Sys_Id;
                admUsr.Remarks = admView.Remarks;
                admUsr.RecStat = 1;
                var _result = _sysAdminContext.Update(admUsr);
                if (_result == 1)
                {
                    if (admView.EntityId > 0)
                    {
                        var entUser = _sysAdminContext.Get().FirstOrDefault(x => x.UsrId == admView.Usr_Id);
                        if (entUser != null)
                        {
                            entUser.EntId = admView.EntityId;
                            _sysAdminContext.Update(entUser);
                        }
                        else
                        {
                            _sysAdminContext.Insert(new AdmEntityUser
                            {
                                EntId = admView.EntityId,
                                UsrId = admView.Usr_Id,
                                RecStat = 1,
                                CrtDateTime = DateTime.Now
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(admView.AspUser_Id))
                    {
                        var user = await _userManager.FindByIdAsync(admView.AspUser_Id);
                        if (user != null)
                        {
                            user.Email = admView.email;
                            user.NormalizedEmail = admView.email;
                            var result = await _userManager.UpdateAsync(user);
                            if (!result.Succeeded)
                            {
                                string errDesc = string.Empty;
                                foreach (var error in result.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                    errDesc = error.Description;
                                }
                                return RedirectToAction("Index", "AdmUser").WithWarning("QCS", errDesc);
                            }
                        }
                    }

                    return RedirectToAction("Index", "AdmUser").WithSuccess("QCS", "Record Updated Successfully");
                }
                else
                {
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Updating Record got failed");
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Updating Record got failed " + ex.Message.ToString());
            }
        }


               
        [HttpGet]
        public IActionResult ChangePasswordGet(int id)
        {
            try
            {
                if (id <= 0)
                {
                    id = HttpContext.Session.GetInt32("userID") ?? 0;
                }

                var objUsr = _sysAdminContext.GetView("_all", 0, "", "").FirstOrDefault(x => x.Usr_Id == id);
                if (objUsr == null && User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
                {
                    objUsr = _sysAdminContext.GetView("_all", 0, "", "").FirstOrDefault(x => string.Equals(x.Usr_Name, User.Identity.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (objUsr == null)
                {
                    return Content("<div class='alert alert-warning'>User account not found.</div>");
                }

                var objEnt = _sysAdminContext.Get().FirstOrDefault(x => x.UsrId == objUsr.Usr_Id);
                objUsr.EntityId = objEnt?.EntId ?? 0;
                return PartialView("_User_Update_Pwd", objUsr);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("SY_USR") == null && (User.Identity == null || !User.Identity.IsAuthenticated))
                {
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                if (id <= 0)
                {
                    id = HttpContext.Session.GetInt32("userID") ?? 0;
                }

                var objUsr = _sysAdminContext.GetView("_all", 0, "", "").FirstOrDefault(x => x.Usr_Id == id);
                if (objUsr == null && User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
                {
                    objUsr = _sysAdminContext.GetView("_all", 0, "", "").FirstOrDefault(x => string.Equals(x.Usr_Name, User.Identity.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (objUsr == null)
                {
                    return RedirectToAction("Index", "Home", new { area = "" }).WithWarning("QCS", "User account not found.");
                }

                var objEnt = _sysAdminContext.Get().FirstOrDefault(x => x.UsrId == objUsr.Usr_Id);
                objUsr.EntityId = objEnt?.EntId ?? 0;
                return View("User_Update_Pwd", objUsr);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // POST: AdmUser/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdmUserView admView)
        {
            int currentUserId = HttpContext.Session.GetInt32("userID") ?? 0;
            int currentUserGrpId = HttpContext.Session.GetInt32("userGrpId") ?? 0;
            string currentUserName = HttpContext.Session.GetString("SY_USR") ?? User.Identity?.Name;
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Local helper for consistent error responses
            IActionResult ReturnError(string errorMsg)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                ModelState.AddModelError(string.Empty, errorMsg);
                return View("User_Update_Pwd", admView).WithWarning("QCS", errorMsg);
            }

            try
            {
                // Fallback resolution for target user id
                if (admView.Usr_Id <= 0)
                {
                    if (!string.IsNullOrEmpty(admView.Usr_Name))
                    {
                        var u = _sysAdminContext.GetAdmUserByUsername(admView.Usr_Name);
                        if (u != null) admView.Usr_Id = u.UsrId;
                    }
                    else if (currentUserId > 0)
                    {
                        admView.Usr_Id = currentUserId;
                    }
                }

                // 1. Validate inputs
                if (string.IsNullOrWhiteSpace(admView.Old_Password))
                {
                    return ReturnError("Please enter your current (old) password.");
                }

                if (string.IsNullOrWhiteSpace(admView.Password))
                {
                    return ReturnError("Please enter a new password.");
                }

                if (string.IsNullOrWhiteSpace(admView.ConfirmPassword))
                {
                    return ReturnError("Please confirm your new password.");
                }

                if (!string.Equals(admView.Password, admView.ConfirmPassword, StringComparison.Ordinal))
                {
                    return ReturnError("'ConfirmPassword' and 'Password' do not match.");
                }

                if (admView.Password.Length < 6)
                {
                    return ReturnError("Password must be at least 6 characters long.");
                }

                if (string.Equals(admView.Old_Password, admView.Password, StringComparison.Ordinal))
                {
                    return ReturnError("New password cannot be the same as your current password.");
                }

                // 2. Fetch target user from ADM_User
                var admUser = _sysAdminContext.GetAdmUser(admView.Usr_Id);
                if (admUser == null && !string.IsNullOrEmpty(admView.Usr_Name))
                {
                    admUser = _sysAdminContext.GetAdmUserByUsername(admView.Usr_Name);
                }
                if (admUser == null && currentUserId > 0)
                {
                    admUser = _sysAdminContext.GetAdmUser(currentUserId);
                }

                if (admUser == null)
                {
                    return ReturnError("User account not found in database.");
                }

                // 3. Verify Old Password against ADM_User (supports AES-256, PBKDF2 hash, and legacy plaintext)
                bool isOldPwdValid = PasswordSecurityHelper.VerifyPassword(admUser.Password, admView.Old_Password, out _);

                // Fallback A: Check HR_EmpGenInfo if ADM_User password was empty or mismatched
                if (!isOldPwdValid && admUser.EmpSysId > 0)
                {
                    var emp = _employeeContext.GetEmpGenInfoByEmpSysId(admUser.EmpSysId);
                    if (emp != null && !string.IsNullOrEmpty(emp.Password))
                    {
                        if (PasswordSecurityHelper.VerifyPassword(emp.Password, admView.Old_Password, out _))
                        {
                            isOldPwdValid = true;
                        }
                    }
                }

                // Locate ASP.NET Identity user
                qHRMUser idUser = null;
                if (!string.IsNullOrEmpty(admView.AspUser_Id))
                {
                    idUser = await _userManager.FindByIdAsync(admView.AspUser_Id);
                }
                if (idUser == null && !string.IsNullOrEmpty(admUser.UsrName))
                {
                    idUser = await _userManager.FindByNameAsync(admUser.UsrName);
                }
                if (idUser == null && admUser.UsrId > 0)
                {
                    idUser = _userManager.Users.FirstOrDefault(u => u.Usr_Id == admUser.UsrId);
                }

                // Fallback B: Check ASP.NET Identity password if ADM_User was not verified
                if (!isOldPwdValid && idUser != null)
                {
                    if (await _userManager.CheckPasswordAsync(idUser, admView.Old_Password))
                    {
                        isOldPwdValid = true;
                    }
                }

                if (!isOldPwdValid)
                {
                    return ReturnError("The current (old) password you entered is incorrect.");
                }

                // 4. Encrypt new password using Application AES-256 Policy (CryptoSecurityHelper)
                string encryptedPassword = CryptoSecurityHelper.Encrypt(admView.Password);

                // Update ADM_User database record with AES-encrypted password
                _sysAdminContext.UpdatePassword(admUser.UsrId, encryptedPassword);

                // 5. Synchronize ASP.NET Identity User (so Identity authentication stays in sync)
                if (idUser != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(idUser);
                    var resetResult = await _userManager.ResetPasswordAsync(idUser, token, admView.Password);
                    if (!resetResult.Succeeded)
                    {
                        string errDesc = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    }
                }
                else
                {
                    // Create Identity record if it was missing
                    idUser = new qHRMUser
                    {
                        UserName = admUser.UsrName,
                        Email = admView.email ?? admUser.UsrName,
                        Usr_Id = admUser.UsrId,
                        Emp_Sys_Id = admUser.EmpSysId
                    };
                    await _userManager.CreateAsync(idUser, admView.Password);
                }

                // Refresh cookie authentication if the current user changed their own password
                if (currentUserId == admUser.UsrId || string.Equals(currentUserName, admUser.UsrName, StringComparison.OrdinalIgnoreCase))
                {
                    await _signInManager.SignInAsync(idUser, isPersistent: false);
                }

                // 6. Return response
                if (isAjax)
                {
                    return Json(new { success = true, message = "Password changed & AES encrypted successfully." });
                }

                if (currentUserGrpId == 1)
                {
                    return RedirectToAction("Index", "AdmUser").WithSuccess("QCS", "Password changed & AES encrypted successfully.");
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { area = "" }).WithSuccess("QCS", "Password changed & AES encrypted successfully.");
                }
            }
            catch (Exception ex)
            {
                return ReturnError("Updating password failed: " + ex.Message);
            }
        }

        // GET: User/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var oUsrGrp = _sysAdminContext.GetView("_all", 0, "", "").Where(x => x.Usr_Id == id).FirstOrDefault();
                return PartialView("_User_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        // POST: User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(AdmUserView admView)
        {
            try
            {
                if (admView == null || admView.Usr_Id <= 0)
                {
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Invalid user selected for deletion.");
                }

                // If Usr_Name or AspUser_Id was not passed, populate from database
                if (string.IsNullOrEmpty(admView.Usr_Name) || string.IsNullOrEmpty(admView.AspUser_Id))
                {
                    var existing = _sysAdminContext.GetView("_all", 0, "", "").FirstOrDefault(x => x.Usr_Id == admView.Usr_Id);
                    if (existing != null)
                    {
                        if (string.IsNullOrEmpty(admView.Usr_Name)) admView.Usr_Name = existing.Usr_Name;
                        if (string.IsNullOrEmpty(admView.AspUser_Id)) admView.AspUser_Id = existing.AspUser_Id;
                    }
                }

                var currentUserName = HttpContext.Session.GetString("SY_USR");
                if (!string.IsNullOrEmpty(currentUserName) && string.Equals(currentUserName, admView.Usr_Name, StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "You cannot delete your own currently logged-in user account.");
                }

                // 1. Delete ASP.NET Identity User if exists
                qHRMUser user = null;
                if (!string.IsNullOrEmpty(admView.AspUser_Id))
                {
                    user = await _userManager.FindByIdAsync(admView.AspUser_Id);
                }

                if (user == null && admView.Usr_Id > 0)
                {
                    user = _userManager.Users.FirstOrDefault(u => u.Usr_Id == admView.Usr_Id);
                }

                if (user == null && !string.IsNullOrEmpty(admView.Usr_Name))
                {
                    user = await _userManager.FindByNameAsync(admView.Usr_Name);
                }

                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        string errDesc = string.Join(", ", result.Errors.Select(e => e.Description));
                        return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Failed to delete login identity: " + errDesc);
                    }
                }

                // 2. Cascade delete AdmUser and associated records (AdmEntityUser, AdmUserLogs)
                var _result = _sysAdminContext.DeleteUser(admView.Usr_Id);
                if (_result == 1)
                {
                    return RedirectToAction("Index", "AdmUser").WithSuccess("QCS", "Record Deleted Successfully");
                }
                else
                {
                    return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Deleting Record got failed");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUser").WithWarning("QCS", "Deleting Record got failed " + ex.Message.ToString());
            }
        }


        [HttpGet]
        public ActionResult IsExists([Bind(Prefix = "Usr_Name")] string value, [Bind(Prefix = "exUsr_Name")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _sysAdminContext.GetView("_all", 0, "", "").Where(x => x.Usr_Name.ToLower() == value.ToLower()).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _sysAdminContext.GetView("_all", 0, "", "").Where(x => x.Usr_Name.ToLower() == value.ToLower()).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }

    }
}