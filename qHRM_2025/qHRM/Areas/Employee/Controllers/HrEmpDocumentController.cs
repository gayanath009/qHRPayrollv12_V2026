using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qHRM.Models;
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
    public class HrEmpDocumentController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpDocument> _dbCommitContext;
        private IHostingEnvironment _hostingEnv;
        private readonly AdmUserGrpPrvContext _prvContext;



        public HrEmpDocumentController(EmployeeContext employeeContext,
                                      SysSelectListContext sysSelectListContext,
                                       DbCommitContext<HrEmpDocument> dbCommitContext,
                                      IHostingEnvironment hostingEnv,
                                       AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._sysSelectListContext = sysSelectListContext;
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
                if (HttpContext.Session.GetString("EM_DOC_SMR") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DOC_SMR").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                var list = _employeeContext.GetEmpDocsByEmpSysId(Id).ToList();                
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
        public IActionResult CreateNew(HrEmpDocument HrEmpDocument)
        {
            try
            {
                ViewBag.lstDpdDocTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_DOC_TYP);
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DOC_SMR").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;               
                //End | Usr Prv//
                return PartialView("_HrEmpDocument_Create", HrEmpDocument);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpDocument hrEmp,IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));           

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
                            filename = _hostingEnv.WebRootPath + @"\attach\empDocs\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }

                            hrEmp.DocName = _cusFilename;
                            hrEmp.DocMime = file.ContentType;
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.Clear();
                        Response.StatusCode = 204;
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                    }

                    hrEmp.EmpSysId = _empSysId;
                    hrEmp.ScnCopy = new byte[] { 0 };
                    int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithSuccess("QCS", "Record Added Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", "Adding Record got failed");
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = _empSysId }).WithWarning("QCS", "Adding Record got failed");
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
            var hrEmp = _employeeContext.GetEmpDocsByDocId(id).FirstOrDefault();
            ViewBag.lstDpdDocTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.EMP_DOC_TYP);
            hrEmp.ex_DocName = hrEmp.DocName;
            hrEmp.ex_DocMime = hrEmp.DocMime ;
            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DOC_SMR").FirstOrDefault();
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//
            return PartialView("_HrEmpDocument_Edit", hrEmp);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpDocument hrEmp, IList<IFormFile> fileUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name            
            int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));

            try
            {
                try
                {
                    try
                    {

                        hrEmp.DocName = hrEmp.ex_DocName;
                        hrEmp.DocMime = hrEmp.ex_DocMime;

                        foreach (var file in fileUpload)
                        {
                            var filename = ContentDispositionHeaderValue
                                                .Parse(file.ContentDisposition)
                                                .FileName
                                                .Trim('"');

                            _ext = Path.GetExtension(filename);
                            _cusFilename = Guid.NewGuid().ToString() + _ext;
                            filename = _hostingEnv.WebRootPath + @"\attach\empDocs\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                            hrEmp.DocName = _cusFilename;
                            hrEmp.DocMime = file.ContentType;


                            //--Delete Existing File-----------------------------------------------------------------------------//
                            try
                            {
                                var exfilename = _hostingEnv.WebRootPath + @"\attach\empDocs\" + $@"\{hrEmp.ex_DocName}";
                                System.IO.File.Delete(exfilename);
                            }
                            catch
                            { throw; }
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.Clear();
                        Response.StatusCode = 204;
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                    }

                    hrEmp.EmpSysId = _empSysId;
                    hrEmp.ScnCopy = new byte[] { 0 };
                    var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithSuccess("QCS", "Record Updated Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", "Updating Record got failed");
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", ex.Message);
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
                var hrEmp = _employeeContext.GetEmpDocsByDocId(id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DOC_SMR").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpDocument_Delete", hrEmp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpDocument hrEmp)
        {   
            int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
            try
            {
                //--Delete Existing File-----------------------------------------------------------------------------//
                try
                {
                    var exfilename = _hostingEnv.WebRootPath + @"\attach\empDocs\" + $@"\{hrEmp.DocName}";
                    System.IO.File.Delete(exfilename);
                }
                catch
                { }

                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrEmp, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", "Deleting Record got failed");

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "HrEmpDocument", new { @Id = _empSysId }).WithWarning("QCS", ex.Message);
            }
        }

                      

    }
}