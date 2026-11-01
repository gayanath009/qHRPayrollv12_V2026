using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using System;

namespace qHRM.Areas.Attendance.Controllers
{
    [Area("Attendance")]
    [Authorize]
    public class HrEmpAtndcStngsStsController : Controller
    {
        private readonly AttnContext _attnContext;

        public HrEmpAtndcStngsStsController(AttnContext attnContext)
        {
            _attnContext = attnContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("ATN_STGNS") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.EmpLst = _attnContext.GetPndngEmpAttnStngs(DateTime.Today);
            return View();
        }
    }
}
