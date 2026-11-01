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
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    
                    //Passing to the Error View//
                    ViewBag.ExceptionPath = "Path Not Found";
                        ViewBag.ExceptionMessage = "Sorry, the resource could not be found";
                    ViewBag.StackTrace = "#404, the resource could not be found";
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
                docFile = "errorLog_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                docPath = Path.Combine(docFolder, docFile);                

                // Retrieve the exception Details//
                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                                             
                //Fill Text file Fields//
                string[] lines = { "Path        : "  + exceptionHandlerPathFeature.Path,
                                   "Message     : "  + exceptionHandlerPathFeature.Error.Message,
                                   "Stack Trace : "  + exceptionHandlerPathFeature.Error.StackTrace};

                //Writing to the Error Log File
                using (StreamWriter outputFile = new StreamWriter(docPath))
                {
                    foreach (string line in lines)
                        outputFile.WriteLine(line);
                }
                //Passing to the Error View//
                ViewBag.ExceptionPath = exceptionHandlerPathFeature.Path;
                ViewBag.ExceptionMessage = exceptionHandlerPathFeature.Error.Message;
                ViewBag.StackTrace = exceptionHandlerPathFeature.Error.Message; //exceptionHandlerPathFeature.Error.StackTrace;
                return View("Error");
            }
            catch (Exception ex)
            {
                throw ex;
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