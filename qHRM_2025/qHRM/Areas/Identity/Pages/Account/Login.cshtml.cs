using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using qHRM.Areas.Identity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Data;
using qHRM.SharedSysFuncs;

namespace qHRM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        public static string _sessionId;
        private readonly UserManager<qHRMUser> _userManager;
        private readonly SignInManager<qHRMUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly LoginContext _loginContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(
            SignInManager<qHRMUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<qHRMUser> userManager,
            LoginContext loginContext,
            AdmUserGrpPrvContext prvContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _loginContext = loginContext;
            _prvContext = prvContext;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required")]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                ErrorMessage = remoteError;
                ModelState.AddModelError(string.Empty, remoteError);
            }
            else if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Normalize returnUrl: if empty or pointing to login page, default to root dashboard
            if (string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("/Login", StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = Url.Content("~/");
            }

            // 1. If user already has an active session in HttpContext.Session
            if (HttpContext.Session.GetInt32("userID") != null)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // If session is present but Identity auth cookie is missing/unauthenticated, attempt to restore it
                var sessionUser = HttpContext.Session.GetString("userName");
                if (!string.IsNullOrEmpty(sessionUser))
                {
                    var idUser = await _userManager.FindByNameAsync(sessionUser);
                    if (idUser != null)
                    {
                        await _signInManager.SignInAsync(idUser, isPersistent: false);
                        return RedirectToAction("Index", "Home", new { area = "" });
                    }
                }

                // If we cannot restore Identity auth, clear stale session to prevent redirect loop
                HttpContext.Session.Clear();
            }

            // 2. If user is authenticated via Identity/Entra ID SSO/Windows Auth
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var upn = User.FindFirst("preferred_username")?.Value
                          ?? User.FindFirst(ClaimTypes.Upn)?.Value
                          ?? User.FindFirst(ClaimTypes.Email)?.Value
                          ?? User.Identity.Name;

                await PopulateSessionForUserAsync(upn, "entra");

                if (HttpContext.Session.GetInt32("userID") != null)
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }
            }

            // Clear any lingering external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;

            return Page();
        }

        /// <summary>
        /// Challenge action for Microsoft Entra ID Single Sign-On
        /// </summary>
        public IActionResult OnGetEntraLogin(string returnUrl = null)
        {
            // Always redirect to dashboard upon successful Entra ID authentication
            var properties = new AuthenticationProperties { RedirectUri = Url.Content("~/") };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Post action for normal login authenticated against encrypted ADM_User table (asynchronous)
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            HttpContext.Session.Clear();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var trimmedUsername = Input.UserName?.Trim();

            // 1. Look up user directly in ADM_User table asynchronously
            var admUser = await _loginContext.GetAdmUserByUsernameAsync(trimmedUsername);
            if (admUser == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Username or Password.");
                return Page();
            }

            // 2. Verify password against encrypted hash or legacy plaintext
            bool isPasswordValid = PasswordSecurityHelper.VerifyPassword(admUser.Password, Input.Password, out bool needsRehash);
            if (!isPasswordValid && string.IsNullOrEmpty(admUser.Password))
            {
                // Self-healing fallback: If ADM_User password was wiped, verify against employee password in HR_EmpGenInfo
                var emp = await _loginContext.GetEmpBySysIdAsync(admUser.EmpSysId);
                if (emp != null && !string.IsNullOrEmpty(emp.Password) && string.Equals(emp.Password, Input.Password))
                {
                    isPasswordValid = true;
                    needsRehash = true;
                }
            }

            if (!isPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid Username or Password.");
                return Page();
            }

            // 3. Re-hash & update to modern encrypted format if needed (asynchronously)
            if (needsRehash)
            {
                var newHashed = PasswordSecurityHelper.HashPassword(Input.Password);
                await _loginContext.UpdateAdmUserPasswordAsync(admUser.UsrId, newHashed);
            }

            // 4. Ensure Identity user exists and is signed in
            var identityUser = await _userManager.FindByNameAsync(admUser.UsrName);
            if (identityUser == null)
            {
                identityUser = new qHRMUser
                {
                    UserName = admUser.UsrName,
                    Usr_Id = admUser.UsrId,
                    Emp_Sys_Id = admUser.EmpSysId
                };
                await _userManager.CreateAsync(identityUser, Input.Password);
            }
            await _signInManager.SignInAsync(identityUser, isPersistent: Input.RememberMe);

            // 5. Populate Session variables with roles and privilege codes asynchronously
            if (await PopulateSessionForUserAsync(admUser.UsrName, "logged", ensureIdentitySignIn: false))
            {
                _logger.LogInformation("User {UserName} logged in successfully.", admUser.UsrName);
                if (string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("/Login", StringComparison.OrdinalIgnoreCase) || returnUrl == "/")
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }
                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User account is not assigned to an active User Group or permissions are missing.");
                return Page();
            }
        }

        /// <summary>
        /// Asynchronous helper to populate HttpContext.Session with user metadata and role privilege codes
        /// </summary>
        private async Task<bool> PopulateSessionForUserAsync(string usernameOrUpn, string loginType, bool ensureIdentitySignIn = true)
        {
            if (string.IsNullOrWhiteSpace(usernameOrUpn)) return false;

            var lstUsr = await _loginContext.GetLoginSystemAsync("_login", 0, usernameOrUpn, "");
            if ((lstUsr == null || !lstUsr.Any()) && usernameOrUpn.Contains("@"))
            {
                var shortName = usernameOrUpn.Split('@')[0];
                lstUsr = await _loginContext.GetLoginSystemAsync("_login", 0, shortName, "");
            }
            else if ((lstUsr == null || !lstUsr.Any()) && usernameOrUpn.Contains("\\"))
            {
                var shortName = usernameOrUpn.Split('\\')[1];
                lstUsr = await _loginContext.GetLoginSystemAsync("_login", 0, shortName, "");
            }

            if (lstUsr != null && lstUsr.Any())
            {
                var userObj = lstUsr.First();
                int grpId = userObj.Grp_Id ?? 0;
                var lstPrv = await _prvContext.GetAsync("_user_priv_group", grpId);
                if (lstPrv != null && lstPrv.Any())
                {
                    foreach (var itm in lstPrv)
                    {
                        HttpContext.Session.SetString(itm.PrvCode, "y");
                    }
                    HttpContext.Session.SetInt32("userID", userObj.Usr_Id ?? 0);
                    HttpContext.Session.SetInt32("userEmpSysId", userObj.Emp_Sys_Id);
                    HttpContext.Session.SetString("userName", userObj.Usr_Name ?? "");
                    HttpContext.Session.SetString("userFullName", userObj.Full_Name ?? "");
                    HttpContext.Session.SetInt32("userEntId", (userObj.Ent_Id.HasValue && userObj.Ent_Id.Value > 0) ? userObj.Ent_Id.Value : 1);
                    HttpContext.Session.SetString("userPhoto", string.IsNullOrWhiteSpace(userObj.Photo_Name) ? "user.png" : userObj.Photo_Name);
                    HttpContext.Session.SetInt32("userGrpId", grpId);
                    HttpContext.Session.SetInt32("userDptId", userObj.Dept_Id ?? 0);
                    HttpContext.Session.SetString("userDashboard", (userObj.Dshbrd_Indx_Txt ?? "").Trim());
                    HttpContext.Session.SetString("mnulUsrLog", loginType);

                    // Ensure Identity user exists and is signed into ApplicationScheme if requested
                    if (ensureIdentitySignIn)
                    {
                        try
                        {
                            var identityUser = await _userManager.FindByNameAsync(userObj.Usr_Name);
                            if (identityUser == null)
                            {
                                identityUser = new qHRMUser
                                {
                                    UserName = userObj.Usr_Name,
                                    Usr_Id = userObj.Usr_Id ?? 0,
                                    Emp_Sys_Id = userObj.Emp_Sys_Id
                                };
                                await _userManager.CreateAsync(identityUser);
                            }
                            await _signInManager.SignInAsync(identityUser, isPersistent: false);
                        }
                        catch
                        {
                        }
                    }

                    return true;
                }
            }
            return false;
        }
    }
}
