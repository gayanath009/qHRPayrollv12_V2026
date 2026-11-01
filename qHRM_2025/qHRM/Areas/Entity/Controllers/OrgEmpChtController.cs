using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Entity.Data;

namespace qHRM.Areas.Entity.Controllers
{
    [Area("Entity")]
    [Authorize]
    public class OrgEmpChtController : Controller
    {
        private readonly OrgEmpChartContext _orgEmpChartContext;

        public OrgEmpChtController(OrgEmpChartContext orgEmpChartContext)
        {
            _orgEmpChartContext = orgEmpChartContext;
        }

        public IActionResult Index()
        {
            return OrgEmpCht_v2();
        }

        // =====================================================================
        // Modern Organization Chart (v2)
        // =====================================================================
        [HttpGet]
        public IActionResult OrgEmpCht_v2()
        {
            if (HttpContext.Session.GetString("ORG_CHT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
            var data = _orgEmpChartContext.GetEmpObjects(empSysId, DateTime.Today);

            ViewBag.CurrentEmpSysId = empSysId;
            return View("OrgEmpCht_v2", data);
        }

        [HttpGet]
        public IActionResult Index_v2()
        {
            return OrgEmpCht_v2();
        }

        [HttpGet]
        public IActionResult GetChartData()
        {
            try
            {
                if (HttpContext.Session.GetString("ORG_CHT") == null)
                    return Json(new { success = false, message = "Session expired." });

                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"));
                var data = _orgEmpChartContext.GetEmpObjects(empSysId, DateTime.Today);
                return Json(new { success = true, currentUserId = empSysId.ToString(), data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class OverviewData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string ReportingPerson { get; set; }
        public string Image { get; set; }

        public OverviewData(string id, string name, string designation, string reportingperson, string image)
        {
            this.Id = id;
            this.Name = name;
            this.Designation = designation;
            this.ReportingPerson = reportingperson;
            this.Image = image;
        }
    }
}
