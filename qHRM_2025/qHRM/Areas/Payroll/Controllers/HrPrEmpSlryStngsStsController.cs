using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Payroll.Data;
using System;

namespace qHRM.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    [Authorize]
    public class HrPrEmpSlryStngsStsController : Controller
    {
        private readonly PayrollContext _payrollContext;

        public HrPrEmpSlryStngsStsController(PayrollContext payrollContext)
        {
            _payrollContext = payrollContext;
        }

        public IActionResult Index()
        {
            // No Access Granted
            if (HttpContext.Session.GetString("SLRY_STGNS") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.EmpLst = _payrollContext.GetPndngEmpSlryStngs(DateTime.Today);
            return View();
        }
    }
}
