using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.Employee.Models;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.SysAdmin.Data;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using qHRM.Data;
using qHRM.Areas.Employee.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpDpndDocController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpDpndDoc> _dbCommitContext;
        private IHostingEnvironment _hostingEnv;
        private readonly AdmUserGrpPrvContext _prvContext;

        public HrEmpDpndDocController(EmployeeContext employeeContext,
                                      SysSelectListContext sysSelectListContext,
                                      DbCommitContext<HrEmpDpndDoc> dbCommitContext,
                                      IHostingEnvironment hostingEnv,
                                      AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._sysSelectListContext = sysSelectListContext;
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
                if (HttpContext.Session.GetString("EM_DEP_DOC") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DOC").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                var list = _employeeContext.GetDocsByDpndId(Id).ToList();
                HttpContext.Session.SetInt32("DpndId", Id); //Adding Dependant ID to the Session
                ViewBag.DataSource = list;
                return View();
            }
            catch (Exception)
            {
                throw;
            }

        }

        // GET: HrEmpGrade/Create
        [HttpGet]
        public IActionResult CreateNew(HrEmpDpndDoc hrEmpDpndDoc)
        {
            try
            {
                ViewBag.lstDpdDocTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_DOC_TYP);
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DOC").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;   
                //End | Usr Prv//
                return PartialView("_HrEmpDpndDoc_Create", hrEmpDpndDoc);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpDpndDoc hrEmpDpnd,IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            int _dpndId = Convert.ToInt32(HttpContext.Session.GetInt32("DpndId"));

            try
            {
                try
                {
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
                            filename = _hostingEnv.WebRootPath + @"\attach\depndDocs\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                            hrEmpDpnd.DocName = _cusFilename;
                            hrEmpDpnd.DocMime = file.ContentType;
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.Clear();
                        Response.StatusCode = 204;
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                    }

                    hrEmpDpnd.DpndId = _dpndId;                 
                    hrEmpDpnd.ScnCopy = new byte[] { 0 };
                    int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithSuccess("QCS", "Record Added Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "Adding Record got failed");
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = _dpndId }).WithWarning("QCS", "Adding Record got failed");
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
            int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            var hrEmpDpnd = _employeeContext.GetDpnDocsByDocId(id).FirstOrDefault();
            ViewBag.lstDpdDocTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_DOC_TYP);
            hrEmpDpnd.ex_DocName = hrEmpDpnd.DocName;
            hrEmpDpnd.ex_DocName = hrEmpDpnd.DocMime;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DOC").FirstOrDefault();            
            ViewBag.RecEdit = objprv.RecEdit;            
            //End | Usr Prv//
            return PartialView("_HrEmpDpndDoc_Edit", hrEmpDpnd);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpDpndDoc hrEmpDpnd, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name            
            int _dpndId = Convert.ToInt32(HttpContext.Session.GetInt32("DpndId"));

            try
            {
                try
                {
                    try
                    {
                        hrEmpDpnd.DocName = hrEmpDpnd.ex_DocName;
                        hrEmpDpnd.DocMime = hrEmpDpnd.ex_DocMime;
                        //--Save File -----------------------------------------------------------------------------------//
                        foreach (var file in fileUpload)
                        {
                            var filename = ContentDispositionHeaderValue
                                                .Parse(file.ContentDisposition)
                                                .FileName
                                                .Trim('"');

                            _ext = Path.GetExtension(filename);
                            _cusFilename = Guid.NewGuid().ToString() + _ext;
                            filename = _hostingEnv.WebRootPath + @"\attach\depndDocs\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                            hrEmpDpnd.DocName = _cusFilename;
                            hrEmpDpnd.DocMime = file.ContentType;

                            //--Delete Existing File-----------------------------------------------------------------------------//
                            try
                            {                                
                                var exfilename = _hostingEnv.WebRootPath + @"\attach\depndDocs\" + $@"\{hrEmpDpnd.ex_DocName}";
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
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                    }



                    hrEmpDpnd.ScnCopy = new byte[] { 0 };
                    var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithSuccess("QCS", "Record Updated Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "Updating Record got failed");
                }
                catch (Exception)
                {

                    return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "Updating Record got failed");
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
                int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));                
                var hrEmpDpnd = _employeeContext.GetDpnDocsByDocId(id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DOC").FirstOrDefault();            
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpDpndDoc_Delete", hrEmpDpnd);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpDpndDoc hrEmpDpnd)
        {
            //int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            int _dpndId = Convert.ToInt32(HttpContext.Session.GetInt32("DpndId"));
            try
            {
                //--Delete Existing File-----------------------------------------------------------------------------//
                try
                {                    
                    var exfilename = _hostingEnv.WebRootPath + @"\attach\depndDocs\" + $@"\{hrEmpDpnd.DocName}";
                    System.IO.File.Delete(exfilename);
                }
                catch
                { }
                

                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));

                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "Deleting Record got failed");

            }
            catch
            {
                return RedirectToAction("Index", "HrEmpDpndDoc", new { @Id = _dpndId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }
    }
}