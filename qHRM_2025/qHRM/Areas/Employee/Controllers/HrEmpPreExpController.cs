using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using qHRM.Data;
using qHRM.Areas.Employee.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpPreExpController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpPreExp> _dbCommitContext;
        private readonly AdmUserGrpPrvContext _prvContext;
        private IHostingEnvironment _hostingEnv;
        


        public HrEmpPreExpController(EmployeeContext employeeContext,
                                     DbCommitContext<HrEmpPreExp> dbCommitContext,
                                     IHostingEnvironment hostingEnv,
                                     AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._hostingEnv = hostingEnv;
            this._dbCommitContext = dbCommitContext;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index(int Id)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_PRV_EXP") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var list = _employeeContext.GetEmpExpEmpSysId(Id).ToList();
                ViewBag.DataSource = list;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRV_EXP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                return View();
            }
            catch (Exception)
            {
                throw;
            }

        }

        // GET: HrEmpGrade/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpPreExp hrExp)
        {
            try
            {
                @ViewBag.Today = DateTime.Today;
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRV_EXP").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;            
                //End | Usr Prv//
                return PartialView("_HrEmpPreExp_Create", hrExp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpPreExp hrExp, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            try
            {
                if (ModelState.IsValid)
                {                                  
                    try
                    {
                        hrExp.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                        
                        hrExp.CrtDateTime = DateTime.Today;

                        try
                        {
                            foreach (var file in fileUpload)
                            {                                
                                var filename = ContentDispositionHeaderValue
                                                    .Parse(file.ContentDisposition)
                                                    .FileName
                                                    .Trim('"');

                                _ext = Path.GetExtension(filename);
                                _cusFilename = Guid.NewGuid().ToString() + _ext;
                                filename = _hostingEnv.WebRootPath + @"\attach\empPreExp\" + $@"\{_cusFilename}";                                
                                long size = 0;
                                size += file.Length;
                                if (!System.IO.File.Exists(filename))
                                {
                                    using (FileStream fs = System.IO.File.Create(filename))
                                    {
                                        file.CopyTo(fs);
                                        fs.Flush();
                                    }
                                }
                                hrExp.SvcFileName = _cusFilename;
                                hrExp.SvcFileMime = file.ContentType;
                            }
                        }
                        catch (Exception ex)
                        {
                            Response.Clear();
                            Response.StatusCode = 204;
                            return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);                            
                        }

                        int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Added Successfully");
                        else                            
                        return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                    }
                    catch (Exception)  {
                        
                        return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                    }
                }
                else
                {
                     return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId });
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpGrade/Edit/5
        public ActionResult EditGet(int id)
        {

            var hrExp = _employeeContext.GetEmpExpById(id).FirstOrDefault();
            //hrExp.ex_CompName = hrExp.CompName;
            hrExp.ex_SvcFileName = hrExp.SvcFileName;
            hrExp.ex_SvcFileMime = hrExp.SvcFileMime;
            
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRV_EXP").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//

            return PartialView("_HrEmpPreExp_Edit", hrExp);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpPreExp hrExp, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        try
                        {
                            hrExp.SvcFileName = hrExp.ex_SvcFileName;
                            hrExp.SvcFileMime = hrExp.ex_SvcFileMime;
                            //--Save File -----------------------------------------------------------------------------------//
                            foreach (var file in fileUpload)
                            {
                                var filename = ContentDispositionHeaderValue
                                                    .Parse(file.ContentDisposition)
                                                    .FileName
                                                    .Trim('"');

                                _ext = Path.GetExtension(filename);
                                _cusFilename = Guid.NewGuid().ToString() + _ext;
                                filename = _hostingEnv.WebRootPath + @"\attach\empPreExp\" + $@"\{_cusFilename}";
                                using (FileStream fs = System.IO.File.Create(filename))
                                {
                                    file.CopyTo(fs);
                                    fs.Flush();
                                }
                                hrExp.SvcFileName = _cusFilename;
                                hrExp.SvcFileMime = file.ContentType;

                                //--Delete Existing File-----------------------------------------------------------------------------//
                                try
                                {
                                    var exfilename = _hostingEnv.WebRootPath + @"\attach\empPreExp\" + $@"\{hrExp.ex_SvcFileName}";
                                    System.IO.File.Delete(exfilename);
                                }
                                catch
                                { }
                                //------------------------------------------------------------------------------------------------//
                            }
                        }
                        catch (Exception ex)
                        {
                            Response.Clear();
                            Response.StatusCode = 204;
                            return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                        }


                        hrExp.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                        
                        hrExp.CrtDateTime = DateTime.Today;
                        var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Updated Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                    catch (Exception)
                    {
                        
                        return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId });
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: HrEmpGrade/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var hrExp = _employeeContext.GetEmpExpById(id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PRV_EXP").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpPreExp_Delete", hrExp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpPreExp hrExp)
        {
            try
            {
                //--Delete Existing File-----------------------------------------------------------------------------//
                try
                {
                    var exfilename = _hostingEnv.WebRootPath + @"\attach\empPreExp\" + $@"\{hrExp.SvcFileName}";
                    System.IO.File.Delete(exfilename);
                }
                catch
                { }
                //------------------------------------------------------------------------------------------------//
                hrExp.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
                
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpPreExp", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }


        //public IActionResult IsExistCompanyName([Bind(Prefix = "CompName")] string value, [Bind(Prefix = "ex_CompName")] string exvalue)
        //{
        //    if (exvalue != null)
        //    {
        //        if (value != exvalue)
        //        {
        //            var _itemfound = _employeeContext.GetEmpExpByCompany(value).FirstOrDefault();
        //            if (_itemfound != null)
        //                return Json(data: false);
        //            else
        //                return Json(data: true);
        //        }
        //    }
        //    else
        //    {
        //        var _itemfound = _employeeContext.GetEmpExpByCompany(value).FirstOrDefault();
        //        if (_itemfound != null)                
        //            return Json(data: false);
        //        else
        //            return Json(data: true);

        //    }
        //    return Json(data: true);
        //}
        
        public IActionResult ValidateDateRange([Bind(Prefix = "StDate")] DateTime? stDate, [Bind(Prefix = "EndDate")] DateTime? endDate)
        {
            if (endDate > stDate)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}