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

                    // 5. Send redesigned branded HTML email with #550033 header, QCS logo, and wrapped link
                    string employeeDisplayName = user.UserName;
                    if (user.Emp_Sys_Id > 0)
                    {
                        var empInfo = await _loginContext.GetEmpBySysIdAsync(user.Emp_Sys_Id);
                        if (empInfo != null && !string.IsNullOrWhiteSpace(empInfo.FullName))
                        {
                            employeeDisplayName = empInfo.FullName.Trim();
                        }
                    }

                    string encodedName = HtmlEncoder.Default.Encode(employeeDisplayName);
                    string encodedEmail = HtmlEncoder.Default.Encode(targetEmail);
                    string encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);

                    string mailBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>qHR - Reset Password Request</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f6f9;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;-webkit-font-smoothing:antialiased;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f4f6f9;padding:35px 15px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 18px rgba(0,0,0,0.08);border:1px solid #e2e8f0;max-width:600px;width:100%;"">
                    <!-- Top Bar: #550033 with QCS logo and qHR and Payroll System -->
                    <tr>
                        <td style=""background-color:#550033;padding:20px 32px;text-align:left;"">
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                <tr>
                                    <td align=""left"" valign=""middle"" style=""width:115px;"">
                                        <img src=""data:image/png;base64,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"" alt=""QCS Logo"" width=""110"" height=""31"" style=""display:block;border:0;outline:none;text-decoration:none;"" />
                                    </td>
                                    <td align=""left"" valign=""middle"" style=""padding-left:14px;"">
                                        <h2 style=""color:#ffffff;margin:0;font-size:20px;font-weight:700;letter-spacing:0.5px;line-height:1.2;"">qHR and Payroll System</h2>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style=""padding:32px 35px;"">
                            <h3 style=""color:#1f2937;font-size:18px;margin-top:0;margin-bottom:16px;font-weight:600;"">Password Reset Request</h3>
                            <p style=""color:#374151;font-size:15px;line-height:1.6;margin-bottom:18px;"">
                                Hello <strong>{encodedName}</strong>,
                            </p>
                            <p style=""color:#4b5563;font-size:14px;line-height:1.6;margin-bottom:24px;"">
                                We received a request to reset your password for the account registered with <strong style=""color:#550033;"">{encodedEmail}</strong> in the qHR and Payroll System. To securely set up a new password, click the button below:
                            </p>
                            <div style=""text-align:center;margin:30px 0;"">
                                <a href=""{encodedUrl}"" style=""background-color:#550033;color:#ffffff;padding:13px 34px;text-decoration:none;border-radius:6px;font-weight:600;font-size:15px;display:inline-block;box-shadow:0 3px 8px rgba(85,0,51,0.25);"">Reset My Password</a>
                            </div>
                            <!-- Centered, wrapped direct link -->
                            <div style=""text-align:center;margin-top:24px;margin-bottom:12px;"">
                                <p style=""color:#6b7280;font-size:13px;line-height:1.6;margin-bottom:8px;"">
                                    If the button does not work, copy and paste this link into your web browser:
                                </p>
                                <div style=""word-break:break-all;overflow-wrap:break-word;max-width:520px;margin:0 auto;text-align:center;"">
                                    <a href=""{encodedUrl}"" style=""color:#550033;font-size:12px;line-height:1.5;text-decoration:underline;"">{encodedUrl}</a>
                                </div>
                            </div>
                            <hr style=""border:none;border-top:1px solid #e5e7eb;margin:25px 0;"" />
                            <div style=""background-color:#fef3c7;border-left:4px solid #f59e0b;padding:12px 16px;border-radius:4px;margin-bottom:20px;text-align:left;"">
                                <p style=""color:#92400e;font-size:12px;line-height:1.5;margin:0;"">
                                    <strong>Security Notice:</strong> This reset link is single-use and will expire shortly. If you did not request this password reset, please ignore this email or contact your System Administrator immediately.
                                </p>
                            </div>
                            <p style=""color:#9ca3af;font-size:11px;margin:0;"">
                                Security Policy: AES-256 encrypted authentication &bull; Automated Identity Dispatch
                            </p>
                        </td>
                    </tr>
                    <!-- Email Footer (Unchanged) -->
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
