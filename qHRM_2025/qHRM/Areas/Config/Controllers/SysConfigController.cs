using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using qHRM.Extensions.Alerts;
using qHRM.Area.Config;
using qHRM.Areas.Email.Models;

namespace qHRM.Areas.Config.Controllers
{

    [Area("Config")]
    //[Authorize]
    public class SysConfigController : Controller
    {
        private readonly SysConfigContext _sysConfigContext;
        private readonly SysConfigContext _emailSender;
        
        public SysConfigController(SysConfigContext sysConfigContext,
                                   SysConfigContext emailSender)
        {
            this._sysConfigContext = sysConfigContext;
            this._emailSender = emailSender;
        }
        public IActionResult Index()
        {
            int? userGrpId = HttpContext.Session.GetInt32("userGrpId");
            if (userGrpId != 1)
            {
                return RedirectToAction("Index", "Home").WithWarning("QCS", "Access Denied: Only administrators have permission to access system settings.");
            }

            var sysCfg = _sysConfigContext.GetSysCongif().Where(x => x.Id == 1).FirstOrDefault();
            if (sysCfg != null && !string.IsNullOrEmpty(sysCfg.ServiceAcctPwd))
            {
                sysCfg.ServiceAcctPwd = qHRM.SharedSysFuncs.CryptoSecurityHelper.Decrypt(sysCfg.ServiceAcctPwd);
            }
            return View(sysCfg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SysConfig sysCfg)
        {
            int? userGrpId = HttpContext.Session.GetInt32("userGrpId");
            if (userGrpId != 1)
            {
                return RedirectToAction("Index", "Home").WithWarning("QCS", "Access Denied: Only administrators have permission to update system settings.");
            }

            try
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(sysCfg.ServiceAcctPwd))
                    {
                        var existing = _sysConfigContext.GetSysCongif().FirstOrDefault(x => x.Id == sysCfg.Id);
                        if (existing != null)
                        {
                            sysCfg.ServiceAcctPwd = existing.ServiceAcctPwd;
                        }
                    }
                    else
                    {
                        sysCfg.ServiceAcctPwd = qHRM.SharedSysFuncs.CryptoSecurityHelper.Encrypt(sysCfg.ServiceAcctPwd.Trim());
                    }

                    int _result = _sysConfigContext.Update(sysCfg);
                    if (_result == 1)
                    {
                        await _emailSender.SendEmailNotificationAsync(
                        sysCfg.TestEmail, //To Email
                        string.Empty, //From Email
                        "qHR - Test Email",
                        "<html><font face=tahoma size=2 color=black> Hello, </font><br /><br/> <font face=tahoma size=2 color=black> Test Email Generated Successfully.</font> <br/><br />" +
                        "<font face=tahoma size=2 color=black>qHR System</font> <br />" +
                        "<font face=tahoma size=1 color=gray>This is a system generated email, do not reply to this email.</font>",
                        string.Empty);

                        return RedirectToAction("Index", "SysConfig").WithSuccess("QCS", "Record Updated & Password Encrypted Successfully");
                    }
                    else
                        return RedirectToAction("Index", "SysConfig").WithWarning("QCS", "Updating Record got failed");
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Index", "SysConfig").WithWarning("QCS", ex.Message.ToString());
                }

            }
            catch
            {
                return View();
            }
        }
    }
}