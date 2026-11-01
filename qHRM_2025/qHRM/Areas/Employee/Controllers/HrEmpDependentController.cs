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
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using qHRM.Data;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.General.Data;

namespace qHRM.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize]
    public class HrEmpDependentController : Controller
    {
        private readonly EmployeeContext _employeeContext;
        private readonly OrganizationContext _organizationContext;
        private readonly SysSelectListContext _sysSelectListContext;
        private readonly DbCommitContext<HrEmpDependent> _dbCommitContext;
        private IHostingEnvironment _hostingEnv;
        private readonly AdmUserGrpPrvContext _prvContext;


        public HrEmpDependentController(EmployeeContext employeeContext,
                                       OrganizationContext organizationContext,
                                       SysSelectListContext sysSelectListContext,
                                       DbCommitContext<HrEmpDependent> dbCommitContext,
                                       IHostingEnvironment hostingEnv,
                                       AdmUserGrpPrvContext prvContext)
        {
            this._employeeContext = employeeContext;
            this._organizationContext = organizationContext;
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
                if (HttpContext.Session.GetString("EM_DEP_DTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;
                ViewBag.RecEdit = objprv.RecEdit;
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//

                var list = _employeeContext.GetDependantsByEmpSysId("_empSysId", Id, 0).ToList();
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
        public IActionResult CreateNew(HrEmpDependent hrEmpDpnd)
        {
            try
            {
                ViewBag.lstRltnTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.RLTN_TYP).Where(x => x.Indx != 0).OrderByDescending(x => x.Indx);
                ViewBag.lstTitle =  _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TITL);
                ViewBag.lstGender = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.GND); 
                ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
                ViewBag.lstNlty = _organizationContext.GetAllNationalityViews();
                ViewBag.lstBldGrp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BLD_GRP); 
                ViewBag.lstDpdPmtTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_DOC_TYP); 
                ViewBag.lstOccup = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_OCC_TYP);
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DTL").FirstOrDefault();
                ViewBag.RecAdd = objprv.RecAdd;                
                //End | Usr Prv//
                return PartialView("_HrEmpDependent_Create", hrEmpDpnd);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Dependent", ex.Message);
            }

        }

        // POST: HrEmpGrade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HrEmpDependent hrEmpDpnd, IList<IFormFile> imgUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            try
            {

                try
                {

                    if (hrEmpDpnd.PmtExpDate == null)
                        hrEmpDpnd.PmtExpDate = Convert.ToDateTime("01/01/1900");
                    if (hrEmpDpnd.PstExpDate == null)
                        hrEmpDpnd.PstExpDate = Convert.ToDateTime("01/01/1900");
                    if (hrEmpDpnd.Occupation == null)
                        hrEmpDpnd.Occupation = "None";
                    if (hrEmpDpnd.NameArb == null)
                        hrEmpDpnd.NameArb = hrEmpDpnd.PstName;


                    try
                    {
                        foreach (var file in imgUpload)
                        {
                            var filename = ContentDispositionHeaderValue
                                                .Parse(file.ContentDisposition)
                                                .FileName
                                                .Trim('"');

                            _ext = Path.GetExtension(filename);
                            _cusFilename = Guid.NewGuid().ToString() + _ext;                            
                            filename = _hostingEnv.WebRootPath + @"\attach\depndImages\" + $@"\{_cusFilename}";
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
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "File failed to upload - " + ex.Message);
                    }



                    
                    if (hrEmpDpnd.NameArb == null) hrEmpDpnd.NameArb = hrEmpDpnd.PstName;
                    hrEmpDpnd.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                    int _result = _dbCommitContext.commitChanges(LogEventTypes.INS, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithSuccess("Dependent", "Record Added Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Adding Record got failed");
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Adding Record got failed");
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
            var hrEmpDpndView = _employeeContext.GetDependantsByEmpSysId("_dpndId", empSysId, id).FirstOrDefault();

            HrEmpDependent hrEmpDpnd = new HrEmpDependent();
            hrEmpDpnd.DpndId = hrEmpDpndView.Dpnd_Id;
            hrEmpDpnd.RltnTyp = hrEmpDpndView.Rltn_Typ;
            hrEmpDpnd.FirstName = hrEmpDpndView.First_Name;
            hrEmpDpnd.MdlName = hrEmpDpndView.Mdl_Name;
            hrEmpDpnd.LastName = hrEmpDpndView.Last_Name;
            hrEmpDpnd.PstName = hrEmpDpndView.Pst_Name;
            hrEmpDpnd.NameArb = hrEmpDpndView.Name_Arb;
            hrEmpDpnd.Title = hrEmpDpndView.Title;
            hrEmpDpnd.Dob = hrEmpDpndView.DOB;
            hrEmpDpnd.Gender = hrEmpDpndView.Gender;
            hrEmpDpnd.NltyId = hrEmpDpndView.Nlty_Id;
            hrEmpDpnd.Photo = hrEmpDpndView.Photo;
            hrEmpDpnd.CntryId = hrEmpDpndView.Cntry_Id;
            hrEmpDpnd.MblPhone = hrEmpDpndView.Mbl_Phone;
            hrEmpDpnd.PsnEmail = hrEmpDpndView.Psn_Email;
            hrEmpDpnd.BldGrp = hrEmpDpndView.Bld_Grp;
            hrEmpDpnd.PmtTyp = hrEmpDpndView.Pmt_Typ;
            hrEmpDpnd.PmtId = hrEmpDpndView.Pmt_Id;
            hrEmpDpnd.PmtExpDate = hrEmpDpndView.Pmt_Exp_Date;
            hrEmpDpnd.PstNo = hrEmpDpndView.Pst_No;
            hrEmpDpnd.PstExpDate = hrEmpDpndView.Pst_Exp_Date;
            hrEmpDpnd.PstCntryId = hrEmpDpndView.Pst_Cntry_Id;
            hrEmpDpnd.Institute = hrEmpDpndView.Institute;
            hrEmpDpnd.Occupation = hrEmpDpndView.Occupation;
            hrEmpDpnd.Note = hrEmpDpndView.Note;
            hrEmpDpnd.PhotoName = hrEmpDpndView.Photo_Name;
            hrEmpDpnd.PhotoMime = hrEmpDpndView.Photo_Name;

            hrEmpDpnd.exPstNo = hrEmpDpndView.Pst_No;
            hrEmpDpnd.exPmtId = hrEmpDpndView.Pmt_Id;
            hrEmpDpnd.exPhotoName = hrEmpDpnd.PhotoName;
            hrEmpDpnd.exPhotoMime = hrEmpDpnd.exPhotoMime;
            hrEmpDpnd.RecStat = 1;


            ViewBag.lstRltnTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.RLTN_TYP).Where(x => x.Indx != 0).OrderByDescending( x => x.Indx );
            ViewBag.lstTitle = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.TITL);
            ViewBag.lstGender = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.GND);
            ViewBag.lstCntry = _organizationContext.GetAllCntryViews();
            ViewBag.lstNlty = _organizationContext.GetAllNationalityViews();
            ViewBag.lstBldGrp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.BLD_GRP);
            ViewBag.lstDpdPmtTyp = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_DOC_TYP);
            ViewBag.lstOccup = _sysSelectListContext.GetElements(SysSelectListContext.SelectListTypeEnum.DPN_OCC_TYP);

            //Start | Usr Prv//
            int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
            var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DTL").FirstOrDefault();
            ViewBag.RecEdit = objprv.RecEdit;
            //End | Usr Prv//

            return PartialView("_HrEmpDependent_Edit", hrEmpDpnd);
        }

        // POST: HrEmpGrade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HrEmpDependent hrEmpDpnd, IList<IFormFile> imgUpload)
        {
            string _ext; //File Extention
            string _cusFilename; //Customer File Name
            try
            {
                try
                {
                    if (hrEmpDpnd.PmtExpDate == null)
                        hrEmpDpnd.PmtExpDate = Convert.ToDateTime("01/01/1900");
                    if (hrEmpDpnd.PstExpDate == null)
                        hrEmpDpnd.PstExpDate = Convert.ToDateTime("01/01/1900");
                    if (hrEmpDpnd.Occupation == null)
                        hrEmpDpnd.Occupation = "None";
                    if (hrEmpDpnd.NameArb == null) 
                        hrEmpDpnd.NameArb = hrEmpDpnd.PstName;

                    hrEmpDpnd.PhotoName = hrEmpDpnd.exPhotoName;
                    hrEmpDpnd.PhotoMime = hrEmpDpnd.exPhotoMime;
                    try
                    {
                        foreach (var file in imgUpload)
                        {
                            var filename = ContentDispositionHeaderValue
                                                .Parse(file.ContentDisposition)
                                                .FileName
                                                .Trim('"');

                            _ext = Path.GetExtension(filename);
                            _cusFilename = Guid.NewGuid().ToString() + _ext;
                            filename = _hostingEnv.WebRootPath + @"\attach\depndImages\" + $@"\{_cusFilename}";
                            using (FileStream fs = System.IO.File.Create(filename))
                            {
                                file.CopyTo(fs);                                
                                fs.Flush();
                            }
                            hrEmpDpnd.PhotoName = _cusFilename;
                            hrEmpDpnd.PhotoMime = file.ContentType;

                            //--Delete Existing File-----------------------------------------------------------------------------//
                            try
                            {
                                var exfilename = _hostingEnv.WebRootPath + @"\attach\depndImages\" + $@"\{hrEmpDpnd.exPhotoName}";
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
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "File failed to upload - " + ex.Message);
                    }



                    hrEmpDpnd.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                    var _result = _dbCommitContext.commitChanges(LogEventTypes.UPD, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                    if (_result == 1)
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithSuccess("Dependent", "Record Updated Successfully");
                    else
                        return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Updating Record got failed");
                }
                catch (Exception)
                {

                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Updating Record got failed");
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
                int empSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var hrEmpDpnd = _employeeContext.GetDependantsByEmpSysId(empSysId).Where(x=>x.DpndId == id).FirstOrDefault();
                //Start | Usr Prv//
                int grpId = Convert.ToInt32(HttpContext.Session.GetInt32("userGrpId"));
                var objprv = _prvContext.Get("_user_priv_group", grpId).Where(x => x.PrvCode == "EM_DEP_DTL").FirstOrDefault();
                ViewBag.RecDelete = objprv.RecDelete;
                //End | Usr Prv//
                return PartialView("_HrEmpDependent_Delete", hrEmpDpnd);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("Dependent", ex.Message);
            }

        }

        // POST: HrEmpGrade/Delete/5
        [HttpPost]
        public IActionResult Delete(HrEmpDependent hrEmpDpnd)
        {
            try
            {

                //--Delete File-----------------------------------------------------------------------------------//
                try
                {
                    var exfilename = _hostingEnv.WebRootPath + @"\attach\depndImages\" + $@"\{hrEmpDpnd.PhotoName}";
                    System.IO.File.Delete(exfilename);
                }
                catch
                { }
                //------------------------------------------------------------------------------------------------//


                hrEmpDpnd.EmpSysId = Convert.ToInt32(HttpContext.Session.GetInt32("EmpSysId"));
                var _result = _dbCommitContext.commitChanges(LogEventTypes.DEL, hrEmpDpnd, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                if (_result == 1)
                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithSuccess("Dependent", "Record Deleted Successfully");
                else
                    return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Deleting Record got failed");

            }
            catch
            {
                return RedirectToAction("Index", "HrEmpDependent", new { @Id = hrEmpDpnd.EmpSysId }).WithWarning("Dependent", "Deleting Record got failed");
            }
        }


        public IActionResult IsExistPstNo([Bind(Prefix = "PstNo")] string value, [Bind(Prefix = "ex_PstNo")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _employeeContext.GetDependantsByEmpSysId("_all", 0, 0).Where(x => x.Pmt_Id == value).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _employeeContext.GetDependantsByEmpSysId("_all", 0, 0).Where(x => x.Pmt_Id == value).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }

        public IActionResult IsExistPmtId([Bind(Prefix = "PmtId")] string value, [Bind(Prefix = "ex_PmtId")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _employeeContext.GetDependantsByEmpSysId("_all", 0, 0).Where(x => x.Pmt_Id == value).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _employeeContext.GetDependantsByEmpSysId("_all", 0, 0).Where(x => x.Pmt_Id == value).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }


        public IActionResult ValidateDOB([Bind(Prefix = "Dob")] DateTime? dob)
        {
            if (dob >= Convert.ToDateTime("01/01/1950") && dob <= DateTime.Now)
            {
                return Json(true);
            }
            return Json(false);
        }


        public IActionResult ValidatePmtExpDate([Bind(Prefix = "PmtExpDate")] DateTime? pmtexpDate)
        {
            if (pmtexpDate >= DateTime.Now.AddMonths(-3))
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}