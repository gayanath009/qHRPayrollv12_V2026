using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using qHRM.Areas.WorkShift.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;

namespace qHRM.Areas.WorkShift.Controllers
{
    [Area("WorkShift")]
    //[Authorize]
    public class HrWkRosterShiftController : Controller
    {
        WorkShiftContext _workShiftContext;
        private readonly DbCommitContext<HrWkRosterShift> _dbCommitContext;

        public HrWkRosterShiftController(WorkShiftContext workShiftContext,
                                   DbCommitContext<HrWkRosterShift> dbCommitContext)
        {
            _workShiftContext = workShiftContext;
            _dbCommitContext = dbCommitContext;
        }
        public IActionResult Index(int rstId)
        {
            try
            {
                //No Access Granted
                //if (HttpContext.Session.GetString("WK_RSTSFT") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                ViewBag.DataSource = _workShiftContext.GetHrWkRstSftDaySmryVwByRstId(rstId).ToList();
                return View();
            }
            catch (Exception ex) { throw ex; }
        }

        // GET: HrWkRosterShift/Create
        [HttpGet]
        public IActionResult CreateNew(int rstId, int sftId, string day, string descEng, string descArb)
        {
            try
            {
                HrWkRosterShift hrWkRosterShift = new HrWkRosterShift { RstId = rstId, SftId = sftId, Day = day, DescEng  = descEng, DescArb = descArb};
                hrWkRosterShift.EntId = (int)HttpContext.Session.GetInt32("userEntId");
                _dbCommitContext.commitChanges(LogEventTypes.INS, hrWkRosterShift, (int)HttpContext.Session.GetInt32("userEmpSysId"));


                _workShiftContext.GetHrWkRstSftDaySmryVwByRstId(rstId).ToList();
                return PartialView("_HrWkShift_Create", hrWkRosterShift);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Roster Shift", ex.Message);
            }
        }

        // GET: HrWkRoster/Edit
        public IActionResult RstEditGet(int rstId)
        {
            try
            {
                var hrWkRoster = _workShiftContext.GetHrWkRosterByRstId(rstId);
                hrWkRoster.ex_RstCode = hrWkRoster.RstCode;
                return PartialView("_HrWkRoster_Edit", hrWkRoster);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Work Roster Shift", ex.Message);
            }
        }
    }
}
