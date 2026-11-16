using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.WorkShift.Data;
using qHRM.Areas.WorkShift.Models;
using qHRM.Data;
using qHRM.Extensions.Alerts;
using qHRM.Models;

namespace qHRM.Areas.WorkShift.Controllers
{
    [Area("WorkShift")]
    [Authorize]
    public class HrEmpWkRosterController : Controller
    {
        private readonly WorkShiftContext _workShiftContext;
        private readonly EmployeeContext _empContext;
        private readonly AttnContext _attnContext;
        private readonly qHRMasterContext _hrmContext;

        private readonly DbCommitContext<HrEmpWkRoster> _dbCommitContext;

        public HrEmpWkRosterController(WorkShiftContext workShiftContext, 
                                       EmployeeContext empContext,
                                       DbCommitContext<HrEmpWkRoster> dbCommitContext,
                                       AttnContext attnContext,
                                       qHRMasterContext hrmContext)
        {
            _workShiftContext = workShiftContext;
            _empContext = empContext;
            _dbCommitContext = dbCommitContext;
            _attnContext = attnContext;
            _hrmContext = hrmContext;
        }

        public IActionResult Index()
        {
            try
            {
                //No Access Granted
                //if (HttpContext.Session.GetString("WK_RST") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

               ViewBag.DataSource = _workShiftContext.GetEmpWrkRstrs();
               return View();
            }
            catch (Exception ex) { throw ex; }
        }

        [HttpGet]
        public IActionResult CreateNew(HrEmpWkRoster hrEmpWkRoster)
        {
            try
            {
                ViewBag.lstEmp = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstRstrs = _workShiftContext.GetAllHrWkRoster(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                hrEmpWkRoster.StDate = DateTime.Today;
                hrEmpWkRoster.EmpTyp = "A";
                return PartialView("_HrEmpWkRoster_Create", hrEmpWkRoster);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", ex.Message);
            }
        }

        // POST: HrWkRoster/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(HrEmpWkRoster hrEmpWkRoster)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            try
        //            {
        //                var refId = 0;
        //                if (hrEmpWkRoster.EmpTyp == "A") //All Employees
        //                {
        //                    var _emplist = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
        //                    foreach (var itm in _emplist)
        //                    {
        //                        var obj = _workShiftContext.GetEmpWrkRstrs().Where(x => x.EmpSysId == itm.ESysId && (x.EndDate >= hrEmpWkRoster.StDate || x.EndDate <= hrEmpWkRoster.EndDate || x.EndDate >= hrEmpWkRoster.EndDate)).FirstOrDefault(); //|| x.EndDate >= hrEmpWkRoster.EndDate
        //                        if (obj != null)
        //                        {
        //                            //Update the Current Roster with 
        //                            HrEmpWkRoster hrEmpWkUPD = new HrEmpWkRoster();
        //                            hrEmpWkUPD.RecId = obj.RecId;
        //                            hrEmpWkUPD.EmpSysId = obj.EmpSysId;
        //                            hrEmpWkUPD.RstId = obj.RstId;
        //                            hrEmpWkUPD.StDate = obj.StDate;                              
        //                            hrEmpWkUPD.EndDate = hrEmpWkRoster.StDate.AddDays(-1);
        //                            hrEmpWkUPD.CndrAtndc = obj.CndrAtndc;
        //                            _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpWkUPD, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //                        }

        //                        HrEmpWkRoster hrEmpWkINS = new HrEmpWkRoster();

        //                        hrEmpWkINS.EmpSysId = itm.ESysId;
        //                        hrEmpWkINS.RstId = hrEmpWkRoster.RstId;
        //                        hrEmpWkINS.StDate = hrEmpWkRoster.StDate;
        //                        //hrEmpWkINS.EndDate = hrEmpWkRoster.EndDate;
        //                        hrEmpWkINS.CndrAtndc = obj==null ? hrEmpWkRoster.CndrAtndc : obj.CndrAtndc;
        //                        _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpWkINS, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //                    }
        //                }
        //                else if (hrEmpWkRoster.EmpTyp == "S") //Individual Employee
        //                {
        //                    refId = hrEmpWkRoster.EmpSysId;
        //                    var obj = _workShiftContext.GetEmpWrkRstrs().Where(x => x.EmpSysId == hrEmpWkRoster.EmpSysId && (x.EndDate >= hrEmpWkRoster.StDate || x.EndDate <= hrEmpWkRoster.EndDate || x.EndDate >= hrEmpWkRoster.EndDate)).FirstOrDefault(); //|| x.EndDate >= hrEmpWkRoster.EndDate
        //                    if (obj != null)
        //                    {
        //                        //Update the Current Roster with 
        //                        HrEmpWkRoster hrEmpWkUPD = new HrEmpWkRoster();
        //                        hrEmpWkUPD.RecId = obj.RecId;
        //                        hrEmpWkUPD.EmpSysId = obj.EmpSysId;
        //                        hrEmpWkUPD.RstId = hrEmpWkRoster.RstId;
        //                        hrEmpWkUPD.StDate = obj.StDate;
        //                        //hrEmpWkUPD.EndDate = hrEmpWkRoster.StDate.AddDays(-1);
        //                        hrEmpWkUPD.CndrAtndc = obj.CndrAtndc;
        //                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpWkUPD, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //                    }

        //                    HrEmpWkRoster hrEmpWkINS = new HrEmpWkRoster();
        //                    hrEmpWkINS.EmpSysId = hrEmpWkRoster.EmpSysId;
        //                    hrEmpWkINS.RstId = hrEmpWkRoster.RstId;
        //                    hrEmpWkINS.StDate = hrEmpWkRoster.StDate;
        //                    hrEmpWkINS.EndDate = hrEmpWkRoster.EndDate;
        //                    hrEmpWkINS.CndrAtndc = obj == null ? hrEmpWkRoster.CndrAtndc : obj.CndrAtndc;
        //                    _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpWkINS, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //                }
        //                //_dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpWkRoster, (int)HttpContext.Session.GetInt32("userEmpSysId"));

        //                //---------- Execute Load Attendance Process ----------//
        //                var prcSrcDtl = "Add new Employee Roster RecId:" + hrEmpWkRoster.RecId;
        //                DateTime toDate = hrEmpWkRoster.EndDate == null ? hrEmpWkRoster.StDate : (DateTime)hrEmpWkRoster.EndDate;
        //                _attnContext.LoadHrEmpAtndc(hrEmpWkRoster.EmpTyp, refId, hrEmpWkRoster.StDate, toDate, AttnContext.AtndcLdPrcTyp.RST_CNG, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //                //----------------------------------------------------//
        //                return RedirectToAction(nameof(Index)).WithSuccess("Employee Work Roster", "Added Successfully");
        //            }
        //            catch (Exception)
        //            {
        //                return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", "Adding Record got failed");
        //            }
        //        }
        //        else
        //        {
        //            return RedirectToAction(nameof(Index));
        //        }
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}


        [HttpGet]     
        public async Task<JsonResult> Create(string EmpTyp, int EmpSysId, int RstId, DateTime StDate, DateTime EndDate, bool CndrAtndc)
        {
            try
            {
                int usrId = (int)(HttpContext.Session.GetInt32("userEmpSysId") ?? 1);
                int entId = (int)(HttpContext.Session.GetInt32("userEntId") ?? 1);

                // 1. Employee Work Roster is assigned to IN-HOUSE employees ONLY (WrktypId == 1)
                var inHouseList = new List<int>();
                if (EmpTyp == "A") // All in-house employees
                {
                    inHouseList = _hrmContext.HrEmpGenInfo
                        .Where(e => e.CurrStat == 0 && e.WrktypId == 1)
                        .Select(e => e.EmpSysId)
                        .ToList();
                }
                else if (EmpTyp == "S") // Individual employee
                {
                    var isEmpInHouse = _hrmContext.HrEmpGenInfo
                        .Any(e => e.EmpSysId == EmpSysId && e.CurrStat == 0 && e.WrktypId == 1);

                    if (!isEmpInHouse)
                    {
                        return Json(new { ok = false, data = "Employee Work Roster can only be assigned to In-House employees.", message = "error" });
                    }
                    inHouseList.Add(EmpSysId);
                }

                if (inHouseList.Count == 0)
                {
                    return Json(new { ok = false, data = "No eligible In-House employees found.", message = "error" });
                }

                bool hasEndDate = EndDate != DateTime.MinValue && EndDate.ToString() != "01/01/0001 00:00:00" && EndDate >= StDate;

                // Identify active Normal Roster ID for auto-resumption
                int normalRosterId = 4;
                var activeNormalRst = _workShiftContext.GetAllHrWkRoster(entId)
                    .FirstOrDefault(r => r.RstCode == "RST_NRM" || r.RstId == 4);
                if (activeNormalRst != null) normalRosterId = activeNormalRst.RstId;

                // 2. Process Roster assignments & Seamless Ramadan <-> Normal switching
                foreach (var empId in inHouseList)
                {
                    // Find the existing active roster covering StDate
                    var currentRst = _hrmContext.HrEmpWkRoster
                        .Where(x => x.EmpSysId == empId && x.RecStat == 1 && (x.EndDate == null || x.EndDate >= StDate) && x.StDate <= StDate)
                        .OrderByDescending(x => x.StDate)
                        .FirstOrDefault();

                    int origRstId = currentRst != null ? currentRst.RstId : normalRosterId;
                    DateTime? origEndDate = currentRst != null ? currentRst.EndDate : null;

                    if (currentRst != null)
                    {
                        if (currentRst.StDate < StDate)
                        {
                            // Truncate previous roster to end the day before the new roster starts
                            currentRst.EndDate = StDate.AddDays(-1);
                            await _dbCommitContext.commitChangesAsync(LogEventTypes.UPD, currentRst, usrId);
                        }
                        else if (currentRst.StDate == StDate)
                        {
                            // Deactivate if exact start date match
                            currentRst.RecStat = 0;
                            await _dbCommitContext.commitChangesAsync(LogEventTypes.UPD, currentRst, usrId);
                        }
                    }

                    // Deactivate any existing future rosters that fall completely inside the new range
                    if (hasEndDate)
                    {
                        var insideRsts = _hrmContext.HrEmpWkRoster
                            .Where(x => x.EmpSysId == empId && x.RecStat == 1 && x.StDate >= StDate && x.StDate <= EndDate)
                            .ToList();
                        foreach (var r in insideRsts)
                        {
                            r.RecStat = 0;
                            await _dbCommitContext.commitChangesAsync(LogEventTypes.UPD, r, usrId);
                        }
                    }

                    // Insert new roster (e.g. Ramadan Roster)
                    HrEmpWkRoster insRst = new HrEmpWkRoster();
                    insRst.EmpSysId = empId;
                    insRst.RstId = RstId;
                    insRst.StDate = StDate;
                    insRst.EndDate = hasEndDate ? EndDate : (DateTime?)null;
                    insRst.CndrAtndc = currentRst == null ? CndrAtndc : currentRst.CndrAtndc;
                    insRst.RecStat = 1;
                    insRst.CrtDateTime = DateTime.Now;
                    await _dbCommitContext.commitChangesAsync(LogEventTypes.INS, insRst, usrId);

                    // 3. SWITCHING LOGIC: If a temporary roster (e.g. Ramadan for 1 month) is assigned with an EndDate,
                    // automatically resume the Normal Roster from EndDate + 1 onwards!
                    if (hasEndDate)
                    {
                        int resumeRstId = (origRstId > 0 && origRstId != RstId) ? origRstId : normalRosterId;
                        var nextDay = EndDate.AddDays(1);

                        // Check if a future roster is already scheduled on or after nextDay
                        var futureRst = _hrmContext.HrEmpWkRoster
                            .FirstOrDefault(x => x.EmpSysId == empId && x.RecStat == 1 && x.StDate >= nextDay);

                        if (futureRst == null)
                        {
                            HrEmpWkRoster resumeRst = new HrEmpWkRoster();
                            resumeRst.EmpSysId = empId;
                            resumeRst.RstId = resumeRstId;
                            resumeRst.StDate = nextDay;
                            resumeRst.EndDate = origEndDate ?? Convert.ToDateTime("2032-12-31");
                            resumeRst.CndrAtndc = insRst.CndrAtndc;
                            resumeRst.RecStat = 1;
                            resumeRst.CrtDateTime = DateTime.Now;
                            await _dbCommitContext.commitChangesAsync(LogEventTypes.INS, resumeRst, usrId);
                        }
                    }
                }

                // 4. Synchronize attendance logs for affected date range
                try
                {
                    DateTime toDate = hasEndDate ? EndDate : StDate.AddDays(30);
                    int refId = (EmpTyp == "S") ? EmpSysId : 0;
                    _attnContext.LoadHrEmpAtndc(EmpTyp, refId, StDate, toDate, AttnContext.AtndcLdPrcTyp.RST_CNG, "Roster Assignment (In-House)", false, usrId);
                }
                catch {}

                return Json(new { ok = true, data = "Employee Work Roster - Assigned Successfully to In-House Staff (" + inHouseList.Count + " employees)", message = "ok" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = "Employee Work Roster - Adding Record failed: " + ex.Message, message = "error" });
            }
        }



        [HttpGet]
        public IActionResult EditGet(int id)
        {
            try
            {
                HrEmpWkRoster hrEmpWkRoster = new HrEmpWkRoster();
                var hrEmpWkRosterVw = _workShiftContext.GetEmpWrkRstrs(id).FirstOrDefault();
                hrEmpWkRoster.RecId = hrEmpWkRosterVw.RecId;
                hrEmpWkRoster.EmpSysId = hrEmpWkRosterVw.EmpSysId;
                hrEmpWkRoster.RstId = hrEmpWkRosterVw.RstId;
                hrEmpWkRoster.StDate = hrEmpWkRosterVw.StDate;
                hrEmpWkRoster.EndDate = hrEmpWkRosterVw.EndDate;
                hrEmpWkRoster.CndrAtndc = hrEmpWkRosterVw.CndrAtndc;
                //hrEmpWkRoster.SkpRstVldtn = 0;

                ViewBag.lstEmp = _empContext.GetActvEmpNameLst(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                ViewBag.lstRstrs = _workShiftContext.GetAllHrWkRoster(Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")));
                return PartialView("_HrEmpWkRoster_Edit", hrEmpWkRoster);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", ex.Message);
            }                     
        }

        // POST: HrWkRoster/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpWkRoster hrEmpWkRoster)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        hrEmpWkRoster.RecStat = 1;                        
                        _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpWkRoster, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //---------- Execute Load Attendance Process ----------//
                        var prcSrcDtl = "Update Employee Roster RecId:" + hrEmpWkRoster.RecId;
                        DateTime toDate = hrEmpWkRoster.EndDate == null ? hrEmpWkRoster.StDate : (DateTime)hrEmpWkRoster.EndDate;
                        _attnContext.LoadHrEmpAtndc("S", hrEmpWkRoster.EmpSysId, hrEmpWkRoster.StDate, toDate, AttnContext.AtndcLdPrcTyp.RST_CNG, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        //----------------------------------------------------//
                        return RedirectToAction(nameof(Index)).WithSuccess("Employee Work Roster", "Updated Successfully");
                    }
                    catch (Exception)
                    {
                        return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", "Adding Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                return View();
            }
        }

        public IActionResult DeleteGet(int id)
        {
            try
            {
                HrEmpWkRoster hrEmpWkRoster = new HrEmpWkRoster();
                var hrEmpWkRosterVw = _workShiftContext.GetEmpWrkRstrs(id).FirstOrDefault();
                ViewBag.EmpName = hrEmpWkRosterVw.FullName;
                ViewBag.RstName = hrEmpWkRosterVw.RstName;
                hrEmpWkRoster.RecId = hrEmpWkRosterVw.RecId;
                hrEmpWkRoster.EmpSysId = hrEmpWkRosterVw.EmpSysId;
                hrEmpWkRoster.RstId = hrEmpWkRosterVw.RstId;
                hrEmpWkRoster.StDate = hrEmpWkRosterVw.StDate;
                hrEmpWkRoster.EndDate = hrEmpWkRosterVw.EndDate;
                hrEmpWkRoster.CndrAtndc = hrEmpWkRosterVw.CndrAtndc;                
                return PartialView("_HrEmpWkRoster_Delete", hrEmpWkRoster);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpWkRoster hrEmpWkRoster)
        {
            try
            {                                
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrEmpWkRoster, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                {
                    //---------- Execute Load Attendance Process ----------//
                    var prcSrcDtl = "Delete Employee Roster RecId:" + hrEmpWkRoster.RecId;
                    DateTime toDate = hrEmpWkRoster.EndDate == null ? hrEmpWkRoster.StDate : (DateTime)hrEmpWkRoster.EndDate;
                    _attnContext.LoadHrEmpAtndc("S", hrEmpWkRoster.EmpSysId, hrEmpWkRoster.StDate, toDate, AttnContext.AtndcLdPrcTyp.RST_CNG, prcSrcDtl, false, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    //----------------------------------------------------//
                    return RedirectToAction(nameof(Index)).WithSuccess("Employee Work Roster", "Record Deleted Successfully");
                }
                else
                    return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", "Deleting Record got failed");

            }
            catch
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", "Deleting Record got failed");
            }
        }

        // POST: HrEmpGrade/GetEmpNotCrtdRst
        [HttpGet]
        public IActionResult GetEmpNotCrtdRst()
        {
            try
            {
                ViewBag.DataSource = _workShiftContext.GetEmpNotCrtdRst();
                return PartialView("_HrEmpWkRoster_EmpNotCrtdRst");

            }
            catch
            {
                return RedirectToAction(nameof(Index)).WithWarning("Employee Work Roster", "Deleting Record got failed");
            }
        }

        public IActionResult IsRosterAssign([Bind(Prefix = "StDate")] DateTime StDate, [Bind(Prefix = "EndDate")] DateTime EndDate, [Bind(Prefix = "EmpSysId")] int empSysId, [Bind(Prefix = "RecId")] int recId, [Bind(Prefix = "SkpRstVldtn")] int skpRstVldtn)
        {
            if (recId == 0)
            {
                if (skpRstVldtn == 1) //Skip Roster Validation
                {
                    var _itemfound = _workShiftContext.IsRosterAssign(empSysId, StDate, EndDate);
                    if (_itemfound.Count() > 0)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }

                return Json(data: true);

            }
            else
            { 
               if (StDate > EndDate)
                    return Json(data: false);
               else
                    return Json(data: true);
            }         
            
        }

        //public JsonResult Validate([Bind(Prefix = "EmpSysId")] int empSysId, [Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        //{
        //    IEnumerable<AttnContext.HrEmpAttendExpView> _obj;
        //    if (empSysId == 0)
        //        _obj = _attdContext.GetHrEmpAttendExeption().Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate));
        //    else
        //        _obj = _attdContext.GetHrEmpAttendExeption().Where(x => (x.StDate >= stDate && x.StDate <= endDate || x.EndDate >= stDate && x.EndDate <= endDate) && x.EmpSysId == empSysId);


        //    if (_obj.Count() > 0)
        //    {
        //        return Json("There is a record already existing for the selected date range");
        //    }
        //    if (endDate < stDate)
        //    {
        //        return Json("Invalid date range");
        //    }
        //    return Json(true);
        //}

        //public IActionResult ValidateDateRange([Bind(Prefix = "Del_StDate")] DateTime? stDate, [Bind(Prefix = "Del_EndDate")] DateTime? endDate)
        //{
        //    if (endDate >= stDate)
        //    {
        //        return Json(true);
        //    }
        //    return Json(false);
        //}

        //public IActionResult Test()
        //{
        //    try
        //    {
        //        //Employee
        //        ViewBag.Employees = _empContext.GetEmpForWkRstr("_all",
        //                            Convert.ToInt32(HttpContext.Session.GetInt32("userEntId")),
        //                            Convert.ToInt32(HttpContext.Session.GetInt32("userEmpSysId"))).ToList();
        //        ViewBag.Resources = new string[] { "Employee" };

        //        //Schedule
        //        //ViewBag.dataSource = _workShiftContext.GetWkRstrData("SHFT_VW", 0, "2020-09-01", "2020-09-30");

        //        //List<EmployeeData> employees = new List<EmployeeData>();
        //        //employees.Add(new EmployeeData { text = "Alice", id = 1, groupId = 1, color = "#bbdc00", designation = "Content writer" });
        //        //employees.Add(new EmployeeData { text = "Nancy", id = 2, groupId = 1, color = "#9e5fff", designation = "Designer" });
        //        //employees.Add(new EmployeeData { text = "Robert", id = 3, groupId = 1, color = "#bbdc00", designation = "Software Engineer" });
        //        //employees.Add(new EmployeeData { text = "Robson", id = 4, groupId = 1, color = "#9e5fff", designation = "Support Engineer" });
        //        //employees.Add(new EmployeeData { text = "Laura", id = 5, groupId = 1, color = "#bbdc00", designation = "Human Resource" });
        //        //employees.Add(new EmployeeData { text = "Margaret", id = 6, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "Gaya", id = 7, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "Matt", id = 8, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "Prior", id = 9, groupId =1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "Test", id = 10, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "PK", id = 11, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //employees.Add(new EmployeeData { text = "Arshad", id = 12, groupId = 1, color = "#9e5fff", designation = "Content Analyst" });
        //        //ViewBag.Employees = employees;

        //        return View();
        //    }
        //    catch (Exception ex) { throw ex; }
        //}

        //public partial class EmployeeData
        //{
        //    public string text { set; get; }
        //    public int id { set; get; }
        //    public int groupId { set; get; }
        //    public string color { set; get; }
        //    public string designation { set; get; }
        //}

        //public class ScheduleData
        //{
        //    public List<AppointmentData> GetScheduleData()
        //    {
        //        List<AppointmentData> appData = new List<AppointmentData>();
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 1,
        //            Subject = "Explosion of Betelgeuse Star",
        //            StartTime = new DateTime(2020, 2, 11, 9, 30, 0),
        //            EndTime = new DateTime(2020, 2, 11, 11, 0, 0),
        //            CategoryColor = "#1aaa55",
        //            EmployeeId = 1
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 2,
        //            Subject = "Thule Air Crash Report",
        //            StartTime = new DateTime(2017, 2, 12, 12, 0, 0),
        //            EndTime = new DateTime(2017, 2, 12, 14, 0, 0),
        //            CategoryColor = "#357cd2",
        //            EmployeeId = 1
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 3,
        //            Subject = "Blue Moon Eclipse",
        //            StartTime = new DateTime(2017, 2, 13, 9, 30, 0),
        //            EndTime = new DateTime(2017, 2, 13, 11, 0, 0),
        //            CategoryColor = "#7fa900",
        //            EmployeeId = 2
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 4,
        //            Subject = "Meteor Showers in 2018",
        //            StartTime = new DateTime(2017, 2, 14, 13, 0, 0),
        //            EndTime = new DateTime(2017, 2, 14, 14, 30, 0),
        //            CategoryColor = "#ea7a57",
        //            EmployeeId = 2
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 5,
        //            Subject = "Milky Way as Melting pot",
        //            StartTime = new DateTime(2017, 2, 15, 12, 0, 0),
        //            EndTime = new DateTime(2017, 2, 15, 14, 0, 0),
        //            CategoryColor = "#00bdae",
        //            EmployeeId = 3
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 6,
        //            Subject = "Mysteries of Bermuda Triangle",
        //            StartTime = new DateTime(2018, 2, 15, 9, 30, 0),
        //            EndTime = new DateTime(2018, 2, 15, 11, 0, 0),
        //            CategoryColor = "#f57f17",
        //            EmployeeId = 3
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 7,
        //            Subject = "Glaciers and Snowflakes",
        //            StartTime = new DateTime(2018, 2, 16, 11, 0, 0),
        //            EndTime = new DateTime(2018, 2, 16, 12, 30, 0),
        //            CategoryColor = "#1aaa55",
        //            EmployeeId = 4
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 8,
        //            Subject = "Life on Mars",
        //            StartTime = new DateTime(2018, 2, 17, 9, 0, 0),
        //            EndTime = new DateTime(2018, 2, 17, 10, 0, 0),
        //            CategoryColor = "#357cd2",
        //            EmployeeId = 1
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 9,
        //            Subject = "Alien Civilization",
        //            StartTime = new DateTime(2018, 2, 19, 11, 0, 0),
        //            EndTime = new DateTime(2018, 2, 19, 13, 0, 0),
        //            CategoryColor = "#7fa900",
        //            EmployeeId = 2
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 10,
        //            Subject = "Wildlife Galleries",
        //            StartTime = new DateTime(2018, 2, 21, 11, 0, 0),
        //            EndTime = new DateTime(2018, 2, 21, 13, 0, 0),
        //            CategoryColor = "#ea7a57",
        //            EmployeeId = 3
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 11,
        //            Subject = "Best Photography 2018",
        //            StartTime = new DateTime(2018, 2, 22, 9, 30, 0),
        //            EndTime = new DateTime(2018, 2, 22, 11, 0, 0),
        //            CategoryColor = "#00bdae",
        //            EmployeeId = 3
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 12,
        //            Subject = "Smarter Puppies",
        //            StartTime = new DateTime(2018, 2, 9, 10, 0, 0),
        //            EndTime = new DateTime(2018, 2, 9, 11, 30, 0),
        //            CategoryColor = "#f57f17",
        //            EmployeeId = 3
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 13,
        //            Subject = "Myths of Andromeda Galaxy",
        //            StartTime = new DateTime(2018, 2, 7, 10, 30, 0),
        //            EndTime = new DateTime(2018, 2, 7, 12, 30, 0),
        //            CategoryColor = "#1aaa55",
        //            EmployeeId = 4
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 14,
        //            Subject = "Aliens vs Humans",
        //            StartTime = new DateTime(2018, 2, 5, 10, 0, 0),
        //            EndTime = new DateTime(2018, 2, 5, 11, 30, 0),
        //            CategoryColor = "#357cd2",
        //            EmployeeId = 9
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 15,
        //            Subject = "Facts of Humming Birds",
        //            StartTime = new DateTime(2018, 2, 20, 9, 30, 0),
        //            EndTime = new DateTime(2018, 2, 20, 11, 0, 0),
        //            CategoryColor = "#7fa900",
        //            EmployeeId = 4
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 16,
        //            Subject = "Sky Gazers",
        //            StartTime = new DateTime(2018, 2, 23, 11, 0, 0),
        //            EndTime = new DateTime(2018, 2, 23, 13, 0, 0),
        //            CategoryColor = "#ea7a57",
        //            EmployeeId = 1
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 17,
        //            Subject = "The Cycle of Seasons",
        //            StartTime = new DateTime(2018, 2, 12, 5, 30, 0),
        //            EndTime = new DateTime(2018, 2, 12, 7, 30, 0),
        //            CategoryColor = "#00bdae",
        //            EmployeeId = 8
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 18,
        //            Subject = "Space Galaxies and Planets",
        //            StartTime = new DateTime(2018, 2, 12, 17, 0, 0),
        //            EndTime = new DateTime(2018, 2, 12, 18, 30, 0),
        //            CategoryColor = "#f57f17",
        //            EmployeeId = 11
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 19,
        //            Subject = "Lifecycle of Bumblebee",
        //            StartTime = new DateTime(2018, 2, 15, 6, 0, 0),
        //            EndTime = new DateTime(2018, 2, 15, 7, 30, 0),
        //            CategoryColor = "#7fa900",
        //            EmployeeId = 7
        //        });
        //        appData.Add(new AppointmentData
        //        {
        //            Id = 20,
        //            Subject = "Sky Gazers",
        //            StartTime = new DateTime(2018, 2, 15, 16, 0, 0),
        //            EndTime = new DateTime(2018, 2, 15, 18, 0, 0),
        //            CategoryColor = "#ea7a57",
        //            EmployeeId = 10
        //        });
        //        return appData;
        //    }

        //    public class AppointmentData
        //    {
        //        public int Id { get; set; }
        //        public string Subject { get; set; }
        //        public DateTime StartTime { get; set; }
        //        public DateTime EndTime { get; set; }
        //        public string CategoryColor { get; set; }
        //        public int EmployeeId { get; set; }
        //    }
        //}

        //public JsonResult ListTest(List<int> ids)
        //{
        //    var list = ids;
        //    return Json(list.Count);
        //}
    }
}
