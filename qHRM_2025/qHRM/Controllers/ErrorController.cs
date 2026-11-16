using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using qHRM.Areas.Identity.Data;
using Microsoft.AspNetCore.Http;

namespace qHRM.Controllers
{
    public class ErrorController : Controller
    {
        private readonly IHostingEnvironment _host;
        private readonly SignInManager<qHRMUser> _signInManager;

        public ErrorController(IHostingEnvironment host,
                              SignInManager<qHRMUser> signInManager)
        {
            _host = host;
            _signInManager = signInManager;
        }

        [Route("Error/{statusCode}")]
        public async Task<IActionResult> HttpStatusCodeHandler(int statusCode)
        {
            ViewBag.StatusCode = statusCode;

            switch (statusCode)
            {
                case 400:
                    ViewBag.ErrorTitle = "Session Expired or Connection Interrupted";
                    ViewBag.ExceptionMessage = "The application was offline or restarted, and your active session has ended.";
                    ViewBag.ExceptionDescription = "For security reasons, your previous session has expired. You are being redirected to the main login page.";
                    ViewBag.IsSessionExpired = true;
                    ViewBag.ActionUrl = "/Identity/Account/Login";
                    ViewBag.ActionText = "Go to Login Page";
                    ViewBag.AutoRedirect = true;
                    ViewBag.RedirectSeconds = 4;
                    try
                    {
                        HttpContext.Session?.Clear();
                        await _signInManager.SignOutAsync();
                    }
                    catch { }
                    break;

                case 401:
                    ViewBag.ErrorTitle = "Authentication Required";
                    ViewBag.ExceptionMessage = "You must be logged in to access this page.";
                    ViewBag.ExceptionDescription = "Your credentials could not be verified or your session has timed out.";
                    ViewBag.IsSessionExpired = true;
                    ViewBag.ActionUrl = "/Identity/Account/Login";
                    ViewBag.ActionText = "Go to Login Page";
                    ViewBag.AutoRedirect = true;
                    ViewBag.RedirectSeconds = 4;
                    try
                    {
                        HttpContext.Session?.Clear();
                        await _signInManager.SignOutAsync();
                    }
                    catch { }
                    break;

                case 403:
                    ViewBag.ErrorTitle = "Access Denied (403)";
                    ViewBag.ExceptionMessage = "You do not have permission to view or access this resource.";
                    ViewBag.ExceptionDescription = "If you believe this is an error, please contact your system administrator.";
                    ViewBag.IsSessionExpired = false;
                    ViewBag.ActionUrl = Url.Action("Index", "Home") ?? "/";
                    ViewBag.ActionText = "Back to Home";
                    ViewBag.AutoRedirect = false;
                    break;

                case 404:
                    ViewBag.ErrorTitle = "Page Not Found (404)";
                    ViewBag.ExceptionMessage = "Sorry, the page or resource you requested could not be found.";
                    ViewBag.ExceptionDescription = "The link may be outdated, moved, or misspelled.";
                    ViewBag.IsSessionExpired = false;
                    ViewBag.ActionUrl = Url.Action("Index", "Home") ?? "/";
                    ViewBag.ActionText = "Back to Home";
                    ViewBag.AutoRedirect = false;
                    break;

                case 500:
                default:
                    ViewBag.ErrorTitle = $"System Notification ({statusCode})";
                    ViewBag.ExceptionMessage = "An unexpected situation occurred while processing your request.";
                    ViewBag.ExceptionDescription = "The server could not complete the operation. Please sign in again or return to the main dashboard.";
                    ViewBag.IsSessionExpired = true;
                    ViewBag.ActionUrl = "/Identity/Account/Login";
                    ViewBag.ActionText = "Go to Login Page";
                    ViewBag.AutoRedirect = false;
                    break;
            }

            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            string docFolder = string.Empty;
            string docFile = string.Empty;
            string docPath = string.Empty;

            try
            {
                docFolder = Path.Combine(_host.WebRootPath, "errorLog");
                if (!Directory.Exists(docFolder)) Directory.CreateDirectory(docFolder);
                docFile = "errorLog_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                docPath = Path.Combine(docFolder, docFile);

                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

                if (exceptionHandlerPathFeature != null)
                {
                    try
                    {
                        string[] lines = {
                            "Path        : " + exceptionHandlerPathFeature.Path,
                            "Message     : " + exceptionHandlerPathFeature.Error?.Message,
                            "Stack Trace : " + exceptionHandlerPathFeature.Error?.StackTrace
                        };

                        using (StreamWriter outputFile = new StreamWriter(docPath))
                        {
                            foreach (string line in lines)
                                outputFile.WriteLine(line);
                        }
                    }
                    catch { }

                    ViewBag.ExceptionPath = exceptionHandlerPathFeature.Path;
                    ViewBag.ExceptionMessage = exceptionHandlerPathFeature.Error?.Message;
                    ViewBag.StackTrace = exceptionHandlerPathFeature.Error?.StackTrace;
                    ViewBag.ErrorTitle = "Application Error";
                    ViewBag.ExceptionDescription = "An unexpected error occurred while processing your request. Details have been logged.";
                }
                else
                {
                    ViewBag.ErrorTitle = "An Error Occurred";
                    ViewBag.ExceptionMessage = "An unexpected issue was encountered.";
                    ViewBag.ExceptionDescription = "Please return to the login page or main dashboard.";
                }

                ViewBag.StatusCode = 500;
                ViewBag.IsSessionExpired = false;
                ViewBag.ActionUrl = "/Identity/Account/Login";
                ViewBag.ActionText = "Go to Login Page";
                ViewBag.AutoRedirect = false;

                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorTitle = "Application Error";
                ViewBag.ExceptionMessage = ex.Message;
                ViewBag.ActionUrl = "/Identity/Account/Login";
                ViewBag.ActionText = "Go to Login Page";
                ViewBag.AutoRedirect = false;
                return View("Error");
            }
        }

        public IActionResult LogOutView()
        {
            return View();
        }

        public ActionResult Logout1()
        {
            try
            {
                HttpContext.Session.Clear(); //Clear Sessions                
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear(); //Clear Sessions                
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
