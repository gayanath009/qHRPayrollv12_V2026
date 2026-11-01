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
    public class HrEmpPhotoController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly DbCommitContext<HrEmpPhoto> _dbCommitContext;
        private IHostingEnvironment _hostingEnv;
        private readonly AdmUserGrpPrvContext _prvContext;
        private static int PhtId;

        
        public HrEmpPhotoController(EmployeeContext employeeContext,
                                      DbCommitContext<HrEmpPhoto> dbCommitContext,
                                      IHostingEnvironment hostingEnv,
                                       AdmUserGrpPrvContext prvContext)
        {
            _dbCommitContext = dbCommitContext;
            this._employeeContext = employeeContext;
            this._hostingEnv = hostingEnv;
            this._prvContext = prvContext;
        }

        // GET: HrEmpGrade
        public ActionResult Index(int Id)
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("EM_PHT_DTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                var list = _employeeContext.GetPhotosByEmpSysId(Id).ToList();
                ViewBag.DataSource = list;
                PhtId = list.Count + 1;

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PHT_DTL").FirstOrDefault();
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
        public IActionResult CreateNew(HrEmpPhoto HrEmpPhoto)
        {
            try
            {
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PHT_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;                
                //End | Usr Prv//

                return PartialView("_HrEmpPhoto_Create", HrEmpPhoto);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpPhoto hrEmpDpnd, IList<IFormFile> fileUpload)
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
                            filename = _hostingEnv.WebRootPath + @"\attach\empImages\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                            hrEmpDpnd.PhotoName = _cusFilename;
                            hrEmpDpnd.PhotoMime = file.ContentType;
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.Clear();
                        Response.StatusCode = 204;
                        return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
                    }

                    hrEmpDpnd.EmpSysId = _empSysId;
                    hrEmpDpnd.PhtId = PhtId;
                    hrEmpDpnd.Photo = new byte[] { 0 };
                    int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithSuccess("QCS", "Record Added Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "Adding Record got failed");
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

        //// GET: HrEmpGrade/Edit/5
        //public ActionResult EditGet(int id)
        //{
        //    int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
        //    var hrEmpDpnd = _employeeContext.GetPhotoByPhotoId(empSysId, id).FirstOrDefault();
        //    hrEmpDpnd.ex_PhotoName = hrEmpDpnd.PhotoName;
        //    hrEmpDpnd.ex_PhotoMime = hrEmpDpnd.PhotoMime;
        //    return PartialView("_HrEmpPhoto_Edit", hrEmpDpnd);
        //}

        //// POST: HrEmpGrade/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Edit(HrEmpPhoto hrEmpDpnd, IList<IFormFile> fileUpload)
        //{
        //    string _ext; //File Extention
        //    string _cusFilename; //Customer File Name            
        //    int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));

        //    try
        //    {
        //        try
        //        {
        //            try
        //            {
        //                hrEmpDpnd.PhotoName = hrEmpDpnd.ex_PhotoName;
        //                hrEmpDpnd.PhotoMime = hrEmpDpnd.ex_PhotoMime;
        //                //--Save File -----------------------------------------------------------------------------------//
        //                foreach (var file in fileUpload)
        //                {
        //                    var filename = ContentDispositionHeaderValue
        //                                        .Parse(file.ContentDisposition)
        //                                        .FileName
        //                                        .Trim('"');

        //                    _ext = Path.GetExtension(filename);
        //                    _cusFilename = Guid.NewGuid().ToString() + _ext;
        //                    filename = _hostingEnv.WebRootPath + @"\attach\empImages\" + $@"\{_cusFilename}";
        //                    using (FileStream fs = System.IO.File.Create(filename))
        //                    {
        //                        file.CopyTo(fs);
        //                        fs.Flush();
        //                    }
        //                    hrEmpDpnd.PhotoName = _cusFilename;
        //                    hrEmpDpnd.PhotoMime = file.ContentType;

        //                    //--Delete Existing File-----------------------------------------------------------------------------//
        //                    try
        //                    {
        //                        var exfilename = _hostingEnv.WebRootPath + @"\attach\empImages\" + $@"\{hrEmpDpnd.ex_PhotoName}";
        //                        System.IO.File.Delete(exfilename);
        //                    }
        //                    catch
        //                    { }
        //                    //------------------------------------------------------------------------------------------------//
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Response.Clear();
        //                Response.StatusCode = 204;
        //                return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "File failed to upload - " + ex.Message);
        //            }

        //            hrEmpDpnd.Photo = new byte[] { 0 };
        //            var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
        //            if (_result == 1)
        //                return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithSuccess("QCS", "Record Updated Successfully");
        //            else
        //                return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "Updating Record got failed");
        //        }
        //        catch (Exception)
        //        {

        //            return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "Updating Record got failed");
        //        }

        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        // GET: HrEmpGrade/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var hrEmpPhoto = _employeeContext.GetPhotoByPhotoId(empSysId, id).FirstOrDefault();

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_PHT_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;                
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpPhoto_Delete", hrEmpPhoto);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("QCS", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpPhoto hrEmpPhoto)
        {
            int _empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));

            try
            {
                var hrEmpPhotoEx = _employeeContext.GetPhotoByPhotoId(hrEmpPhoto.EmpSysId, hrEmpPhoto.PhtId).FirstOrDefault();
                //--Delete Existing File-----------------------------------------------------------------------------//
                var exfilename = _hostingEnv.WebRootPath + @"\attach\empImages\" + $@"\{hrEmpPhotoEx.PhotoName}";
                    System.IO.File.Delete(exfilename);

                var _result = _employeeContext.DeleteEmpPhoto(hrEmpPhotoEx);
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithSuccess("QCS", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
            catch
            {
                return RedirectToAction("Index", "HrEmpPhoto", new { @Id = _empSysId }).WithWarning("QCS", "Deleting Record got failed");
            }
        }
    }
}