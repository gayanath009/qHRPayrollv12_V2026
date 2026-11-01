using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using qHRM.Areas.Identity.Data;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Data;
using qHRM.SharedSysFuncs;

namespace qHRM.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<qHRMUser> _userManager;
        private readonly SysAdminContext _sysAdminContext;
        private readonly LoginContext _loginContext;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(
            UserManager<qHRMUser> userManager,
            SysAdminContext sysAdminContext,
            LoginContext loginContext,
            ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _sysAdminContext = sysAdminContext;
            _loginContext = loginContext;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email or Username is required.")]
            [Display(Name = "Email or Username")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("Password", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                return RedirectToPage("./ForgotPassword");
            }

            try
            {
                string decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                Input = new InputModel
                {
                    Code = decodedCode,
                    Email = email ?? string.Empty
                };
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode password reset code.");
                return BadRequest("Invalid or expired password reset token.");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var trimmedIdentifier = Input.Email?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedIdentifier))
            {
                ModelState.AddModelError(string.Empty, "Email or Username is required.");
                return Page();
            }

            // Security complexity enforcement: minimum 6 chars, mixture of letters and numbers
            if (Input.Password.Length < 6 || !Regex.IsMatch(Input.Password, @"[A-Za-z]") || !Regex.IsMatch(Input.Password, @"[0-9]"))
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 6 characters long and contain both letters and numbers.");
                return Page();
            }

            // 1. Locate user by Email or Username
            var user = await _userManager.FindByEmailAsync(trimmedIdentifier);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(trimmedIdentifier);
            }

            if (user == null)
            {
                // Anti-enumeration protection
                _logger.LogWarning("Reset password attempted for non-existent account: {Identifier}", trimmedIdentifier);
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // 2. Perform ASP.NET Core Identity reset
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                try
                {
                    // 3. Synchronize ADM_User table with application AES-256 encryption standard
                    string encryptedPassword = PasswordSecurityHelper.EncryptPassword(Input.Password);

                    var admUser = _sysAdminContext.GetAdmUser(user.Usr_Id);
                    if (admUser == null && !string.IsNullOrEmpty(user.UserName))
                    {
                        admUser = _sysAdminContext.GetAdmUserByUsername(user.UserName);
                    }

                    if (admUser != null)
                    {
                        _sysAdminContext.UpdatePassword(admUser.UsrId, encryptedPassword);
                    }
                    else
                    {
                        var admUser2 = await _loginContext.GetAdmUserByUsernameAsync(user.UserName);
                        if (admUser2 != null)
                        {
                            await _loginContext.UpdateAdmUserPasswordAsync(admUser2.UsrId, encryptedPassword);
                        }
                    }

                    // 4. Invalidate prior active sessions by rotating security stamp
                    await _userManager.UpdateSecurityStampAsync(user);

                    // 5. Ensure email is marked confirmed
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }

                    _logger.LogInformation("Password successfully reset and AES encrypted for user {UserName} ({Email})", user.UserName, user.Email);
                }
                catch (Exception exSync)
                {
                    _logger.LogError(exSync, "Password reset succeeded in Identity, but failed to synchronize ADM_User for {UserName}", user.UserName);
                }

                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
