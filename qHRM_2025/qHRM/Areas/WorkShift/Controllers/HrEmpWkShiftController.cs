using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.WorkShift.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Data;
using static qHRM.Areas.WorkShift.Data.WorkShiftContext;

namespace qHRM.Areas.WorkShift.Controllers
{
    [Area("WorkShift")]
    [Authorize]
    public class HrEmpWkShiftController : Controller
    {
        private readonly WorkShiftContext _workShiftContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpWkShift> _dbCommitContext;

        public HrEmpWkShiftController(WorkShiftContext workShiftContext,
                                        SysSelectListContext sysSelectListContext,
                                        DbCommitContext<HrEmpWkShift> dbCommitContext)
        {
            _workShiftContext = workShiftContext;
            _dbCommitContext = dbCommitContext;
            _sysSelectListContext = sysSelectListContext;
        }

        //public IActionResult Index()
        //{
        //    //No Access Granted
        //    if (HttpContext.Session.GetString("WK_SFT_EMP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        //    var x = _workShiftContext.GetEmployeeShiftData(SftSchTypes.EMP, 3, DateTime.Today.AddMonths(-1).AddDays(1), DateTime.Today, 0);
        //    ViewBag.lstMonths = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.MONTHS);
        //    ViewBag.Employees = x.EmployeeDataLst;
        //    ViewBag.Resources = x.EmpShiftDataLst;

        //    ViewBag.curYear = DateTime.Today.Year;
        //    ViewBag.curMonth = DateTime.Today.Month;

        //    return View();
        //}

        public IActionResult Index()
        {
            //    //No Access Granted
            if (HttpContext.Session.GetString("WK_SFT_EMP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            ViewBag.dataSource = GetScheduleData();
            List<EmployeeData> employees = new List<EmployeeData>();
            employees.Add(new EmployeeData { text = "Alice", id = 1, groupId = 1, color = "#bbdc00", designation = "Content writer" });
            employees.Add(new EmployeeData { text = "Nancy", id = 2, groupId = 2, color = "#9e5fff", designation = "Designer" });
            employees.Add(new EmployeeData { text = "Robert", id = 3, groupId = 1, color = "#bbdc00", designation = "Software Engineer" });
            employees.Add(new EmployeeData { text = "Robson", id = 4, groupId = 2, color = "#9e5fff", designation = "Support Engineer" });
            employees.Add(new EmployeeData { text = "Laura", id = 5, groupId = 1, color = "#bbdc00", designation = "Human Resource" });
            employees.Add(new EmployeeData { text = "Margaret", id = 6, groupId = 2, color = "#9e5fff", designation = "Content Analyst" });
            ViewBag.Employees = employees;

            ViewBag.Resources = new string[] { "Employee" };
            return View();
        }

        public List<AppointmentData> GetScheduleData()
        {
            List<AppointmentData> appData = new List<AppointmentData>();
            appData.Add(new AppointmentData
            {
                Id = 1,
                Subject = "Explosion of Betelgeuse Star",
                StartTime = new DateTime(2018, 2, 11, 9, 30, 0),
                EndTime = new DateTime(2018, 2, 11, 11, 0, 0),
                EmployeeId = 1
            });
            appData.Add(new AppointmentData
            {
                Id = 2,
                Subject = "Thule Air Crash Report",
                StartTime = new DateTime(2018, 2, 12, 12, 0, 0),
                EndTime = new DateTime(2018, 2, 12, 14, 0, 0),
                EmployeeId = 1
            });
            appData.Add(new AppointmentData
            {
                Id = 3,
                Subject = "Blue Moon Eclipse",
                StartTime = new DateTime(2018, 2, 13, 9, 30, 0),
                EndTime = new DateTime(2018, 2, 13, 11, 0, 0),
                EmployeeId = 2
            });
            appData.Add(new AppointmentData
            {
                Id = 4,
                Subject = "Meteor Showers in 2018",
                StartTime = new DateTime(2018, 2, 14, 13, 0, 0),
                EndTime = new DateTime(2018, 2, 14, 14, 30, 0),
                EmployeeId = 2
            });
            appData.Add(new AppointmentData
            {
                Id = 5,
                Subject = "Milky Way as Melting pot",
                StartTime = new DateTime(2018, 2, 15, 12, 0, 0),
                EndTime = new DateTime(2018, 2, 15, 14, 0, 0),
                EmployeeId = 3
            });
            return appData;
        }

        public class AppointmentData
        {
            public int Id { get; set; }
            public string Subject { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int EmployeeId { get; set; }
        }

        public class EmployeeData
        {
            public string text { set; get; }
            public int id { set; get; }
            public int groupId { set; get; }
            public string color { set; get; }
            public string designation { set; get; }
        }
    }
}