using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Notify.Data;
using Notify.Models;

namespace Notify.Controllers
{
    public class NotifyController : Controller
    {
        private readonly ILogger<NotifyController> _logger;
        

        public NotifyController(ILogger<NotifyController> logger)
        {
            _logger = logger;
            
        }

        [HttpGet("{Notify}/Index")]
        public IActionResult Index()
        {
            return View();
        }

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
