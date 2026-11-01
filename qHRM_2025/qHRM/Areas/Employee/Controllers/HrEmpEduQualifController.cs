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
    public class HrEmpEduQualifController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpEduQualif> _dbCommitContext;
        [Obsolete]
        private IHostingEnvironment _hostingEnv;
        private readonly AdmUserGrpPrvContext _prvContext;

        [Obsolete]
        public HrEmpEduQualifController(EmployeeContext employeeContext,
                                     DbCommitContext<HrEmpEduQualif> dbCommitContext,
                                     IHostingEnvironment hostingEnv,
                                     AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._dbCommitContext = dbCommitContext;
            this._hostingEnv = hostingEnv;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index(int Id)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_EDU_CLF") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
                var list = _employeeContext.GetQualficationEmpSysId(Id).ToList();
                ViewBag.DataSource = list;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_EDU_CLF").FirstOrDefault();
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
        public IActionResult CreateNew(HrEmpEduQualif hrExp)
        {
            try
            {
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_EDU_CLF").FirstOrDefault();
                hrExp.StDate = DateTime.Today;
                hrExp.EndDate = DateTime.Today;
                ViewBag.RecAdd = objprv.RecAdd;                
                //End | Usr Prv//
                return PartialView("_HrEmpEduQualif_Create", hrExp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpEduQualif hrExp, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            try
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
                                filename = _hostingEnv.WebRootPath + @"\attach\empEduQualif\" + $@"\{_cusFilename}";                                
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
                                hrExp.CertName = _cusFilename;
                                hrExp.CertMime= file.ContentType;
                            }
                        }
                        catch (Exception ex)
                        {
                            Response.Clear();
                            Response.StatusCode = 204;
                            return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);                            
                        }

                        int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Added Successfully");
                        else                            
                        return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
                           }
                    catch (Exception)  {
                        
                        return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Adding Record got failed");
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

            var hrExp = _employeeContext.GetQualficationById(id).FirstOrDefault();            
            hrExp.ex_CertName = hrExp.CertName;
            hrExp.ex_CertMime = hrExp.CertMime;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_EDU_CLF").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//
            return PartialView("_HrEmpEduQualif_Edit", hrExp);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpEduQualif hrExp, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _filename; //Customer File Name
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        try
                        {
                            hrExp.CertName = hrExp.ex_CertName;
                            hrExp.CertMime = hrExp.ex_CertMime;
                            //--Save File -----------------------------------------------------------------------------------//
                            foreach (var file in fileUpload)
                            {
                                var filename = ContentDispositionHeaderValue
                                                    .Parse(file.ContentDisposition)
                                                    .FileName
                                                    .Trim('"');

                                _ext = Path.GetExtension(filename);
                                _filename = Guid.NewGuid().ToString() + _ext;
                                filename = _hostingEnv.WebRootPath + @"\attach\empEduQualif\" + $@"\{_filename}";
                                using (FileStream fs = System.IO.File.Create(filename))
                                {
                                    file.CopyTo(fs);
                                    fs.Flush();
                                }
                                hrExp.CertName = _filename;
                                hrExp.CertMime = file.ContentType;

                                //--Delete Existing File-----------------------------------------------------------------------------//
                                try
                                {
                                    var exfilename = _hostingEnv.WebRootPath + @"\attach\empEduQualif\" + $@"\{hrExp.ex_CertName}";
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
                            return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                        }


                        hrExp.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                        
                        hrExp.CrtDateTime = DateTime.Today;
                        var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                        if (_result == 1)
                            return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Updated Successfully");
                        else
                            return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                    catch (Exception)
                    {
                        
                        return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Updating Record got failed");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId });
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
                var hrExp = _employeeContext.GetQualficationById(id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_EDU_CLF").FirstOrDefault();                
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpEduQualif_Delete", hrExp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpEduQualif hrExp)
        {
            try
            {
                //--Delete Existing File-----------------------------------------------------------------------------//
                try
                {
                    var exfilename = _hostingEnv.WebRootPath + @"\attach\empEduQualif\" + $@"\{hrExp.CertName}";
                    System.IO.File.Delete(exfilename);
                }
                catch
                { }
                //------------------------------------------------------------------------------------------------//
                hrExp.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrExp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
                
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpEduQualif", new { @Id = hrExp.EmpSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }

           
        
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