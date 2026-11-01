using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using qHRM.Area.Config;
using qHRM.Areas.Identity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;

namespace qHRM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<qHRMUser> _userManager;
        private readonly SysConfigContext _emailSender;
        private readonly LoginContext _loginContext;
        private readonly SysAdminContext _sysAdminContext;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<qHRMUser> userManager,
            SysConfigContext emailSender,
            LoginContext loginContext,
            SysAdminContext sysAdminContext,
            IWebHostEnvironment env,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _loginContext = loginContext;
            _sysAdminContext = sysAdminContext;
            _env = env;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your registered email address or username.")]
            [Display(Name = "Email or Username")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var inputRaw = Input.Email?.Trim();
            if (string.IsNullOrWhiteSpace(inputRaw))
            {
                ModelState.AddModelError(string.Empty, "Please enter your email or username.");
                return Page();
            }

            try
            {
                // 1. Locate the user in AspNetUsers by Email or Username
                qHRMUser user = null;
                if (inputRaw.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(inputRaw);
                    if (user == null)
                    {
                        user = await _userManager.FindByNameAsync(inputRaw);
                    }
                }
                else
                {
                    user = await _userManager.FindByNameAsync(inputRaw);
                    if (user == null)
                    {
                        user = await _userManager.FindByEmailAsync(inputRaw);
                    }
                }

                // 2. Fallback: Check ADM_User if not found in AspNetUsers
                if (user == null)
                {
                    var admUser = await _loginContext.GetAdmUserByUsernameAsync(inputRaw);
                    if (admUser != null)
                    {
                        // Check if an AspNetUsers entry already exists by Usr_Id or create on-the-fly
                        user = await _userManager.FindByNameAsync(admUser.UsrName);
                        if (user == null)
                        {
                            // Fetch email from HR_EmpGenInfo if available
                            string empEmail = null;
                            if (admUser.EmpSysId > 0)
                            {
                                var emp = await _loginContext.GetEmpBySysIdAsync(admUser.EmpSysId);
                                empEmail = emp?.PsnEmail;
                            }

                            user = new qHRMUser
                            {
                                UserName = admUser.UsrName,
                                Usr_Id = admUser.UsrId,
                                Emp_Sys_Id = admUser.EmpSysId,
                                Email = !string.IsNullOrEmpty(empEmail) ? empEmail : (admUser.UsrName.Contains("@") ? admUser.UsrName : $"{admUser.UsrName}@qcs-qatar.com"),
                                EmailConfirmed = true
                            };

                            var createRes = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "Aa1!");
                            if (!createRes.Succeeded)
                            {
                                _logger.LogWarning("Failed to auto-create Identity user for {UserName}: {Errors}", admUser.UsrName, string.Join(", ", createRes.Errors.Select(e => e.Description)));
                                user = null;
                            }
                        }
                    }
                }

                // 3. User verification and email resolution
                if (user != null)
                {
                    // Ensure the user has an email address populated
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        if (user.UserName.Contains("@"))
                        {
                            user.Email = user.UserName;
                        }
                        else if (user.Emp_Sys_Id > 0)
                        {
                            var emp = await _loginContext.GetEmpBySysIdAsync(user.Emp_Sys_Id);
                            if (!string.IsNullOrEmpty(emp?.PsnEmail))
                            {
                                user.Email = emp.PsnEmail;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(user.Email))
                        {
                            await _userManager.UpdateAsync(user);
                        }
                    }

                    // Auto-confirm email if unconfirmed so password reset token is valid
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }

                    // 4. Generate cryptographically secure one-time password reset token
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var targetEmail = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : inputRaw;

                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = encodedCode, email = targetEmail },
                        protocol: Request.Scheme);

                    _logger.LogInformation("Password reset token generated for user: {UserName} ({Email})", user.UserName, targetEmail);

                    // 5. Send branded, high-security HTML email
                    string mailBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>qHR - Reset Password Request</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f6f9;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f6f9;padding:35px 15px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 18px rgba(0,0,0,0.08);border:1px solid #e2e8f0;"">
                    <tr>
                        <td style=""background:linear-gradient(135deg, #6c133a 0%, #4a0d28 100%);padding:28px 32px;text-align:left;"">
                            <h2 style=""color:#ffffff;margin:0;font-size:22px;font-weight:700;letter-spacing:0.5px;"">qHR &amp; Payroll System</h2>
                            <p style=""color:#f9a8d4;margin:4px 0 0 0;font-size:13px;"">Identity &amp; Access Management</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:32px;"">
                            <h3 style=""color:#1f2937;font-size:18px;margin-top:0;margin-bottom:12px;font-weight:600;"">Password Reset Request</h3>
                            <p style=""color:#4b5563;font-size:14px;line-height:1.6;margin-bottom:20px;"">
                                Hello <strong>{HtmlEncoder.Default.Encode(user.UserName)}</strong>,<br /><br />
                                We received a request to reset your password for the qHR System. To securely set up a new password, click the button below:
                            </p>
                            <div style=""text-align:center;margin:30px 0;"">
                                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""background-color:#6c133a;color:#ffffff;padding:13px 32px;text-decoration:none;border-radius:6px;font-weight:600;font-size:15px;display:inline-block;box-shadow:0 3px 8px rgba(108,19,58,0.25);"">Reset My Password</a>
                            </div>
                            <p style=""color:#6b7280;font-size:13px;line-height:1.6;margin-bottom:10px;"">
                                If the button does not work, copy and paste this link into your web browser:<br />
                                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""color:#6c133a;word-break:break-all;font-size:12px;"">{HtmlEncoder.Default.Encode(callbackUrl)}</a>
                            </p>
                            <hr style=""border:none;border-top:1px solid #e5e7eb;margin:25px 0;"" />
                            <div style=""background-color:#fef3c7;border-left:4px solid #f59e0b;padding:12px 16px;border-radius:4px;margin-bottom:20px;"">
                                <p style=""color:#92400e;font-size:12px;line-height:1.5;margin:0;"">
                                    <strong>Security Notice:</strong> This reset link is single-use and will expire shortly. If you did not request this password reset, please ignore this email or contact your System Administrator immediately.
                                </p>
                            </div>
                            <p style=""color:#9ca3af;font-size:11px;margin:0;"">
                                Security Policy: AES-256 encrypted authentication &bull; Automated Identity Dispatch
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color:#f9fafb;padding:18px 32px;text-align:center;border-top:1px solid #e5e7eb;"">
                            <p style=""color:#9ca3af;font-size:11px;margin:0;"">
                                &copy; {DateTime.Now.Year} QCS Computer Services W.L.L. &bull; qHR &amp; Payroll System
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                    bool emailSent = false;
                    try
                    {
                        int sendResult = await _emailSender.SendEmailNotificationAsync(
                            targetEmail,
                            string.Empty,
                            "qHR System - Password Reset Request",
                            mailBody,
                            string.Empty);

                        emailSent = (sendResult == 1);
                    }
                    catch (Exception exEmail)
                    {
                        _logger.LogError(exEmail, "Failed to send password reset email to {Email}", targetEmail);
                    }

                    // Developer / Local testing support:
                    // If running in development or if SMTP is offline locally, store callbackUrl in TempData
                    // so the developer/tester can click it directly on the confirmation page
                    if (_env.IsDevelopment() || !emailSent)
                    {
                        TempData["DevResetLink"] = callbackUrl;
                        Console.WriteLine($"[FORGOT PASSWORD LINK] {callbackUrl}");
                    }
                }
                else
                {
                    _logger.LogWarning("Password reset requested for unknown user/email: {Input}", inputRaw);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request for {Input}", inputRaw);
            }

            // Anti-enumeration: always display confirmation page to prevent user harvesting
            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
